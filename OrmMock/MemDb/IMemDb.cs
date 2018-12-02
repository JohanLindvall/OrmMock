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

namespace OrmMock.MemDb
{
    using System.Collections.Generic;
    using Shared;

    /// <summary>
    /// Defines the memory database public interface.
    /// </summary>
    public interface IMemDb
    {
        /// <summary>
        /// Adds an object to the memory database instance.
        /// </summary>
        /// <param name="o">The object to add.</param>
        void Add(object o);

        /// <summary>
        /// Adds an enumerable of objects to the memory database instance.
        /// </summary>
        /// <param name="objects">The enumerable of objects to add.</param>
        void AddMany(IEnumerable<object> objects);

        /// <summary>
        /// Removes an object from the memory database instance.
        /// </summary>
        /// <param name="o">The object to remove.</param>
        /// <returns>True if the object was removed; false otherwise.</returns>
        bool Remove(object o);

        /// <summary>
        /// Removes the object of the given type and the given primary keys from the memory database instance.
        /// </summary>
        /// <typeparam name="T">The type of the object to remove.</typeparam>
        /// <param name="keys">The primary keys of the object to remove.</param>
        /// <returns>True if the object was removed; false otherwise.</returns>
        bool Remove<T>(Keys keys);

        /// <summary>
        /// Commits changes to the memory database instance. References and keys are updated.
        /// </summary>
        void Commit();

        /// <summary>
        /// Gets an enumerable of objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the objects in the enumerable.</typeparam>
        /// <returns>An enumerable of objects of the given type.</returns>
        IEnumerable<T> Get<T>();

        /// <summary>
        /// Returns the number of objects in the memory database instance.
        /// </summary>
        /// <returns>The number of objects.</returns>
        int Count();

        /// <summary>
        /// Returns the number of objects og the given type in the memory database instance.
        /// </summary>
        /// <typeparam name="T">The type of the objects to obtain the instance count of.</typeparam>
        /// <returns>The number of objects.</returns>
        int Count<T>();

        /// <summary>
        /// Gets an object of the given type, having the given primary keys.
        /// </summary>
        /// <typeparam name="T">The type of the objects to retrieve.</typeparam>
        /// <param name="keys">The primary keys of the object to retrieve.</param>
        /// <returns>An object, or null of no object with the given properties exist.</returns>
        T Get<T>(Keys keys);
    }
}
