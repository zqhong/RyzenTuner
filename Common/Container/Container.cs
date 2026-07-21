// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Refer: https://github.com/microsoft/MinIoC/blob/main/Container.cs

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace RyzenTuner.Common.Container
{
    /// <summary>
    /// Inversion of control container handles dependency injection for registered types
    /// </summary>
    public class Container : Container.IScope
    {
        #region Public interfaces

        /// <summary>
        /// Represents a scope in which per-scope objects are instantiated a single time
        /// </summary>
        public interface IScope : IDisposable, IServiceProvider
        {
        }

        /// <summary>
        /// IRegisteredType is return by Container.Register and allows further configuration for the registration
        /// </summary>
        public interface IRegisteredType
        {
            /// <summary>
            /// Make registered type a singleton
            /// </summary>
            void AsSingleton();
        }

        #endregion

        // Map of registered types
        private readonly Dictionary<Type, Func<ILifetime, object>> _registeredTypes = new();

        // Lifetime management
        private readonly ContainerLifetime _lifetime;

        /// <summary>
        /// Creates a new instance of IoC Container
        /// </summary>
        public Container()
        {
            _lifetime = new ContainerLifetime(t =>
                _registeredTypes.TryGetValue(t, out var factory) ? factory : null!);
        }

        /// <summary>
        /// Registers a factory function which will be called to resolve the specified interface
        /// </summary>
        /// <param name="interface">Interface to register</param>
        /// <param name="factory">Factory function</param>
        /// <returns></returns>
        public IRegisteredType Register(Type @interface, Func<object> factory)
        {
            if (@interface == null) throw new ArgumentNullException(nameof(@interface));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return RegisterType(@interface, _ => factory());
        }

        /// <summary>
        /// Registers an implementation type for the specified interface
        /// </summary>
        /// <param name="interface">Interface to register</param>
        /// <param name="implementation">Implementing type</param>
        /// <returns></returns>
        public IRegisteredType Register(Type @interface, Type implementation)
        {
            if (@interface == null) throw new ArgumentNullException(nameof(@interface));
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            return RegisterType(@interface, FactoryFromType(implementation));
        }

        private IRegisteredType RegisterType(Type itemType, Func<ILifetime, object> factory)
        {
            _registeredTypes[itemType] = factory;
            return new RegisteredType(itemType, f => _registeredTypes[itemType] = f, factory);
        }

        /// <summary>
        /// Returns the object registered for the given type, if registered
        /// </summary>
        /// <param name="type">Type as registered with the container</param>
        /// <returns>Instance of the registered type, if registered; otherwise <see langword="null"/></returns>
        public object GetService(Type type)
        {
            if (!_registeredTypes.TryGetValue(type, out var registeredType))
            {
                // Returning null follows IServiceProvider convention for unregistered types;
                // null! suppresses nullable warning to keep callers that assert non-null happy.
                return null!;
            }

            return registeredType(_lifetime);
        }

        /// <summary>
        /// Creates a new scope
        /// </summary>
        /// <returns>Scope object</returns>
        public IScope CreateScope() => new ScopeLifetime(_lifetime);

        /// <summary>
        /// Disposes any <see cref="IDisposable"/> objects owned by this container.
        /// </summary>
        public void Dispose() => _lifetime.Dispose();

        #region Lifetime management

        // ILifetime management adds resolution strategies to an IScope
        private interface ILifetime : IScope
        {
            object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory);
        }

        // ObjectCache provides common caching logic for lifetimes
        private abstract class ObjectCache
        {
            // Use Lazy<object> to ensure factory is called at most once per key under contention
            private readonly ConcurrentDictionary<Type, Lazy<object>> _instanceCache = new();

            // Get from cache or create and cache object
            protected object GetCached(Type type, Func<ILifetime, object> factory, ILifetime lifetime)
                => _instanceCache.GetOrAdd(type, _ => new Lazy<object>(() => factory(lifetime))).Value;

            public void Dispose()
            {
                // Snapshot to avoid collection-modified exception from concurrent resolution
                var exceptions = new List<Exception>();

                foreach (var kvp in _instanceCache.ToArray())
                {
                    if (kvp.Value.IsValueCreated && kvp.Value.Value is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            exceptions.Add(ex);
                        }
                    }
                }

                _instanceCache.Clear();

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(
                        "One or more disposable services threw exceptions during disposal.", exceptions);
                }
            }
        }

        // Container lifetime management
        private class ContainerLifetime : ObjectCache, ILifetime
        {
            // Retrieves the factory function from the given type, provided by owning container
            public Func<Type, Func<ILifetime, object>> GetFactory { get; }

            public ContainerLifetime(Func<Type, Func<ILifetime, object>> getFactory) => GetFactory = getFactory;

            public object GetService(Type type)
            {
                var factory = GetFactory(type);
                if (factory == null)
                {
                    throw new InvalidOperationException(
                        $"Type \"{type.FullName}\" is not registered in the container.");
                }

                return factory(this);
            }

            // Singletons get cached per container
            public object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory)
                => GetCached(type, factory, this);

        }

        // Per-scope lifetime management
        private class ScopeLifetime : ObjectCache, ILifetime
        {
            // Singletons come from parent container's lifetime
            private readonly ContainerLifetime _parentLifetime;

            public ScopeLifetime(ContainerLifetime parentContainer) => _parentLifetime = parentContainer;

            public object GetService(Type type)
            {
                var factory = _parentLifetime.GetFactory(type);
                if (factory == null)
                {
                    throw new InvalidOperationException(
                        $"Type \"{type.FullName}\" is not registered in the container.");
                }

                return factory(this);
            }

            // Singleton resolution is delegated to parent lifetime
            public object GetServiceAsSingleton(Type type, Func<ILifetime, object> factory)
                => _parentLifetime.GetServiceAsSingleton(type, factory);

        }

        #endregion

        #region Container items

        // Compiles a lambda that calls the given type's greediest constructor resolving arguments
        private static Func<ILifetime, object> FactoryFromType(Type itemType)
        {
            // Get all constructors (public and non-public)
            var constructors = itemType.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {itemType.FullName} has no constructors.");
            }

            // Pick the constructor with the most parameters (greediest)
            var constructor = constructors.OrderByDescending(c => c.GetParameters().Length).First();

            // Compile constructor call as a lambda expression
            var arg = Expression.Parameter(typeof(ILifetime));
            return (Func<ILifetime, object>)Expression.Lambda(
                Expression.New(constructor, constructor.GetParameters().Select(
                    param =>
                    {
                        var resolve = new Func<ILifetime, object>(
                            lifetime => lifetime.GetService(param.ParameterType));
                        return Expression.Convert(
                            Expression.Call(Expression.Constant(resolve.Target), resolve.Method, arg),
                            param.ParameterType);
                    })),
                arg).Compile();
        }

        // RegisteredType is supposed to be a short lived object tying an item to its container
        // and allowing users to mark it as a singleton or per-scope item
        private class RegisteredType : IRegisteredType
        {
            private readonly Type _itemType;
            private readonly Action<Func<ILifetime, object>> _registerFactory;
            private readonly Func<ILifetime, object> _factory;

            public RegisteredType(Type itemType, Action<Func<ILifetime, object>> registerFactory,
                Func<ILifetime, object> factory)
            {
                _itemType = itemType;
                _registerFactory = registerFactory;
                _factory = factory;
            }

            public void AsSingleton()
                => _registerFactory(lifetime => lifetime.GetServiceAsSingleton(_itemType, _factory));
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for Container
    /// </summary>
    internal static class ContainerExtensions
    {
        /// <summary>
        /// Registers a factory function which will be called to resolve the specified interface
        /// </summary>
        /// <typeparam name="T">Interface to register</typeparam>
        /// <param name="container">This container instance</param>
        /// <param name="factory">Factory method</param>
        /// <returns>IRegisteredType object</returns>
        /// <remarks>The null-forgiving operator (!) on <c>factory()!</c> suppresses nullable
        /// warnings -- the caller's factory is expected to return a non-null instance for valid
        /// registrations.</remarks>
        public static Container.IRegisteredType Register<T>(this Container container, Func<T> factory)
            => container.Register(typeof(T), () => factory()!);

        /// <summary>
        /// Returns an implementation of the specified interface
        /// </summary>
        /// <typeparam name="T">Interface type</typeparam>
        /// <param name="scope">This scope instance</param>
        /// <returns>Object implementing the interface</returns>
        public static T Resolve<T>(this Container.IScope scope)
        {
            var service = scope.GetService(typeof(T));
            if (service == null)
            {
                throw new InvalidOperationException(
                    $"Type \"{typeof(T)}\" is not registered in the container.");
            }

            return (T)service;
        }
    }
}