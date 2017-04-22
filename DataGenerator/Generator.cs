using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace DataGenerator
{
    public class Generator
    {
        private readonly HashSet<PropertyInfo> without = new HashSet<PropertyInfo>();

        private readonly Dictionary<PropertyInfo, int> include = new Dictionary<PropertyInfo, int>();

        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        private readonly Dictionary<PropertyInfo, Func<object, object>> with = new Dictionary<PropertyInfo, Func<object, object>>();

        public int Limit { get; set; } = 1000;

        public Generator Without<T>(Expression<Func<T, object>> e)
        {
            this.without.Add(((e.Body as MemberExpression).Member) as PropertyInfo);
            return this;
        }

        public Generator Include<T>(Expression<Func<T, object>> e, int count = 3)
        {
            var pi = ((e.Body as MemberExpression).Member) as PropertyInfo;

            if (pi.PropertyType.GetGenericTypeDefinition() != typeof(ICollection<>))
            {
                throw new ArgumentException("Must be ICollection<>");
            }

            this.include.Add(pi, count);
            return this;
        }

        public Generator With<T, T2>(Expression<Func<T, T2>> e, Func<T, T2> value)
        {
            this.with.Add(((e.Body as MemberExpression).Member) as PropertyInfo, o => (object)value((T)o));
            return this;
        }

        public Generator With<T>(Func<T> creator)
        {
            return this;
        }

        public Generator Singleton<T>()
        {
            this.singletons.Add(typeof(T), null);
            return this;
        }

        public Generator Singleton<T>(T value)
        {
            this.singletons.Add(typeof(T), value);
            return this;
        }

        public T GetSingletonValue<T>()
        {
            return (T)this.singletons[typeof(T)];
        }

        public PropertyInfo[] GetPropertyInfos(Type t)
        {
            return t.GetProperties();
        }

        public T Create<T>()
            where T : new()
        {
            return (T)Create(typeof(T), new List<object>(), 0);
        }

        public object Create(Type t, IList<object> sources, int count)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                // TODO FIX
                return null;
            }
            else if (t == typeof(string))
            {
                return "foo";
            }
            else if (t == typeof(short))
            {
                return (short)123;
            }
            else if (t == typeof(int))
            {
                return 123;
            }
            else if (t == typeof(long))
            {
                return (long)123;
            }
            else if (t == typeof(double))
            {
                return 123.456;
            }
            else if (t == typeof(decimal))
            {
                return 123.456m;
            }
            else if (t == typeof(bool))
            {
                return false;
            }
            else if (t == typeof(Guid))
            {
                return Guid.NewGuid();
            }
            else if (t == typeof(DateTimeOffset))
            {
                return DateTimeOffset.Now;
            }
            else
            {

            }

            var isSingleton = false;
            object singleton;
            if (this.singletons.TryGetValue(t, out singleton))
            {
                isSingleton = true;
                if (singleton != null)
                {
                    return singleton;
                }
            }

            var diag = $"{new string(' ', 4 * sources.Count)}{t}";
            Console.WriteLine(diag);

            if (++count > this.Limit)
            {
                throw new InvalidOperationException($"Attempt to create more than {this.Limit} objects.");
            }

            var result = Activator.CreateInstance(t);

            sources.Add(result);

            SetProperties(result, sources, count);

            sources.RemoveAt(sources.Count - 1);

            if (isSingleton)
            {
                this.singletons[t] = result;
            }

            return result;
        }

        public object GetSource(IList<object> sources, Type sourceType)
        {
            for (var i = sources.Count - 2; i >= 0; --i)
            {
                if (sources[i].GetType() == sourceType)
                {
                    return sources[i];
                }
            }

            return null;
        }

        public void SetProperties(object o, IList<object> sources, int count)
        {
            var t = o.GetType();

            foreach (var p in GetPropertyInfos(t))
            {
                Func<object, object> valueFunc;
                if (this.with.TryGetValue(p, out valueFunc))
                {
                    p.SetMethod.Invoke(o, new[] { valueFunc(o) });
                    continue;
                }

                if (this.without.Contains(p))
                {
                    continue;
                }

                var pt = p.PropertyType;

                if (pt.IsGenericType && pt.GetGenericTypeDefinition() != typeof(Nullable<>))
                {
                    if (pt.GetGenericTypeDefinition() == typeof(ICollection<>))
                    {
                        var elementType = pt.GetGenericArguments()[0];
                        var collection = p.GetMethod.Invoke(o, new object[0]);
                        if (collection == null)
                        {
                            collection = Activator.CreateInstance(typeof(HashSet<>).MakeGenericType(elementType));
                            p.SetMethod.Invoke(o, new[] { collection });
                        }
                        var add = collection.GetType().GetMethod("Add");
                        var source = GetSource(sources, elementType);
                        if (source != null)
                        {
                            // Set back reference.
                            add.Invoke(collection, new[] { source });
                        }
                        else
                        {
                            var elementCount = 0;
                            if (!this.include.TryGetValue(p, out elementCount))
                            {
                                elementCount = count == 1 ? 3 : 0;
                            }
                            // Create three new elements for root
                            for (var i = 0; i < elementCount; ++i)
                            {
                                add.Invoke(collection, new[] { Create(elementType, sources, count) });
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Unsupported type");
                    }
                }
                else
                {
                    var source = GetSource(sources, pt) ?? Create(pt, sources, count);

                    p.SetMethod.Invoke(o, new[] { source });
                }
            }
        }

        public bool IsNormalProperty(Type t)
        {
            return false;
        }
    }
}
