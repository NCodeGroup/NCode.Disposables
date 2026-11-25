// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Contains factory methods for various <see cref="IDisposable"/> implementations.
/// </summary>
/// <remarks>
/// <para>
/// This static class serves as the primary entry point for creating disposable instances in the NCode.Disposables library.
/// It provides a fluent API for creating various types of disposables including action-based disposables, collections,
/// aggregates, shared references, and context-aware disposables.
/// </para>
/// <para>
/// All factory methods in this class return instances that are idempotent by default, meaning multiple calls to
/// <see cref="IDisposable.Dispose"/> will only perform the disposal operation once. <see cref="CreateRepeatable"/>
/// is the factory that isn't idempotent: it invokes its action on every call.
/// </para>
/// </remarks>
public static class Disposable
{
    /// <summary>
    /// Gets a singleton instance of <see cref="IDisposable"/> that performs
    /// nothing when <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <value>
    /// A singleton <see cref="IDisposable"/> instance that is a no-op.
    /// </value>
    /// <remarks>
    /// This property is useful as a default or placeholder value where an <see cref="IDisposable"/>
    /// is required but no actual cleanup is needed. The same singleton instance is always returned.
    /// </remarks>
    public static DisposableEmpty Empty => DisposableEmpty.Singleton;

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableAggregate"/>
    /// that contains (i.e. aggregates) a property to another <see cref="IDisposable"/>
    /// instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance that the <see cref="DisposableAggregate"/> will contain (i.e. aggregate).</param>
    /// <returns>A new <see cref="DisposableAggregate"/> instance wrapping the specified disposable.</returns>
    /// <remarks>
    /// <para>
    /// The aggregate pattern allows the underlying disposable to be replaced at runtime through the
    /// <see cref="DisposableAggregate.Disposable"/> property. This is useful in scenarios where the
    /// actual resource to be disposed is determined or changed after the aggregate is created.
    /// </para>
    /// <para>
    /// When the aggregate is disposed, it will dispose whatever <see cref="IDisposable"/> instance
    /// is currently held. If <paramref name="disposable"/> is <see langword="null"/>, no action is taken on disposal.
    /// </para>
    /// </remarks>
    public static DisposableAggregate Aggregate(IDisposable? disposable)
    {
        return new DisposableAggregate(disposable);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableAggregate"/>
    /// that does not yet contain an <see cref="IDisposable"/> instance.
    /// </summary>
    /// <returns>A new, empty <see cref="DisposableAggregate"/> instance.</returns>
    /// <remarks>
    /// The underlying disposable can be assigned later through the
    /// <see cref="DisposableAggregate.Disposable"/> property. If no disposable is assigned,
    /// no action is taken on disposal.
    /// </remarks>
    public static DisposableAggregate Aggregate()
    {
        return new DisposableAggregate();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableSerial"/>
    /// that does not yet contain an <see cref="IDisposable"/> instance.
    /// </summary>
    /// <returns>A new, empty <see cref="DisposableSerial"/> instance.</returns>
    /// <remarks>
    /// Each time a new <see cref="IDisposable"/> is assigned through the
    /// <see cref="DisposableSerial.Disposable"/> property, the previously held instance is disposed.
    /// </remarks>
    public static DisposableSerial Serial()
    {
        return new DisposableSerial();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableSerial"/>
    /// that initially contains the specified <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The initial <see cref="IDisposable"/> instance that the <see cref="DisposableSerial"/> will contain.</param>
    /// <returns>A new <see cref="DisposableSerial"/> instance holding the specified disposable.</returns>
    /// <remarks>
    /// Each time a new <see cref="IDisposable"/> is assigned through the
    /// <see cref="DisposableSerial.Disposable"/> property, the previously held instance is disposed.
    /// If <paramref name="disposable"/> is <see langword="null"/>, nothing is held initially.
    /// </remarks>
    public static DisposableSerial Serial(IDisposable? disposable)
    {
        return new DisposableSerial(disposable);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableSingleAssignment"/>
    /// that does not yet contain an <see cref="IDisposable"/> instance.
    /// </summary>
    /// <returns>A new, unassigned <see cref="DisposableSingleAssignment"/> instance.</returns>
    /// <remarks>
    /// The underlying disposable can be assigned once through the
    /// <see cref="DisposableSingleAssignment.Disposable"/> property.
    /// </remarks>
    public static DisposableSingleAssignment SingleAssignment()
    {
        return new DisposableSingleAssignment();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableGuard{T}"/>
    /// that guards the specified <see cref="IDisposable"/> value.
    /// </summary>
    /// <typeparam name="T">The type of the guarded disposable value.</typeparam>
    /// <param name="value">The <see cref="IDisposable"/> instance that the <see cref="DisposableGuard{T}"/> will guard.</param>
    /// <returns>A new <see cref="DisposableGuard{T}"/> instance guarding the specified value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The guarded value is disposed when the guard is disposed, unless ownership of the value
    /// has been released from the guard.
    /// </remarks>
    public static DisposableGuard<T> Guard<T>(T value)
        where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(value);

        return new DisposableGuard<T>(value);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableCancellation"/>
    /// with a new <see cref="CancellationTokenSource"/>.
    /// </summary>
    /// <returns>A new <see cref="DisposableCancellation"/> instance.</returns>
    /// <remarks>
    /// When the returned instance is disposed, its <see cref="CancellationTokenSource"/> is cancelled and then disposed.
    /// </remarks>
    public static DisposableCancellation Cancellation()
    {
        return new DisposableCancellation();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableCancellation"/>
    /// that cancels the specified <see cref="CancellationTokenSource"/>.
    /// </summary>
    /// <param name="source">The <see cref="CancellationTokenSource"/> to cancel when the returned instance is disposed.</param>
    /// <returns>A new <see cref="DisposableCancellation"/> instance using the specified source.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned instance cancels <paramref name="source"/> without disposing it.
    /// The caller remains responsible for disposing <paramref name="source"/>.
    /// </remarks>
    public static DisposableCancellation Cancellation(CancellationTokenSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return new DisposableCancellation(source);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableFlag"/>
    /// that has not been disposed.
    /// </summary>
    /// <returns>A new <see cref="DisposableFlag"/> instance.</returns>
    /// <remarks>
    /// The <see cref="DisposableFlag.IsDisposed"/> property reports whether the flag has been disposed.
    /// </remarks>
    public static DisposableFlag Flag()
    {
        return new DisposableFlag();
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="ValueDisposable{TState}"/>
    /// that invokes an action with a state value when it is disposed.
    /// </summary>
    /// <typeparam name="TState">The type of the state value that is passed to the action.</typeparam>
    /// <param name="action">The action to invoke when the returned value is disposed.</param>
    /// <param name="state">The state value that is passed to <paramref name="action"/>.</param>
    /// <returns>A new <see cref="ValueDisposable{TState}"/> value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned value does not allocate and is not idempotent; every call to
    /// <see cref="IDisposable.Dispose"/> invokes <paramref name="action"/>.
    /// </remarks>
    public static ValueDisposable<TState> CreateValue<TState>(Action<TState> action, TState state)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new ValueDisposable<TState>(action, state);
    }

    /// <summary>
    /// Creates and returns a new instance of an <see cref="IDisposable"/>
    /// that will invoke the <see cref="IDisposable.Dispose"/> method of an underlying
    /// resource synchronously from a <see cref="SynchronizationContext"/>.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to synchronize.</param>
    /// <param name="context">The <see cref="SynchronizationContext"/> to invoke the operation.</param>
    /// <returns>A new <see cref="DisposableContext"/> instance that wraps the specified disposable with context-aware disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is particularly useful for disposing resources that must be cleaned up on a specific thread,
    /// such as UI components that require disposal on the UI thread.
    /// </para>
    /// <para>
    /// The <see cref="SynchronizationContext.Send"/> method is used, which blocks until disposal is complete.
    /// <see cref="PostingContext"/> returns immediately instead.
    /// </para>
    /// </remarks>
    public static DisposableContext Context(IDisposable disposable, SynchronizationContext context)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        return new DisposableContext(disposable, context);
    }

    /// <summary>
    /// Creates and returns a new instance of an <see cref="IDisposable"/>
    /// that will invoke the <see cref="IDisposable.Dispose"/> method of an underlying
    /// resource asynchronously from a <see cref="SynchronizationContext"/>.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to synchronize.</param>
    /// <param name="context">The <see cref="SynchronizationContext"/> to invoke the operation.</param>
    /// <returns>A new <see cref="DisposableContext"/> instance that wraps the specified disposable with context-aware disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This method is particularly useful for disposing resources that must be cleaned up on a specific thread,
    /// such as UI components that require disposal on the UI thread.
    /// </para>
    /// <para>
    /// The <see cref="SynchronizationContext.Post"/> method is used, which returns immediately.
    /// <c>Context</c> blocks until disposal is complete instead.
    /// </para>
    /// </remarks>
    public static DisposableContext PostingContext(
        IDisposable disposable,
        SynchronizationContext context
    )
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        return new DisposableContext(disposable, context) { IsPosting = true };
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="SharedReferenceLease{T}"/>
    /// which contains an <see cref="IDisposable"/> resource that uses reference
    /// counting and only disposes the underlying resource when all the
    /// references have been released (i.e. reference count is zero).
    /// </summary>
    /// <typeparam name="T">The type of the disposable resource being shared.</typeparam>
    /// <param name="value">The underlying <see cref="IDisposable"/> instance that this <see cref="SharedReferenceLease{T}"/> will contain.</param>
    /// <returns>A new <see cref="SharedReferenceLease{T}"/> representing a lease on the shared resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// The shared reference pattern allows multiple consumers to share ownership of a disposable resource.
    /// Each consumer receives a lease that must be disposed when no longer needed. The underlying resource
    /// is only disposed when all leases have been released.
    /// </para>
    /// <para>
    /// The returned lease starts with a reference count of 1. Additional leases can be created by calling
    /// <see cref="SharedReferenceLease{T}.AddReference"/>.
    /// </para>
    /// </remarks>
    public static SharedReferenceLease<T> Shared<T>(T value)
        where T : IDisposable
    {
        ArgumentNullException.ThrowIfNull(value);

        return SharedReference.Create(value);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="SharedReferenceLease{T}"/>
    /// which contains a value that uses reference counting and invokes a custom
    /// release callback when all the references have been released (i.e. reference count is zero).
    /// </summary>
    /// <typeparam name="T">The type of the value being shared.</typeparam>
    /// <param name="value">The underlying value that the <see cref="SharedReferenceLease{T}"/> will contain.</param>
    /// <param name="onRelease">The callback method used to release/cleanup the shared reference when the reference count reaches zero.</param>
    /// <returns>A new <see cref="SharedReferenceLease{T}"/> representing a lease on the shared resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="onRelease"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// This overload allows sharing values that do not implement <see cref="IDisposable"/> by providing
    /// a custom release callback. The callback is invoked exactly once when the reference count reaches zero.
    /// </para>
    /// <para>
    /// The returned lease starts with a reference count of 1. Additional leases can be created by calling
    /// <see cref="SharedReferenceLease{T}.AddReference"/>.
    /// </para>
    /// </remarks>
    public static SharedReferenceLease<T> Shared<T>(T value, Action<T> onRelease)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onRelease);

        return SharedReference.Create(value, onRelease);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableCollection"/>
    /// which contains other <see cref="IDisposable"/> items that will be
    /// disposed when the collection itself is disposed. The collection is
    /// initialized from a variable argument array of <see cref="IDisposable"/>
    /// items.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="DisposableCollection"/>.</param>
    /// <returns>A new <see cref="DisposableCollection"/> containing the specified disposables.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> contains a <see langword="null"/> item.</exception>
    /// <remarks>
    /// <para>
    /// Items are disposed in LIFO (last-in, first-out) order when the collection is disposed.
    /// This behavior mirrors typical resource acquisition patterns where resources acquired last
    /// should be released first.
    /// </para>
    /// <para>
    /// Additional items can be added to the collection after creation.
    /// </para>
    /// </remarks>
    public static DisposableCollection Collection(params IDisposable[] collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return Collection(collection.AsEnumerable());
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableCollection"/>
    /// which contains other <see cref="IDisposable"/> items that will be
    /// disposed when the collection itself is disposed. The collection is
    /// initialized from an enumeration of <see cref="IDisposable"/> items.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="DisposableCollection"/>.</param>
    /// <returns>A new <see cref="DisposableCollection"/> containing copies of the elements from the specified collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> contains a <see langword="null"/> item.</exception>
    /// <remarks>
    /// <para>
    /// The elements from <paramref name="collection"/> are enumerated and copied during construction.
    /// Items are disposed in LIFO (last-in, first-out) order when the collection is disposed.
    /// </para>
    /// <para>
    /// When the collection is disposed, every item is still disposed; then the one exception is
    /// rethrown, or an <see cref="AggregateException"/> holding every exception is thrown when several items throw.
    /// Use <c>CollectionIgnoringExceptions</c> to ignore the exceptions items throw.
    /// </para>
    /// </remarks>
    public static DisposableCollection Collection(IEnumerable<IDisposable> collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return new DisposableCollection(collection);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableCollection"/>
    /// which contains other <see cref="IDisposable"/> items that will be
    /// disposed when the collection itself is disposed, ignoring the exceptions that items throw.
    /// The collection is initialized from a variable argument array of <see cref="IDisposable"/> items.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="DisposableCollection"/>.</param>
    /// <returns>A new <see cref="DisposableCollection"/> containing the specified disposables.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> contains a <see langword="null"/> item.</exception>
    /// <remarks>
    /// <para>
    /// Items are disposed in LIFO (last-in, first-out) order when the collection is disposed.
    /// </para>
    /// <para>
    /// Disposal continues for all items even if some throw exceptions, and the exceptions are ignored.
    /// </para>
    /// </remarks>
    public static DisposableCollection CollectionIgnoringExceptions(params IDisposable[] collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        var items = collection.AsEnumerable();

        return CollectionIgnoringExceptions(items);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="DisposableCollection"/>
    /// which contains other <see cref="IDisposable"/> items that will be
    /// disposed when the collection itself is disposed, ignoring the exceptions that items throw.
    /// The collection is initialized from an enumeration of <see cref="IDisposable"/> items.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="DisposableCollection"/>.</param>
    /// <returns>A new <see cref="DisposableCollection"/> containing copies of the elements from the specified collection.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="collection"/> contains a <see langword="null"/> item.</exception>
    /// <remarks>
    /// <para>
    /// The elements from <paramref name="collection"/> are enumerated and copied during construction.
    /// Items are disposed in LIFO (last-in, first-out) order when the collection is disposed.
    /// </para>
    /// <para>
    /// Disposal continues for all items even if some throw exceptions, and the exceptions are ignored.
    /// </para>
    /// </remarks>
    public static DisposableCollection CollectionIgnoringExceptions(
        IEnumerable<IDisposable> collection
    )
    {
        ArgumentNullException.ThrowIfNull(collection);

        return new DisposableCollection(collection) { IgnoresExceptions = true };
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned instance is idempotent by default, meaning multiple calls to <see cref="IDisposable.Dispose"/>
    /// will only invoke the action once.
    /// </remarks>
    public static DisposableAction Create(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(action);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> on every call to <see cref="IDisposable.Dispose"/>.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke each time <see cref="IDisposable.Dispose"/> is called.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action on every disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned instance is not idempotent: each call to <see cref="IDisposable.Dispose"/> invokes the action.
    /// Use this with caution as it deviates from the standard dispose pattern.
    /// </remarks>
    public static DisposableAction CreateRepeatable(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(action) { IsRepeatable = true };
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<TResult>(Func<TResult> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action());
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T}"/> with a captured argument when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T">The type of the argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg">The argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured argument on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The argument is captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T>(Action<T> action, T arg)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T, TResult}"/> with a captured argument when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T">The type of the argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg">The argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured argument on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T, TResult>(Func<T, TResult> action, T arg)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, TResult>(
        Func<T1, T2, TResult> action,
        T1 arg1,
        T2 arg2
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2, T3}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3>(
        Action<T1, T2, T3> action,
        T1 arg1,
        T2 arg2,
        T3 arg3
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, T3, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <param name="arg3">The third argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, TResult>(
        Func<T1, T2, T3, TResult> action,
        T1 arg1,
        T2 arg2,
        T3 arg3
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2, T3, T4}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4>(
        Action<T1, T2, T3, T4> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, T3, T4, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the function.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <param name="arg3">The third argument to pass to the function on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, TResult>(
        Func<T1, T2, T3, T4, TResult> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2, T3, T4, T5}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5>(
        Action<T1, T2, T3, T4, T5> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, T3, T4, T5, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the function.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the function.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <param name="arg3">The third argument to pass to the function on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the function on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, TResult>(
        Func<T1, T2, T3, T4, T5, TResult> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2, T3, T4, T5, T6}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, T6>(
        Action<T1, T2, T3, T4, T5, T6> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5,
        T6 arg6
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, T3, T4, T5, T6, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the function.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the function.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the function.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <param name="arg3">The third argument to pass to the function on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the function on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the function on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, T6, TResult>(
        Func<T1, T2, T3, T4, T5, T6, TResult> action,
        T1 arg1,
        T2 arg2,
        T3 arg3,
        T4 arg4,
        T5 arg5,
        T6 arg6
    )
    {
        ArgumentNullException.ThrowIfNull(action);

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2, T3, T4, T5, T6, T7}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the action.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the action on disposal.</param>
    /// <param name="arg7">The seventh argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, T6, T7>(
        Action<T1, T2, T3, T4, T5, T6, T7> action,
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

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, T3, T4, T5, T6, T7, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the function.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the function.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the function.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the function.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <param name="arg3">The third argument to pass to the function on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the function on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the function on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the function on disposal.</param>
    /// <param name="arg7">The seventh argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, T6, T7, TResult>(
        Func<T1, T2, T3, T4, T5, T6, T7, TResult> action,
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

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action{T1, T2, T3, T4, T5, T6, T7, T8}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the action.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the action.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the action.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the action.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the action.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the action.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument to pass to the action.</typeparam>
    /// <typeparam name="T8">The type of the eighth argument to pass to the action.</typeparam>
    /// <param name="action">Specifies the action to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the action on disposal.</param>
    /// <param name="arg2">The second argument to pass to the action on disposal.</param>
    /// <param name="arg3">The third argument to pass to the action on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the action on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the action on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the action on disposal.</param>
    /// <param name="arg7">The seventh argument to pass to the action on disposal.</param>
    /// <param name="arg8">The eighth argument to pass to the action on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified action with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The arguments are captured at creation time, allowing type-safe disposal patterns.
    /// The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, T6, T7, T8>(
        Action<T1, T2, T3, T4, T5, T6, T7, T8> action,
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

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke a <see cref="Func{T1, T2, T3, T4, T5, T6, T7, T8, TResult}"/> with captured arguments when
    /// <see cref="IDisposable.Dispose"/> is called. The result of the function is disregarded.
    /// </summary>
    /// <typeparam name="T1">The type of the first argument to pass to the function.</typeparam>
    /// <typeparam name="T2">The type of the second argument to pass to the function.</typeparam>
    /// <typeparam name="T3">The type of the third argument to pass to the function.</typeparam>
    /// <typeparam name="T4">The type of the fourth argument to pass to the function.</typeparam>
    /// <typeparam name="T5">The type of the fifth argument to pass to the function.</typeparam>
    /// <typeparam name="T6">The type of the sixth argument to pass to the function.</typeparam>
    /// <typeparam name="T7">The type of the seventh argument to pass to the function.</typeparam>
    /// <typeparam name="T8">The type of the eighth argument to pass to the function.</typeparam>
    /// <typeparam name="TResult">The return type of the function, which is ignored.</typeparam>
    /// <param name="action">Specifies the function to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first argument to pass to the function on disposal.</param>
    /// <param name="arg2">The second argument to pass to the function on disposal.</param>
    /// <param name="arg3">The third argument to pass to the function on disposal.</param>
    /// <param name="arg4">The fourth argument to pass to the function on disposal.</param>
    /// <param name="arg5">The fifth argument to pass to the function on disposal.</param>
    /// <param name="arg6">The sixth argument to pass to the function on disposal.</param>
    /// <param name="arg7">The seventh argument to pass to the function on disposal.</param>
    /// <param name="arg8">The eighth argument to pass to the function on disposal.</param>
    /// <returns>A new <see cref="IDisposable"/> instance that invokes the specified function with the captured arguments on disposal.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// This overload is useful when you have an existing method that returns a value but you only care
    /// about its side effects during disposal. The returned instance is idempotent by default.
    /// </remarks>
    public static DisposableAction Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(
        Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> action,
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

        return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    }
}
