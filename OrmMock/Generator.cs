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
    using System.Linq.Expressions;

    /// <summary>
    /// Defines a class for generating object hierarchies.
    /// </summary>
    public class Generator
    {
        /// <summary>
        /// Holds the structure data.
        /// </summary>
        private readonly Structure structure = new Structure();

        /// <summary>
        /// Gets the relations.
        /// </summary>
        public Relations Relations => this.structure.Relations;

        /// <summary>
        /// Excludes a property from being set.
        /// </summary>
        /// <typeparam name="T">The type of the object where the property resides.</typeparam>
        /// <param name="e">The expression func for the object.</param>
        /// <returns>The generator.</returns>
        public Generator Without<T>(Expression<Func<T, object>> e)
            where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                this.structure.WithoutProperty.Add(property);
            }

            return this;
        }

        /// <summary>
        /// Excludes a type from being created.
        /// </summary>
        /// <typeparam name="T">The type of the object to exclude.</typeparam>
        /// <returns>The generator.</returns>
        public Generator Without<T>()
        {
            this.structure.WithoutType.Add(typeof(T));

            return this;
        }

        /// <summary>
        /// Excludes a property using ancestry when being set.
        /// </summary>
        /// <typeparam name="T">The type of the object where the property resides.</typeparam>
        /// <param name="e">The expression func for the object.</param>
        /// <returns>The generator.</returns>
        public Generator WithoutAncestry<T>(Expression<Func<T, object>> e)
            where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                this.structure.WithoutAncestryForProperty.Add(property);
            }

            return this;
        }

        /// <summary>
        /// Excludes ancestry from being used when creating constructor parameters for a type.
        /// </summary>
        /// <typeparam name="T">The type of the object to construct.</typeparam>
        /// <returns>The generator.</returns>
        public Generator WithoutAncestryForConstructor<T>()
        {
            this.structure.WithoutAncestryForConstructor.Add(typeof(T));

            return this;
        }

        /// <summary>
        /// Excludes ancestry from being used when setting properties on the type.
        /// </summary>
        /// <typeparam name="T">The type of the object for which to ignore ancestry.</typeparam>
        /// <returns>The generator.</returns>
        public Generator WithoutAncestry<T>()
        {
            this.structure.WithoutAncestryForType.Add(typeof(T));

            return this;
        }

        /// <summary>
        /// Includes a navigation property to be added to.
        /// </summary>
        /// <typeparam name="T">The type of the object where the navigation property resides.</typeparam>
        /// <param name="e">The expression func for the object</param>
        /// <param name="count">The number of items to create. Default is 3.</param>
        /// <returns>The generator.</returns>
        public Generator Include<T>(Expression<Func<T, object>> e, int count = 3)
            where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                if (property.PropertyType.GetGenericTypeDefinition() != typeof(ICollection<>))
                {
                    throw new ArgumentException("Must be ICollection<>");
                }

                this.structure.Include.Add(property, count);
            }

            return this;
        }

        /// <summary>
        /// Sets a property to a specific value.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="T2">The property value.</typeparam>
        /// <param name="e">The property expression.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generator.</returns>
        public Generator With<T, T2>(Expression<Func<T, T2>> e, T2 value)
            where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                this.structure.CustomPropertySetters.Add(property, _ => value);
            }

            return this;
        }

        /// <summary>
        /// Sets a property to a specific value.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="T2">The property value.</typeparam>
        /// <param name="e">The property expression.</param>
        /// <param name="value">The value generator.</param>
        /// <returns>The generator.</returns>
        public Generator With<T, T2>(Expression<Func<T, T2>> e, Func<ObjectContext, T2> value)
            where T : class
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                this.structure.CustomPropertySetters.Add(property, ctx => value(ctx));
            }

            return this;
        }

        /// <summary>
        /// Sets a type to a specific value
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The generator.</returns>
        public Generator With<T>(Func<T> creator)
        {
            this.structure.CustomConstructors.Add(typeof(T), (_, __) => creator());

            return this;
        }

        /// <summary>
        /// Sets a type to a specific value
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The generator.</returns>
        public Generator With<T>(Func<string, T> creator)
        {
            this.structure.CustomConstructors.Add(typeof(T), (_, s) => creator(s));

            return this;
        }

        /// <summary>
        /// Registers a specific object type to be a singleton.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The generator.</returns>
        public Generator Singleton<T>()
        {
            this.structure.Singletons.Add(typeof(T));

            return this;
        }

        /// <summary>
        /// Creates an object context from the static structure.
        /// </summary>
        /// <returns>An object context.</returns>
        public ObjectContext CreateContext()
        {
            return new ObjectContext(this.structure);
        }
    }
}
