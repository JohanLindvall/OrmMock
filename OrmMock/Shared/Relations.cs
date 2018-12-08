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
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Defines relations for objects, including primary keys and foreign keys.
    /// </summary>
    public class Relations : IRelations, IRelationsCustomization
    {
        /// <summary>
        /// Holds the dictionary of types to key properties
        /// </summary>
        private readonly Dictionary<Type, PropertyInfo[]> primaryKeys = new Dictionary<Type, PropertyInfo[]>();

        /// <summary>
        /// Holds the dictionary of type, type to foreign key properties
        /// </summary>
        private readonly Dictionary<Tuple<Type, Type>, PropertyInfo[]> foreignKeys = new Dictionary<Tuple<Type, Type>, PropertyInfo[]>();

        public Relations()
        {
            this.DefaultPrimaryKey = type =>
            {
                var property = type.GetProperty("Id");
                return property == null ? null : new[] { property };
            };

            this.DefaultForeignKey = (thisType, foreignType) =>
            {
                var relationProperty = thisType.GetProperties().SingleOrDefault(p => p.PropertyType == foreignType);
                var property = relationProperty == null ? null : thisType.GetProperty(relationProperty.Name + "Id");
                return property == null ? null : new[] { property };
            };
        }

        /// <summary>
        /// Gets or set the function used to get the default primary key.
        /// </summary>
        public Func<Type, PropertyInfo[]> DefaultPrimaryKey { get; set; }

        /// <summary>
        /// Gets or set the function used to get the default foreign key.
        /// </summary>
        public Func<Type, Type, PropertyInfo[]> DefaultForeignKey { get; set; }

        /// <inheritdoc />

        public IRelationsCustomization RegisterPrimaryKey<T>(Expression<Func<T, object>> expression)
            where T : class
        {
            this.primaryKeys.Add(typeof(T), ExpressionUtility.GetPropertyInfo(expression));

            return this;
        }

        /// <inheritdoc />

        public IRelationsCustomization RegisterForeignKeys<TThis, TForeign, TKey>(Expression<Func<TThis, TKey>> expression)
        {
            return this.RegisterForeignKeys<TThis, TForeign>(ExpressionUtility.GetPropertyInfo(expression));
        }

        /// <inheritdoc />
        public IRelationsCustomization RegisterForeignKeys<TThis, TForeign>(PropertyInfo[] keys)
        {
            this.ValidateForeignKeys(typeof(TThis), typeof(TForeign), keys);

            this.foreignKeys.Add(new Tuple<Type, Type>(typeof(TThis), typeof(TForeign)), keys);

            return this;
        }

        /// <inheritdoc />
        public IRelationsCustomization RegisterNullForeignKeys<TThis, TForeign>()
        {
            // Bypass validation of foreign keys and primary keys
            this.foreignKeys.Add(new Tuple<Type, Type>(typeof(TThis), typeof(TForeign)), new PropertyInfo[0]);

            return this;
        }

        /// <inheritdoc />
        public IRelationsCustomization Register11Relation<TThis, TForeign>(Expression<Func<TThis, object>> expression1, Expression<Func<TForeign, object>> expression2)
        {
            this.RegisterForeignKeys<TThis, TForeign, object>(expression1);
            this.RegisterForeignKeys<TForeign, TThis, object>(expression2);

            return this;
        }

        /// <inheritdoc />
        public PropertyInfo[] GetPrimaryKeys(Type t)
        {
            if (!this.primaryKeys.TryGetValue(t, out var result))
            {
                result = this.DefaultPrimaryKey(t);

                if (result == null)
                {
                    throw new InvalidOperationException($@"Unable to determine key for type '{t.Name}'.");
                }

                this.primaryKeys.Add(t, result);
            }

            return result;
        }

        /// <inheritdoc />
        public PropertyInfo[] GetForeignKeys(Type tThis, Type tForeign)
        {
            var key = new Tuple<Type, Type>(tThis, tForeign);

            if (!this.foreignKeys.TryGetValue(key, out var result))
            {
                result = this.DefaultForeignKey(tThis, tForeign);

                if (result == null)
                {
                    throw new InvalidOperationException($@"Unable to determine foreign keys from '{tThis.Name}' to '{tForeign.Name}'.");
                }

                this.ValidateForeignKeys(tThis, tForeign, result);

                this.foreignKeys.Add(key, result);
            }

            return result;
        }

        /// <summary>
        /// Validates that the type of the foreign keys match the primary keys of the foreign object.
        /// </summary>
        /// <param name="tThis">The type of the object.</param>
        /// <param name="tForeign">The type of the foreign object.</param>
        /// <param name="foreignKeyProperties">The foreign keys</param>

        private void ValidateForeignKeys(Type tThis, Type tForeign, PropertyInfo[] foreignKeyProperties)
        {
            var primaryKeyTypes = this.GetPrimaryKeys(tForeign).Select(p => p.PropertyType);

            var foreignKeyTypes = foreignKeyProperties.Select(p => p.PropertyType).ToArray();

            if (foreignKeyTypes.Count(fk => Nullable.GetUnderlyingType(fk) != null) == foreignKeyTypes.Length)
            {
                // All nullable
                foreignKeyTypes = foreignKeyTypes.Select(Nullable.GetUnderlyingType).ToArray();
            }

            if (!primaryKeyTypes.SequenceEqual(foreignKeyTypes))
            {
                throw new InvalidOperationException($"Primary keys and foreign keys for '{tForeign}' in '{tThis}' do not match.");
            }
        }
    }
}
