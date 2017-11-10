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
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Defines methods for extracting property info from expressions.
    /// </summary>
    public static class ExpressionUtility
    {
        /// <summary>
        /// Extracts property info from the given expression.
        /// </summary>
        /// <typeparam name="T">The type of the object in the expression</typeparam>
        /// <typeparam name="T2">The type of the property in the expression.</typeparam>
        /// <param name="expression">The expression.</param>
        /// <returns>An array of property infos.</returns>
        public static PropertyInfo[] GetPropertyInfo<T, T2>(Expression<Func<T, T2>> expression)
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
                    return mes.Select(a => a.Member as PropertyInfo).ToArray();
                }
            }
            else if ((me = body as MemberExpression) != null)
            {
                var pi = me.Member as PropertyInfo;

                if (pi != null)
                {
                    return new[] { pi };
                }
            }

            throw new InvalidOperationException("Unsupported expression.");
        }
    }
}
