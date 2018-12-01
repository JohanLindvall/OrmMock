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

using OrmMock.Shared;

namespace OrmMock.DataGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Implements a for-type context, where type-specific information is set and used.
    /// </summary>
    /// <typeparam name="T">The type of the of the object to configure or create.</typeparam>
    public class ForTypeContext<T>
    {
        /// <summary>
        /// Holds the object context.
        /// </summary>
        private readonly DataGenerator objectContext;

        /// <summary>
        /// Holds the customization data.
        /// </summary>
        private readonly Customization customization;

        /// <summary>
        /// Initializes a new instance of the ForTypeContext class.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="customization">The customization data.</param>
        public ForTypeContext(DataGenerator objectContext, Customization customization)
        {
            this.objectContext = objectContext;
            this.customization = customization;
        }

        /// <summary>
        /// Sets the type to be a singleton.
        /// </summary>
        /// <param name="singleton">The singleton value.</param>
        /// <returns>The typed context.</returns>
        public ForTypeContext<T> With(T singleton)
        {
            this.objectContext.Singleton(singleton);

            return this;
        }

        /// <summary>
        /// Sets a type to a specific value
        /// </summary>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The typed context.</returns>
        public ForTypeContext<T> With(Func<T> creator)
        {
            this.With(_ => creator());

            return this;
        }

        /// <summary>
        /// Sets a type to a specific value
        /// </summary>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The typed context.</returns>
        public ForTypeContext<T> With(Func<string, T> creator)
        {
            this.customization.SetLookbackCount(typeof(T), 0);
            this.customization.SetCustomConstructor(typeof(T), (_, s) => creator(s));

            return this;
        }

        /// <summary>
        /// Registers the given type as a singleton.
        /// </summary>
        /// <returns>The typed context.</returns>
        public ForTypeContext<T> RegisterSingleton()
        {
            this.customization.RegisterSingleton(typeof(T));

            return this;
        }

        /// <summary>
        /// Includes a navigation property to be added to.
        /// </summary>
        /// <param name="e">The expression to include.</param>
        /// <param name="count">The number of items to create. Default is 3.</param>
        /// <returns>The typed context.</returns>
        public ForTypeContext<T> Include(Expression<Func<T, object>> e, int count = 3)
        {
            return this.ForEach(e, pi => { HandleInclude(count, pi); });
        }

        /// <summary>
        /// Includes a navigation property to be added to.
        /// </summary>
        /// <param name="properties">The properties to include.</param>
        /// <param name="count">The number of items to create. Default is 3.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> Include(IList<PropertyInfo> properties, int count = 3)
        {
            return this.ForEach(properties, pi => { HandleInclude(count, pi); });
        }

        /// <summary>
        /// Sets a property to a specific value.
        /// </summary>
        /// <param name="e">The property expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> With<T2>(Expression<Func<T, T2>> e, T2 value)
        {
            return this.With(e, _ => value);
        }

        /// <summary>
        /// Sets a property to a specific value generator.
        /// </summary>
        /// <param name="e">The property expression.</param>
        /// <param name="value">The value generator.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> With<T2>(Expression<Func<T, T2>> e, Func<DataGenerator, T2> value)
        {
            return this.ForEach(e, pi =>
            {
                this.customization.SetLookbackCount(pi, 0);
                this.customization.SetPropertySetter(pi, ctx => value(ctx));
            });
        }

        /// <summary>
        /// Excludes the current type from being set. The default value will be used.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> Without()
        {
            this.customization.Skip(typeof(T));

            return this;
        }

        /// <summary>
        /// Excludes a property from being set. The default value will be used.
        /// </summary>
        /// <param name="e">The property expression.</param>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> Without(Expression<Func<T, object>> e) => this.ForEach(e, this.customization.Skip);

        /// <summary>
        /// Excludes a property from being set. The default value will be used.
        /// </summary>
        /// <param name="properties">The properties to exclude.</param>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> Without(IList<PropertyInfo> properties) => this.ForEach(properties, this.customization.Skip);

        /// <summary>
        /// Excludes a property from being set. The default value will be used.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> SetLookback(Expression<Func<T, object>> e, int lookback) => this.ForEach(e, pi => this.customization.SetLookbackCount(pi, lookback));

        /// <summary>
        /// Excludes a property from being set. The default value will be used.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> SetLookback(IList<PropertyInfo> properties, int lookback) => this.ForEach(properties, pi => this.customization.SetLookbackCount(pi, lookback));

        /// <summary>
        /// Sets the look-back count for the given type.
        /// </summary>
        /// <param name="count">The number of look-back levels.</param>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> SetLookback(int count)
        {
            this.customization.SetLookbackCount(typeof(T), count);
            return this;
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <returns>The created object.</returns>
        public T Create()
        {
            return this.objectContext.Create<T>();
        }

        /// <summary>
        /// Creates many objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created objects.</returns>
        public IEnumerable<T> CreateMany(int create = 3)
        {
            while (create-- > 0)
            {
                yield return this.Create();
            }
        }

        /// <summary>
        /// Gets the for context for the given type.
        /// </summary>
        /// <typeparam name="T2">The type of the for context.</typeparam>
        /// <returns>A typed for context.</returns>
        public ForTypeContext<T2> For<T2>()
        {
            return new ForTypeContext<T2>(this.objectContext, this.customization);
        }

        /// <summary>
        /// Performs the action for the properties in the expression.
        /// </summary>
        /// <typeparam name="T2">The expression property type</typeparam>
        /// <param name="e">The list of expressions.</param>
        /// <param name="action">The action to apply.</param>
        /// <returns>The current context.</returns>
        private ForTypeContext<T> ForEach<T2>(Expression<Func<T, T2>> e, Action<PropertyInfo> action) => this.ForEach(ExpressionUtility.GetPropertyInfo(e), action);

        /// <summary>
        /// Performs the action for the given list of properties.
        /// </summary>
        /// <param name="properties">The list of properties.</param>
        /// <param name="action">The action to apply.</param>
        /// <returns>The current context.</returns>
        private ForTypeContext<T> ForEach(IList<PropertyInfo> properties, Action<PropertyInfo> action)
        {
            foreach (var property in properties)
            {
                action(property);
            }

            return this;
        }

        /// <summary>
        /// Set the include count for the given property.
        /// </summary>
        /// <param name="count">The include count.</param>
        /// <param name="property">The property for which to set the include count.</param>
        private void HandleInclude(int count, PropertyInfo property)
        {
            if (property.PropertyType.GetGenericTypeDefinition() != typeof(ICollection<>))
            {
                throw new ArgumentException("Must be ICollection<>");
            }

            this.customization.SetIncludeCount(property, count);
        }
    }
}
