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

namespace OrmMock
{
    using System;
    using Fasterflect;

    /// <summary>
    /// Defines reflection helper methods.
    /// </summary>
    public static class Reflection
    {
        /// <summary>
        /// Gets a function that creates an object of the given type by calling its parameter-less public constructor.
        /// </summary>
        /// <param name="t">The type of the object to create.</param>
        /// <returns>A function creating object of the given type.</returns>
        public static Func<object> ParameterlessConstructorInvoker(Type t)
        {
            var constructor = t.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
            {
                throw new InvalidOperationException($@"The type {t.Name} has no public parameter-less constructor");
            }

            var constructorDelegate = constructor.DelegateForCreateInstance();

            return () => constructorDelegate();
        }

        public static Action<object, object> SetPropertyValueInvoker(Type t, string property)
        {
            var setPropertyDelegate = t.DelegateForSetPropertyValue(property);

            return (obj, value) => setPropertyDelegate(obj, value);
        }

        public static Func<object, object> GetPropertyValueInvoker(Type t, string property)
        {
            var getPropertyDelegate = t.DelegateForGetPropertyValue(property);

            return obj => getPropertyDelegate(obj);
        }

        public static Func<object, object, object> CallMethodWithOneArgumentInvoker(Type t, Type argumentType, string method)
        {
            var callMethodDelegate = t.DelegateForCallMethod(method, argumentType);

            return (obj, arg) => callMethodDelegate(obj, arg);
        }
    }
}
