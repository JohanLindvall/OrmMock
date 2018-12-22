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
    /// Defines Reflection extension methods.
    /// </summary>
    public static class ReflectionExtensions
    {
        /// <summary>
        /// Creates a list of setters from the given property info list.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfos">The property info enumerable.</param>
        /// <returns>A list of setters.</returns>
        public static IList<Action<object, object>> Setters(this IReflection reflection, IEnumerable<PropertyInfo> propertyInfos) => propertyInfos.Select(reflection.Setter).ToList();

        /// <summary>
        /// Creates a list of getters from the given property info list.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfos">The property info enumerable.</param>
        /// <returns>A list of getters.</returns>
        public static IList<Func<object, object>> Getters(this IReflection reflection, IEnumerable<PropertyInfo> propertyInfos) => propertyInfos.Select(reflection.Getter).ToList();

        /// <summary>
        /// Creates a key getter from the given property infos.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfos">The property info enumerable.</param>
        /// <returns>A list of getters.</returns>
        public static Func<object, Keys> KeyGetter(this IReflection reflection, IEnumerable<PropertyInfo> propertyInfos)
        {
            var getters = reflection.Getters(propertyInfos);

            return local =>
            {
                // Avoid LINQ, too slow.
                var arr = new object[getters.Count];
                for (var i = 0; i < arr.Length; ++i)
                {
                    arr[i] = getters[i](local);
                }

                return new Keys(arr);
            };
        }

        /// <summary>
        /// Creates a key setter from the given property infos.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfos">The property info enumerable.</param>
        /// <returns>A list of getters.</returns>
        public static Action<object, Keys> KeySetter(this IReflection reflection, IEnumerable<PropertyInfo> propertyInfos)
        {
            var setters = reflection.Setters(propertyInfos);

            return (local, keys) =>
            {
                if (setters.Count != 0 && setters.Count != keys.Data.Length)
                {
                    throw new InvalidOperationException("Setters and keys must be of equal length.");
                }

                for (var i = 0; i < setters.Count; ++i)
                {
                    setters[i](local, keys.Data[i]);
                }
            };
        }

        /// <summary>
        /// Creates a function clearing setting the given properties on the object to null.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfos">The property info enumerable.</param>
        /// <returns>A function object setting the properties to null.</returns>
        public static Action<object> PropertyClearer(this IReflection reflection, IList<PropertyInfo> propertyInfos)
        {
            if (propertyInfos.Any(fk => !ReflectionUtility.IsNullableOrString(fk.PropertyType)))
            {
                throw new InvalidOperationException("Not all properties are nullable.");
            }

            var setters = reflection.Setters(propertyInfos);

            return toClear =>
            {
                foreach (var setter in setters)
                {
                    setter(toClear, null);
                }
            };
        }

        /// <summary>
        /// Creates a function setting the contents of the ICollection of the given property to the list of items.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="defaultCollectionType">The default collection type to use if a new collection needs to be created.</param>
        /// <returns>A function object setting the contents of the collection to the given list of items.</returns>
        public static Action<object, IEnumerable<object>> CollectionSetter(this IReflection reflection, PropertyInfo propertyInfo, Type defaultCollectionType) => reflection.CollectionSetter(propertyInfo, defaultCollectionType, true);

        /// <summary>
        /// Creates a function adding the list of items to the ICollection of the given property.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="defaultCollectionType">The default collection type to use if a new collection needs to be created.</param>
        /// <returns>A function object setting the contents of the collection to the given list of items.</returns>
        public static Action<object, IEnumerable<object>> CollectionAdder(this IReflection reflection, PropertyInfo propertyInfo, Type defaultCollectionType) => reflection.CollectionSetter(propertyInfo, defaultCollectionType, false);

        /// <summary>
        /// Creates a function setting the contents of the ICollection of the given property to the list of items.
        /// </summary>
        /// <param name="reflection">The IReflection instance.</param>
        /// <param name="propertyInfo">The property info.</param>
        /// <param name="defaultCollectionType">The default collection type to use if a new collection needs to be created.</param>
        /// <param name="clear">Determines if the collection should be cleared.</param>
        /// <returns>A function object setting the contents of the collection to the given list of items.</returns>
        public static Action<object, IEnumerable<object>> CollectionSetter(this IReflection reflection, PropertyInfo propertyInfo, Type defaultCollectionType, bool clear)
        {
            var genericArgument = propertyInfo.PropertyType.GenericTypeArguments[0];
            var interfaceType = typeof(ICollection<>).MakeGenericType(genericArgument);
            if (!interfaceType.IsAssignableFrom(propertyInfo.PropertyType))
            {
                throw new InvalidOperationException($@"Only collections based on ICollection<> are supported.");
            }

            var constructor = reflection.Constructor(defaultCollectionType.MakeGenericType(genericArgument));
            var adder = reflection.Caller(interfaceType, genericArgument, nameof(ICollection<int>.Add));
            var clearer = clear ? reflection.Caller(interfaceType, nameof(ICollection<int>.Clear)) : null;
            var setValue = ReflectionUtility.HasSetter(propertyInfo) ? reflection.Setter(propertyInfo) : null;
            var getValue = reflection.Getter(propertyInfo);

            return (o, list) =>
            {
                var propertyValue = getValue(o);
                if (propertyValue == null)
                {
                    if (setValue == null)
                    {
                        throw new InvalidOperationException($@"Unable to set property {propertyInfo.Name} because it has no setter."); ;
                    }

                    propertyValue = constructor();
                    setValue(o, propertyValue);
                }
                else
                {
                    clearer?.Invoke(propertyValue);
                }

                foreach (var item in list)
                {
                    adder(propertyValue, item);
                }
            };
        }
    }
}
