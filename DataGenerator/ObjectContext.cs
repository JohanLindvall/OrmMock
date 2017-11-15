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

    using Fasterflect;

    /// <summary>
    /// Defines an object context containing the created object.
    /// </summary>
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
        /// Holds the method property cache.
        /// </summary>
        private readonly Dictionary<Type, List<Action<object, IList<object>, bool>>> methodPropertyCache = new Dictionary<Type, List<Action<object, IList<object>, bool>>>();

        /// <summary>
        /// Holds the constructor cache for the given type.
        /// </summary>
        private readonly Dictionary<Type, Func<IList<object>, object>> constructorCache = new Dictionary<Type, Func<IList<object>, object>>();

        /// <summary>
        /// Holds the structure data.
        /// </summary>
        private readonly Structure structure;

        /// <summary>
        /// Holds the random generator used by this instance.
        /// </summary>
        private readonly Random random = new Random();

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
        /// <param name="objectType">The type of the object to create.</param>
        /// <param name="sources">The sources.</param>
        /// <returns></returns>
        private object CreateObject(Type objectType, IList<object> sources)
        {
            if (!this.constructorCache.TryGetValue(objectType, out var constructor))
            {
                var simpleCreator = this.ValueCreator(objectType);

                if (simpleCreator != null)
                {
                    constructor = _ => simpleCreator(string.Empty);
                }
                else
                {
                    var ctor = objectType.GetConstructors().SingleOrDefault();

                    if (ctor == null)
                    {
                        throw new InvalidOperationException($@"Cannot construct {objectType.Name}.");
                    }

                    var ctorParameters = new List<Func<IList<object>, object>>();

                    foreach (var constructorParameter in ctor.GetParameters())
                    {
                        var constructorParameterType = constructorParameter.ParameterType;

                        if (this.structure.WithoutType.Contains(constructorParameterType))
                        {
                            ctorParameters.Add(_ => null);
                            continue;
                        }

                        var valueCreator = this.ValueCreator(constructorParameterType);

                        if (valueCreator != null)
                        {
                            ctorParameters.Add(_ => valueCreator(objectType.Name));
                            continue;
                        }

                        var noAncestry = this.structure.WithoutAncestryForType.Contains(constructorParameterType) || this.structure.WithoutAncestryForConstructor.Contains(constructorParameterType);

                        ctorParameters.Add(localSources =>
                        {
                            var source = noAncestry ? null : GetSource(localSources, constructorParameterType);
                            return source ?? CreateObject(constructorParameterType, localSources);
                        });
                    }

                    var constructorDelegate = ctor.DelegateForCreateInstance();

                    this.structure.PostCreate.TryGetValue(objectType, out var postCreate);

                    var handleSingleton = this.structure.Singletons.Contains(objectType) || this.singletons.ContainsKey(objectType);

                    constructor = localSources =>
                    {
                        if (handleSingleton && this.singletons.TryGetValue(objectType, out object singleton))
                        {
                            // Register possible back references in singleton.
                            this.SetProperties(singleton, objectType, localSources, true);

                            return singleton;
                        }

                        if (this.Logging)
                        {
                            var diag = $"{new string(' ', 4 * localSources.Count)}{objectType}";
                            Console.WriteLine(diag);
                        }

                        if (++this.objectCount > this.ObjectLimit)
                        {
                            throw new InvalidOperationException($"Attempt to create more than {this.ObjectLimit} objects.");
                        }

                        var result = constructorDelegate(ctorParameters.Select(ca => ca(localSources)).ToArray());

                        localSources.Add(result);

                        this.createdObjects.Add(result);

                        this.SetProperties(result, objectType, localSources, false);

                        localSources.RemoveAt(localSources.Count - 1);

                        postCreate?.Invoke(result);

                        if (handleSingleton)
                        {
                            this.singletons[objectType] = result;
                        }

                        return result;
                    };
                }

                this.constructorCache.Add(objectType, constructor);
            }

            return constructor(sources);
        }

        /// <summary>
        /// Gets an existing object of the given type from the sources list.
        /// </summary>
        /// <param name="sources">The list of existing object.</param>
        /// <param name="sourceType">The source type.</param>
        /// <returns>An exisiting object or null.</returns>
        private object GetSource(IList<object> sources, Type sourceType)
        {
            if (sources.Count > 1)
            {
                var source = sources[sources.Count - 1];
                if (source.GetType() == sourceType)
                {
                    return source;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets properties for the given input object, with the specified type.
        /// </summary>
        /// <param name="inputObject">The input object.</param>
        /// <param name="inputType">The type of the input object.</param>
        /// <param name="inputSources">The input object sources.</param>
        /// <param name="inputSingleton">Determines if the input object is a singleton.</param>
        private void SetProperties(object inputObject, Type inputType, IList<object> inputSources, bool inputSingleton)
        {
            if (!this.methodPropertyCache.TryGetValue(inputType, out var methods))
            {
                methods = new List<Action<object, IList<object>, bool>>();

                var referenceProperties = new List<PropertyInfo>();
                var propertyPlacement = new Dictionary<PropertyInfo, int>();

                foreach (var p in inputType.GetProperties())
                {
                    var property = p;
                    var propertyType = property.PropertyType;

                    if (this.structure.WithoutProperty.Contains(property) || this.structure.WithoutType.Contains(propertyType))
                    {
                        // add nothing to methods.
                        continue;
                    }

                    if (this.structure.CustomPropertySetters.TryGetValue(property, out Func<object, object> valueFunc))
                    {
                        propertyPlacement.Add(property, methods.Count);
                        var setterDelegate = inputType.DelegateForSetPropertyValue(property.Name);
                        methods.Add((currentObject, _, currentSingleton) =>
                        {
                            if (!currentSingleton)
                            {
                                setterDelegate(currentObject, valueFunc(currentObject));
                            }
                        });

                        continue;
                    }

                    var setter = this.ValueCreator(propertyType);
                    if (setter != null)
                    {
                        propertyPlacement.Add(property, methods.Count);
                        var setterDelegate = inputType.DelegateForSetPropertyValue(property.Name);
                        methods.Add((currentObject, _, currentSingleton) =>
                        {
                            if (!currentSingleton)
                            {
                                setterDelegate(currentObject, setter(property.Name));
                            }
                        });
                    }
                    else
                    {
                        referenceProperties.Add(property);
                    }
                }

                for (var pass = 1; pass <= 2; ++pass)
                {
                    // Pass 1, update pk id for 1:1 relation.
                    // Pass 2, the rest.
                    foreach (var p in referenceProperties)
                    {
                        var property = p;
                        var propertyType = property.PropertyType;
                        var noSources = this.structure.WithoutAncestryForType.Contains(inputType) || this.structure.WithoutAncestryForProperty.Contains(property);

                        if (propertyType.IsGenericType)
                        {
                            if (propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                            {
                                if (pass != 2)
                                {
                                    continue;
                                }

                                var elementType = propertyType.GetGenericArguments()[0];
                                var collectionType = typeof(HashSet<>).MakeGenericType(elementType);
                                var hashSetAdder = collectionType.DelegateForCallMethod(nameof(HashSet<int>.Add), elementType);
                                var hashSetCreator = collectionType.DelegateForCreateInstance();
                                var collectionSetter = inputType.DelegateForSetPropertyValue(property.Name);
                                var collectionGetter = inputType.DelegateForGetPropertyValue(property.Name);
                                var foreignKeyNullableGetDelegates = this.structure.Relations.GetForeignKeys(elementType, inputType)?.Where(fkp => Nullable.GetUnderlyingType(fkp.PropertyType) != null).Select(fkp => elementType.DelegateForGetPropertyValue(fkp.Name)).ToList();

                                methods.Add((currentObject, currentSources, currentSingleton) =>
                                {
                                    var collection = collectionGetter(currentObject);
                                    var adder = hashSetAdder;
                                    if (collection == null)
                                    {
                                        collection = hashSetCreator();
                                        collectionSetter(currentObject, collection);
                                    }
                                    else
                                    {
                                        var existingCollectionType = collection.GetType();

                                        if (existingCollectionType != collectionType)
                                        {
                                            adder = collection.GetType().DelegateForCallMethod("Add", elementType);
                                        }
                                    }

                                    var source = noSources ? null : GetSource(currentSources, elementType);

                                    if (source != null)
                                    {
                                        if (foreignKeyNullableGetDelegates == null || foreignKeyNullableGetDelegates.Count == 0 || foreignKeyNullableGetDelegates.All(fkgd => fkgd.Invoke(source) != null))
                                        {
                                            adder(collection, source);
                                        }
                                    }
                                    else
                                    {
                                        if (currentSingleton)
                                        {
                                            return;
                                        }

                                        if (!this.structure.Include.TryGetValue(property, out int elementCount))
                                        {
                                            elementCount = inputSources.Count == 1 ? this.RootCollectionMembers : this.NonRootCollectionMembers;
                                        }

                                        for (var i = 0; i < elementCount; ++i)
                                        {
                                            adder(collection, CreateObject(elementType, currentSources));
                                        }
                                    }
                                });
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
                            var foreignKeyProps = this.structure.Relations.GetForeignKeys(inputType, propertyType);
                            var primaryKeyProps = this.structure.Relations.GetPrimaryKeys(inputType);

                            if (foreignKeyProps == null)
                            {
                                throw new InvalidOperationException($@"Unable to determine foreign keys from '{inputType.Name}' to '{propertyType.Name}'.");
                            }

                            if (primaryKeyProps == null)
                            {
                                throw new InvalidOperationException($@"Unable to determine primary keys for '{inputType.Name}'.");
                            }

                            // Pass 1, only handle the case where the foreign key props and the primary key props are equal.
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

                            var primaryKeysOfForeignObject = this.structure.Relations.GetPrimaryKeys(propertyType);

                            if (primaryKeysOfForeignObject == null)
                            {
                                throw new InvalidOperationException($@"Unable to determine primary keys for '{propertyType.Name}'.");
                            }

                            var foreignKeySetDelegates = foreignKeyProps.Select(fkp => inputType.DelegateForSetPropertyValue(fkp.Name)).ToList();
                            var primaryKeyGetDelegates = primaryKeyProps.Select(pkp => propertyType.DelegateForGetPropertyValue(pkp.Name)).ToList();
                            var foreignObjectGetter = inputType.DelegateForGetPropertyValue(property.Name);
                            var foreignObjectSetter = inputType.DelegateForSetPropertyValue(property.Name);

                            var foreignKeyNullableGetDelegates = foreignKeyProps.Where(fkp => Nullable.GetUnderlyingType(fkp.PropertyType) != null).Select(fkp => inputType.DelegateForGetPropertyValue(fkp.Name)).ToList();

                            methods.Add((currentObject, currentSources, currentSingleton) =>
                            {
                                // Handle nullable
                                object foreignObject = null;

                                if (foreignKeyNullableGetDelegates.Count == 0 || foreignKeyNullableGetDelegates.All(fkgd => fkgd.Invoke(currentObject) != null))
                                {
                                    foreignObject = (noSources ? null : GetSource(currentSources, propertyType)) ?? this.CreateObject(propertyType, currentSources);
                                }

                                if (currentSingleton)
                                {
                                    var existing = foreignObjectGetter(currentObject);

                                    if (!ReferenceEquals(foreignObject, existing) && existing != null)
                                    {
                                        throw new InvalidOperationException($"Ambiguous property for singleton {inputType.Name}.{p.Name}.");
                                    }
                                }

                                if (foreignObject != null)
                                {
                                    // Set foreign keys to primary keys of related object.
                                    for (var i = 0; i < foreignKeyProps.Length; ++i)
                                    {
                                        foreignKeySetDelegates[i](currentObject, primaryKeyGetDelegates[i](foreignObject));
                                    }

                                    foreignObjectSetter(currentObject, foreignObject);
                                }
                            });

                            if (foreignKeyNullableGetDelegates.Count == 0)
                            {
                                foreach (var foreignKeyProp in foreignKeyProps)
                                {
                                    if (propertyPlacement.TryGetValue(foreignKeyProp, out var methodIndex))
                                    {
                                        methods[methodIndex] = null;
                                    }
                                }
                            }
                        }
                    }
                }

                methods = methods.Where(method => method != null).ToList();

                this.methodPropertyCache.Add(inputType, methods);
            }

            foreach (var method in methods)
            {
                method(inputObject, inputSources, inputSingleton);
            }
        }

        /// <summary>
        /// Returns a value creating delegate for the given type.
        /// </summary>
        /// <param name="t">The type for which to create values.</param>
        /// <returns></returns>
        private Func<string, object> ValueCreator(Type t)
        {
            if (this.structure.CustomConstructors.TryGetValue(t, out var creator))
            {
                return s => creator(s);
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                t = Nullable.GetUnderlyingType(t);
                var inner = this.ValueCreator(t);
                if (inner == null)
                {
                    return null;
                }

                return s =>
                {
                    if (this.random.Next(0, 2) == 0)
                    {
                        return null;
                    }
                    else
                    {
                        return inner(s);
                    }
                };
            }

            if (t.IsEnum)
            {
                var values = Enum.GetValues(t);
                return _ => values.GetValue(this.random.Next(0, values.Length));
            }
            else if (t == typeof(string))
            {
                return s =>
                {
                    var rnd = new byte[21];
                    this.random.NextBytes(rnd);
                    return s + Convert.ToBase64String(rnd);
                };
            }
            else if (t == typeof(byte))
            {
                return _ => (byte)this.random.Next(byte.MinValue, byte.MaxValue + 1);
            }
            else if (t == typeof(short))
            {
                return _ => (short)this.random.Next(short.MinValue, short.MaxValue + 1);
            }
            else if (t == typeof(ushort))
            {
                return _ => (ushort)this.random.Next(ushort.MinValue, ushort.MaxValue + 1);
            }
            else if (t == typeof(int))
            {
                return _ => this.random.Next(int.MinValue, int.MaxValue); // TODO capped at maxValue - 1
            }
            else if (t == typeof(uint))
            {
                return _ => (uint)this.random.Next(int.MinValue, int.MaxValue); // TODO capped at maxValue - 1
            }
            else if (t == typeof(long))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.random.NextBytes(rnd);
                    return BitConverter.ToInt64(rnd, 0);
                };
            }
            else if (t == typeof(ulong))
            {
                return _ =>
                {
                    var rnd = new byte[8];
                    this.random.NextBytes(rnd);
                    return BitConverter.ToUInt64(rnd, 0);
                };
            }
            else if (t == typeof(double))
            {
                return _ => (this.random.NextDouble() - 0.5) * double.MaxValue;
            }
            else if (t == typeof(float))
            {
                return _ => (float)(this.random.NextDouble() - 0.5) * float.MaxValue;
            }
            else if (t == typeof(decimal))
            {
                return _ => (decimal)(this.random.NextDouble() - 0.5) * 1e2m; // TODO look into overflows?
            }
            else if (t == typeof(bool))
            {
                return _ => this.random.Next(0, 2) == 1;
            }
            else if (t == typeof(Guid))
            {
                return _ =>
                {
                    var rnd = new byte[16];
                    this.random.NextBytes(rnd);
                    return new Guid(rnd);
                };
            }
            else if (t == typeof(DateTime))
            {
                return _ => DateTime.Now + TimeSpan.FromMilliseconds((this.random.NextDouble() - 0.5) * 62e9);
            }
            else if (t == typeof(DateTimeOffset))
            {
                return _ => DateTimeOffset.Now + TimeSpan.FromMilliseconds((this.random.NextDouble() - 0.5) * 62e9);
            }

            return null;
        }
    }
}
