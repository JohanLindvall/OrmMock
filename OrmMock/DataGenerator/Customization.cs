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
        /// Register a post-create action to be performed once an object of the given type has been created.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="action">The post-create action.</param>
        public void PostCreate<T>(Action<T> action)
        {
            this.GetOrAdd(typeof(T)).PostCreate = o => action((T)o);
        }

        /// <summary>
        /// Register a post-create action to be performed once an object of the given type has been created.
        /// </summary>
        /// <param name="pi">The property info.</param>
        /// <param name="action">The post-create action.</param>
        public void PostCreate<T>(PropertyInfo pi, Action<T> action)
        {
            this.GetOrAdd(pi).PostCreate = o => action((T)o);
        }


        /// <summary>
        /// Gets the look-back count for the given property.
        /// </summary>
        /// <param name="property">The property to get the look-back count for. Falls back tot the look-back count for the type if nothing is set for the property.</param>
        /// <param name="defaultLookBackCount">The default look-back count.</param>
        /// <returns>The look-back count</returns>
        public int GetLookBackCount(PropertyInfo property, int defaultLookBackCount)
        {
            return this.Get(property)?.LookBackCount ?? this.Get(property.PropertyType)?.LookBackCount ?? this.ancestor?.GetLookBackCount(property, defaultLookBackCount) ?? defaultLookBackCount;
        }

        /// <summary>
        /// Sets the look-back count for the given type.
        /// </summary>
        /// <param name="type">The type for which to set the look-back count.</param>
        /// <param name="lookBackCount">The look-back count to set.</param>
        public void SetLookBackCount(Type type, int lookBackCount)
        {
            this.GetOrAdd(type).LookBackCount = lookBackCount;
        }

        /// <summary>
        /// Sets the look-back count for the given property.
        /// </summary>
        /// <param name="property">The property for which to set the look-back count.</param>
        /// <param name="lookBackCount">The look-back count to set.</param>
        public void SetLookBackCount(PropertyInfo property, int lookBackCount)
        {
            this.GetOrAdd(property).LookBackCount = lookBackCount;
        }

        public void SetPropertySetter(PropertyInfo property, Func<DataGenerator, object> customValueFactory)
        {
            this.GetOrAdd(property).CustomValue = customValueFactory;
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

        public Action<object> GetPostCreateAction(Type type)
        {
            return this.Get(type)?.PostCreate ?? this.ancestor?.GetPostCreateAction(type);
        }

        public Action<object> GetPostCreateAction(PropertyInfo pi)
        {
            return this.Get(pi)?.PostCreate ?? this.ancestor?.GetPostCreateAction(pi);
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

            public int? LookBackCount { get; set; }

            public Func<DataGenerator, string, object> Constructor { get; set; }

            public Action<object> PostCreate { get; set; }
        }

        private class PropertyCustomization
        {
            public int? IncludeCount { get; set; }

            public bool? Skip { get; set; }

            public int? LookBackCount { get; set; }

            public Func<DataGenerator, object> CustomValue { get; set; }

            public Action<object> PostCreate { get; set; }
        }
    }
}
