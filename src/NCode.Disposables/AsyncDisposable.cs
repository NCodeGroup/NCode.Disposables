// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides factory methods for creating various <see cref="IAsyncDisposable"/> implementations,
/// including empty disposables, adapters, aggregates, shared references, collections, and action-based disposables.
/// </summary>
public static class AsyncDisposable
{
    /// <summary>
    /// Gets a singleton <see cref="AsyncDisposableEmpty"/> instance that performs
    /// no operation when <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <value>A singleton <see cref="AsyncDisposableEmpty"/> instance that does nothing on disposal.</value>
    public static AsyncDisposableEmpty Empty => AsyncDisposableEmpty.Singleton;

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAdapter"/> that wraps an existing <see cref="IDisposable"/> instance,
    /// enabling asynchronous disposal support. The adapter is idempotent by default.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    /// <returns>A new <see cref="AsyncDisposableAdapter"/> instance that wraps the specified disposable.</returns>
    public static AsyncDisposableAdapter Adapt(IDisposable disposable)
    {
        return new AsyncDisposableAdapter(disposable);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAdapter"/> that wraps an existing <see cref="IDisposable"/> instance,
    /// enabling asynchronous disposal support with configurable idempotency.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    /// <param name="idempotent">
    /// <see langword="true"/> to ensure multiple calls to <see cref="IAsyncDisposable.DisposeAsync"/>
    /// will only dispose the underlying instance once; <see langword="false"/> to allow multiple disposals.
    /// </param>
    /// <returns>A new <see cref="AsyncDisposableAdapter"/> instance that wraps the specified disposable.</returns>
    public static AsyncDisposableAdapter Adapt(IDisposable disposable, bool idempotent)
    {
        return new AsyncDisposableAdapter(disposable, idempotent);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAggregate"/> instance that wraps and manages
    /// another <see cref="IAsyncDisposable"/> instance. The aggregate allows the underlying
    /// disposable to be replaced or cleared during its lifetime.
    /// </summary>
    /// <param name="disposable">
    /// The initial <see cref="IAsyncDisposable"/> instance to aggregate, or <see langword="null"/> to create an empty aggregate.
    /// </param>
    /// <returns>A new <see cref="AsyncDisposableAggregate"/> instance containing the specified disposable.</returns>
    public static AsyncDisposableAggregate Aggregate(IAsyncDisposable? disposable)
    {
        return new AsyncDisposableAggregate(disposable);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> that wraps an <see cref="IAsyncDisposable"/> resource
    /// with reference counting semantics. The underlying resource is only disposed when all leases
    /// have been released (i.e., the reference count reaches zero).
    /// </summary>
    /// <typeparam name="T">The type of the disposable resource, which must implement <see cref="IAsyncDisposable"/>.</typeparam>
    /// <param name="value">The underlying <see cref="IAsyncDisposable"/> instance to share.</param>
    /// <returns>A new <see cref="AsyncSharedReferenceLease{T}"/> representing the initial lease on the shared resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static AsyncSharedReferenceLease<T> Shared<T>(T value)
        where T : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(value);

        return AsyncSharedReference.Create(value);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> that wraps a resource with reference counting semantics
    /// and a custom release callback. The callback is invoked when all leases have been released
    /// (i.e., the reference count reaches zero).
    /// </summary>
    /// <typeparam name="T">The type of the resource to share.</typeparam>
    /// <param name="value">The underlying value to share.</param>
    /// <param name="onRelease">The asynchronous callback to invoke when the last reference is released.</param>
    /// <returns>A new <see cref="AsyncSharedReferenceLease{T}"/> representing the initial lease on the shared resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="onRelease"/> is <see langword="null"/>.</exception>
    public static AsyncSharedReferenceLease<T> Shared<T>(T value, Func<T, ValueTask> onRelease)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onRelease);

        return AsyncSharedReference.Create(value, onRelease);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableCollection"/> that manages multiple <see cref="IAsyncDisposable"/>
    /// instances. All contained items are disposed when the collection is disposed.
    /// Items are disposed in reverse order of their addition.
    /// </summary>
    /// <param name="collection">The <see cref="IAsyncDisposable"/> items to add to the collection.</param>
    /// <returns>A new <see cref="AsyncDisposableCollection"/> containing the specified items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public static AsyncDisposableCollection Collection(params IAsyncDisposable[] collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return Collection(collection.AsEnumerable());
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableCollection"/> that manages multiple <see cref="IAsyncDisposable"/>
    /// instances with configurable exception handling. All contained items are disposed when the collection is disposed.
    /// Items are disposed in reverse order of their addition.
    /// </summary>
    /// <param name="ignoreExceptions">
    /// <see langword="true"/> to suppress exceptions thrown by individual items during disposal and continue disposing remaining items;
    /// <see langword="false"/> to propagate exceptions.
    /// </param>
    /// <param name="collection">The <see cref="IAsyncDisposable"/> items to add to the collection.</param>
    /// <returns>A new <see cref="AsyncDisposableCollection"/> containing the specified items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public static AsyncDisposableCollection Collection(
        bool ignoreExceptions,
        params IAsyncDisposable[] collection
    )
    {
        ArgumentNullException.ThrowIfNull(collection);

        return Collection(collection.AsEnumerable(), ignoreExceptions);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableCollection"/> that manages multiple <see cref="IAsyncDisposable"/>
    /// instances from an enumerable source. All contained items are disposed when the collection is disposed.
    /// Items are disposed in reverse order of their addition.
    /// </summary>
    /// <param name="collection">The enumerable collection of <see cref="IAsyncDisposable"/> items to add.</param>
    /// <param name="ignoreExceptions">
    /// <see langword="true"/> to suppress exceptions thrown by individual items during disposal and continue disposing remaining items;
    /// <see langword="false"/> (the default) to propagate exceptions.
    /// </param>
    /// <returns>A new <see cref="AsyncDisposableCollection"/> containing the specified items.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    // The parameter shape shipped in 6.x; 7.0 changes only the return type.
#pragma warning disable RS0027 // optional parameter on an overload with fewer parameters than another
    public static AsyncDisposableCollection Collection(
#pragma warning restore RS0027
        IEnumerable<IAsyncDisposable> collection,
        bool ignoreExceptions = false
    )
    {
        ArgumentNullException.ThrowIfNull(collection);

        return new AsyncDisposableCollection(collection, ignoreExceptions);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes the specified asynchronous action
    /// when <see cref="IAsyncDisposable.DisposeAsync"/> is called. The returned instance is idempotent by default.
    /// </summary>
    /// <param name="action">The asynchronous action to invoke on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static AsyncDisposableAction Create(Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(action);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes the specified asynchronous action
    /// when <see cref="IAsyncDisposable.DisposeAsync"/> is called, with configurable idempotency.
    /// </summary>
    /// <param name="action">The asynchronous action to invoke on disposal.</param>
    /// <param name="idempotent">
    /// <see langword="true"/> to ensure multiple calls to <see cref="IAsyncDisposable.DisposeAsync"/>
    /// will only invoke the action once; <see langword="false"/> to allow multiple invocations.
    /// </param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public static AsyncDisposableAction Create(Func<ValueTask> action, bool idempotent)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(action, idempotent);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes the specified synchronous action
    /// when <see cref="IAsyncDisposable.DisposeAsync"/> is called. The returned instance is idempotent by default.
    /// </summary>
    /// <param name="action">The synchronous action to invoke on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The synchronous action is wrapped to complete synchronously via <see cref="ValueTask.CompletedTask"/>.
    /// </remarks>
    public static AsyncDisposableAction Create(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() =>
        {
            action();
            return ValueTask.CompletedTask;
        });
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes the specified synchronous action
    /// when <see cref="IAsyncDisposable.DisposeAsync"/> is called, with configurable idempotency.
    /// </summary>
    /// <param name="action">The synchronous action to invoke on disposal.</param>
    /// <param name="idempotent">
    /// <see langword="true"/> to ensure multiple calls to <see cref="IAsyncDisposable.DisposeAsync"/>
    /// will only invoke the action once; <see langword="false"/> to allow multiple invocations.
    /// </param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The synchronous action is wrapped to complete synchronously via <see cref="ValueTask.CompletedTask"/>.
    /// </remarks>
    public static AsyncDisposableAction Create(Action action, bool idempotent)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(
            () =>
            {
                action();
                return ValueTask.CompletedTask;
            },
            idempotent
        );
    }
}
