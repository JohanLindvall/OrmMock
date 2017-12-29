// Copyright(c) 2017 Johan Lindvall
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


namespace OrmMock
{
    using Fasterflect;

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Defines a class for a simple in-memory DB using LINQ as the query language.
    /// </summary>
    public class MemDb
    {
        /// <summary>
        /// Holds the dictionary of types to objects.
        /// </summary>
        private readonly Dictionary<Type, Dictionary<object[], object>> heldObjects = new Dictionary<Type, Dictionary<object[], object>>();

        /// <summary>
        /// Holds the dictionary of cached key getters.
        /// </summary>
        private readonly Dictionary<Type, MemberGetter[]> keyGetters = new Dictionary<Type, MemberGetter[]>();

        /// <summary>
        /// Holds the dictionary of cached object reference handlers.
        /// </summary>
        private readonly Dictionary<Type, Action<object, HashSet<object>>> referenceHandlers = new Dictionary<Type, Action<object, HashSet<object>>>();

        /// <summary>
        /// Holds the object relations.
        /// </summary>
        private readonly Relations relations = new Relations();

        /// <summary>
        /// Gets or sets the include filter, defining which types to include in the db.
        /// </summary>
        public Func<Type, bool> IncludeFilter { get; set; }

        /// <summary>
        /// Gets the count of objects for a specific type.
        /// </summary>
        /// <typeparam name="T">The type of the object</typeparam>
        /// <returns>The count of objects of the given type.</returns>
        public int Count<T>() where T : class => this.Queryable<T>().Count();

        /// <summary>
        /// Gets the total count of objects
        /// </summary>
        /// <returns>The total count of objects</returns>
        public int Count() => this.heldObjects.Select(h => h.Value.Count).Sum();

        /// <summary>
        /// Registers an expression defining the primary keys of an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression defining the primary keys.</param>
        public void RegisterKey<T>(Expression<Func<T, object>> expression) where T : class => this.relations.RegisterPrimaryKeys(expression);

        /// <summary>
        /// Adds an object to the DB.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="obj">The object to add.</param>
        public void Add<T>(T obj)
            where T : class
        {
            this.Add(typeof(T), obj, null);
        }

        /// <summary>
        /// Gets an object from the db.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="keys">The keys.</param>
        /// <returns>The stored object of the given type, or null if the object was not found.</returns>
        public T Get<T>(params object[] keys)
            where T : class
        {
            if (this.heldObjects.TryGetValue(typeof(T), out var objects) && objects.TryGetValue(keys, out var result))
            {
                return (T)result;
            }

            return default(T);
        }

        /// <summary>
        /// Gets a queryable of a given type.
        /// </summary>
        /// <typeparam name="T">The type of the objects.</typeparam>
        /// <returns>A queryable.</returns>
        public IQueryable<T> Queryable<T>()
            where T : class
        {
            if (!this.heldObjects.TryGetValue(typeof(T), out var objs))
            {
                return new T[0].AsQueryable();
            }

            return objs.Select(o => (T)o.Value).AsQueryable();
        }

        /// <summary>
        /// Removes an object from the database.
        /// </summary>
        /// <typeparam name="T">The type of the object to remove.</typeparam>
        /// <param name="obj">The object to remove.</param>
        /// <returns>True if the object was removed, false otherwise.</returns>
        public bool Remove<T>(T obj)
            where T : class
        {
            var t = typeof(T);

            if (this.heldObjects.TryGetValue(t, out var objects))
            {
                return objects.Remove(this.GetKeys(t, obj));
            }

            return false;
        }

        private MemberGetter[] GetKeyGetters(Type t)
        {
            if (!this.keyGetters.TryGetValue(t, out var result))
            {
                result = this.GetKeyProperties(t).Select(p => p.DelegateForGetPropertyValue()).ToArray();

                this.keyGetters.Add(t, result);
            }

            return result;
        }

        private object[] GetKeys(Type t, object input)
        {
            var getters = this.GetKeyGetters(t);
            var result = new object[getters.Length];
            for (var i = 0; i < getters.Length; ++i)
            {
                result[i] = getters[i](input);
            }

            return result;
        }

        private class ObjectArrayComparer : IEqualityComparer<object[]>
        {
            public bool Equals(object[] x, object[] y)
            {
                for (var i = 0; i < x.Length; ++i)
                {
                    if (!x[i].Equals(y[i])) return false;
                }

                return true;
            }

            public int GetHashCode(object[] obj)
            {
                unchecked
                {
                    int hash = 17;

                    foreach (var singleObj in obj)
                    {
                        hash = hash * 31 + singleObj.GetHashCode();
                    }

                    return hash;
                }
            }
        }

        private void Add(Type t, object obj, HashSet<object> visited)
        {
            if (this.IncludeFilter != null && !this.IncludeFilter(t) || t == typeof(string) || t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return;
            }

            if (visited != null && visited.Contains(obj))
            {
                return;
            }

            if (!this.heldObjects.TryGetValue(t, out var objs))
            {
                objs = new Dictionary<object[], object>(new ObjectArrayComparer());
                this.heldObjects.Add(t, objs);
            }

            var keyValues = this.GetKeys(t, obj);
            objs.Add(keyValues, obj);

            var handler = this.GetReferenceHandler(t);

            if (handler != null)
            {
                if (visited == null)
                {
                    visited = new HashSet<object>(new ObjectEqualityComparer());
                }
            }

            visited?.Add(obj);

            handler?.Invoke(obj, visited);
        }

        private Action<object, HashSet<object>> GetReferenceHandler(Type t)
        {
            if (!this.referenceHandlers.TryGetValue(t, out var result))
            {
                var props = t.GetProperties().Where(p => p.PropertyType.IsClass || p.PropertyType.IsInterface).ToArray();
                var propGetters = props.Select(prop => prop.DelegateForGetPropertyValue()).ToArray();
                var propertyTypes = props.Select(prop => prop.PropertyType).ToArray();
                var elementTypes = props.Select(prop => prop.PropertyType.GetInterfaces().Any(xf => xf.IsGenericType && xf.GetGenericTypeDefinition() == typeof(ICollection<>)) ? prop.PropertyType.GetGenericArguments()[0] : null).ToArray();

                if (props.Length != 0)
                {
                    result = (obj, visited) =>
                    {
                        for (var i = 0; i < props.Length; ++i)
                        {
                            var dest = propGetters[i](obj);

                            if (dest == null)
                            {
                                continue;
                            }

                            var pt = propertyTypes[i];
                            var elementType = elementTypes[i];

                            if (elementType != null)
                            {
                                foreach (var collObj in dest as IEnumerable)
                                {
                                    this.Add(elementType, collObj, visited);
                                }
                            }
                            else
                            {
                                this.Add(pt, dest, visited);
                            }
                        }
                    };
                }

                this.referenceHandlers.Add(t, result);
            }

            return result;
        }

        private PropertyInfo[] GetKeyProperties(Type t) => this.relations.GetPrimaryKeys(t);

        private class ObjectEqualityComparer : IEqualityComparer<object>
        {
            bool IEqualityComparer<object>.Equals(object x, object y)
            {
                return object.ReferenceEquals(x, y);
            }

            int IEqualityComparer<object>.GetHashCode(object obj)
            {
                return RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
