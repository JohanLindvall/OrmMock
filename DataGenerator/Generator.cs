using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace DataGenerator
{
    /// <summary>
    /// Defines a class for generating object hierarchies.
    /// </summary>
    public class Generator
    {
        /// <summary>
        /// Holds the number of objects generated.
        /// </summary>
        private int objectCount;

        /// <summary>
        /// Holds the hashset of properties to exclude.
        /// </summary>
        private readonly HashSet<PropertyInfo> without = new HashSet<PropertyInfo>();

        /// <summary>
        /// Holds the hashset of types to exclude.
        /// </summary>
        private readonly HashSet<Type> withoutType = new HashSet<Type>();

        /// <summary>
        /// Holds the hashset of properties for which ancestry should be ignored..
        /// </summary>
        private readonly HashSet<PropertyInfo> withoutAncestry = new HashSet<PropertyInfo>();

        /// <summary>
        /// Holds the dictionary of navigation properties to include and the count of items to create.
        /// </summary>
        private readonly Dictionary<PropertyInfo, int> include = new Dictionary<PropertyInfo, int>();

        /// <summary>
        /// Holds the dictionary of singleton objects.
        /// </summary>
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        /// <summary>
        /// Holds the dictionary of overridden property values.
        /// </summary>
        private readonly Dictionary<PropertyInfo, Func<object, object>> with = new Dictionary<PropertyInfo, Func<object, object>>();

        /// <summary>
        /// Holds the dictionary of overridden types.
        /// </summary>
        private readonly Dictionary<Type, Func<object>> typeWith = new Dictionary<Type, Func<object>>();

        /// <summary>
        /// Gets or sets the limit of how many object to create in one pass.
        /// </summary>
        public int ObjectLimit { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the number of items top create in root level object collections.
        /// </summary>
        public int RootCollectionMembers { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of items top create in leaf level object collections.
        /// </summary>
        public int LeafCollectionMembers { get; set; } = 0;

        /// <summary>
        /// Gets or sets value determining if logging of object creation should be enabled.
        /// </summary>
        public bool Logging { get; set; }

        public Random Random { get; } = new Random();

        /// <summary>
        /// Excludes a property from being set.
        /// </summary>
        /// <typeparam name="T">The type of the object where the property resides.</typeparam>
        /// <param name="e">The expression func for the object.</param>
        /// <returns>The generator.</returns>
        public Generator Without<T>(Expression<Func<T, object>> e)
        {
            this.without.Add(((e.Body as MemberExpression).Member) as PropertyInfo);
            return this;
        }

        /// <summary>
        /// Excludes a type from being created.
        /// </summary>
        /// <typeparam name="T">The type of the object to exclude.</typeparam>
        /// <returns>The generator.</returns>
        public Generator Without<T>()
        {
            this.withoutType.Add(typeof(T));
            return this;
        }

        /// <summary>
        /// Excludes a property using ancestry when being set.
        /// </summary>
        /// <typeparam name="T">The type of the object where the property resides.</typeparam>
        /// <param name="e">The expression func for the object.</param>
        /// <returns>The generator.</returns>
        public Generator WithoutAncestry<T>(Expression<Func<T, object>> e)
        {
            this.withoutAncestry.Add(((e.Body as MemberExpression).Member) as PropertyInfo);
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
        {
            var pi = ((e.Body as MemberExpression).Member) as PropertyInfo;

            if (pi.PropertyType.GetGenericTypeDefinition() != typeof(ICollection<>))
            {
                throw new ArgumentException("Must be ICollection<>");
            }

            this.include.Add(pi, count);
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
        public Generator With<T, T2>(Expression<Func<T, T2>> e, Func<T, T2> value)
        {
            this.with.Add(((e.Body as MemberExpression).Member) as PropertyInfo, o => (object)value((T)o));
            return this;
        }

        /// <summary>
        /// Sets a type to a speficic valuer
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="creator">The function returning an object.</param>
        /// <returns>The generator.</returns>
        public Generator With<T>(Func<T> creator)
        {
            this.typeWith.Add(typeof(T), () => creator());
            return this;
        }

        /// <summary>
        /// Registers a specific object type to be a singleton.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The generator.</returns>
        public Generator Singleton<T>()
        {
            this.singletons.Add(typeof(T), null);
            return this;
        }

        /// <summary>
        /// Registers a specific object type to be a singleton of a specific value
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The singleton value to register.</param>
        /// <returns>The generator.</returns>
        public Generator Singleton<T>(T value)
        {
            this.singletons.Add(typeof(T), value);
            return this;
        }

        /// <summary>
        /// Gets a singleton value for the given type.
        /// </summary>
        /// <typeparam name="T">The singleton type.</typeparam>
        /// <returns>The singleton value.</returns>
        public T GetSingleton<T>()
        {
            return (T)this.singletons[typeof(T)];
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created object.</returns>
        public T Create<T>()
            where T : new()
        {
            object result;
            if (this.TryCreateValue(typeof(T), string.Empty, out result))
            {
                return (T)result;
            }
            return (T)Create(typeof(T), new List<object>());
        }

        private PropertyInfo[] GetPropertyInfos(Type t)
        {
            return t.GetProperties();
        }

        private object Create(Type t, IList<object> sources)
        {
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

            if (++this.objectCount > this.ObjectLimit)
            {
                throw new InvalidOperationException($"Attempt to create more than {this.ObjectLimit} objects.");
            }

            var result = Activator.CreateInstance(t);

            sources.Add(result);

            SetProperties(result, sources);

            sources.RemoveAt(sources.Count - 1);

            if (isSingleton)
            {
                this.singletons[t] = result;
            }

            return result;
        }

        private bool TryCreateValue(Type t, string prefix, out object result)
        {
            Func<object> creator;
            if (this.typeWith.TryGetValue(t, out creator))
            {
                result = creator();
                return true;
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                if (this.Random.Next(0, 1) == 0)
                {
                    result = null;
                    return true;
                }
                else
                {
                    t = Nullable.GetUnderlyingType(t);
                }
            }

            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                result = values.GetValue(this.Random.Next(0, values.Length - 1));
            }
            else if (t == typeof(string))
            {
                result = prefix + BitConverter.ToString(Enumerable.Range(0, 20).Select(_ => (byte)this.Random.Next(0, 255)).ToArray());
            }
            else if (t == typeof(short))
            {
                result = (short)this.Random.Next(short.MinValue, short.MaxValue);
            }
            else if (t == typeof(int))
            {
                result = this.Random.Next(int.MinValue, int.MaxValue);
            }
            else if (t == typeof(long))
            {
                result = BitConverter.ToInt64(Enumerable.Range(0, 16).Select(_ => (byte)this.Random.Next(0, 255)).ToArray(), 0);
            }
            else if (t == typeof(double))
            {
                result = (this.Random.NextDouble() - 0.5) * double.MaxValue;
            }
            else if (t == typeof(float))
            {
                result = (float)(this.Random.NextDouble() - 0.5) * float.MaxValue;
            }
            else if (t == typeof(decimal))
            {
                result = (decimal)(this.Random.NextDouble() - 0.5) * decimal.MaxValue;
            }
            else if (t == typeof(bool))
            {
                result = this.Random.Next(0, 1) == 1;
            }
            else if (t == typeof(Guid))
            {
                result = new Guid(Enumerable.Range(0, 16).Select(_ => (byte)this.Random.Next(0, 255)).ToArray());
            }
            else if (t == typeof(DateTimeOffset))
            {
                result = DateTimeOffset.Now + TimeSpan.FromMilliseconds((this.Random.NextDouble() - 0.5) * 62e9);
            }
            else
            {
                result = null;
                return false;
            }

            return true;
        }

        private object GetSource(IList<object> sources, Type sourceType)
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

        private void SetProperties(object o, IList<object> sources)
        {
            var t = o.GetType();
            var unhandled = new List<PropertyInfo>();
            var handled = new List<PropertyInfo>();

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

                if (this.withoutType.Contains(pt))
                {
                    continue;
                }

                object val = null;
                if (this.TryCreateValue(pt, p.Name, out val))
                {
                    p.SetMethod.Invoke(o, new[] { val });
                    handled.Add(p);
                }
                else
                {
                    unhandled.Add(p);
                }
            }

            foreach (var p in unhandled)
            {
                var noSources = this.withoutAncestry.Contains(p);
                var pt = p.PropertyType;

                if (pt.IsGenericType)
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
                        var source = noSources ? null : GetSource(sources, elementType);
                        if (source != null)
                        {
                            add.Invoke(collection, new[] { source });
                        }
                        else
                        {
                            if (this.withoutType.Contains(elementType))
                            {
                                continue;
                            }

                            var elementCount = 0;

                            if (!this.include.TryGetValue(p, out elementCount))
                            {
                                elementCount = this.objectCount == 1 ? this.RootCollectionMembers : this.LeafCollectionMembers;
                            }

                            for (var i = 0; i < elementCount; ++i)
                            {
                                add.Invoke(collection, new[] { Create(elementType, sources) });
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
                    var source = noSources ? null : GetSource(sources, pt);

                    if (source == null)
                    {
                        source = this.Create(pt, sources);
                    }

                    var backRefId = handled.SingleOrDefault(idp => idp.Name == p.Name + "Id");

                    if (backRefId == null)
                    {
                        // 1:1 relation, special case. will change pk of current object.
                        // TODO this should be done first in this loop so that the object is complete when creating related objects.
                        backRefId = handled.SingleOrDefault(idp => idp.Name == "Id");
                    }

                    var id = source.GetType().GetProperty("Id");

                    if (id == null || backRefId == null)
                    {
                        // TODO register customizer for this case.
                        throw new InvalidOperationException($"Unable to connect {t.Name} back to {pt.Name}");
                    }

                    backRefId.SetMethod.Invoke(o, new[] { id.GetMethod.Invoke(source, new object[0]) });

                    p.SetMethod.Invoke(o, new[] { source });
                }
            }
        }
    }
}
