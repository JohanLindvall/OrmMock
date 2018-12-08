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
    using System.Reflection;

    public class PropertyAccessor
    {
        private readonly IRelations relations;

        private readonly Dictionary<Type, PropertyInfo[]> propertyCache = new Dictionary<Type, PropertyInfo[]>();

        private readonly Dictionary<PropertyInfo, Func<object, object>> getterCache = new Dictionary<PropertyInfo, Func<object, object>>();

        private readonly Dictionary<PropertyInfo, Action<object, object>> setterCache = new Dictionary<PropertyInfo, Action<object, object>>();

        private readonly Dictionary<Type, Func<object, Keys>> keyGetterCache = new Dictionary<Type, Func<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Func<object, Keys>> foreignKeyGetterCache = new Dictionary<Tuple<Type, Type>, Func<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object, Keys>> foreignKeySetterCache = new Dictionary<Tuple<Type, Type>, Action<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object>> foreignKeyClearerCache = new Dictionary<Tuple<Type, Type>, Action<object>>();

        private readonly Dictionary<PropertyInfo, Action<object, IList<object>>> collectionSetterCache = new Dictionary<PropertyInfo, Action<object, IList<object>>>();

        private readonly Dictionary<PropertyInfo, Action<object, IList<object>>> collectionAdderCache = new Dictionary<PropertyInfo, Action<object, IList<object>>>();

        private readonly IReflection reflection;

        public PropertyAccessor(IRelations relations)
        {
            this.relations = relations;
            this.reflection = new FasterflectReflection();
        }

        private static TResult GetOrCreate<TKey, TResult>(Dictionary<TKey, TResult> dictionary, TKey key, Func<TResult> factory)
        {
            if (!dictionary.TryGetValue(key, out var action))
            {
                action = factory();
                dictionary.Add(key, action);
            }

            return action;
        }

        public void SetCollection(PropertyInfo property, object o, IList<object> contents)
        {
            GetOrCreate(this.collectionSetterCache, property, () => this.reflection.CollectionSetter(property))(o, contents);
        }

        public void AddToCollection(PropertyInfo property, object o, IList<object> contents)
        {
            GetOrCreate(this.collectionAdderCache, property, () => this.reflection.CollectionAdder(property))(o, contents);
        }

        public PropertyInfo[] GetProperties(Type t)
        {
            return GetOrCreate(this.propertyCache, t, () => ReflectionUtility.GetPublicPropertiesWithGetters(t).ToArray());
        }

        public object GetValue(object o, PropertyInfo property)
        {
            return GetOrCreate(this.getterCache, property, () => this.reflection.Getter(property))(o);
        }

        public void SetValue(object o, PropertyInfo property, object value)
        {
            GetOrCreate(this.setterCache, property, () => this.reflection.Setter(property))(o, value);
        }

        public Keys GetPrimaryKeys(object o)
        {
            var thisType = o.GetType();

            return GetOrCreate(this.keyGetterCache, thisType, () => this.reflection.PrimaryKeyGetter(this.relations, thisType))(o);
        }

        public Keys GetForeignKeys(object o, Type foreignType)
        {
            var thisType = o.GetType();

            return GetOrCreate(this.foreignKeyGetterCache, Tuple.Create(thisType, foreignType), () => this.reflection.ForeignKeyGetter(this.relations, thisType, foreignType))(o);
        }

        public void SetForeignKeys(object o, Type foreignType, Keys foreignKeys)
        {
            var thisType = o.GetType();

            GetOrCreate(this.foreignKeySetterCache, Tuple.Create(thisType, foreignType), () => this.reflection.ForeignKeySetter(this.relations, thisType, foreignType))(o, foreignKeys);
        }

        public void ClearForeignKeys(object o, Type foreignType)
        {
            var thisType = o.GetType();

            GetOrCreate(this.foreignKeyClearerCache, Tuple.Create(thisType, foreignType), () => this.reflection.ForeignKeyClearer(this.relations, thisType, foreignType))(o);
        }
    }
}
