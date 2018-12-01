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

namespace OrmMock.DataGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    /// <summary>
    /// Defines customization information for the data generator.
    /// </summary>
    public class Customization
    {
        /// <summary>
        /// Holds the dictionary of type customizations.
        /// </summary>
        private readonly Dictionary<Type, TypeCustomization> typeCustomizations = new Dictionary<Type, TypeCustomization>();

        /// <summary>
        /// Holds the dictionary of property customizations.
        /// </summary>
        private readonly Dictionary<PropertyInfo, PropertyCustomization> propertyCustomizations = new Dictionary<PropertyInfo, PropertyCustomization>();

        /// <summary>
        /// Holds the ancestor of the customization instance.
        /// </summary>
        private readonly Customization ancestor;

        /// <summary>
        /// Initializes a new instance of the Customization class.
        /// </summary>
        public Customization() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the Customization class.
        /// </summary>
        /// <param name="ancestor">The customization ancestor.</param>
        public Customization(Customization ancestor)
        {
            this.ancestor = ancestor;
        }

        /// <summary>
        /// Returns a value indicating whether the given type should be skipped when encountered in the object graph.
        /// </summary>
        /// <param name="t">The type of the object.</param>
        /// <returns></returns>
        public bool ShouldSkip(Type t)
        {
            return this.Get(t)?.Skip ?? this.ancestor?.ShouldSkip(t) ?? false;
        }

        /// <summary>
        /// Skips the given type from inclusion in the object graph.
        /// </summary>
        /// <param name="t">The type of the object to skip.</param>
        public void Skip(Type t)
        {
            this.GetOrAdd(t).Skip = true;
        }

        /// <summary>
        /// Returns a value indicating whether the given property should be skipped when encountered in the object graph.
        /// </summary>
        /// <param name="pi"></param>
        /// <returns></returns>
        public bool ShouldSkip(PropertyInfo pi)
        {
            return this.GetOrAdd(pi)?.Skip ?? this.GetOrAdd(pi.PropertyType)?.Skip ?? this.ancestor?.ShouldSkip(pi) ?? false;
        }

        /// <summary>
        /// Skips the given property in the object graph.
        /// </summary>
        /// <param name="pi"></param>
        public void Skip(PropertyInfo pi)
        {
            this.GetOrAdd(pi).Skip = true;
        }

        /// <summary>
        /// Gets the include count for the given property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="includeCount">The default include count.</param>
        /// <returns>True if the include count was found, false otherwise.</returns>
        public int GetIncludeCount(PropertyInfo property, int includeCount)
        {
            return this.Get(property)?.IncludeCount ?? this.ancestor?.GetIncludeCount(property, includeCount) ?? includeCount;
        }

        /// <summary>
        /// Sets the include count for the given property.
        /// </summary>
        /// <param name="property">The property.</param>
        /// <param name="includeCount">The include count.</param>
        public void SetIncludeCount(PropertyInfo property, int includeCount)
        {
            this.GetOrAdd(property).IncludeCount = includeCount;
        }

        /// <summary>
        /// Gets the lookback count for the given type.
        /// </summary>
        /// <param name="type">The type of the property.</param>
        /// <param name="defaultLookback">The default lookback count.</param>
        /// <returns>The lookback count</returns>
        public int GetLookbackCount(Type type, int defaultLookback)
        {
            return this.Get(type)?.LookbackCount ?? this.ancestor?.Get(type)?.LookbackCount ?? defaultLookback;
        }

        public void SetLookbackCount(Type type, int lookbackCount)
        {
            this.GetOrAdd(type).LookbackCount = lookbackCount;
        }

        public int GetLookbackCount(PropertyInfo property, int defaultLookbackCount)
        {
            return this.Get(property)?.LookbackCount ?? this.Get(property.PropertyType)?.LookbackCount ?? this.ancestor?.GetLookbackCount(property, defaultLookbackCount) ?? defaultLookbackCount;
        }

        public void SetLookbackCount(PropertyInfo property, int lookbackCount)
        {
            this.GetOrAdd(property).LookbackCount = lookbackCount;
        }

        public void SetPropertySetter(PropertyInfo property, Func<DataGenerator, object> setter)
        {
            this.GetOrAdd(property).CustomValue = setter;
        }

        public Func<DataGenerator, object> GetPropertyConstructor(PropertyInfo pi)
        {
            var setter = this.Get(pi)?.CustomValue;

            if (setter == null)
            {
                var constructor = this.Get(pi.PropertyType).Constructor;

                if (constructor != null)
                {
                    setter = ctx => constructor(ctx, pi.Name);
                }
            }

            return setter ?? this.ancestor?.GetPropertyConstructor(pi);
        }

        public void SetCustomConstructor(Type type, Func<DataGenerator, string, object> constructor)
        {
            this.GetOrAdd(type).Constructor = constructor;
        }

        public Func<DataGenerator, string, object> GetCustomConstructor(Type type)
        {
            return this.Get(type)?.Constructor ?? this.ancestor?.GetCustomConstructor(type);
        }

        public void RegisterSingleton(Type t)
        {
            this.GetOrAdd(t).Singleton = true;
        }

        public bool ShouldBeSingleton(Type t)
        {
            return this.Get(t)?.Singleton ?? this.ancestor?.ShouldBeSingleton(t) ?? false;
        }

        private TypeCustomization Get(Type t)
        {
            this.typeCustomizations.TryGetValue(t, out var result);

            return result;
        }

        private TypeCustomization GetOrAdd(Type t)
        {
            if (!this.typeCustomizations.TryGetValue(t, out var result))
            {
                result = new TypeCustomization();
                this.typeCustomizations.Add(t, result);
            }

            return result;
        }

        private PropertyCustomization Get(PropertyInfo propertyInfo)
        {
            this.propertyCustomizations.TryGetValue(propertyInfo, out var result);

            return result;
        }

        private PropertyCustomization GetOrAdd(PropertyInfo propertyInfo)
        {
            if (!this.propertyCustomizations.TryGetValue(propertyInfo, out var result))
            {
                result = new PropertyCustomization();
                this.propertyCustomizations.Add(propertyInfo, result);
            }

            return result;
        }

        private class TypeCustomization
        {
            public bool? Singleton { get; set; }

            public bool? Skip { get; set; }

            public int? LookbackCount { get; set; }

            public Func<DataGenerator, string, object> Constructor { get; set; }
        }

        private class PropertyCustomization
        {
            public int? IncludeCount { get; set; }

            public bool? Skip { get; set; }

            public int? LookbackCount { get; set; }

            public Func<DataGenerator, object> CustomValue { get; set; }
        }
    }
}
