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

namespace DataGenerator
{
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
        private readonly Dictionary<Type, List<object>> heldObjects = new Dictionary<Type, List<object>>();

        /// <summary>
        /// Holds the dictionary of types to key properties
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> knownKeys = new Dictionary<Type, PropertyInfo[]>();

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
        public void RegisterKey<T>(Expression<Func<T, object>> expression)
            where T : class
        {
            var body = expression.Body;
            var ue = body as UnaryExpression;
            if (ue != null && ue.NodeType == ExpressionType.Convert)
            {
                body = ue.Operand;
            }

            NewExpression ne;
            MemberExpression me;
            if ((ne = body as NewExpression) != null)
            {
                var mes = ne.Arguments.Select(a => a as MemberExpression).ToArray();
                if (mes.All(t => t != null))
                {
                    this.knownKeys.Add(typeof(T), mes.Select(a => a.Member as PropertyInfo).ToArray());
                    return;
                }
            }
            else if ((me = body as MemberExpression) != null)
            {
                var pi = me.Member as PropertyInfo;
                if (pi != null)
                {
                    this.knownKeys.Add(typeof(T), new[] { me.Member as PropertyInfo });
                }
                return;
            }

            throw new InvalidOperationException("Unsupported expression.");
        }

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
            var t = typeof(T);
            var kp = this.GetKeyProperties(t);
            var i = this.IndexOf(t, kp, keys);
            if (i != -1)
            {
                return (T)this.heldObjects[t][i];
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
            List<object> objs;
            if (!this.heldObjects.TryGetValue(typeof(T), out objs))
            {
                objs = new List<object>();
            }

            return objs.Select(o => (T)o).AsQueryable();
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
            var kp = this.GetKeyProperties(t);
            var ks = kp.Select(p => p.GetMethod.Invoke(obj, new object[0])).ToArray();
            var i = this.IndexOf(t, kp, ks);
            if (i != -1)
            {
                this.heldObjects[t].RemoveAt(i);
            }

            return i != -1;
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

            var keyProperties = this.GetKeyProperties(t);
            var keyValues = keyProperties.Select(p => p.GetMethod.Invoke(obj, new object[0])).ToArray();
            if (this.IndexOf(t, keyProperties, keyValues) != -1)
            {
                if (visited == null)
                {
                    throw new InvalidOperationException($@"An object of type '{t.Name}' and the keys '{string.Join(", ", keyValues.Select(k => k.ToString()))}' already exists.");
                }

                return;
            }

            List<object> objs;
            if (!this.heldObjects.TryGetValue(t, out objs))
            {
                objs = new List<object>();
                this.heldObjects.Add(t, objs);
            }

            objs.Add(obj);

            if (visited == null)
            {
                visited = new HashSet<object>(new ObjectEqualityComparer());
            }

            visited.Add(obj);

            foreach (var prop in t.GetProperties().Where(p => p.PropertyType.IsClass || p.PropertyType.IsInterface))
            {
                var dest = prop.GetMethod.Invoke(obj, new object[0]);

                if (dest == null)
                {
                    continue;
                }

                var pt = prop.PropertyType;

                if (pt.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICollection<>)))
                {
                    var elementType = pt.GetGenericArguments()[0];

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
        }

        private int IndexOf(Type t, PropertyInfo[] kp, object[] keys)
        {
            List<object> objs;
            if (this.heldObjects.TryGetValue(t, out objs))
            {
                for (var idx = 0; idx < objs.Count; ++idx)
                {
                    var obj = objs[idx];
                    var okeys = kp.Select(p => p.GetMethod.Invoke(obj, new object[0])).ToArray();

                    if (keys.Length == okeys.Length)
                    {
                        var miss = false;

                        for (var i = 0; i < keys.Length; ++i)
                        {
                            if (!object.Equals(keys[i], okeys[i]))
                            {
                                miss = true;
                                break;
                            }
                        }

                        if (!miss)
                        {
                            return idx;
                        }
                    }
                }
            }

            return -1;
        }

        private PropertyInfo[] GetKeyProperties(Type t)
        {
            PropertyInfo[] result;

            if (!this.knownKeys.TryGetValue(t, out result))
            {
                var key = t.GetProperty("Id");

                if (key == null)
                {
                    throw new InvalidOperationException($@"Unable to determine key for type '{t.Name}'.");
                }

                result = new[] { key };
                this.knownKeys.Add(t, result);
            }

            return result;
        }

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
