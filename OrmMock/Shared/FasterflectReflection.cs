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
    using System;
    using System.Reflection;
    using Fasterflect;

    /// <summary>
    /// Defines reflection helper methods using Fasterflect.
    /// </summary>
    public class FasterflectReflection : IReflection
    {
        /// <inheritdoc />
        public Func<object> Constructor(Type t)
        {
            var constructor = t.GetConstructor(Type.EmptyTypes);

            if (constructor == null)
            {
                throw new InvalidOperationException($@"The type {t.Name} has no public parameter-less constructor");
            }

            var constructorDelegate = constructor.DelegateForCreateInstance();

            return () => constructorDelegate();
        }

        /// <inheritdoc />
        public Action<object, object> Setter(PropertyInfo propertyInfo)
        {
            var setPropertyDelegate = propertyInfo.DeclaringType.DelegateForSetPropertyValue(propertyInfo.Name);

            return (obj, value) => setPropertyDelegate(obj, value);
        }

        /// <inheritdoc />
        public Func<object, object> Getter(PropertyInfo propertyInfo)
        {
            var getPropertyDelegate = propertyInfo.DeclaringType.DelegateForGetPropertyValue(propertyInfo.Name);

            return obj => getPropertyDelegate(obj);
        }

        /// <inheritdoc />
        public Func<object, object> Caller(Type t, string method)
        {
            var callMethodDelegate = t.DelegateForCallMethod(method, Type.EmptyTypes);

            return obj => callMethodDelegate(obj, null);
        }

        /// <inheritdoc />
        public Func<object, object, object> Caller(Type t, Type argumentType, string method)
        {
            var callMethodDelegate = t.DelegateForCallMethod(method, argumentType);

            return (obj, arg) => callMethodDelegate(obj, arg);
        }
    }
}
