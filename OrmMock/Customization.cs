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
        /// Holds the set of types to skip (i.e. not include when creating objects).
        /// </summary>
        private readonly HashSet<Type> typeSkipDict = new HashSet<Type>();

        /// <summary>
        /// Holds the set of properties to skip (not include when creating objects).
        /// </summary>
        private readonly HashSet<PropertyInfo> propertySkipDict = new HashSet<PropertyInfo>();

        /// <summary>
        /// Gets the lookback dictionary for types.
        /// </summary>
        private readonly Dictionary<Type, int> typeLookbackDictionary = new Dictionary<Type, int>();

        /// <summary>
        /// Gets the lookback dictionary for properties.
        /// </summary>
        private readonly Dictionary<PropertyInfo, int> propertyLookbackDictionary = new Dictionary<PropertyInfo, int>();

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

        public bool ShouldSkip(Type t)
        {
            return this.typeSkipDict.Contains(t) || (this.ancestor?.ShouldSkip(t) ?? false);
        }

        public void Skip(Type t)
        {
            this.typeSkipDict.Add(t);
        }

        public bool ShouldSkip(PropertyInfo pi)
        {
            return this.typeSkipDict.Contains(pi.PropertyType) || this.propertySkipDict.Contains(pi) || (this.ancestor?.ShouldSkip(pi) ?? false);
        }

        public void Skip(PropertyInfo pi)
        {
            this.propertySkipDict.Add(pi);
        }

        public bool TryGetIncludeCount(PropertyInfo property, out int includeCount)
        {
            return this.includeCountDict.TryGetValue(property, out includeCount) || this.ancestor != null && this.ancestor.TryGetIncludeCount(property, out includeCount);
        }

        public void SetIncludeCount(PropertyInfo property, int includeCount)
        {
            this.includeCountDict[property] = includeCount;
        }

        public bool TryGetLookbackCount(Type type, out int lookbackCount)
        {
            return this.typeLookbackDictionary.TryGetValue(type, out lookbackCount) || this.ancestor != null && this.ancestor.TryGetLookbackCount(type, out lookbackCount);
        }

        public void SetLookbackCount(Type type, int lookbackCount)
        {
            this.typeLookbackDictionary[type] = lookbackCount;
        }

        public bool TryGetLookbackCount(PropertyInfo property, out int lookbackCount)
        {
            return this.propertyLookbackDictionary.TryGetValue(property, out lookbackCount) || this.typeLookbackDictionary.TryGetValue(property.PropertyType, out lookbackCount) || (this.ancestor?.TryGetLookbackCount(property, out lookbackCount) ?? false);
        }

        public void SetLookbackCount(PropertyInfo property, int lookbackCount)
        {
            this.propertyLookbackDictionary[property] = lookbackCount;
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
