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

namespace OrmMock.Shared.Comparers
{

    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Compares objects using reference equality.
    /// </summary>
    public class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        /// <summary>
        /// Gets the default instance.
        /// </summary>
        public static ReferenceEqualityComparer Default => new ReferenceEqualityComparer();

        /// <inheritdoc />
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return object.ReferenceEquals(x, y);
        }

        /// <inheritdoc />
        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }
}
