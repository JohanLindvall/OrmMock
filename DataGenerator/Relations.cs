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
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Defines relations for objects, including primary keys and foreign keys.
    /// </summary>
    public class Relations
    {
        /// <summary>
        /// Holds the dictionary of types to key properties
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> primaryKeys = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Holds the dictionary of type, type to foreign key properties
        /// </summary>
        private readonly Dictionary<Tuple<Type, Type>, PropertyInfo[]> foreignKeys = new Dictionary<Tuple<Type, Type>, PropertyInfo[]>();

        /// <summary>
        /// Registers an expression defining the primary keys of an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression defining the primary keys.</param>
        public void RegisterPrimaryKeys<T>(Expression<Func<T, object>> expression)
            where T : class
        {
            this.primaryKeys.Add(typeof(T), ExpressionUtility.GetPropertyInfo(expression));
        }

        /// <summary>
        /// Registers an expression defining the foreign keys of an object.
        /// </summary>
        /// <typeparam name="TThis">The type of the object.</typeparam>
        /// <typeparam name="TForeign">The type of the foreign object.</typeparam>
        /// <param name="expression">The expression defining the foreign keys.</param>
        public void RegisterForeignKeys<TThis, TForeign>(Expression<Func<TThis, object>> expression)
            where TThis : class
            where TForeign : class
        {
            this.foreignKeys.Add(new Tuple<Type, Type>(typeof(TThis), typeof(TForeign)), ExpressionUtility.GetPropertyInfo(expression));
        }

        /// <summary>
        /// Gets the primary key properties for a given type.
        /// </summary>
        /// <param name="t">The type of the object.</param>
        /// <returns></returns>
        public PropertyInfo[] GetPrimaryKeys(Type t)
        {
            if (!this.primaryKeys.TryGetValue(t, out var result))
            {
                var key = t.GetProperty("Id");

                if (key == null)
                {
                    throw new InvalidOperationException($@"Unable to determine key for type '{t.Name}'.");
                }

                result = new[] { key };

                this.primaryKeys.Add(t, result);
            }

            return result;
        }

        /// <summary>
        /// Gets the primary key properties for a given type.
        /// </summary>
        /// <param name="tThis">The type of this object.</param>
        /// <param name="tForeign">The type of the foreign object..</param>
        /// <returns></returns>
        public PropertyInfo[] GetForeignKeys(Type tThis, Type tForeign)
        {
            var key = new Tuple<Type, Type>(tThis, tForeign);

            if (!this.foreignKeys.TryGetValue(key, out var result))
            {
                var foreign = tThis.GetProperty(TrimTypeName(tForeign.Name) + "Id");

                if (foreign == null)
                {
                    throw new InvalidOperationException($@"Unable to determine foreign key for type '{tThis.Name}' to '{tForeign.Name}'.");
                }

                result = new[] { foreign };

                this.foreignKeys.Add(key, result);
            }

            var primary = this.GetPrimaryKeys(tForeign);

            var mismatch = false;

            if (result.Length != primary.Length)
            {
                mismatch = true;
            }
            else
            {
                for (var i = 0; i < result.Length; ++i)
                {
                    if (!object.ReferenceEquals(result[i], primary[i]))
                    {
                        mismatch = true;
                        break;
                    }
                }
            }

            if (mismatch)
            {
                throw new InvalidOperationException($"Primary keys and foreign keys for '{tForeign}' in '{tThis}' do not match.");
            }

            return result;
        }

        private static string TrimTypeName(string input)
        {
            if (input.StartsWith("Db", StringComparison.Ordinal))
            {
                return input.Substring(2);
            }

            return input;
        }
    }
}
