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

namespace DataGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;

    public class ObjectContext
    {
        /// <summary>
        /// Holds the number of objects generated.
        /// </summary>
        private int objectCount;

        /// <summary>
        /// Holds the list of created objects.
        /// </summary>
        private readonly IList<object> createdObjects = new List<object>();

        /// <summary>
        /// Holds the created singletons.
        /// </summary>
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        /// <summary>
        /// Holds the structure data.
        /// </summary>
        private readonly Structure structure;

        /// <summary>
        /// Holds the generator for simple values.
        /// </summary>
        public SimpleValueGenerator SimpleValueGenerator { get; } = new SimpleValueGenerator();

        /// <summary>
        /// Gets or sets the limit of how many object to create in one pass.
        /// </summary>
        public int ObjectLimit { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the number of items to create in root level object collections.
        /// </summary>
        public int RootCollectionMembers { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of items to create in leaf level object collections.
        /// </summary>
        public int NonRootCollectionMembers { get; set; } = 0;

        /// <summary>
        /// Gets or sets value determining if logging of object creation should be enabled.
        /// </summary>
        public bool Logging { get; set; }

        public ObjectContext(Structure structure)
        {
            this.structure = structure;
        }

        /// <summary>
        /// Registers a specific object type to be a singleton of a specific value
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The singleton value to register.</param>
        /// <returns>The generator.</returns>
        public ObjectContext Singleton<T>(T value)
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
        /// Gets the object of the specified type at the given index.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="index">The index of the object to get.</param>
        /// <returns>The object at the given index.</returns>
        public T GetObject<T>(int index)
        {
            return (T)this.createdObjects.Where(o => o.GetType() == typeof(T)).Skip(index).First();
        }

        /// <summary>
        /// Gets the object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The objectsx.</returns>
        public IEnumerable<T> GetObjects<T>()
        {
            return this.createdObjects.Where(o => o.GetType() == typeof(T)).Select(o => (T)o);
        }

        /// <summary>
        /// Gets the object of the specified type at the given index.
        /// </summary>
        /// <param name="index">The index of the object to get.</param>
        /// <returns>The object at the given index.</returns>
        public object GetObject(int index)
        {
            return this.createdObjects.Skip(index).First();
        }

        /// <summary>
        /// Gets the object of the specified type.
        /// </summary>
        /// <returns>The objects.</returns>
        public IEnumerable<object> GetObjects()
        {
            return this.createdObjects;
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created object.</returns>
        public T Create<T>()
        {
            if (this.TryCreateValue(typeof(T), string.Empty, out var result))
            {
                return (T)result;
            }

            return (T)CreateObject(typeof(T), new List<object>());
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="T2">The type of the parameter.</typeparam>
        /// <param name="e">The setter expression</param>
        /// <param name="value">The value</param>
        /// <returns>The created object.</returns>
        public T Create<T, T2>(Expression<Func<T, T2>> e, T2 value)
            where T : class
        {
            var result = this.Create<T>();

            foreach (var property in ExpressionUtility.GetPropertyInfo(e))
            {
                property.SetMethod.Invoke(result, new[] { (object)value });
            }

            return result;
        }

        /// <summary>
        /// Creates many objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created objects.</returns>
        public IEnumerable<T> CreateMany<T>(int create = 3)
            where T : new()
        {
            while (create-- > 0)
            {
                yield return this.Create<T>();
            }
        }

        /// <summary>
        /// Creates many objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <typeparam name="T2">The type of the parameter.</typeparam>
        /// <param name="e">The setter expression</param>
        /// <param name="values">The values</param>
        /// <returns>The created objects.</returns>
        public IEnumerable<T> CreateMany<T, T2>(Expression<Func<T, T2>> e, params T2[] values)
            where T : class, new()
        {
            foreach (var value in values)
            {
                yield return this.Create(e, value);
            }
        }

        /// <summary>
        /// Creates an object of type t, considering the sources for references.
        /// </summary>
        /// <param name="t">The type of the object to create.</param>
        /// <param name="sources">The sources.</param>
        /// <returns></returns>
        private object CreateObject(Type t, IList<object> sources)
        {
            if (this.singletons.TryGetValue(t, out object singleton))
            {
                // Register possible back references in singleton.
                this.SetProperties(singleton, sources, true);

                return singleton;
            }

            var ctor = t.GetConstructors().SingleOrDefault();

            var constructorArgs = ctor?.GetParameters().Select(p =>
            {
                if (this.TryCreateValue(p.ParameterType, t.Name, out object constructorParameter))
                {
                    return constructorParameter;
                }

                if (this.structure.WithoutType.Contains(p.ParameterType))
                {
                    return null;
                }

                var source = (this.structure.WithoutAncestryForType.Contains(p.ParameterType) || this.structure.WithoutAncestryForConstructor.Contains(p.ParameterType)) ? null : GetSource(sources, p.ParameterType);
                return source ?? CreateObject(p.ParameterType, sources);
            }).ToArray();

            if (this.Logging)
            {
                var diag = $"{new string(' ', 4 * sources.Count)}{t}";
                Console.WriteLine(diag);
            }

            if (++this.objectCount > this.ObjectLimit)
            {
                throw new InvalidOperationException($"Attempt to create more than {this.ObjectLimit} objects.");
            }

            var result = Activator.CreateInstance(t, constructorArgs);

            sources.Add(result);

            this.createdObjects.Add(result);

            this.SetProperties(result, sources, false);

            sources.RemoveAt(sources.Count - 1);

            if (this.structure.Singletons.Contains(t))
            {
                this.singletons[t] = result;
            }

            return result;
        }

        private bool TryCreateValue(Type t, string prefix, out object result)
        {
            if (this.structure.CustomConstructors.TryGetValue(t, out Func<object> creator))
            {
                result = creator();
                return true;
            }

            return this.SimpleValueGenerator.TryCreateValue(t, prefix, out result);
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

        private void SetProperties(object o, IList<object> sources, bool singleton)
        {
            var t = o.GetType();
            var referenceProperties = new List<PropertyInfo>();

            foreach (var p in t.GetProperties())
            {
                if (this.structure.CustomPropertySetters.TryGetValue(p, out Func<object, object> valueFunc))
                {
                    if (!singleton)
                    {
                        p.SetMethod.Invoke(o, new[] { valueFunc(o) });
                    }

                    continue;
                }

                if (this.structure.WithoutProperty.Contains(p))
                {
                    continue;
                }

                var pt = p.PropertyType;

                if (this.structure.WithoutType.Contains(pt))
                {
                    continue;
                }

                if (this.TryCreateValue(pt, p.Name, out object val))
                {
                    if (!singleton)
                    {
                        p.SetMethod.Invoke(o, new[] { val });
                    }
                }
                else
                {
                    referenceProperties.Add(p);
                }
            }

            for (var pass = 1; pass <= 2; ++pass)
            {
                // Pass 1, update pk id for 1:1 relation.
                // Pass 2, the rest.
                foreach (var p in referenceProperties)
                {
                    var noSources = this.structure.WithoutAncestryForType.Contains(t) || this.structure.WithoutAncestryForProperty.Contains(p);
                    var pt = p.PropertyType;

                    if (pt.IsGenericType)
                    {
                        if (pt.GetGenericTypeDefinition() == typeof(ICollection<>))
                        {
                            if (pass != 2)
                            {
                                continue;
                            }

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
                                if (singleton)
                                {
                                    continue;
                                }

                                if (!this.structure.Include.TryGetValue(p, out int elementCount))
                                {
                                    elementCount = this.objectCount == 1 ? this.RootCollectionMembers : this.NonRootCollectionMembers;
                                }

                                for (var i = 0; i < elementCount; ++i)
                                {
                                    add.Invoke(collection, new[] { CreateObject(elementType, sources) });
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
                        // Going from t to pt
                        // Note that primary keys and foreign keys may be equal.
                        var foreignKeyProps = this.structure.Relations.GetForeignKeys(t, pt.GetElementType());
                        var primaryKeyProps = this.structure.Relations.GetPrimaryKeys(t);

                        if (foreignKeyProps == null)
                        {
                            throw new InvalidOperationException($@"Unable to determine foreign keys from '{t.Name}' to '{pt.GetElementType().Name}'.");
                        }

                        if (primaryKeyProps == null)
                        {
                            throw new InvalidOperationException($@"Unable to determine primary keys for '{t.Name}'.");
                        }

                        // Pass 1, handle the case where the foreign key props and the primary key props are equal in pass 1 only.
                        if (foreignKeyProps.SequenceEqual(primaryKeyProps))
                        {
                            if (pass == 2)
                            {
                                continue;
                            }
                        }
                        else if (pass == 1)
                        {
                            continue;
                        }

                        var foreignObject = (noSources ? null : GetSource(sources, pt)) ?? this.CreateObject(pt, sources);

                        var primaryKeysOfForeignObject = this.structure.Relations.GetPrimaryKeys(foreignObject.GetType());

                        if (primaryKeysOfForeignObject == null)
                        {
                            throw new InvalidOperationException($@"Unable to determine primary keys for '{foreignObject.GetType().Name}'.");
                        }

                        if (singleton)
                        {
                            var existing = p.GetMethod.Invoke(o, new object[0]);

                            if (foreignObject != existing && existing != null)
                            {
                                throw new InvalidOperationException($"Ambiguous property for singleton {t.Name}.{p.Name}.");
                            }
                        }

                        // Set foreign keys to primary keys of related object.
                        for (var i = 0; i < foreignKeyProps.Length; ++i)
                        {
                            foreignKeyProps[i].SetMethod.Invoke(o, new[] { primaryKeysOfForeignObject[i].GetMethod.Invoke(foreignObject, new object[0]) });
                        }

                        p.SetMethod.Invoke(o, new[] { foreignObject });
                    }
                }
            }
        }
    }
}
