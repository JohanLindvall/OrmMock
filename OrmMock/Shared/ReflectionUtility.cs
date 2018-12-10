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
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Defines reflection utility methods.
    /// </summary>
    public static class ReflectionUtility
    {
        public static IList<PropertyInfo> GetPublicPropertiesWithGetters(Type t) => t.GetProperties().Where(HasGetter).ToList();

        public static IList<PropertyInfo> GetPublicPropertiesWithGettersAndSetters(Type t) => t.GetProperties().Where(p => HasGetter(p) && HasSetter(p)).ToList();

        /// <summary>
        /// Determines if the given property has a setter.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the given property has a setter; false otherwise</returns>
        public static bool HasSetter(PropertyInfo property) => property.SetMethod != null;

        /// <summary>
        /// Determines if the given property has a getter.
        /// </summary>
        /// <param name="property">The property to check.</param>
        /// <returns>True if the given property has a getter; false otherwise</returns>
        public static bool HasGetter(PropertyInfo property) => property.GetMethod != null;

        /// <summary>
        /// Returns true if the type is a nullable value type or string.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a nullable value type or string, false otherwise.</returns>
        public static bool IsNullableOrString(Type type) => Nullable.GetUnderlyingType(type) != null || type == typeof(string);

        /// <summary>
        /// Returns true if the type is a value type, or a nullable value type or string.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is value type, false otherwise.</returns>
        public static bool IsValueTypeOrNullableOrString(Type type) => type.IsValueType || IsNullableOrString(type);

        /// <summary>
        /// Returns true if the type is a non-generic reference type (but not nullable or string)
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is reference type, false otherwise.</returns>
        public static bool IsNonGenericReferenceType(Type type) => type.IsClass && !type.IsGenericType && !IsNullableOrString(type);

        /// <summary>
        /// Returns true if the type is a a collection type of a non generic reference type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a a collection type of a non generic reference type, false otherwise.</returns>
        public static bool IsCollectionType(Type type)
        {
            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();

                return (typeof(ICollection<>).MakeGenericType(args).IsAssignableFrom(type)) && IsNonGenericReferenceType(args[0]);
            }

            return false;
        }
    }
}
