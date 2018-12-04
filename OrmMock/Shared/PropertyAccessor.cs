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
        private readonly Relations relations;

        private readonly Dictionary<Type, PropertyInfo[]> propertyCache = new Dictionary<Type, PropertyInfo[]>();

        private readonly Dictionary<PropertyInfo, Func<object, object>> getterCache = new Dictionary<PropertyInfo, Func<object, object>>();

        private readonly Dictionary<PropertyInfo, Action<object, object>> setterCache = new Dictionary<PropertyInfo, Action<object, object>>();

        private readonly Dictionary<Type, Func<object, Keys>> keyGetterCache = new Dictionary<Type, Func<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Func<object, Keys>> foreignKeyGetterCache = new Dictionary<Tuple<Type, Type>, Func<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object, Keys>> foreignKeySetterCache = new Dictionary<Tuple<Type, Type>, Action<object, Keys>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object>> foreignKeyClearerCache = new Dictionary<Tuple<Type, Type>, Action<object>>();

        private readonly Dictionary<PropertyInfo, Action<object, bool, IList<object>>> collectionSetterCache = new Dictionary<PropertyInfo, Action<object, bool, IList<object>>>();

        public PropertyAccessor(Relations relations)
        {
            this.relations = relations;
        }

        public void SetCollection(PropertyInfo property, object o, IList<object> contents) => this.SetCollection(property, o, true, contents);

        public void AddToCollection(PropertyInfo property, object o, IList<object> contents) => this.SetCollection(property, o, false, contents);

        private void SetCollection(PropertyInfo property, object o, bool clear, IList<object> contents)
        {
            if (!this.collectionSetterCache.TryGetValue(property, out var collectionSetter))
            {
                var genericArgument = property.PropertyType.GenericTypeArguments[0];
                var interfaceType = typeof(ICollection<>).MakeGenericType(genericArgument);
                var constructor = Reflection.Constructor(typeof(HashSet<>).MakeGenericType(genericArgument));
                var clearer = Reflection.Caller(interfaceType, nameof(ICollection<int>.Clear));
                var adder = Reflection.Caller(interfaceType, genericArgument, nameof(ICollection<int>.Add));

                collectionSetter = (localObjects, localClear, localContents) =>
                {
                    var propertyValue = this.GetValue(o, property);
                    if (propertyValue == null)
                    {
                        propertyValue = constructor();
                        this.SetValue(o, property, propertyValue);
                    }

                    if (localClear)
                    {
                        clearer(propertyValue);
                    }

                    foreach (var item in localContents)
                    {
                        adder(propertyValue, item);
                    }
                };

                this.collectionSetterCache.Add(property, collectionSetter);
            }

            collectionSetter(o, clear, contents);
        }

        public PropertyInfo[] GetProperties(Type t)
        {
            if (!this.propertyCache.TryGetValue(t, out var properties))
            {
                properties = t.GetProperties();

                this.propertyCache.Add(t, properties);
            }

            return properties;
        }

        public object GetValue(object o, PropertyInfo property)
        {
            if (!this.getterCache.TryGetValue(property, out var getter))
            {
                getter = Reflection.Getter(property);
                this.getterCache.Add(property, getter);
            }

            return getter(o);
        }

        public void SetValue(object o, PropertyInfo property, object value)
        {
            if (!this.setterCache.TryGetValue(property, out var setter))
            {
                setter = Reflection.Setter(property);
                this.setterCache.Add(property, setter);
            }

            setter(o, value);
        }

        public Keys GetPrimaryKeys(object o)
        {
            var t = o.GetType();

            if (!this.keyGetterCache.TryGetValue(t, out var keyGetter))
            {
                var getters = Reflection.Getters(this.relations.GetPrimaryKeys(t));

                keyGetter = local => new Keys(getters.Select(g => g(local)).ToArray());

                this.keyGetterCache.Add(t, keyGetter);
            }

            return keyGetter(o);
        }

        public Keys GetForeignKeys(object o, Type foreignType)
        {
            var thisType = o.GetType();

            var key = Tuple.Create(thisType, foreignType);

            if (!this.foreignKeyGetterCache.TryGetValue(key, out var keyGetter))
            {
                var getters = Reflection.Getters(this.relations.GetForeignKeys(thisType, foreignType));

                keyGetter = local => new Keys(getters.Select(g => g(local)).ToArray());

                this.foreignKeyGetterCache.Add(key, keyGetter);
            }

            return keyGetter(o);
        }

        public void SetForeignKeys(object o, Type foreignType, Keys foreignKeys)
        {
            var thisType = o.GetType();

            var key = Tuple.Create(thisType, foreignType);

            if (!this.foreignKeySetterCache.TryGetValue(key, out var keySetter))
            {
                var setters = Reflection.Setters(this.relations.GetForeignKeys(thisType, foreignType));

                keySetter = (local, keys) =>
                {
                    if (setters.Count != 0 && setters.Count != keys.Data.Length)
                    {
                        throw new InvalidOperationException("Setters and keys must be of equal length.");
                    }

                    for (var i = 0; i < setters.Count; ++i)
                    {
                        setters[i](local, keys.Data[i]);
                    }
                };

                this.foreignKeySetterCache.Add(key, keySetter);
            }


            keySetter(o, foreignKeys);
        }

        public void ClearForeignKeys(object o, Type foreignType)
        {
            var thisType = o.GetType();

            var key = Tuple.Create(thisType, foreignType);

            if (!this.foreignKeyClearerCache.TryGetValue(key, out var clearer))
            {
                var foreignKeys = this.relations.GetForeignKeys(thisType, foreignType);

                if (foreignKeys.Any(fk => !Reflection.IsNullable(fk)))
                {
                    throw new InvalidOperationException("Not all keys are nullable.");
                }

                var setters = Reflection.Setters(foreignKeys);

                clearer = toClear =>
                {
                    foreach (var setter in setters)
                    {
                        setter(toClear, null);
                    }
                };

                this.foreignKeyClearerCache.Add(key, clearer);
            }

            clearer(o);
        }
    }
}
