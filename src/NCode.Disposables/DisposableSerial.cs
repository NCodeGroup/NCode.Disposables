// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe holder for a single <see cref="IDisposable"/> instance that disposes
/// the previous instance whenever a new one is assigned.
/// </summary>
/// <remarks>
/// <para>
/// All operations on the <see cref="Disposable"/> property are thread-safe using lock-free atomic operations.
/// </para>
/// <para>
/// Setting the <see cref="Disposable"/> property to a different instance disposes the instance that was held.
/// Setting it to the instance it already holds does nothing.
/// </para>
/// <para>
/// When <see cref="Dispose"/> is called, the currently held instance is disposed.
/// The serial becomes disposed, after which the <see cref="Disposable"/> getter returns
/// <see langword="null"/> and the setter disposes the value it is given.
/// </para>
/// </remarks>
public sealed class DisposableSerial : IDisposable, IDisposalStatus
{
    private static IDisposable Sentinel { get; } = new DisposableEmpty();

    private IDisposable? _disposable;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => ReferenceEquals(Volatile.Read(ref _disposable), Sentinel);

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableSerial"/> class
    /// with no <see cref="IDisposable"/> instance.
    /// </summary>
    public DisposableSerial()
    {
        // nothing
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableSerial"/> class
    /// with the specified <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">
    /// The initial <see cref="IDisposable"/> instance to hold,
    /// or <see langword="null"/> to create an empty serial.
    /// </param>
    public DisposableSerial(IDisposable? disposable)
    {
        _disposable = disposable;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes the currently held <see cref="IDisposable"/> instance, if any.
    /// This method is idempotent; subsequent calls will have no effect.
    /// </para>
    /// <para>
    /// After disposal, the <see cref="Disposable"/> getter returns <see langword="null"/>
    /// and the setter disposes the value it is given.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        var previous = Interlocked.Exchange(ref _disposable, Sentinel);
        if (!ReferenceEquals(previous, Sentinel))
            previous?.Dispose();
    }

    /// <summary>
    /// Gets or sets the currently held <see cref="IDisposable"/> instance.
    /// </summary>
    /// <value>
    /// The currently held instance, or <see langword="null"/> when none is held or the serial is disposed.
    /// </value>
    /// <remarks>
    /// <para>
    /// The getter returns the current instance using a volatile read for thread-safety.
    /// </para>
    /// <para>
    /// The setter uses a lock-free spin-wait pattern to atomically replace the held instance
    /// and then disposes the instance it replaced. Setting the instance that is already held does nothing.
    /// </para>
    /// <para>
    /// After disposal, the getter returns <see langword="null"/> and the setter disposes the value it is given.
    /// </para>
    /// </remarks>
    public IDisposable? Disposable
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
                var current = Volatile.Read(ref _disposable);
                if (ReferenceEquals(current, Sentinel))
                {
                    // ADR-1004
                    value?.Dispose();
                    return;
                }

                if (ReferenceEquals(current, value))
                    return;

                var snapshot = Interlocked.CompareExchange(ref _disposable, value, current);
                if (ReferenceEquals(current, snapshot))
                {
                    current?.Dispose();
                    return;
                }

                spinWait.SpinOnce();
            }
        }
    }
}
