// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe holder for a single <see cref="IAsyncDisposable"/> instance that disposes
/// the previous instance whenever a new one is assigned.
/// </summary>
/// <remarks>
/// <para>
/// All operations on the serial are thread-safe using lock-free atomic operations.
/// </para>
/// <para>
/// Calling <see cref="SetDisposableAsync"/> with a different instance awaits <see cref="IAsyncDisposable.DisposeAsync"/>
/// on the instance that was held. Calling it with the instance already held does nothing.
/// </para>
/// <para>
/// When <see cref="DisposeAsync"/> is called, the currently held instance is disposed.
/// The serial becomes disposed, after which the <see cref="Disposable"/> property returns
/// <see langword="null"/> and <see cref="SetDisposableAsync"/> disposes the value it is given.
/// </para>
/// </remarks>
public sealed class AsyncDisposableSerial : IAsyncDisposable, IDisposalStatus
{
    private static IAsyncDisposable Sentinel { get; } = new AsyncDisposableEmpty();

    private IAsyncDisposable? _disposable;

    /// <summary>Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.</summary>
    public bool IsDisposed => ReferenceEquals(Volatile.Read(ref _disposable), Sentinel);

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableSerial"/> class
    /// with no <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    public AsyncDisposableSerial()
    {
        // nothing
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableSerial"/> class
    /// with the specified <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">
    /// The initial <see cref="IAsyncDisposable"/> instance to hold,
    /// or <see langword="null"/> to create an empty serial.
    /// </param>
    public AsyncDisposableSerial(IAsyncDisposable? disposable)
    {
        _disposable = disposable;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes the currently held <see cref="IAsyncDisposable"/> instance, if any.
    /// This method is idempotent; subsequent calls will have no effect.
    /// </para>
    /// <para>
    /// After disposal, the <see cref="Disposable"/> property returns <see langword="null"/>
    /// and <see cref="SetDisposableAsync"/> disposes the value it is given.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        var previous = Interlocked.Exchange(ref _disposable, Sentinel);
        if (!ReferenceEquals(previous, Sentinel) && previous is not null)
            await previous.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the currently held <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <value>
    /// The currently held instance, or <see langword="null"/> when none is held or the serial is disposed.
    /// </value>
    /// <remarks>
    /// The getter returns the current instance using a volatile read for thread-safety.
    /// </remarks>
    public IAsyncDisposable? Disposable
    {
        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel) ? null : current;
        }
    }

    /// <summary>
    /// Replaces the currently held <see cref="IAsyncDisposable"/> instance and disposes the instance it replaced.
    /// </summary>
    /// <param name="disposable">
    /// The <see cref="IAsyncDisposable"/> instance to hold, or <see langword="null"/> to hold nothing.
    /// </param>
    /// <returns>A task that completes when the replaced instance has been disposed.</returns>
    /// <remarks>
    /// <para>
    /// This method uses a lock-free spin-wait pattern to atomically replace the held instance
    /// and then awaits <see cref="IAsyncDisposable.DisposeAsync"/> on the instance it replaced.
    /// Setting the instance that is already held does nothing.
    /// </para>
    /// <para>
    /// After the serial has been disposed, this method awaits <see cref="IAsyncDisposable.DisposeAsync"/>
    /// on <paramref name="disposable"/> instead of storing it, and it does not throw
    /// an <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    public async ValueTask SetDisposableAsync(IAsyncDisposable? disposable)
    {
        // Uses SpinLock pattern:
        // http://www.albahari.com/threading/part5.aspx#_SpinLock_and_SpinWait
        // http://www.adammil.net/blog/v111_Creating_High-Performance_Locks_and_Lock-free_Code_for_NET_.html

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

            if (ReferenceEquals(current, disposable))
                return;

            var snapshot = Interlocked.CompareExchange(ref _disposable, disposable, current);
            if (ReferenceEquals(current, snapshot))
            {
                if (current is not null)
                    await current.DisposeAsync().ConfigureAwait(false);
                return;
            }

            spinWait.SpinOnce();
        }
    }
}
