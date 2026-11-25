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
    /// Creates and returns a new instance of <see cref="AsyncDisposableAggregate"/>
    /// that does not yet contain an <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <returns>A new, empty <see cref="AsyncDisposableAggregate"/> instance.</returns>
    /// <remarks>
    /// The underlying disposable can be assigned later through the
    /// <see cref="AsyncDisposableAggregate.Disposable"/> property. If no disposable is assigned,
    /// no action is taken on disposal.
    /// </remarks>
    public static AsyncDisposableAggregate Aggregate()
    {
        return new AsyncDisposableAggregate();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableSerial"/>
    /// that does not yet contain an <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <returns>A new, empty <see cref="AsyncDisposableSerial"/> instance.</returns>
    /// <remarks>
    /// Each time a new <see cref="IAsyncDisposable"/> is assigned through the
    /// <see cref="AsyncDisposableSerial.Disposable"/> property, the previously held instance is disposed.
    /// </remarks>
    public static AsyncDisposableSerial Serial()
    {
        return new AsyncDisposableSerial();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableSerial"/>
    /// that initially contains the specified <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The initial <see cref="IAsyncDisposable"/> instance that the <see cref="AsyncDisposableSerial"/> will contain.</param>
    /// <returns>A new <see cref="AsyncDisposableSerial"/> instance holding the specified disposable.</returns>
    /// <remarks>
    /// Each time a new <see cref="IAsyncDisposable"/> is assigned through the
    /// <see cref="AsyncDisposableSerial.Disposable"/> property, the previously held instance is disposed.
    /// If <paramref name="disposable"/> is <see langword="null"/>, nothing is held initially.
    /// </remarks>
    public static AsyncDisposableSerial Serial(IAsyncDisposable? disposable)
    {
        return new AsyncDisposableSerial(disposable);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableSingleAssignment"/>
    /// that does not yet contain an <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <returns>A new, unassigned <see cref="AsyncDisposableSingleAssignment"/> instance.</returns>
    /// <remarks>
    /// The underlying disposable can be assigned once through the
    /// <see cref="AsyncDisposableSingleAssignment.Disposable"/> property.
    /// </remarks>
    public static AsyncDisposableSingleAssignment SingleAssignment()
    {
        return new AsyncDisposableSingleAssignment();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableGuard{T}"/>
    /// that guards the specified <see cref="IAsyncDisposable"/> value.
    /// </summary>
    /// <typeparam name="T">The type of the guarded disposable value.</typeparam>
    /// <param name="value">The <see cref="IAsyncDisposable"/> instance that the <see cref="AsyncDisposableGuard{T}"/> will guard.</param>
    /// <returns>A new <see cref="AsyncDisposableGuard{T}"/> instance guarding the specified value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The guarded value is disposed when the guard is disposed, unless ownership of the value
    /// has been released from the guard.
    /// </remarks>
    public static AsyncDisposableGuard<T> Guard<T>(T value)
        where T : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(value);

        return new AsyncDisposableGuard<T>(value);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableCancellation"/>
    /// with a new <see cref="CancellationTokenSource"/>.
    /// </summary>
    /// <returns>A new <see cref="AsyncDisposableCancellation"/> instance.</returns>
    /// <remarks>
    /// When the returned instance is disposed, its <see cref="CancellationTokenSource"/> is cancelled and then disposed.
    /// </remarks>
    public static AsyncDisposableCancellation Cancellation()
    {
        return new AsyncDisposableCancellation();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableCancellation"/>
    /// that cancels the specified <see cref="CancellationTokenSource"/>.
    /// </summary>
    /// <param name="source">The <see cref="CancellationTokenSource"/> to cancel when the returned instance is disposed.</param>
    /// <returns>A new <see cref="AsyncDisposableCancellation"/> instance using the specified source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned instance cancels <paramref name="source"/> without disposing it.
    /// The caller remains responsible for disposing <paramref name="source"/>.
    /// </remarks>
    public static AsyncDisposableCancellation Cancellation(CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new AsyncDisposableCancellation(source);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncDisposableContext"/>
    /// that disposes the specified <see cref="IAsyncDisposable"/> on the specified <see cref="SynchronizationContext"/>.
    /// </summary>
    /// <param name="disposable">The <see cref="IAsyncDisposable"/> instance to dispose on <paramref name="context"/>.</param>
    /// <param name="context">The <see cref="SynchronizationContext"/> on which <paramref name="disposable"/> is disposed.</param>
    /// <returns>A new <see cref="AsyncDisposableContext"/> instance.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public static AsyncDisposableContext Context(
        IAsyncDisposable disposable,
        SynchronizationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        return new AsyncDisposableContext(disposable, context);
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
    // Callers that pass only the collection bind to this overload, so it keeps its optional parameter.
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

    // ADR-1005: no Action<…> overloads; they would drop a Task-returning lambda's Task.
    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T, ValueTask}"/> with a captured argument when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T">The type of the argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg">The argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured argument on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The argument is captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T>(Func<T, ValueTask> action, T arg)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2>(
        Func<T1, T2, ValueTask> action,
        T1 arg1,
        T2 arg2
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg1, arg2));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, T3, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2, T3>(
        Func<T1, T2, T3, ValueTask> action,
        T1 arg1,
        T2 arg2,
        T3 arg3
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg1, arg2, arg3));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, T3, T4, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2, T3, T4>(
        Func<T1, T2, T3, T4, ValueTask> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, T3, T4, T5, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2, T3, T4, T5>(
        Func<T1, T2, T3, T4, T5, ValueTask> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, T3, T4, T5, T6, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2, T3, T4, T5, T6>(
        Func<T1, T2, T3, T4, T5, T6, ValueTask> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5,
        T6 arg6
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, T3, T4, T5, T6, T7, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the action.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the action on disposal.</param>
    /// <param name="arg7">The seventh argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2, T3, T4, T5, T6, T7>(
        Func<T1, T2, T3, T4, T5, T6, T7, ValueTask> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5,
        T6 arg6,
        T7 arg7
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>
    /// Creates a new <see cref="AsyncDisposableAction"/> that invokes a
    /// <see cref="Func{T1, T2, T3, T4, T5, T6, T7, T8, ValueTask}"/> with captured arguments when
    /// <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the action.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument to pass to the action.</typeparam>
    /// <typeparam name="T8">The type of the eighth argument to pass to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the action on disposal.</param>
    /// <param name="arg7">The seventh argument to pass to the action on disposal.</param>
    /// <param name="arg8">The eighth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="AsyncDisposableAction"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static AsyncDisposableAction Create<T1, T2, T3, T4, T5, T6, T7, T8>(
        Func<T1, T2, T3, T4, T5, T6, T7, T8, ValueTask> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5,
        T6 arg6,
        T7 arg7,
        T8 arg8
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() =>
            action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8)
        );
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="AsyncValueDisposable{TState}"/>
    /// that invokes an asynchronous action with a state value when it is disposed.
    /// </summary>
    /// <typeparam name="TState">The type of the state value that is passed to the action.</typeparam>
    /// <param name="action">The asynchronous action to invoke when the returned value is disposed.</param>
    /// <param name="state">The state value that is passed to <paramref name="action"/>.</param>
    /// <returns>A new <see cref="AsyncValueDisposable{TState}"/> value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned value does not allocate and is not idempotent; every call to
    /// <see cref="IAsyncDisposable.DisposeAsync"/> invokes <paramref name="action"/>.
    /// </remarks>
    public static AsyncValueDisposable<TState> CreateValue<TState>(
        Func<TState, ValueTask> action,
        TState state
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncValueDisposable<TState>(action, state);
    }
}
