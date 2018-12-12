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

        private IList<object> heldObjects = new List<object>();

        private readonly HashSet<object> deletedObjects = new HashSet<object>(new ReferenceEqualityComparer());

        private readonly Dictionary<PropertyInfo, long> autoIncrement = new Dictionary<PropertyInfo, long>();

        private readonly IReflection reflection;

        public MemDb()
        {
            this.Relations = new Relations();
            this.reflection = new StandardReflection();
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
            var keyGetter = this.PrimaryKeyGetter(type);

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
        public IEnumerable<object> TraverseObjectGraph(object root) => this.TraverseObjectGraph(null, root, new Dictionary<object, HashSet<object>>(new ReferenceEqualityComparer()));

        /// <summary>
        /// Removes an object of the given type having the given primary keys.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private bool Remove(Keys keys, Type type)
        {
            var keyGetter = this.PrimaryKeyGetter(type);

            var result = this.RemoveFrom(this.heldObjects, keys, type, keyGetter);
            var result2 = this.RemoveFrom(this.newObjects, keys, type, keyGetter);

            return result || result2;
        }

        private bool RemoveFrom(IList<object> objects, Keys keys, Type type, Func<object, Keys> keyGetter)
        {
            var result = false;

            foreach (var obj in objects)
            {
                if (obj.GetType() == type)
                {
                    if (keys.Equals(keyGetter(obj)))
                    {
                        if (this.deletedObjects.Add(obj))
                        {
                            result = true;
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Updates object relations and foreign keys of the held objects.
        /// </summary>
        /// <param name="seenObjects">The seen objects.</param>
        private void UpdateObjectRelations(Dictionary<object, HashSet<object>> seenObjects)
        {
            this.heldObjects = this.heldObjects.Where(o => !this.deletedObjects.Contains(o)).ToList();

            var primaryKeyLookup = CachedFunc.Create(() => this.heldObjects.ToDictionary(heldObject => Tuple.Create(heldObject.GetType(), this.GetPrimaryKeys(heldObject)), heldObject => heldObject));

            var incomingObjectsLookup = new Dictionary<Tuple<Type, Type, Keys>, IList<object>>();

            // handle outgoing simple properties.
            foreach (var currentObject in this.heldObjects)
            {
                var properties = this.GetProperties(currentObject);

                foreach (var property in properties)
                {
                    var propertyType = property.PropertyType;

                    if (this.IsNonGenericReferenceType(propertyType))
                    {
                        var foreignObject = this.GetValue(currentObject, property);

                        var isDeleted = foreignObject != null && this.deletedObjects.Contains(foreignObject);

                        if (isDeleted)
                        {
                            foreignObject = null;
                        }
                        else if (foreignObject == null && seenObjects.TryGetValue(currentObject, out var fromObjects) && fromObjects != null)
                        {
                            foreach (var fromObject in fromObjects)
                            {
                                if (fromObject?.GetType() == propertyType)
                                {
                                    foreignObject = fromObject;
                                    break;
                                }
                            }
                        }

                        if (foreignObject != null && seenObjects.ContainsKey(foreignObject))
                        {
                            // Update foreign keys to match foreignObject
                            var primaryKeysOfForeignObject = this.GetPrimaryKeys(foreignObject);

                            this.SetForeignKeys(currentObject, foreignObject.GetType(), primaryKeysOfForeignObject); // For 1:1 primary keys may change.
                        }
                        else if (!isDeleted && primaryKeyLookup().TryGetValue(Tuple.Create(propertyType, this.GetForeignKeys(currentObject, propertyType)), out foreignObject))
                        {
                            this.SetValue(currentObject, property, foreignObject);
                        }
                        else
                        {
                            // Object not found. Clear nullable foreign keys.
                            if (!this.Relations.GetPrimaryKeys(currentObject.GetType()).SequenceEqual(this.Relations.GetForeignKeys(currentObject.GetType(), propertyType))) // Relax check to see if foreign keys is a subset of the primary keys
                            {
                                this.ClearForeignKeys(currentObject, propertyType); // unless 1:1
                                this.SetValue(currentObject, property, null);
                            }
                            else
                            {
                                this.SetValue(currentObject, property, null);
                            }
                        }

                        if (foreignObject != null)
                        {
                            // Build up reverse mapping of incoming objects at foreignObject
                            var incomingObjectsKey = Tuple.Create(propertyType, currentObject.GetType(), this.GetForeignKeys(currentObject, propertyType));

                            if (!incomingObjectsLookup.TryGetValue(incomingObjectsKey, out var incomingObjects))
                            {
                                incomingObjects = new List<object>();
                                incomingObjectsLookup.Add(incomingObjectsKey, incomingObjects);
                            }

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

                    if (this.IsCollectionType(propertyType))
                    {
                        var incomingObjectsKey = Tuple.Create(currentObject.GetType(), propertyType.GetGenericArguments()[0], this.GetPrimaryKeys(currentObject));

                        if (incomingObjectsLookup.TryGetValue(incomingObjectsKey, out var incomingObjects))
                        {
                            // Set ICollection to incoming objects.
                            this.SetCollection(currentObject, property, incomingObjects);
                        }
                    }
                }
            }

            this.deletedObjects.Clear();
        }


        /// <summary>
        /// Discovers new objects by traversing the graph of added objects.
        /// </summary>
        /// <returns>The hashset of all held objects (including the new objects).</returns>
        private Dictionary<object, HashSet<object>> DiscoverNewObjects()
        {
            var seenObjects = this.heldObjects.ToDictionary<object, object, HashSet<object>>(h => h, h => null, new ReferenceEqualityComparer());

            foreach (var newObject in this.heldObjects.Concat(this.newObjects).ToList())
            {
                foreach (var descendant in this.TraverseObjectGraph(null, newObject, seenObjects))
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
        /// <param name="from">The graph from.</param>
        /// <param name="root">The graph root.</param>
        /// <param name="discoveredObjects">The dictionary of seen object.</param>
        /// <returns>An enumerable of discovered objects.</returns>
        private IEnumerable<object> TraverseObjectGraph(object from, object root, Dictionary<object, HashSet<object>> discoveredObjects)
        {
            if (root != null)
            {
                if (!discoveredObjects.TryGetValue(root, out var fromObjects))
                {
                    yield return root; // completely new object.
                }

                if (fromObjects == null)
                {
                    fromObjects = new HashSet<object>(new ReferenceEqualityComparer());
                    discoveredObjects[root] = fromObjects;
                }

                if (from == null || fromObjects.Add(from))
                {
                    foreach (var property in this.GetProperties(root))
                    {
                        if (this.IsCollectionType(property.PropertyType) && this.GetValue(root, property) is IEnumerable enumerable)
                        {
                            foreach (var collectionItem in enumerable)
                            {
                                foreach (var descendant in this.TraverseObjectGraph(root, collectionItem, discoveredObjects))
                                {
                                    yield return descendant;
                                }
                            }
                        }
                        else if (this.IsNonGenericReferenceType(property.PropertyType))
                        {
                            foreach (var descendant in this.TraverseObjectGraph(root, this.GetValue(root, property), discoveredObjects))
                            {
                                yield return descendant;
                            }
                        }
                    }
                }
            }
        }

        private static readonly Dictionary<Type, bool> isCollectionTypeDict = new Dictionary<Type, bool>();

        private static readonly Dictionary<Type, bool> isNonGenericReferenceTypeDict = new Dictionary<Type, bool>();

        private readonly Dictionary<Type, Func<object>> createValueDict = new Dictionary<Type, Func<object>>();

        private static readonly Dictionary<PropertyInfo, Func<object, object>> getValueDict = new Dictionary<PropertyInfo, Func<object, object>>();

        private readonly Dictionary<Type, Func<object, Keys>> primaryKeyGetterDict = new Dictionary<Type, Func<object, Keys>>();

        private static readonly Dictionary<Type, IList<PropertyInfo>> propertyInfoDict = new Dictionary<Type, IList<PropertyInfo>>();

        private static readonly Dictionary<PropertyInfo, Action<object, object>> setValueDict = new Dictionary<PropertyInfo, Action<object, object>>();

        private readonly Dictionary<Tuple<Type, Type>, Func<object, Keys>> getForeignKeysDict = new Dictionary<Tuple<Type, Type>, Func<object, Keys>>();

        private static readonly Dictionary<PropertyInfo, Action<object, IList<object>>> setCollectionDict = new Dictionary<PropertyInfo, Action<object, IList<object>>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object, Keys>> setForeignKeysDict = new Dictionary<Tuple<Type, Type>, Action<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object>> clearForeignKeysDict = new Dictionary<Tuple<Type, Type>, Action<object>>();

        private bool IsNonGenericReferenceType(Type type) => this.Memoization(isNonGenericReferenceTypeDict, type, () => ReflectionUtility.IsNonGenericReferenceType(type));

        private bool IsCollectionType(Type type) => this.Memoization(isCollectionTypeDict, type, () => ReflectionUtility.IsCollectionType(type));

        private object CreateObject(Type type) => this.Memoization(this.createValueDict, type, () => this.reflection.Constructor(type))();

        private Func<object, Keys> PrimaryKeyGetter(Type type) => this.Memoization(this.primaryKeyGetterDict, type, () => this.reflection.PrimaryKeyGetter(this.Relations, type));

        private Keys GetPrimaryKeys(object o) => this.PrimaryKeyGetter(o.GetType())(o);

        private IList<PropertyInfo> GetProperties(object o) => this.Memoization(propertyInfoDict, o.GetType(), () => ReflectionUtility.GetPublicPropertiesWithGetters(o.GetType()));

        private object GetValue(object o, PropertyInfo pi) => this.Memoization(getValueDict, pi, () => this.reflection.Getter(pi))(o);

        private void SetValue(object o, PropertyInfo pi, object value) => this.Memoization(setValueDict, pi, () => this.reflection.Setter(pi))(o, value);

        private Keys GetForeignKeys(object o, Type foreignType) => this.Memoization(this.getForeignKeysDict, Tuple.Create(o.GetType(), foreignType), () => this.reflection.ForeignKeyGetter(this.Relations, o.GetType(), foreignType))(o);

        private void SetCollection(object o, PropertyInfo pi, IList<object> values) => this.Memoization(setCollectionDict, pi, () => this.reflection.CollectionSetter(pi, typeof(HashSet<>)))(o, values);

        private void SetForeignKeys(object o, Type foreignType, Keys keys) => this.Memoization(this.setForeignKeysDict, Tuple.Create(o.GetType(), foreignType), () => this.reflection.ForeignKeySetter(this.Relations, o.GetType(), foreignType))(o, keys);

        private void ClearForeignKeys(object o, Type foreignType) => this.Memoization(this.clearForeignKeysDict, Tuple.Create(o.GetType(), foreignType), () => this.reflection.ForeignKeyClearer(this.Relations, o.GetType(), foreignType))(o);

        private TValue Memoization<TKey, TValue>(Dictionary<TKey, TValue> dict, TKey key, Func<TValue> factory)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = factory();
                dict.Add(key, value);
            }

            return value;
        }
    }
}
