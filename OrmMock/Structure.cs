// Copyright(c) 2017 Johan Lindvall
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
    /// Defines structural information for the data generator.
    /// </summary>
    public class Structure
    {
        /// <summary>
        /// Gets the customized types.
        /// </summary>
        public Dictionary<Type, CreationOptions> TypeCustomization { get; } = new Dictionary<Type, CreationOptions>();

        /// <summary>
        /// Gets the customized properties.
        /// </summary>
        public Dictionary<PropertyInfo, CreationOptions> PropertyCustomization { get; } = new Dictionary<PropertyInfo, CreationOptions>();

        /// <summary>
        /// Gets the customized constructor types..
        /// </summary>
        public Dictionary<Type, CreationOptions> ConstructorCustomization { get; } = new Dictionary<Type, CreationOptions>();

        /// <summary>
        /// Holds the dictionary of navigation properties to include and the count of items to create.
        /// </summary>
        public Dictionary<PropertyInfo, int> Include { get; } = new Dictionary<PropertyInfo, int>();

        /// <summary>
        /// Holds the dictionary of singleton types.
        /// </summary>
        public HashSet<Type> Singletons { get; } = new HashSet<Type>();

        /// <summary>
        /// Holds the dictionary of custom property setters.
        /// </summary>
        public Dictionary<PropertyInfo, Func<ObjectContext, object>> CustomPropertySetters { get; } = new Dictionary<PropertyInfo, Func<ObjectContext, object>>();

        /// <summary>
        /// Holds the dictionary of custom constructors.
        /// </summary>
        public Dictionary<Type, Func<ObjectContext, string, object>> CustomConstructors { get; } = new Dictionary<Type, Func<ObjectContext, string, object>>();

        /// <summary>
        /// Holds the relations
        /// </summary>
        public Relations Relations { get; } = new Relations();
    }
}
