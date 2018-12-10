// Copyright(c) 2017, 2018 Johan Lindvall
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace OrmMock.MemDb
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    using Shared;
    using Shared.Comparers;

    public class MemDb : IMemDb, IMemDbCustomization
    {
        private readonly IList<object> newObjects = new List<object>();

        private readonly IList<object> heldObjects = new List<object>();

        private readonly Dictionary<PropertyInfo, long> autoIncrement = new Dictionary<PropertyInfo, long>();

        private readonly Memoization memoization = new Memoization();

        private readonly IReflection reflection;

        public MemDb()
        {
            this.Relations = new Relations();
            this.reflection = new FasterflectReflection();
        }

        /// <inheritdoc />
        public Relations Relations { get; }

        /// <inheritdoc />
        public void RegisterAutoIncrement<T>(Expression<Func<T, object>> expression) where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(expression))
            {
                this.autoIncrement.Add(property, 0);
            }
        }

        /// <inheritdoc />
        public void Add(object o)
        {
            this.newObjects.Add(o);
        }

        /// <inheritdoc />
        public void AddMany(IEnumerable<object> objects)
        {
            foreach (var o in objects)
            {
                this.Add(o);
            }
        }

        /// <inheritdoc />
        public bool Remove(object o) => this.Remove(this.GetPrimaryKeys(o), o.GetType());

        /// <inheritdoc />
        public bool Remove<T>(Keys keys) => this.Remove(keys, typeof(T));

        /// <inheritdoc />
        public void Commit()
        {
            var seenObjects = this.DiscoverNewObjects();

            this.UpdateObjectRelations(seenObjects);
        }

        /// <inheritdoc />
        public IEnumerable<T> Get<T>()
        {
            return this.heldObjects.Where(o => o.GetType() == typeof(T)).Select(o => (T)o);
        }

        /// <inheritdoc />
        public int Count() => this.heldObjects.Count;

        /// <inheritdoc />
        public int Count<T>() => this.heldObjects.Count(o => o.GetType() == typeof(T));

        /// <inheritdoc />
        public T Get<T>(Keys keys)
        {
            var type = typeof(T);
            var keyGetter = PrimaryKeyGetter(type);

            foreach (var heldObject in this.heldObjects)
            {
                if (heldObject.GetType() == type && keyGetter(heldObject).Equals(keys))
                {
                    return (T)heldObject;
                }
            }

            return default(T);
        }

        /// <inheritdoc />
        public T Create<T>() => (T)this.CreateObject(typeof(T));

        /// <summary>
        /// Traverse the object graph from the root, following references and collections.
        /// </summary>
        /// <param name="root">The root object.</param>
        /// <returns>An enumerable of objects.</returns>
        public IEnumerable<object> TraverseObjectGraph(object root) => this.TraverseObjectGraph(root, new HashSet<object>(new ReferenceEqualityComparer()));

        /// <summary>
        /// Removes an object of the given type having the given primary keys.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool Remove(Keys keys, Type type)
        {
            var keyGetter = PrimaryKeyGetter(type);

            var result = this.RemoveFrom(this.heldObjects, keys, type, keyGetter);
            var result2 = this.RemoveFrom(this.newObjects, keys, type, keyGetter);

            return result || result2;
        }

        private bool RemoveFrom(IList<object> objects, Keys keys, Type type, Func<object, Keys> keyGetter)
        {
            var result = false;

            var i = objects.Count - 1;

            while (i >= 0)
            {
                var toCheck = objects[i];

                if (toCheck.GetType() == type)
                {
                    var toCheckKeys = keyGetter(toCheck);

                    if (keys.Equals(toCheckKeys))
                    {
                        objects.RemoveAt(i);
                        result = true;
                    }
                }

                --i;
            }

            return result;
        }

        /// <summary>
        /// Updates object relations and foreign keys of the held objects.
        /// </summary>
        /// <param name="seenObjects">The seen objects.</param>
        private void UpdateObjectRelations(HashSet<object> seenObjects)
        {
            var primaryKeyLookup = CachedFunc.Create(() => this.heldObjects.ToDictionary(heldObject => Tuple.Create(heldObject.GetType(), this.GetPrimaryKeys(heldObject)), heldObject => heldObject));

            var incomingObjectsLookup = new Dictionary<Tuple<Type, Type>, IList<object>>();

            // handle outgoing simple properties.
            foreach (var currentObject in this.heldObjects)
            {
                var properties = this.GetProperties(currentObject);

                foreach (var property in properties)
                {
                    var propertyType = property.PropertyType;

                    if (ReflectionUtility.IsNonGenericReferenceType(propertyType))
                    {
                        var foreignObject = this.GetValue(currentObject, property);

                        if (seenObjects.Contains(foreignObject))
                        {
                            // Update foreign keys to match foreignObject
                            var primaryKeysOfForeignObject = this.GetPrimaryKeys(foreignObject);

                            this.SetForeignKeys(currentObject, foreignObject.GetType(), primaryKeysOfForeignObject); // For 1:1 primary keys may change.
                        }
                        else if (primaryKeyLookup().TryGetValue(Tuple.Create(propertyType, this.GetForeignKeys(currentObject, propertyType)), out foreignObject))
                        {
                            this.SetValue(currentObject, property, foreignObject);
                        }
                        else
                        {
                            // Object not found. Clear nullable foreign keys.
                            this.SetValue(currentObject, property, null);
                            if (!this.Relations.GetPrimaryKeys(currentObject.GetType()).SequenceEqual(this.Relations.GetForeignKeys(currentObject.GetType(), propertyType)))
                            {
                                this.ClearForeignKeys(currentObject, propertyType); // unless 1:1
                            }
                        }

                        // Build up reverse mapping of incoming objects at foreignObject
                        var incomingObjectsKey = Tuple.Create(propertyType, currentObject.GetType());

                        if (!incomingObjectsLookup.TryGetValue(incomingObjectsKey, out var incomingObjects))
                        {
                            incomingObjects = new List<object>();
                            incomingObjectsLookup.Add(incomingObjectsKey, incomingObjects);
                        }

                        if (foreignObject != null)
                        {
                            incomingObjects.Add(currentObject);
                        }
                    }
                }
            }

            //  handle outgoing collection properties.
            foreach (var currentObject in this.heldObjects)
            {
                var properties = this.GetProperties(currentObject);

                foreach (var property in properties)
                {
                    var propertyType = property.PropertyType;

                    if (ReflectionUtility.IsCollectionType(propertyType))
                    {
                        var incomingObjectsKey = Tuple.Create(currentObject.GetType(), propertyType.GetGenericArguments()[0]);

                        if (incomingObjectsLookup.TryGetValue(incomingObjectsKey, out var incomingObjects))
                        {
                            // Set ICollection to incoming objects.
                            this.SetCollection(currentObject, property, incomingObjects);
                        }
                    }
                }
            }

        }


        /// <summary>
        /// Discovers new objects by traversing the graph of added objects.
        /// </summary>
        /// <returns>The hashset of all held objects (including the new objects).</returns>
        private HashSet<object> DiscoverNewObjects()
        {
            var seenObjects = new HashSet<object>(this.heldObjects, new ReferenceEqualityComparer());

            foreach (var newObject in this.newObjects)
            {
                foreach (var descendant in this.TraverseObjectGraph(newObject, seenObjects))
                {
                    this.heldObjects.Add(descendant);

                    // Auto-increment for new objects.
                    var properties = this.GetProperties(descendant);

                    foreach (var property in properties)
                    {
                        if (this.autoIncrement.TryGetValue(property, out var value))
                        {
                            this.SetValue(descendant, property, Convert.ChangeType(++value, property.PropertyType));
                            this.autoIncrement[property] = value;
                        }
                    }
                }
            }

            this.newObjects.Clear();

            return seenObjects;
        }

        /// <summary>
        /// Traverses the object graph starting from root. Traversed object are entered into seenObjects so that the same object is not traversed twice.
        /// </summary>
        /// <param name="root">The graph root.</param>
        /// <param name="seenObjects">The dictionary of seen object.</param>
        /// <returns>An enumerable of discovered objects.</returns>
        private IEnumerable<object> TraverseObjectGraph(object root, HashSet<object> seenObjects)
        {
            if (root != null && seenObjects.Add(root))
            {
                yield return root;

                foreach (var property in this.GetProperties(root))
                {
                    if (ReflectionUtility.IsCollectionType(property.PropertyType) && this.GetValue(root, property) is IEnumerable enumerable)
                    {
                        foreach (var collectionItem in enumerable)
                        {
                            foreach (var descendant in this.TraverseObjectGraph(collectionItem, seenObjects))
                            {
                                yield return descendant;
                            }
                        }
                    }
                    else if (ReflectionUtility.IsNonGenericReferenceType(property.PropertyType))
                    {
                        foreach (var descendant in this.TraverseObjectGraph(this.GetValue(root, property), seenObjects))
                        {
                            yield return descendant;
                        }
                    }
                }
            }
        }

        private object CreateObject(Type type) => this.memoization.Get(nameof(this.CreateObject), type, () => this.reflection.Constructor(type))();

        private Func<object, Keys> PrimaryKeyGetter(Type type) => this.memoization.Get(nameof(this.PrimaryKeyGetter), type, () => this.reflection.PrimaryKeyGetter(this.Relations, type));

        private Keys GetPrimaryKeys(object o) => this.PrimaryKeyGetter(o.GetType())(o);

        private IList<PropertyInfo> GetProperties(object o) => this.memoization.Get(nameof(this.GetProperties), o.GetType(), () => ReflectionUtility.GetPublicPropertiesWithGetters(o.GetType()));

        private object GetValue(object o, PropertyInfo pi) => this.memoization.Get(nameof(this.GetValue), pi, () => this.reflection.Getter(pi))(o);

        private void SetValue(object o, PropertyInfo pi, object value) => this.memoization.Get(nameof(this.SetValue), pi, () => this.reflection.Setter(pi))(o, value);

        private Keys GetForeignKeys(object o, Type foreignType) => this.memoization.Get(nameof(this.GetForeignKeys), o.GetType(), foreignType, () => this.reflection.ForeignKeyGetter(this.Relations, o.GetType(), foreignType))(o);

        private void SetCollection(object o, PropertyInfo pi, IList<object> values) => this.memoization.Get(nameof(this.SetCollection), pi, () => this.reflection.CollectionSetter(pi, typeof(HashSet<>)))(o, values);

        private void SetForeignKeys(object o, Type foreignType, Keys keys) => this.memoization.Get(nameof(this.SetForeignKeys), o.GetType(), foreignType, () => this.reflection.ForeignKeySetter(this.Relations, o.GetType(), foreignType))(o, keys);

        private void ClearForeignKeys(object o, Type foreignType) => this.memoization.Get(nameof(this.ClearForeignKeys), o.GetType(), foreignType, () => this.reflection.ForeignKeyClearer(this.Relations, o.GetType(), foreignType))(o);
    }
}
