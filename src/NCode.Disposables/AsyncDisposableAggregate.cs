// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe holder that manages
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
public sealed class AsyncDisposableAggregate : IAsyncDisposable, IDisposalStatus
{
    private static readonly IAsyncDisposable Sentinel = new AsyncDisposableEmpty();
    private IAsyncDisposable? _disposable;

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.
    /// </summary>
    public bool IsDisposed
    {
        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel);
        }
    }

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

    /// <summary>
    /// Gets or sets the underlying <see cref="IAsyncDisposable"/> instance that this aggregate holds.
    /// </summary>
    /// <value>
    /// The underlying <see cref="IAsyncDisposable"/> instance, or <see langword="null"/> when none is set or the aggregate is disposed.
    /// </value>
    /// <exception cref="ObjectDisposedException">The setter is called after the aggregate is disposed.</exception>
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
        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel) ? null : current;
        }
        set
        {
            // Lock-free: retry the CompareExchange, backing off with SpinWait, until no other thread changed the field in between.
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
