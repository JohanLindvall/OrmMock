﻿// Copyright(c) 2017, 2018 Johan Lindvall
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
    using System.Linq.Expressions;

    /// <summary>
    /// Implements a for-type context, where type-specific information is set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ForTypeContext<T> : ICreationsOptions<T>
    {
        /// <summary>
        /// Holds the object context.
        /// </summary>
        private readonly ObjectContext objectContext;

        /// <summary>
        /// Holds the structure.
        /// </summary>
        private readonly Structure structure;

        /// <summary>
        /// Holds the relations.
        /// </summary>
        private readonly Relations relations;

        /// <summary>
        /// Initializes a new instance of the ForTypeContext class.
        /// </summary>
        /// <param name="objectContext">The object context.</param>
        /// <param name="structure">The structure.</param>
        /// <param name="relations">The relations.</param>
        public ForTypeContext(ObjectContext objectContext, Structure structure, Relations relations)
        {
            this.objectContext = objectContext;
            this.structure = structure;
            this.relations = relations;
        }

        /// <summary>
        /// Sets the type 
        /// </summary>
        /// <param name="singleton"></param>
        public void Use(T singleton)
        {
            this.objectContext.Singleton(singleton);
        }

        /// <summary>
        /// Sets a type to a specific value
        /// </summary>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The generator.</returns>
        public void Use(Func<T> creator)
        {
            this.structure.TypeCustomization[typeof(T)] = CreationOptions.IgnoreInheritance;
            this.structure.CustomConstructors.Add(typeof(T), (_, __) => creator());
        }

        /// <summary>
        /// Sets a type to a specific value
        /// </summary>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The generator.</returns>
        public void Use(Func<string, T> creator)
        {
            this.structure.TypeCustomization[typeof(T)] = CreationOptions.IgnoreInheritance;
            this.structure.CustomConstructors.Add(typeof(T), (_, s) => creator(s));
        }

        /// <summary>
        /// Registers the given type as a singleton.
        /// </summary>
        /// <returns></returns>
        public ForTypeContext<T> RegisterSingleton()
        {
            this.structure.Singletons.Add(typeof(T));

            return this;
        }

        /// <summary>
        /// Includes a navigation property to be added to.
        /// </summary>
        /// <param name="e">The expression to include.</param>
        /// <param name="count">The number of items to create. Default is 3.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> Include(Expression<Func<T, object>> e, int count = 3)
        {
            return this.ForEach(e, pi =>
            {
                if (pi.PropertyType.GetGenericTypeDefinition() != typeof(ICollection<>))
                {
                    throw new ArgumentException("Must be ICollection<>");
                }

                this.structure.Include.Add(pi, count);
            });
        }

        /// <summary>
        /// Sets a property to a specific value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> Use<T2>(Expression<Func<T, object>> e, T2 value)
        {
            return this.ForEach(e, pi =>
            {
                this.structure.PropertyCustomization[pi] = CreationOptions.IgnoreInheritance;
                this.structure.CustomPropertySetters.Add(pi, _ => value);
            });
        }

        /// <summary>
        /// Sets a property to a specific value.
        /// </summary>
        /// <param name="value">The value generator.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> Use<T2>(Expression<Func<T, object>> e, Func<ObjectContext, T2> value)
        {
            return this.ForEach(e, pi =>
            {
                this.structure.PropertyCustomization[pi] = CreationOptions.IgnoreInheritance;
                this.structure.CustomPropertySetters.Add(pi, ctx => value(ctx));
            });
        }

        /// <summary>
        /// Excludes a property from being set. The default value will be used.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> Skip(Expression<Func<T, object>> e) => this.SetCreationOptions(e, CreationOptions.Skip);

        /// <summary>
        /// Parents are ignored for this property.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> IgnoreParents(Expression<Func<T, object>> e) => this.SetCreationOptions(e, CreationOptions.IgnoreInheritance);

        /// <summary>
        /// Only direct parents are used for this parameter. A new object is not created.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> OnlyDirectParent(Expression<Func<T, object>> e) => this.SetCreationOptions(e, CreationOptions.OnlyDirectInheritance);

        /// <summary>
        /// Only parents are used for this parameter. A new object is not created.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> OnlyParents(Expression<Func<T, object>> e) => this.SetCreationOptions(e, CreationOptions.OnlyInheritance);

        private ForTypeContext<T> SetCreationOptions(Expression<Func<T, object>> e, CreationOptions options) => this.ForEach(e, pi => this.structure.PropertyCustomization[pi] = options);

        public ForTypeContext<T> Has11Relation<T3, T2>(Expression<Func<T, T2>> e1, Expression<Func<T3, T2>> e2)
            where T3 : class
        {
            this.relations.RegisterForeignKeyProperties<T, T3>(ExpressionUtility.GetPropertyInfo(e1));
            this.relations.RegisterForeignKeyProperties<T3, T>(ExpressionUtility.GetPropertyInfo(e2));
            return this;
        }

        public ForTypeContext<T> IgnoreParents() => this.SetCreationOptions(CreationOptions.IgnoreInheritance);

        public ForTypeContext<T> OnlyDirectParent() => this.SetCreationOptions(CreationOptions.OnlyDirectInheritance);

        public ForTypeContext<T> OnlyParents() => this.SetCreationOptions(CreationOptions.OnlyInheritance);

        public ForTypeContext<T> Skip() => this.SetCreationOptions(CreationOptions.Skip);

        private ForTypeContext<T> SetCreationOptions(CreationOptions options)
        {
            this.structure.TypeCustomization[typeof(T)] = options;

            return this;
        }

        private ForTypeContext<T> ForEach<T2>(Expression<Func<T, T2>> e, Action<PropertyInfo> pi)
        {
            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                pi(property);
            }

            return this;
        }
    }
}
