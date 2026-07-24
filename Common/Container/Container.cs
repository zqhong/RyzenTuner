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
    public sealed class Container : Container.IScope
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
        private readonly ConcurrentDictionary<Type, Func<ILifetime, object>> _registeredTypes = new();

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
            if (!@interface.IsAssignableFrom(implementation))
                throw new ArgumentException($"Type '{implementation.FullName}' does not implement interface '{@interface.FullName}'.", nameof(implementation));
            return RegisterType(@interface, FactoryFromType(implementation));
        }

        private IRegisteredType RegisterType(Type itemType, Func<ILifetime, object> factory)
        {
            if (_lifetime.IsDisposed)
                throw new ObjectDisposedException(nameof(Container));

            if (!_registeredTypes.TryAdd(itemType, factory))
            {
                throw new InvalidOperationException(
                    $"Type \"{itemType.FullName}\" is already registered in the container.");
            }

            return new RegisteredType(itemType, f => _registeredTypes[itemType] = f, factory);
        }

        /// <summary>
        /// Returns the object registered for the given type, if registered
        /// </summary>
        /// <param name="type">Type as registered with the container</param>
        /// <returns>Instance of the registered type, if registered; otherwise <see langword="null"/></returns>
        public object? GetService(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (_lifetime.IsDisposed)
                throw new ObjectDisposedException(nameof(Container));

            if (!_registeredTypes.TryGetValue(type, out var registeredType))
            {
                // Returning null follows IServiceProvider convention for unregistered types.
                return null;
            }

            return registeredType(_lifetime);
        }

        /// <summary>
        /// Creates a new scope
        /// </summary>
        /// <returns>Scope object</returns>
        public IScope CreateScope()
        {
            if (_lifetime.IsDisposed)
                throw new ObjectDisposedException(nameof(Container));
            return new ScopeLifetime(_lifetime);
        }

        /// <summary>
        /// Disposes any <see cref="IDisposable"/> objects owned by this container.
        /// </summary>
        public void Dispose()
        {
            _lifetime.Dispose();
        }

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
            private readonly object _disposeLock = new();
            protected volatile bool _disposed;

            // Get from cache or create and cache object
            protected object GetCached(Type type, Func<ILifetime, object> factory, ILifetime lifetime)
            {
                if (_disposed)
                    throw new ObjectDisposedException(nameof(ObjectCache));

                Lazy<object> lazy;
                lock (_disposeLock)
                {
                    if (_disposed)
                        throw new ObjectDisposedException(nameof(ObjectCache));

                    lazy = _instanceCache.GetOrAdd(type, _ => new Lazy<object>(() =>
                    {
                        if (_disposed)
                            throw new ObjectDisposedException(nameof(ObjectCache));
                        return factory(lifetime);
                    }));
                }

                var instance = lazy.Value;

                lock (_disposeLock)
                {
                    // Container disposed while factory was executing. Dispose()
                    // may have already iterated the cache before this Lazy's
                    // IsValueCreated became true, leaving this instance undisposed.
                    // Dispose it now to prevent a resource leak.
                    if (_disposed)
                    {
                        if (instance is IDisposable disposable)
                            disposable.Dispose();
                        throw new ObjectDisposedException(nameof(ObjectCache));
                    }
                }

                return instance;
            }

            public void Dispose()
            {
                lock (_disposeLock)
                {
                    if (_disposed)
                        return;

                    _disposed = true;

                    foreach (var kvp in _instanceCache.ToArray())
                    {
                        if (!kvp.Value.IsValueCreated)
                            continue;

                        object instance;
                        try
                        {
                            instance = kvp.Value.Value;
                        }
                        catch
                        {
                            // Lazy<object> may be in a faulted state (e.g. factory threw due
                            // to disposal). Swallow so remaining entries are still disposed.
                            continue;
                        }

                        if (instance is IDisposable disposable)
                        {
                            try
                            {
                                disposable.Dispose();
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine(
                                    $"ObjectCache.Dispose: {ex}");
                            }
                        }
                    }

                    _instanceCache.Clear();
                }
            }
        }

        // Container lifetime management
        private class ContainerLifetime : ObjectCache, ILifetime
        {
            // Retrieves the factory function from the given type, provided by owning container
            public Func<Type, Func<ILifetime, object>?> GetFactory { get; }

            public ContainerLifetime(Func<Type, Func<ILifetime, object>?> getFactory)
            {
                if (getFactory == null) throw new ArgumentNullException(nameof(getFactory));
                GetFactory = getFactory;
            }

            public bool IsDisposed => _disposed;

            public object? GetService(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));

                if (_disposed)
                    throw new ObjectDisposedException(nameof(ContainerLifetime));

                var factory = GetFactory(type);
                if (factory == null)
                {
                    return null;
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

            public ScopeLifetime(ContainerLifetime parentLifetime)
            {
                _parentLifetime = parentLifetime ?? throw new ArgumentNullException(nameof(parentLifetime));
            }

            public object? GetService(Type type)
            {
                if (type == null) throw new ArgumentNullException(nameof(type));

                if (_disposed)
                    throw new ObjectDisposedException(nameof(ScopeLifetime));

                // If the parent container has been disposed, reject all resolution
                // attempts rather than silently succeeding for transient objects while
                // singletons throw (the singleton path checks _parentLifetime._disposed
                // via GetCached). Keeping behavior consistent avoids subtle bugs.
                if (_parentLifetime.IsDisposed)
                    throw new ObjectDisposedException(nameof(Container));

                var factory = _parentLifetime.GetFactory(type);
                if (factory == null)
                {
                    return null;
                }

                // Do not cache the result in the scope's own _instanceCache:
                // singleton instances belong to the parent container's cache and
                // would be prematurely disposed if the scope cached and later disposed them.
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
                BindingFlags.Instance | BindingFlags.Public);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Type {itemType.FullName} has no public constructors.");
            }

            // Pick the constructor with the most parameters (greediest)
            var maxParams = constructors.Max(c => c.GetParameters().Length);
            var candidates = constructors.Where(c => c.GetParameters().Length == maxParams).ToArray();

            if (candidates.Length > 1)
            {
                throw new InvalidOperationException(
                    $"Type {itemType.FullName} has multiple constructors with " +
                    $"{maxParams} parameters; " +
                    "the constructor cannot be determined automatically.");
            }

            var constructor = candidates[0];

            // Compile constructor call as a lambda expression
            var arg = Expression.Parameter(typeof(ILifetime));
            return (Func<ILifetime, object>)Expression.Lambda(
                Expression.New(constructor, constructor.GetParameters().Select(
                    param =>
                    {
                        var resolve = new Func<ILifetime, object>(lifetime =>
                        {
                            var service = lifetime.GetService(param.ParameterType);
                            if (service == null)
                                throw new InvalidOperationException(
                                    $"Cannot resolve parameter '{param.Name}' of type '{param.ParameterType}' for type '{itemType.FullName}'. Ensure the type is registered in the container.");
                            return service;
                        });
                        return Expression.Convert(
                            Expression.Invoke(Expression.Constant(resolve), arg),
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
        /// <exception cref="ArgumentNullException"><paramref name="container"/> or <paramref name="factory"/> is null.</exception>
        /// <remarks>The null-forgiving operator (!) on <c>factory()!</c> suppresses nullable
        /// warnings -- the caller's factory is expected to return a non-null instance for valid
        /// registrations.</remarks>
        public static Container.IRegisteredType Register<T>(this Container container, Func<T> factory)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            return container.Register(typeof(T), () => factory()!);
        }

        /// <summary>
        /// Returns an implementation of the specified interface
        /// </summary>
        /// <typeparam name="T">Interface type</typeparam>
        /// <param name="scope">This scope instance</param>
        /// <returns>Object implementing the interface</returns>
        /// <exception cref="ArgumentNullException"><paramref name="scope"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Type <typeparamref name="T"/> is not registered in the container.</exception>
        public static T Resolve<T>(this Container.IScope scope) where T : class
        {
            if (scope == null) throw new ArgumentNullException(nameof(scope));

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