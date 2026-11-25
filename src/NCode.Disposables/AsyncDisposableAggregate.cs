// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Represents an <see cref="IAsyncDisposable"/> container that aggregates and manages
/// a reference to another underlying <see cref="IAsyncDisposable"/> instance, allowing
/// the underlying instance to be replaced or cleared during the aggregate's lifetime.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables scenarios where the disposable resource needs to be swapped
/// or updated dynamically, while still providing proper disposal semantics.
/// </para>
/// <para>
/// When the aggregate is disposed, it will also dispose the currently held underlying instance.
/// </para>
/// </remarks>
public interface IAsyncDisposableAggregate : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the underlying <see cref="IAsyncDisposable"/> instance managed by this aggregate.
    /// </summary>
    /// <value>
    /// The current underlying <see cref="IAsyncDisposable"/> instance, or <see langword="null"/> if no instance is set.
    /// </value>
    /// <exception cref="ObjectDisposedException">
    /// The setter was called after the current <see cref="IAsyncDisposableAggregate"/> instance has been disposed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Setting this property does not dispose the previous instance. If disposal of the previous
    /// instance is required, it must be done explicitly before setting a new value.
    /// </para>
    /// <para>
    /// After the aggregate has been disposed, the getter returns <see langword="null"/>
    /// and the setter throws an <see cref="ObjectDisposedException"/> without taking the value.
    /// </para>
    /// </remarks>
    IAsyncDisposable? Disposable { get; set; }
}

/// <summary>
/// Provides a thread-safe implementation of <see cref="IAsyncDisposableAggregate"/> that manages
/// a mutable reference to an underlying <see cref="IAsyncDisposable"/> instance.
/// </summary>
/// <remarks>
/// <para>
/// This class allows the underlying disposable to be replaced or cleared during the aggregate's lifetime.
/// All operations on the <see cref="Disposable"/> property are thread-safe using lock-free atomic operations.
/// </para>
/// <para>
/// When <see cref="DisposeAsync"/> is called, the currently held underlying instance is disposed.
/// The aggregate becomes disposed, after which the <see cref="Disposable"/> getter returns
/// <see langword="null"/> and the <see cref="Disposable"/> setter throws an <see cref="ObjectDisposedException"/>.
/// </para>
/// <para>
/// Setting the <see cref="Disposable"/> property does not automatically dispose the previous instance.
/// </para>
/// <para>
/// <see cref="SetDisposableAsync"/> replaces the underlying instance like the setter does, but after the
/// aggregate has been disposed it disposes the value it is given instead of throwing.
/// </para>
/// </remarks>
public sealed class AsyncDisposableAggregate : IAsyncDisposableAggregate, IDisposalStatus
{
    private static readonly IAsyncDisposable Sentinel = new AsyncDisposableEmpty();
    private IAsyncDisposable? _disposable;

    /// <summary>Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.</summary>
    public bool IsDisposed => ReferenceEquals(Volatile.Read(ref _disposable), Sentinel);

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableAggregate"/> class
    /// with no underlying <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    public AsyncDisposableAggregate()
    {
        // nothing
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableAggregate"/> class
    /// with the specified underlying <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">
    /// The initial underlying <see cref="IAsyncDisposable"/> instance to aggregate,
    /// or <see langword="null"/> to create an empty aggregate.
    /// </param>
    public AsyncDisposableAggregate(IAsyncDisposable? disposable)
    {
        _disposable = disposable;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes the currently held underlying <see cref="IAsyncDisposable"/> instance, if any.
    /// This method is idempotent; subsequent calls will have no effect.
    /// </para>
    /// <para>
    /// After disposal, the <see cref="Disposable"/> getter returns <see langword="null"/>
    /// and the setter throws an <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        var disposable = Interlocked.Exchange(ref _disposable, Sentinel);
        if (disposable is not null)
            await disposable.DisposeAsync().ConfigureAwait(false);
    }

    private IAsyncDisposable? ThrowIfDisposed(IAsyncDisposable? disposable)
    {
        var isDisposed = ReferenceEquals(disposable, Sentinel);
        ObjectDisposedException.ThrowIf(isDisposed, this);
        return disposable;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// The getter returns the current underlying instance using a volatile read for thread-safety.
    /// </para>
    /// <para>
    /// The setter uses a lock-free spin-wait pattern to atomically replace the underlying instance.
    /// Note that setting a new value does not dispose the previous instance.
    /// </para>
    /// <para>
    /// After disposal, the getter returns <see langword="null"/> and the setter throws
    /// an <see cref="ObjectDisposedException"/> without taking the value.
    /// </para>
    /// </remarks>
    public IAsyncDisposable? Disposable
    {
        // Uses SpinLock pattern:
        // http://www.albahari.com/threading/part5.aspx#_SpinLock_and_SpinWait
        // http://www.adammil.net/blog/v111_Creating_High-Performance_Locks_and_Lock-free_Code_for_NET_.html

        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel) ? null : current;
        }
        set
        {
            var spinWait = new SpinWait();
            while (true)
            {
                var current = ThrowIfDisposed(Volatile.Read(ref _disposable));
                var snapshot = Interlocked.CompareExchange(ref _disposable, value, current);
                if (ReferenceEquals(current, snapshot))
                    return;
                spinWait.SpinOnce();
            }
        }
    }

    /// <summary>
    /// Replaces the underlying <see cref="IAsyncDisposable"/> instance, or disposes the given instance
    /// when the aggregate has already been disposed.
    /// </summary>
    /// <param name="disposable">
    /// The new underlying <see cref="IAsyncDisposable"/> instance, or <see langword="null"/> to clear it.
    /// </param>
    /// <returns>A task that completes when the value has been stored or disposed.</returns>
    /// <remarks>
    /// <para>
    /// Setting a new value does not dispose the previous instance.
    /// </para>
    /// <para>
    /// After the aggregate has been disposed, this method awaits <see cref="IAsyncDisposable.DisposeAsync"/>
    /// on <paramref name="disposable"/> instead of storing it, and it does not throw
    /// an <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    public async ValueTask SetDisposableAsync(IAsyncDisposable? disposable)
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var current = Volatile.Read(ref _disposable);
            if (ReferenceEquals(current, Sentinel))
            {
                // ADR-1004
                if (disposable is not null)
                    await disposable.DisposeAsync().ConfigureAwait(false);
                return;
            }

            var snapshot = Interlocked.CompareExchange(ref _disposable, disposable, current);
            if (ReferenceEquals(current, snapshot))
                return;
            spinWait.SpinOnce();
        }
    }
}
