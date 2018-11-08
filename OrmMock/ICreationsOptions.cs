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
    public interface ICreationsOptions<T>
    {
        /// <summary>
        /// Excludes a value from being set. The default value will be used.
        /// </summary>
        /// <returns>The type context.</returns>
        ForTypeContext<T> Skip();

        /// <summary>
        /// Parents are ignored for this value.
        /// </summary>
        /// <returns>The type context.</returns>
        ForTypeContext<T> IgnoreParents();

        /// <summary>
        /// Only direct parents are used for this value. A new object is not created.
        /// </summary>
        /// <returns>The type context.</returns>
        ForTypeContext<T> OnlyDirectParent();

        /// <summary>
        /// Only parents are used for this value. A new object is not created.
        /// </summary>
        /// <returns>The type context.</returns>
        ForTypeContext<T> OnlyParents();
    }
}
