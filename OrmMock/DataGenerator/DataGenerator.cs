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

using OrmMock.Shared;

namespace OrmMock.DataGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Defines an object context containing the created object.
    /// </summary>
    public class DataGenerator
    {
        /// <summary>
        /// Holds the number of objects generated.
        /// </summary>
        private int objectCount;

        /// <summary>
        /// Holds the list of created objects.
        /// </summary>
        private readonly List<object> createdObjects = new List<object>();

        /// <summary>
        /// Holds the created singletons.
        /// </summary>
        private readonly Dictionary<Type, object> singletons = new Dictionary<Type, object>();

        /// <summary>
        /// Holds the cache of computed constructors and setters.
        /// </summary>
        private readonly Cache cache = new Cache();

        /// <summary>
        /// Holds the object logging chain. Only used if logging is enabled.
        /// </summary>
        private readonly IList<IList<object>> loggingChain = new List<IList<object>>();

        /// <summary>
        /// Holds the customization data.
        /// </summary>
        private readonly Customization customization;

        /// <summary>
        /// Holds the value creator used by this instance.
        /// </summary>
        private readonly ValueCreator valueCreator = new ValueCreator();

        /// <summary>
        /// Holds the reflection interface.
        /// </summary>
        private readonly IReflection reflection;

        /// <summary>
        /// Gets or sets the limit of how many object to create in one pass.
        /// </summary>
        public int ObjectLimit { get; set; } = 1000;

        /// <summary>
        /// Gets or sets the limit of how deep the object hierarchy can be.
        /// </summary>
        public int RecursionLimit { get; set; } = 100;

        /// <summary>
        /// Gets or sets the number of items to create in root level object collections.
        /// </summary>
        public int RootCollectionMembers { get; set; } = 3;

        /// <summary>
        /// Gets or sets the number of items to create in leaf level object collections.
        /// </summary>
        public int NonRootCollectionMembers { get; set; } = 0;

        /// <summary>
        /// Gets or sets the default look-back value.
        /// </summary>
        public int DefaultLookback { get; set; } = 1;

        /// <summary>
        /// Gets or sets a value determining if logging of object creation should be enabled.
        /// </summary>
        public bool Logging { get; set; }

        /// <summary>
        /// Holds the relations between objects.
        /// </summary>
        public Relations Relations { get; }

        public DataGenerator()
        {
            this.customization = new Customization();
            this.Relations = new Relations();
            this.reflection = new FasterflectReflection();
        }

        private DataGenerator(Customization customization, Relations relations, IReflection reflection)
        {
            this.customization = customization;
            this.Relations = relations;
            this.reflection = reflection;
        }

        public DataGenerator WithoutRelations()
        {
            this.Relations.DefaultPrimaryKey = _ => new PropertyInfo[0];
            this.Relations.DefaultForeignKey = (_, __) => new PropertyInfo[0];

            return this;
        }

        /// <summary>
        /// Registers a specific object type to be a singleton of a specific value
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="value">The singleton value to register.</param>
        /// <returns>The generator.</returns>
        public DataGenerator Singleton<T>(T value)
        {
            this.singletons.Add(typeof(T), value);

            return this;
        }

        /// <summary>
        /// Gets the for context for the given type.
        /// </summary>
        /// <typeparam name="T">The type of the for context.</typeparam>
        /// <returns>A typed for context.</returns>
        public ForTypeContext<T> For<T>()
        {
            return new ForTypeContext<T>(this, this.customization);
        }

        /// <summary>
        /// Forks the current object context.
        /// </summary>
        /// <returns>A typed build context.</returns>
        public ForTypeContext<T> Build<T>()
        {
            return new DataGenerator(new Customization(this.customization), this.Relations, this.reflection).For<T>();
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
        /// Gets the object of the specified type..
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The object at the given index.</returns>
        public T GetObject<T>()
        {
            return (T)this.createdObjects.Single(o => o.GetType() == typeof(T));
        }

        /// <summary>
        /// Gets the object of the specified type at the given index.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="index">The index of the object to get.</param>
        /// <returns>The object at the given index.</returns>
        public T GetObject<T>(int index)
        {
            return this.GetObjects<T>().Skip(index).First();
        }

        /// <summary>
        /// Gets the object of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The objects.</returns>
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
            return (T)this.Create(typeof(T));
        }

        /// <summary>
        /// Creates an object of the given type.
        /// </summary>
        /// <param name="t">The type of the object.</param>
        /// <returns>The created object.</returns>
        public object Create(Type t)
        {
            return CreateObject(t, new List<object>());
        }

        /// <summary>
        /// Creates many objects of the given type.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <returns>The created objects.</returns>
        public IEnumerable<T> CreateMany<T>(int create = 3)
        {
            while (create-- > 0)
            {
                yield return this.Create<T>();
            }
        }

        /// <summary>
        /// Creates an object of type t, considering the sources for references.
        /// </summary>
        /// <param name="objectType">The type of the object to create.</param>
        /// <param name="sourceObjects">The sources.</param>
        /// <returns></returns>
        private object CreateObject(Type objectType, IList<object> sourceObjects)
        {
            if (this.Logging && sourceObjects.Count == 0)
            {
                this.loggingChain.Clear();
            }

            if (!this.cache.ConstructorCache.TryGetValue(objectType, out var constructor))
            {
                var postCreate = this.customization.GetPostCreateAction(objectType);

                var simpleCreator = this.GetValueCreator(objectType);

                if (simpleCreator != null)
                {
                    constructor = _ =>
                    {
                        var result = simpleCreator(string.Empty);
                        postCreate?.Invoke(result);
                        return result;
                    };
                }
                else
                {
                    var constructorDelegate = this.reflection.Constructor(objectType);

                    constructor = localSources =>
                    {
                        if (localSources.Count >= this.RecursionLimit)
                        {
                            throw new InvalidOperationException($@"Recursion limit of {this.RecursionLimit} exceeded.");
                        }

                        var handleSingleton = this.customization.ShouldBeSingleton(objectType) || this.singletons.ContainsKey(objectType);

                        if (handleSingleton && this.singletons.TryGetValue(objectType, out object singleton))
                        {
                            // Register possible back references in singleton.
                            this.SetProperties(singleton, localSources, objectType, true);

                            return singleton;
                        }

                        if (++this.objectCount > this.ObjectLimit)
                        {
                            throw new InvalidOperationException($"Attempt to create more than {this.ObjectLimit} objects.");
                        }

                        var result = constructorDelegate();

                        this.createdObjects.Add(result);

                        this.SetProperties(result, localSources, objectType, false);

                        if (handleSingleton)
                        {
                            this.singletons[objectType] = result;
                        }

                        if (this.Logging)
                        {
                            this.loggingChain.Add(localSources.Concat(new[] { result }).ToList());
                        }

                        postCreate?.Invoke(result);

                        return result;
                    };
                }

                this.cache.ConstructorCache.Add(objectType, constructor);
            }

            var newObject = constructor(sourceObjects);

            if (this.Logging && sourceObjects.Count == 0)
            {
                foreach (var chain in this.loggingChain.Reverse())
                {
                    var obj = chain.Last();
                    var pkstr = string.Join(", ", this.Relations.GetPrimaryKeys(obj.GetType()).Select(k => k.GetMethod.Invoke(obj, new object[0]).ToString()));

                    var diag = $"{new string(' ', 4 * (chain.Count - 1))}{obj.GetType().Name} {pkstr}";
                    Console.WriteLine(diag);
                }
            }

            return newObject;
        }

        /// <summary>
        /// Gets an existing object of the given type from the sources list.
        /// </summary>
        /// <param name="sources">The existing object.</param>
        /// <param name="sourceType">The source type.</param>
        /// <param name="maxLevel">The maximum level.</param>
        /// <returns>An existing object or null.</returns>
        private object GetSource(IList<object> sources, Type sourceType, int maxLevel)
        {
            for (var i = sources.Count - 1; i >= 0 && maxLevel > 0; --i, --maxLevel)
            {
                if (sources[i].GetType() == sourceType)
                {
                    return sources[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Sets properties for the given input object, with the specified type.
        /// </summary>
        /// <param name="inputObject">The input object.</param>
        /// <param name="sourceObjects">The source object.</param>
        /// <param name="inputType">The type of the input object.</param>
        /// <param name="inputSingleton">Determines if the input object is a singleton.</param>
        private void SetProperties(object inputObject, IList<object> sourceObjects, Type inputType, bool inputSingleton)
        {
            if (!this.cache.MethodPropertyCache.TryGetValue(inputType, out var methods))
            {
                methods = new List<Action<object, IList<object>, bool>>();

                var allProperties = ReflectionUtility.GetPublicPropertiesWithGettersAndSetters(inputType);
                var pkProperties = this.Relations.GetPrimaryKeys(inputType);
                var pkPropertiesDict = new HashSet<PropertyInfo>(pkProperties);
                var fkDict = allProperties.Where(p => ReflectionUtility.IsNonGenericReferenceType(p.PropertyType)).ToDictionary(p => p, p => this.Relations.GetForeignKeys(inputType, p.PropertyType));
                var first11Property = fkDict.SingleOrDefault(kvp => kvp.Value.SequenceEqual(pkProperties)).Key;
                var fkPropertiesDict = new HashSet<PropertyInfo>(fkDict.Values.SelectMany(v => v));
                var propertiesToSet = allProperties.Where(p => !this.customization.ShouldSkip(p)).Where(p => pkPropertiesDict.Contains(p) || !fkPropertiesDict.Contains(p)).ToList(); // Remove foreign keys (keep primary keys)

                var creatorDict = propertiesToSet.ToDictionary(p => p, p =>
                {
                    var valueFunc = this.customization.GetPropertyConstructor(p);
                    if (valueFunc == null)
                    {
                        var localValueCreator = this.valueCreator.Get(p.PropertyType);
                        if (localValueCreator != null)
                        {
                            valueFunc = _ => localValueCreator(p.Name);
                        }
                    }

                    return valueFunc;
                });

                // Set plain properties (without references)
                foreach (var property in propertiesToSet.Where(p => ReflectionUtility.IsValueTypeOrNullableOrString(p.PropertyType)))
                {
                    if (creatorDict.TryGetValue(property, out var valueFunc))
                    {
                        var setter = this.reflection.Setter(property);
                        var postCreate = this.customization.GetPostCreateAction(property);
                        methods.Add((currentObject, _, currentSingleton) =>
                        {
                            if (!currentSingleton)
                            {
                                setter(currentObject, valueFunc(this));
                                postCreate?.Invoke(currentObject);
                            }
                        });
                    }
                    // Else nothing to set
                }

                // Has any 1:1 inbound? If so, set primary keys of current object to primary keys of foreign (inbound) object.
                if (first11Property != null)
                {
                    var pkSetter = this.reflection.KeySetter(pkProperties);
                    var pkGetterOfForeignObject = this.reflection.PrimaryKeyGetter(this.Relations, first11Property.PropertyType);

                    methods.Add((currentObject, currentSources, currentSingleton) =>
                    {
                        var source = GetSource(currentSources, first11Property.PropertyType, 1);
                        if (source != null)
                        {
                            pkSetter(currentObject, pkGetterOfForeignObject(source));
                        }
                    });
                }

                foreach (var property in propertiesToSet.Where(p => !ReflectionUtility.IsValueTypeOrNullableOrString(p.PropertyType)))
                {
                    var propertyType = property.PropertyType;
                    creatorDict.TryGetValue(property, out var propertyValueCreator);
                    var lookBackCount = this.customization.GetLookBackCount(property, DefaultLookback);
                    var postCreate = this.customization.GetPostCreateAction(property);

                    if (ReflectionUtility.IsCollectionType(propertyType))
                    {
                        var elementType = propertyType.GetGenericArguments()[0];
                        var adder = this.reflection.CollectionAdder(property, typeof(HashSet<>)); // will fail if generic type is not based on ICollection<>

                        methods.Add((currentObject, currentSources, currentSingleton) =>
                        {
                            var source = GetSource(currentSources, elementType, lookBackCount);

                            if (source != null)
                            {
                                adder(currentObject, new[] { source });
                            }
                            else
                            {
                                if (currentSingleton)
                                {
                                    return; // The current value is an already existing singleton. Do not change any of its values.
                                }

                                var includeCount = this.customization.GetIncludeCount(property, currentSources.Count == 0 ? this.RootCollectionMembers : this.NonRootCollectionMembers);

                                currentSources.Add(currentObject);

                                for (var i = 0; i < includeCount; ++i)
                                {
                                    adder(currentObject, new[] { CreateObject(elementType, currentSources) });
                                }

                                currentSources.RemoveAt(currentSources.Count - 1);
                            }

                            postCreate?.Invoke(currentObject);
                        });
                    }
                    else if (ReflectionUtility.IsNonGenericReferenceType(propertyType))
                    {
                        // Going from t to pt
                        // Note that primary keys and foreign keys may be equal.
                        fkDict.TryGetValue(property, out var foreignKeyProps);
                        foreignKeyProps = foreignKeyProps ?? new PropertyInfo[0];

                        var pkFkEqual = foreignKeyProps.SequenceEqual(pkProperties);

                        var isNullable = foreignKeyProps.Any(p => ReflectionUtility.IsNullableOrString(p.PropertyType)) && this.customization.GetIncludeCount(property, 0) == 0;
                        Func<bool> determineNullable = null;
                        if (isNullable)
                        {
                            determineNullable = () => (bool)this.valueCreator.Get(typeof(bool))(string.Empty);
                        }
                        var propertyGetter = this.reflection.Getter(property);
                        var propertySetter = this.reflection.Setter(property);
                        var foreignKeySetter = this.reflection.KeySetter(foreignKeyProps);
                        var primaryKeyGetterOfForeignObject = this.reflection.PrimaryKeyGetter(this.Relations, propertyType);
                        methods.Add((currentObject, currentSources, currentSingleton) =>
                        {
                            var foreignObject = GetSource(currentSources, propertyType, lookBackCount);

                            if (foreignObject == null)
                            {
                                if (propertyValueCreator != null)
                                {
                                    foreignObject = propertyValueCreator(this);
                                }
                                else
                                {
                                    if (determineNullable == null || determineNullable())
                                    {
                                        currentSources.Add(currentObject);

                                        foreignObject = this.CreateObject(propertyType, currentSources);

                                        currentSources.RemoveAt(currentSources.Count - 1);
                                    }
                                }
                            }

                            if (currentSingleton)
                            {
                                var existing = propertyGetter(currentObject);

                                if (!ReferenceEquals(foreignObject, existing) && existing != null)
                                {
                                    throw new InvalidOperationException($"Ambiguous property for singleton {inputType.Name}.{property.Name}.");
                                }
                            }

                            if (foreignObject != null)
                            {
                                if (!pkFkEqual)
                                {
                                    // pkFkEqual is handled in the 1:1 step above.
                                    foreignKeySetter(currentObject, primaryKeyGetterOfForeignObject(foreignObject));
                                }

                                propertySetter(currentObject, foreignObject);
                            }
                            // Else should be nullable. Leave fk and property default.

                            postCreate?.Invoke(currentObject);
                        });
                    }
                    else
                    {
                        throw new InvalidOperationException($@"Unsupported property type {propertyType.Name}.");
                    }
                }

                this.cache.MethodPropertyCache.Add(inputType, methods);
            }

            foreach (var method in methods)
            {
                method(inputObject, sourceObjects, inputSingleton);
            }
        }

        /// <summary>
        /// Returns a value creating delegate for the given type.
        /// </summary>
        /// <param name="t">The type for which to create values.</param>
        /// <returns></returns>
        private Func<string, object> GetValueCreator(Type t)
        {
            var constructor = this.customization.GetCustomConstructor(t);

            if (constructor != null)
            {
                return s => constructor(this, s);
            }

            return this.valueCreator.Get(t);
        }

        /// <summary>
        /// Holds the cache of constructors and object setters.
        /// </summary>
        private class Cache
        {
            /// <summary>
            /// Holds the method property cache.
            /// </summary>
            public readonly Dictionary<Type, List<Action<object, IList<object>, bool>>> MethodPropertyCache = new Dictionary<Type, List<Action<object, IList<object>, bool>>>();

            /// <summary>
            /// Holds the constructor cache for the given type.
            /// </summary>
            public readonly Dictionary<Type, Func<IList<object>, object>> ConstructorCache = new Dictionary<Type, Func<IList<object>, object>>();
        }
    }
}
