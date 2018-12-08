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
    using System.Linq.Expressions;
    using System.Reflection;


    /// <summary>
    /// Defines the relations customization interface.
    /// </summary>
    public interface IRelationsCustomization
    {
        /// <summary>
        /// Gets or set the function used to get the default primary key.
        /// </summary>
        Func<Type, PropertyInfo[]> DefaultPrimaryKey { get; set; }

        /// <summary>
        /// Gets or set the function used to get the default foreign key.
        /// </summary>
        Func<Type, Type, PropertyInfo[]> DefaultForeignKey { get; set; }

        /// <summary>
        /// Registers an expression defining the primary keys of an object.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="expression">The expression defining the primary keys.</param>
        IRelationsCustomization RegisterPrimaryKey<T>(Expression<Func<T, object>> expression) where T : class;

        /// <summary>
        /// Registers an expression defining the foreign keys of an object.
        /// </summary>
        /// <typeparam name="TThis">The type of the object.</typeparam>
        /// <typeparam name="TForeign">The type of the foreign object.</typeparam>
        /// <typeparam name="TKey">The type of the key property.</typeparam>
        /// <param name="expression">The expression defining the foreign keys.</param>
        IRelationsCustomization RegisterForeignKeys<TThis, TForeign, TKey>(Expression<Func<TThis, TKey>> expression);

        /// <summary>
        /// Registers an expression defining the foreign keys of an object.
        /// </summary>
        /// <typeparam name="TThis">The type of the object.</typeparam>
        /// <typeparam name="TForeign">The type of the foreign object.</typeparam>
        /// <param name="keys">The foreign keys.</param>
        IRelationsCustomization RegisterForeignKeys<TThis, TForeign>(PropertyInfo[] keys);

        /// <summary>
        /// Registers an empty set of foreign keys for the given relation.
        /// </summary>
        /// <typeparam name="TThis">The type of the object.</typeparam>
        /// <typeparam name="TForeign">The type of the foreign object.</typeparam>
        IRelationsCustomization RegisterNullForeignKeys<TThis, TForeign>();

        /// <summary>
        /// Registers a 1:1 relation between TThis and TForeign.
        /// </summary>
        /// <typeparam name="TThis">The first object type.</typeparam>
        /// <typeparam name="TForeign">The second object type.</typeparam>
        /// <param name="expression1">The expression defining the foreign keys of the first object.</param>
        /// <param name="expression2">The expression defining the foreign keys of the second object.</param>
        IRelationsCustomization Register11Relation<TThis, TForeign>(Expression<Func<TThis, object>> expression1, Expression<Func<TForeign, object>> expression2);
    }
}
