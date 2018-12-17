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

        private Dictionary<PropertyInfo, long> autoIncrement = new Dictionary<PropertyInfo, long>();

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

        public void Reset()
        {
            this.newObjects.Clear();
            this.heldObjects.Clear();
            this.deletedObjects.Clear();
            this.autoIncrement = this.autoIncrement.ToDictionary(kvp => kvp.Key, _ => (long)0);
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
        public IList<object> TraverseObjectGraph(object root)
        {
            var output = new List<object>();

            this.TraverseObjectGraph(null, new[] { root }, new Dictionary<object, IList<object>>(new ReferenceEqualityComparer()), output);

            return output;
        }

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

        /// <summary>
        /// Locates the object in the input list of objects and if found adds it to the deleted objects structure.
        /// </summary>
        /// <param name="objects">The list of objects.</param>
        /// <param name="keys">The keys of the object to delete.</param>
        /// <param name="type">The type of the object to delete</param>
        /// <param name="keyGetter">The key getter of the object of the give type.</param>
        /// <returns>True if the object was found and added to the deleted objects list.</returns>
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
        private void UpdateObjectRelations(Dictionary<object, IList<object>> seenObjects)
        {
            this.heldObjects = this.heldObjects.Where(o => !this.deletedObjects.Contains(o)).ToList();

            var primaryKeyLookup = CachedFunc.Create(() => this.heldObjects.ToDictionary(heldObject => Tuple.Create(heldObject.GetType(), this.GetPrimaryKeys(heldObject)), heldObject => heldObject));

            var incomingObjectsLookup = new Dictionary<object, IList<Tuple<Type, object>>>(new ReferenceEqualityComparer());

            // handle outgoing simple properties.
            foreach (var currentObject in this.heldObjects)
            {
                foreach (var property in this.GetNonGenericReferenceProperties(currentObject))
                {
                    var propertyType = property.PropertyType;

                    var foreignObject = this.GetValue(currentObject, property);

                    var foreignObjectDeleted = foreignObject != null && this.deletedObjects.Contains(foreignObject);

                    if (foreignObjectDeleted)
                    {
                        foreignObject = null;
                        this.SetValue(currentObject, property, null);
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

                    if (foreignObject != null)
                    {
                        // Update foreign keys to match foreignObject
                        var primaryKeysOfForeignObject = this.GetPrimaryKeys(foreignObject);

                        this.SetForeignKeys(currentObject, foreignObject.GetType(), primaryKeysOfForeignObject); // For 1:1 primary keys may change.
                    }
                    else if (!foreignObjectDeleted && primaryKeyLookup().TryGetValue(Tuple.Create(propertyType, this.GetForeignKeys(currentObject, propertyType)), out foreignObject))
                    {
                        this.SetValue(currentObject, property, foreignObject);
                    }
                    else
                    {
                        // Object not found. Clear nullable foreign keys.
                        if (!this.Are11Relation(currentObject.GetType(), propertyType))
                        {
                            this.ClearForeignKeys(currentObject, propertyType);
                        }
                    }

                    if (foreignObject != null)
                    {
                        // Build up reverse mapping of incoming objects at foreignObject
                        if (!incomingObjectsLookup.TryGetValue(foreignObject, out var incomingObjects))
                        {
                            incomingObjects = new List<Tuple<Type, object>>();
                            incomingObjectsLookup.Add(foreignObject, incomingObjects);
                        }

                        incomingObjects.Add(Tuple.Create(currentObject.GetType(), currentObject));
                    }
                }
            }

            //  handle outgoing collection properties.
            foreach (var currentObject in this.heldObjects)
            {
                foreach (var property in this.GetCollectionProperties(currentObject))
                {
                    var propertyType = property.PropertyType;
                    var incomingType = propertyType.GetGenericArguments()[0];

                    if (incomingObjectsLookup.TryGetValue(currentObject, out var incomingObjects))
                    {
                        // Set ICollection to incoming objects.
                        this.SetCollection(currentObject, property, incomingObjects.Where(inc => inc.Item1 == incomingType).Select(inc => inc.Item2).ToList());
                    }
                }
            }

            this.deletedObjects.Clear();
        }

        /// <summary>
        /// Discovers new objects by traversing the graph of added objects.
        /// </summary>
        /// <returns>The hashset of all held objects (including the new objects).</returns>
        private Dictionary<object, IList<object>> DiscoverNewObjects()
        {
            var seenObjects = this.heldObjects.ToDictionary<object, object, IList<object>>(h => h, h => null, new ReferenceEqualityComparer());

            var output = new List<object>();

            this.TraverseObjectGraph(null, this.heldObjects.Concat(this.newObjects).ToList(), seenObjects, output);

            foreach (var descendant in output)
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

            this.newObjects.Clear();

            return seenObjects;
        }

        /// <summary>
        /// Traverses the object graph starting from root. Traversed object are entered into seenObjects so that the same object is not traversed twice.
        /// </summary>
        /// <param name="from">The graph from.</param>
        /// <param name="roots">The graph roots.</param>
        /// <param name="discoveredObjects">The dictionary of seen object.</param>
        /// <param name="output">The new objects.</param>
        private void TraverseObjectGraph(object from, IList<object> roots, Dictionary<object, IList<object>> discoveredObjects, IList<object> output)
        {
            var relations = new List<object>();

            foreach (var root in roots)
            {
                relations.Clear();

                if (!discoveredObjects.TryGetValue(root, out var fromObjects))
                {
                    output.Add(root); // completely new object.
                }

                if (fromObjects == null)
                {
                    // Completely new object or existing object not visited yet.
                    fromObjects = new List<object>();
                    discoveredObjects[root] = fromObjects;

                    this.RetrieveRelations(root, relations);
                }

                if (from != null)
                {
                    fromObjects.Add(from);
                }

                this.TraverseObjectGraph(root, relations, discoveredObjects, output);
            }
        }

        /// <summary>
        /// Holds the cache of relation retrievers.
        /// </summary>
        private readonly Dictionary<Type, Action<object, IList<object>>> relationsRetrieverDict = new Dictionary<Type, Action<object, IList<object>>>();

        private Action<object, IList<object>> RelationsRetriever(Type t)
        {
            var methods = new List<Action<object, IList<object>>>();

            foreach (var property in ReflectionUtility.GetPublicPropertiesWithGetters(t))
            {
                if (ReflectionUtility.IsCollectionType(property.PropertyType))
                {
                    var getter = this.reflection.Getter(property);
                    methods.Add((obj, list) =>
                    {
                        var collection = getter(obj);
                        if (collection != null)
                        {
                            foreach (var item in (IEnumerable)collection)
                            {
                                list.Add(item);
                            }
                        }
                    });
                }
                else if (ReflectionUtility.IsNonGenericReferenceType(property.PropertyType))
                {
                    var getter = this.reflection.Getter(property);
                    methods.Add((obj, list) =>
                    {
                        var item = getter(obj);
                        if (item != null)
                        {
                            list.Add(item);
                        }
                    });
                }
            }

            return (obj, list) =>
            {
                foreach (var method in methods)
                {
                    method(obj, list);
                }
            };
        }

        private void RetrieveRelations(object o, IList<object> objects) => this.Memoization(this.relationsRetrieverDict, o.GetType(), () => this.RelationsRetriever(o.GetType()))(o, objects);

        private bool Are11Relation(Type firstType, Type secondType)
        {
            var key = Tuple.Create(firstType, secondType);

            if (!this.are11RelationsDict.TryGetValue(key, out var value))
            {
                value = this.Relations.GetPrimaryKeys(firstType).SequenceEqual(this.Relations.GetForeignKeys(firstType, secondType)); // Relax check to see if foreign keys are a subset of the primary keys

                this.are11RelationsDict.Add(key, value);
            }

            return value;
        }

        private readonly Dictionary<Tuple<Type, Type>, bool> are11RelationsDict = new Dictionary<Tuple<Type, Type>, bool>();

        private readonly Dictionary<Type, Func<object>> createValueDict = new Dictionary<Type, Func<object>>();

        private readonly Dictionary<PropertyInfo, Func<object, object>> getValueDict = new Dictionary<PropertyInfo, Func<object, object>>();

        private readonly Dictionary<Type, Func<object, Keys>> primaryKeyGetterDict = new Dictionary<Type, Func<object, Keys>>();

        private readonly Dictionary<Type, IList<PropertyInfo>> propertyInfoDict = new Dictionary<Type, IList<PropertyInfo>>();

        private readonly Dictionary<Type, IList<PropertyInfo>> collectionPropertyInfoDict = new Dictionary<Type, IList<PropertyInfo>>();

        private readonly Dictionary<Type, IList<PropertyInfo>> nonGenericReferencePropertyInfoDict = new Dictionary<Type, IList<PropertyInfo>>();

        private readonly Dictionary<PropertyInfo, Action<object, object>> setValueDict = new Dictionary<PropertyInfo, Action<object, object>>();

        private readonly Dictionary<Tuple<Type, Type>, Func<object, Keys>> getForeignKeysDict = new Dictionary<Tuple<Type, Type>, Func<object, Keys>>();

        private readonly Dictionary<PropertyInfo, Action<object, IList<object>>> setCollectionDict = new Dictionary<PropertyInfo, Action<object, IList<object>>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object, Keys>> setForeignKeysDict = new Dictionary<Tuple<Type, Type>, Action<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object>> clearForeignKeysDict = new Dictionary<Tuple<Type, Type>, Action<object>>();

        private object CreateObject(Type type) => this.Memoization(this.createValueDict, type, () => this.reflection.Constructor(type))();

        private Func<object, Keys> PrimaryKeyGetter(Type type) => this.Memoization(this.primaryKeyGetterDict, type, () => this.reflection.PrimaryKeyGetter(this.Relations, type));

        private Keys GetPrimaryKeys(object o) => this.PrimaryKeyGetter(o.GetType())(o);

        private IList<PropertyInfo> GetProperties(object o) => this.Memoization(this.propertyInfoDict, o.GetType(), () => ReflectionUtility.GetPublicPropertiesWithGetters(o.GetType()));

        private IList<PropertyInfo> GetCollectionProperties(object o) => this.Memoization(this.collectionPropertyInfoDict, o.GetType(), () => ReflectionUtility.GetPublicPropertiesWithGetters(o.GetType()).Where(pi => ReflectionUtility.IsCollectionType(pi.PropertyType)).ToList());

        private IList<PropertyInfo> GetNonGenericReferenceProperties(object o) => this.Memoization(this.nonGenericReferencePropertyInfoDict, o.GetType(), () => ReflectionUtility.GetPublicPropertiesWithGetters(o.GetType()).Where(pi => ReflectionUtility.IsNonGenericReferenceType(pi.PropertyType)).ToList());

        private object GetValue(object o, PropertyInfo pi) => this.Memoization(this.getValueDict, pi, () => this.reflection.Getter(pi))(o);

        private void SetValue(object o, PropertyInfo pi, object value) => this.Memoization(this.setValueDict, pi, () => this.reflection.Setter(pi))(o, value);

        private Keys GetForeignKeys(object o, Type foreignType) => this.Memoization(this.getForeignKeysDict, Tuple.Create(o.GetType(), foreignType), () => this.reflection.ForeignKeyGetter(this.Relations, o.GetType(), foreignType))(o);

        private void SetCollection(object o, PropertyInfo pi, IList<object> values) => this.Memoization(this.setCollectionDict, pi, () => this.reflection.CollectionSetter(pi, typeof(HashSet<>)))(o, values);

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
