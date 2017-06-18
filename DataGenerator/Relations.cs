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
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Defines relations for objects, including primary keys and foreign keys.
    /// </summary>
    class Relations
    {
        /// <summary>
        /// Holds the dictionary of types to key properties
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> primaryKeys = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Registers an expression defining the primary keys of an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression defining the primary keys.</param>
        public void RegisterPrimaryKeys<T>(Expression<Func<T, object>> expression)
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
                    this.primaryKeys.Add(typeof(T), mes.Select(a => a.Member as PropertyInfo).ToArray());
                    return;
                }
            }
            else if ((me = body as MemberExpression) != null)
            {
                var pi = me.Member as PropertyInfo;
                if (pi != null)
                {
                    this.primaryKeys.Add(typeof(T), new[] { me.Member as PropertyInfo });
                }
                return;
            }

            throw new InvalidOperationException("Unsupported expression.");
        }

        /// <summary>
        /// Gets the primary key properties for a given type.
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public PropertyInfo[] GetPrimaryKeys(Type t)
        {
            PropertyInfo[] result;

            if (!this.primaryKeys.TryGetValue(t, out result))
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
    }
}
