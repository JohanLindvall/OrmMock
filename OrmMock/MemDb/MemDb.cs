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


using OrmMock.Shared;
using OrmMock.Shared.Comparers;

namespace OrmMock.MemDb
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class MemDb
    {
        public Relations Relations { get; }

        private readonly IList<object> newObjects = new List<object>();

        private readonly IList<object> heldObjects = new List<object>();

        private readonly PropertyAccessor propertyAccessor;

        private readonly Dictionary<PropertyInfo, long> autoIncrement = new Dictionary<PropertyInfo, long>();

        public MemDb()
        {
            this.Relations = new Relations();
            this.propertyAccessor = new PropertyAccessor(this.Relations);
        }

        public void Add(object o)
        {
            this.newObjects.Add(o);
        }

        public void AddMany(IEnumerable<object> objects)
        {
            foreach (var o in objects)
            {
                this.Add(o);
            }
        }

        public void RegisterAutoIncrement<T>(Expression<Func<T, object>> expression) where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(expression))
            {
                this.autoIncrement.Add(property, 0);
            }
        }

        public bool Remove(object o)
        {
            var type = o.GetType();
            var keys = this.propertyAccessor.GetPrimaryKeys(o);
            var result = false;

            var i = this.heldObjects.Count - 1;

            while (i >= 0)
            {
                var toCheck = this.heldObjects[i];

                if (toCheck.GetType() == type)
                {
                    var toCheckKeys = this.propertyAccessor.GetPrimaryKeys(toCheck);

                    if (keys.Equals(toCheckKeys))
                    {
                        this.heldObjects.RemoveAt(i);
                        result = true;
                    }
                }

                --i;
            }

            return result;
        }

        public void Commit()
        {
            var seenObjects = this.DiscoverNewObjects();

            this.UpdateObjectRelations(seenObjects);
        }

        private void UpdateObjectRelations(HashSet<object> seenObjects)
        {
            var primaryKeyLookup = Deferred(() => this.heldObjects.ToDictionary(heldObject => Tuple.Create(heldObject.GetType(), this.propertyAccessor.GetPrimaryKeys(heldObject)), heldObject => heldObject));

            var incomingObjectsLookup = new Dictionary<Tuple<Type, Type>, IList<object>>();

            for (var pass = 0; pass < 2; ++pass)
            {
                foreach (var currentObject in this.heldObjects)
                {
                    var properties = this.propertyAccessor.GetProperties(currentObject.GetType());

                    foreach (var property in properties)
                    {
                        var propertyType = property.PropertyType;

                        if (FollowCollectionType(propertyType))
                        {
                            if (pass == 0)
                            {
                                continue;
                            }

                            var incomingObjectsKey = Tuple.Create(currentObject.GetType(), propertyType.GetGenericArguments()[0]);

                            if (incomingObjectsLookup.TryGetValue(incomingObjectsKey, out var incomingObjects))
                            {
                                // Set ICollection to incoming objects.
                                this.propertyAccessor.SetCollection(property, currentObject, incomingObjects);
                            }

                            // else leave untouched (unknown collection property)
                        }
                        else if (FollowType(propertyType))
                        {
                            if (pass == 1)
                            {
                                continue;
                            }

                            var foreignObject = this.propertyAccessor.GetValue(currentObject, property);

                            if (seenObjects.Contains(foreignObject))
                            {
                                // Update foreign keys to match foreignObject
                                var primaryKeysOfForeignObject = this.propertyAccessor.GetPrimaryKeys(foreignObject);

                                this.propertyAccessor.SetForeignKeys(currentObject, foreignObject.GetType(), primaryKeysOfForeignObject); // For 1:1 primary keys may change.
                            }
                            else if (primaryKeyLookup().TryGetValue(Tuple.Create(propertyType, this.propertyAccessor.GetForeignKeys(currentObject, propertyType)), out foreignObject))
                            {
                                this.propertyAccessor.SetValue(currentObject, property, foreignObject);
                            }
                            else
                            {
                                // Object not found. Clear nullable foreign keys.
                                this.propertyAccessor.SetValue(currentObject, property, null);
                                this.propertyAccessor.ClearForeignKeys(currentObject, propertyType);
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
            }
        }

        private HashSet<object> DiscoverNewObjects()
        {
            var seenObjects = new HashSet<object>(this.heldObjects, new ReferenceEqualityComparer());

            foreach (var newObject in this.newObjects)
            {
                foreach (var descendant in this.GetObjects(newObject, seenObjects))
                {
                    this.heldObjects.Add(descendant);

                    // Auto-increment for new objects.
                    var properties = this.propertyAccessor.GetProperties(descendant.GetType());

                    foreach (var property in properties)
                    {
                        if (this.autoIncrement.TryGetValue(property, out var value))
                        {
                            this.propertyAccessor.SetValue(descendant, property, Convert.ChangeType(++value, property.PropertyType));
                            this.autoIncrement[property] = value;
                        }
                    }
                }
            }

            this.newObjects.Clear();

            return seenObjects;
        }

        public IQueryable<T> Queryable<T>()
        {
            return this.heldObjects.Where(o => o.GetType() == typeof(T)).Select(o => (T)o).AsQueryable();
        }

        public int Count() => this.heldObjects.Count;

        public int Count(Type t) => this.heldObjects.Count(o => o.GetType() == t);

        public int Count<T>() => this.Count(typeof(T));

        public T Get<T>(params object[] keys)
        {
            var kh = new KeyHolder(keys);

            foreach (var heldObject in this.heldObjects)
            {
                if (heldObject.GetType() == typeof(T) && this.propertyAccessor.GetPrimaryKeys(heldObject).Equals(kh))
                {
                    return (T)heldObject;
                }
            }

            return default(T);
        }

        private static Func<T> Deferred<T>(Func<T> creator)
        {
            bool initialized = false;
            T result = default(T);

            return () =>
            {
                if (!initialized)
                {
                    result = creator();
                    initialized = true;
                }

                return result;
            };
        }

        private static bool FollowType(Type t)
        {
            return t.IsClass && !t.IsGenericType && Nullable.GetUnderlyingType(t) == null && t != typeof(string);
        }

        private static bool FollowCollectionType(Type t)
        {
            if (t.IsGenericType)
            {
                var args = t.GetGenericArguments();

                return (typeof(ICollection<>).MakeGenericType(args).IsAssignableFrom(t)) && FollowType(args[0]);
            }

            return false;
        }

        private IEnumerable<object> GetObjects(object root, HashSet<object> seenObjects)
        {
            if (root == null)
            {
                yield break;
            }

            if (seenObjects.Add(root))
            {
                yield return root;

                var properties = this.propertyAccessor.GetProperties(root.GetType());

                foreach (var property in properties)
                {
                    var propertyType = property.PropertyType;

                    if (FollowCollectionType(propertyType))
                    {
                        var enumerable = (IEnumerable)this.propertyAccessor.GetValue(root, property);

                        foreach (var collectionItem in enumerable)
                        {
                            foreach (var descendant in this.GetObjects(collectionItem, seenObjects))
                            {
                                yield return descendant;
                            }
                        }
                    }
                    else if (FollowType(propertyType))
                    {
                        foreach (var descendant in this.GetObjects(this.propertyAccessor.GetValue(root, property), seenObjects))
                        {
                            yield return descendant;
                        }
                    }
                }
            }
        }
    }
}
