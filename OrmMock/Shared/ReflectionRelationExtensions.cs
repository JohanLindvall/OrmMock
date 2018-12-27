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

    /// <summary>
    /// Defines reflection and relation extensions.
    /// </summary>
    public static class ReflectionRelationExtensions
    {
        /// <summary>
        /// Creates a primary key getter for the given type and relations.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="relations">The relations.</param>
        /// <param name="thisType">The type of the object.</param>
        /// <returns>A function retrieving the primary keys of an object.</returns>
        public static Func<object, Keys> PrimaryKeyGetter(this IReflection reflection, IRelations relations, Type thisType)
        {
            return reflection.KeyGetter(relations.GetPrimaryKeys(thisType));
        }

        /// <summary>
        /// Creates a primary key setter for the given type and relations.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="relations">The relations.</param>
        /// <param name="thisType">The type of the object.</param>
        /// <returns>A function setting the primary keys of an object.</returns>
        public static Action<object, Keys> PrimaryKeySetter(this IReflection reflection, IRelations relations, Type thisType)
        {
            return reflection.KeySetter(relations.GetPrimaryKeys(thisType));
        }

        /// <summary>
        /// Creates a foreign key getter for the given types and relations.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="relations">The relations.</param>
        /// <param name="thisType">The type of the object.</param>
        /// <param name="foreignType">The type of the foreign object.</param>
        /// <returns>A function retrieving the foreign keys of an object.</returns>
        public static Func<object, Keys> ForeignKeyGetter(this IReflection reflection, IRelations relations, Type thisType, Type foreignType)
        {
            return reflection.KeyGetter(relations.GetForeignKeys(thisType, foreignType));
        }

        /// <summary>
        /// Creates a foreign key setter for the given types and relations.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="relations">The relations.</param>
        /// <param name="thisType">The type of the object.</param>
        /// <param name="foreignType">The type of the foreign object.</param>
        /// <returns>A function setting the foreign keys of an object.</returns>
        public static Action<object, Keys> ForeignKeySetter(this IReflection reflection, IRelations relations, Type thisType, Type foreignType)
        {
            return reflection.KeySetter(relations.GetForeignKeys(thisType, foreignType));
        }

        /// <summary>
        /// Creates a function clearing the foreign keys for the given types and relations.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="relations">The relations.</param>
        /// <param name="thisType">The type of the object.</param>
        /// <param name="foreignType">The type of the foreign object.</param>
        /// <returns>A function clearing the foreign keys of an object.</returns>
        public static Action<object> ForeignKeyClearer(this IReflection reflection, IRelations relations, Type thisType, Type foreignType)
        {
            return reflection.PropertyClearer(relations.GetForeignKeys(thisType, foreignType));
        }
    }
}
