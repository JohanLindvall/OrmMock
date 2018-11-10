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

namespace OrmMock
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
        /// Gets the customized types.
        /// </summary>
        private readonly Dictionary<Type, CreationOptions> typeCustomizationDict = new Dictionary<Type, CreationOptions>();

        /// <summary>
        /// Gets the customized properties.
        /// </summary>
        private readonly Dictionary<PropertyInfo, CreationOptions> propertyCustomizationDict = new Dictionary<PropertyInfo, CreationOptions>();

        /// <summary>
        /// Holds the dictionary of navigation properties to include and the count of items to create.
        /// </summary>
        private readonly Dictionary<PropertyInfo, int> includeCountDict = new Dictionary<PropertyInfo, int>();

        /// <summary>
        /// Holds the dictionary of singleton types.
        /// </summary>
        private readonly HashSet<Type> singletons = new HashSet<Type>();

        /// <summary>
        /// Holds the dictionary of custom property setters.
        /// </summary>
        private readonly Dictionary<PropertyInfo, Func<ObjectContext, object>> customPropertySetters = new Dictionary<PropertyInfo, Func<ObjectContext, object>>();

        /// <summary>
        /// Holds the dictionary of custom constructors.
        /// </summary>
        private readonly Dictionary<Type, Func<ObjectContext, string, object>> customConstructors = new Dictionary<Type, Func<ObjectContext, string, object>>();

        private readonly Customization ancestor;

        public Customization() : this(null)
        {

        }

        public Customization(Customization ancestor)
        {
            this.ancestor = ancestor;
        }

        private bool TryGetPropertyCustomization(PropertyInfo property, out CreationOptions creationOptions)
        {
            return this.propertyCustomizationDict.TryGetValue(property, out creationOptions) || this.ancestor != null && !this.ancestor.TryGetPropertyCustomization(property, out creationOptions);

        }

        private bool TryGetTypeCustomization(Type type, out CreationOptions creationOptions)
        {
            return this.typeCustomizationDict.TryGetValue(type, out creationOptions) || this.ancestor != null && !this.ancestor.TryGetTypeCustomization(type, out creationOptions);

        }

        public CreationOptions GetEffectiveCreationsOptions(PropertyInfo property)
        {
            if (!this.TryGetPropertyCustomization(property, out var effectiveCreationOptions))
            {
                if (!this.TryGetTypeCustomization(property.PropertyType, out effectiveCreationOptions))
                {
                    effectiveCreationOptions = CreationOptions.Default;
                }
            }

            return effectiveCreationOptions;
        }

        public bool TryGetIncludeCount(PropertyInfo property, out int includeCount)
        {
            return this.includeCountDict.TryGetValue(property, out includeCount) || this.ancestor != null && this.ancestor.TryGetIncludeCount(property, out includeCount);
        }

        public void SetIncludeCount(PropertyInfo property, int includeCount)
        {
            this.includeCountDict[property] = includeCount;
        }

        public void SetPropertyCustomization(PropertyInfo property, CreationOptions options)
        {
            this.propertyCustomizationDict[property] = options;
        }

        public void SetTypeCustomization(Type type, CreationOptions options)
        {
            this.typeCustomizationDict[type] = options;
        }

        public void SetPropertySetter(PropertyInfo property, Func<ObjectContext, object> setter)
        {
            this.customPropertySetters[property] = setter;
        }

        public bool TryGetPropertySetter(PropertyInfo pi, out Func<ObjectContext, object> setter)
        {
            return this.customPropertySetters.TryGetValue(pi, out setter) || this.ancestor != null && this.ancestor.TryGetPropertySetter(pi, out setter);
        }

        public void SetCustomConstructor(Type type, Func<ObjectContext, string, object> constructor)
        {
            this.customConstructors[type] = constructor;
        }

        public bool TryGetCustomConstructor(Type type, out Func<ObjectContext, string, object> constructor)
        {
            return this.customConstructors.TryGetValue(type, out constructor) || this.ancestor != null && this.ancestor.TryGetCustomConstructor(type, out constructor);
        }

        public void RegisterSingleton(Type t)
        {
            this.singletons.Add(t);
        }

        public bool ShouldBeSingleton(Type t)
        {
            return this.singletons.Contains(t) || this.ancestor != null && this.ancestor.ShouldBeSingleton(t);
        }
    }
}
