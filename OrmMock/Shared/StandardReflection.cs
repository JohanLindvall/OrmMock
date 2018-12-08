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

    class StandardReflection : IReflection
    {
        public Func<object> Constructor(Type t) => () => t.GetConstructor(Type.EmptyTypes)?.Invoke(new object[0]) ?? throw new InvalidOperationException();

        public Action<object, object> Setter(PropertyInfo propertyInfo) => propertyInfo.SetValue;

        public Func<object, object> Getter(PropertyInfo propertyInfo) => propertyInfo.GetValue;

        public Func<object, object> Caller(Type resultType, string method) => o => o.GetType().GetMethod(method, Type.EmptyTypes)?.Invoke(o, new object[0]) ?? throw new InvalidOperationException();

        public Func<object, object, object> Caller(Type resultType, Type argumentType, string method) => (o, a) => o.GetType().GetMethod(method, new[] { argumentType })?.Invoke(o, new[] { a }) ?? throw new InvalidOperationException();
    }
}
