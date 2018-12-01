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

using System.Collections.Generic;

namespace OrmMock.Shared.Comparers
{
    /// <summary>
    /// Implements a comparer comparing the held array contents.
    /// </summary>
    public class ObjectArrayComparer : IEqualityComparer<object[]>
    {
        /// <summary>
        /// Holds the default shared instance.
        /// </summary>
        public static readonly ObjectArrayComparer Default = new ObjectArrayComparer();

        /// <inheritdoc />
        public bool Equals(object[] x, object[] y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }

            if (x == null || y == null)
            {
                return false;
            }

            if (x.Length != y.Length)
            {
                return false;
            }

            for (var i = 0; i < x.Length; ++i)
            {
                if (!x[i].Equals(y[i])) return false;
            }

            return true;
        }

        /// <inheritdoc />
        public int GetHashCode(object[] obj)
        {
            unchecked
            {
                int hash = 17;

                foreach (var singleObj in obj)
                {
                    hash = hash * 31 + singleObj.GetHashCode();
                }

                return hash;
            }
        }
    }
}
