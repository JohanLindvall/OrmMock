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
    using Comparers;

    /// <summary>
    /// Defines a class for holding and comparing object keys.
    /// </summary>
    public class Keys
    {
        /// <summary>
        /// Initializes a new instance of the KeyHolder class.
        /// </summary>
        /// <param name="keys">The array of object keys.</param>
        public Keys(params object[] keys)
        {
            this.Data = keys;
        }

        /// <summary>
        /// Gets the underlying key data.
        /// </summary>
        public object[] Data { get; }

        /// <summary>
        /// Implements object equality.
        /// </summary>
        /// <param name="other">The other instance.</param>
        /// <returns>Tru if the objects are equal, false otherwise.</returns>
        public bool Equals(Keys other) => ObjectArrayComparer.Default.Equals(this.Data, other.Data);

        ///  <inheritdoc />
        public override bool Equals(object other)
        {
            if (other is Keys k)
            {
                return this.Equals(k);
            }

            return false;
        }

        ///  <inheritdoc />
        public override int GetHashCode() => ObjectArrayComparer.Default.GetHashCode(this.Data);
    }
}
