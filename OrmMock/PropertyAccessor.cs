using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OrmMock
{
    public class PropertyAccessor
    {
        private readonly Relations relations;

        private readonly Dictionary<Type, PropertyInfo[]> propertyCache = new Dictionary<Type, PropertyInfo[]>();

        private readonly Dictionary<PropertyInfo, Func<object, object>> getterCache = new Dictionary<PropertyInfo, Func<object, object>>();

        private readonly Dictionary<PropertyInfo, Action<object, object>> setterCache = new Dictionary<PropertyInfo, Action<object, object>>();

        private readonly Dictionary<Type, Func<object, KeyHolder>> keyGetterCache = new Dictionary<Type, Func<object, KeyHolder>>();

        private readonly Dictionary<Tuple<Type, Type>, Func<object, KeyHolder>> foreignKeyGetterCache = new Dictionary<Tuple<Type, Type>, Func<object, KeyHolder>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object, KeyHolder>> foreignKeySetterCache = new Dictionary<Tuple<Type, Type>, Action<object, KeyHolder>>();

        private readonly Dictionary<Tuple<Type, Type>, Action<object>> foreignKeyClearerCache = new Dictionary<Tuple<Type, Type>, Action<object>>();

        private readonly Dictionary<PropertyInfo, Action<object, IList<object>>> collectionSetterCache = new Dictionary<PropertyInfo, Action<object, IList<object>>>();


        public PropertyAccessor(Relations relations)
        {
            this.relations = relations;
        }

        public void SetCollection(PropertyInfo property, object o, IList<object> contents)
        {
            if (!this.collectionSetterCache.TryGetValue(property, out var collectionSetter))
            {
                var constructor = Reflection.Constructor(typeof(HashSet<>).MakeGenericType(property.PropertyType.GenericTypeArguments[0]));

                collectionSetter = (localObjects, localContents) =>
                {
                    var propertyValue = this.GetValue(o, property);
                    if (propertyValue == null)
                    {
                        propertyValue = constructor();
                        this.SetValue(o, property, propertyValue);
                    }

                    var clearer = Reflection.Caller(propertyValue.GetType(), nameof(ICollection<int>.Clear));
                    var adder = Reflection.Caller(propertyValue.GetType(), property.PropertyType.GetGenericArguments()[0], nameof(ICollection<int>.Add));

                    clearer(propertyValue);

                    foreach (var item in localContents)
                    {
                        adder(propertyValue, item);
                    }
                };

                this.collectionSetterCache.Add(property, collectionSetter);
            }

            collectionSetter(o, contents);
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

        public KeyHolder GetPrimaryKeys(object o)
        {
            var t = o.GetType();

            if (!this.keyGetterCache.TryGetValue(t, out var keyGetter))
            {
                var getters = Reflection.Getters(this.relations.GetPrimaryKeys(t));

                keyGetter = local => new KeyHolder(getters.Select(g => g(local)).ToArray());

                this.keyGetterCache.Add(t, keyGetter);
            }

            return keyGetter(o);
        }

        public KeyHolder GetForeignKeys(object o, Type foreignType)
        {
            var thisType = o.GetType();

            var key = Tuple.Create(thisType, foreignType);

            if (!this.foreignKeyGetterCache.TryGetValue(key, out var keyGetter))
            {
                var getters = Reflection.Getters(this.relations.GetForeignKeys(thisType, foreignType));

                keyGetter = local => new KeyHolder(getters.Select(g => g(local)).ToArray());

                this.foreignKeyGetterCache.Add(key, keyGetter);
            }

            return keyGetter(o);
        }

        public void SetForeignKeys(object o, Type foreignType, KeyHolder foreignKeys)
        {
            var thisType = o.GetType();

            var key = Tuple.Create(thisType, foreignType);

            if (!this.foreignKeySetterCache.TryGetValue(key, out var keySetter))
            {
                var setters = Reflection.Setters(this.relations.GetForeignKeys(thisType, foreignType));

                keySetter = (local, keys) =>
                {
                    if (setters.Count != 0 && setters.Count != keys.Keys.Length)
                    {
                        throw new InvalidOperationException("Setters and keys must be of equal length.");
                    }

                    for (var i = 0; i < setters.Count; ++i)
                    {
                        setters[i](o, keys.Keys[i]);
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

                if (foreignKeys.Any(fk => !CanSetKeyToNull(fk.PropertyType)))
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


        private static bool CanSetKeyToNull(Type t)
        {
            return t == typeof(string) || Nullable.GetUnderlyingType(t) != null;
        }
    }
}
