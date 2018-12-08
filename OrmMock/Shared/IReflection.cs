using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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

namespace OrmMock.Shared
{
    /// <summary>
    /// Defines the basic reflection interface.
    /// </summary>
    public interface IReflection
    {
        /// <summary>
        /// Gets a function that creates an object of the given type by calling its parameter-less public constructor.
        /// </summary>
        /// <param name="t">The type of the object to create.</param>
        /// <returns>A function creating object of the given type.</returns>
        Func<object> Constructor(Type t);

        /// <summary>
        /// Gets a function setting a specific property on an object.
        /// </summary>
        /// <param name="propertyInfo">The property to set.</param>
        /// <returns>A function setting the property.</returns>
        Action<object, object> Setter(PropertyInfo propertyInfo);

        /// <summary>
        /// Gets a function getting a specific property on an object.
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <returns>A function getting the property.</returns>
        Func<object, object> Getter(PropertyInfo propertyInfo);

        /// <summary>
        /// Gets a function calling a specific parameter-less method on an object, returning a value.
        /// </summary>
        /// <param name="resultType">The type of the result.</param>
        /// <param name="method">The name of the method</param>
        /// <returns></returns>
        Func<object, object> Caller(Type resultType, string method);

        /// <summary>
        ///Gets a function calling a specific method with one parameter on an object, returning a value.
        /// </summary>
        /// <param name="resultType">The type of the result</param>
        /// <param name="argumentType">The type of the argument.</param>
        /// <param name="method">The name of the method.</param>
        /// <returns></returns>
        Func<object, object, object> Caller(Type resultType, Type argumentType, string method);
    }
}
