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
    using System.Collections.Generic;

    /// <summary>
    /// Holds a memoization cache (used for storing reflection expressions)
    /// </summary>
    public class Memoization
    {
        /// <summary>
        /// Holds the dictionary backing store for the cache.
        /// </summary>
        private readonly Dictionary<object, object> dict = new Dictionary<object, object>();

        /// <summary>
        /// Gets an item from the or creates the item (and stores it in the cache).
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="identifier">The string identifier.</param>
        /// <param name="key">The key instance.</param>
        /// <param name="factory">The value factory.</param>
        /// <returns>A value.</returns>
        public TValue Get<TKey, TValue>(string identifier, TKey key, Func<TValue> factory) => this.GetInner(Tuple.Create(identifier, key), factory);

        /// <summary>
        /// Gets an item from the or creates the item (and stores it in the cache).
        /// </summary>
        /// <typeparam name="TKey1">The type of the first key.</typeparam>
        /// <typeparam name="TKey2">The type of the second key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        /// <param name="identifier">The string identifier.</param>
        /// <param name="key1">The first key instance.</param>
        /// <param name="key2">The second key instance.</param>
        /// <param name="factory">The value factory.</param>
        /// <returns>A value.</returns>
        public TValue Get<TKey1, TKey2, TValue>(string identifier, TKey1 key1, TKey2 key2, Func<TValue> factory) => this.GetInner(Tuple.Create(identifier, key1, key2), factory);

        private TValue GetInner<TKey, TValue>(TKey key, Func<TValue> factory)
        {
            if (this.dict.TryGetValue(key, out var value))
            {
                return (TValue)value;
            }
            else
            {
                var result = factory();
                this.dict.Add(key, result);
                return result;
            }
        }
    }
}
