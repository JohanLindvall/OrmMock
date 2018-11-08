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
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class ForPropertyContext<T, T2> : ICreationsOptions<T>
    {
        private readonly ForTypeContext<T> typeContext;

        private readonly Structure structure;

        private readonly Relations relations;

        private readonly IList<PropertyInfo> properties;

        public ForPropertyContext(ForTypeContext<T> typeContext, Structure structure, Relations relations, IList<PropertyInfo> properties)
        {
            this.typeContext = typeContext;
            this.structure = structure;
            this.relations = relations;
            this.properties = properties;
        }

        /// <summary>
        /// Excludes a property from being set. The default value will be used.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> Skip() => this.SetCreationOptions(CreationOptions.Skip);

        /// <summary>
        /// Parents are ignored for this property.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> IgnoreParents() => this.SetCreationOptions(CreationOptions.IgnoreInheritance);

        /// <summary>
        /// Only direct parents are used for this parameter. A new object is not created.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> OnlyDirectParent() => this.SetCreationOptions(CreationOptions.OnlyDirectInheritance);

        /// <summary>
        /// Only parents are used for this parameter. A new object is not created.
        /// </summary>
        /// <returns>The type context.</returns>
        public ForTypeContext<T> OnlyParents() => this.SetCreationOptions(CreationOptions.OnlyInheritance);

        /// <summary>
        /// Includes a navigation property to be added to.
        /// </summary>
        /// <param name="count">The number of items to create. Default is 3.</param>
        /// <returns>The generator.</returns>
        public ForTypeContext<T> Include(int count = 3)
        {
            return this.ForEach(pi =>
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
        public ForTypeContext<T> Use(T2 value)
        {
            return this.ForEach(pi =>
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
        public ForTypeContext<T> Use(Func<ObjectContext, T2> value)
        {
            return this.ForEach(pi =>
            {
                this.structure.PropertyCustomization[pi] = CreationOptions.IgnoreInheritance;
                this.structure.CustomPropertySetters.Add(pi, ctx => value(ctx));
            });
        }

        public ForTypeContext<T> Has11Relation<T3>(Expression<Func<T3, T2>> e)
        where T3 : class
        {
            this.relations.RegisterForeignKeyProperties<T, T3>(this.properties.ToArray());
            this.relations.RegisterForeignKeys<T3, T, T2>(e);
            return this.typeContext;
        }

        private ForTypeContext<T> SetCreationOptions(CreationOptions options) => this.ForEach(pi => this.structure.PropertyCustomization[pi] = options);

        private ForTypeContext<T> ForEach(Action<PropertyInfo> pi)
        {
            foreach (var property in this.properties)
            {
                pi(property);
            }

            return this.typeContext;
        }
    }
}
