// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Represents an <see cref="IDisposable"/> container that aggregates and manages
/// a reference to another underlying <see cref="IDisposable"/> instance, allowing
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
public interface IDisposableAggregate : IDisposable
{
    /// <summary>
    /// Gets or sets the underlying <see cref="IDisposable"/> instance managed by this aggregate.
    /// </summary>
    /// <value>
    /// The current underlying <see cref="IDisposable"/> instance, or <see langword="null"/> if no instance is set.
    /// </value>
    /// <remarks>
    /// <para>
    /// Setting this property does not dispose the previous instance. If disposal of the previous
    /// instance is required, it must be done explicitly before setting a new value.
    /// </para>
    /// <para>
    /// After the aggregate has been disposed, the getter returns <see langword="null"/>
    /// and the setter disposes the value it is given.
    /// </para>
    /// </remarks>
    IDisposable? Disposable { get; set; }
}

/// <summary>
/// Provides a thread-safe implementation of <see cref="IDisposableAggregate"/> that manages
/// a mutable reference to an underlying <see cref="IDisposable"/> instance.
/// </summary>
/// <remarks>
/// <para>
/// This class allows the underlying disposable to be replaced or cleared during the aggregate's lifetime.
/// All operations on the <see cref="Disposable"/> property are thread-safe using lock-free atomic operations.
/// </para>
/// <para>
/// When <see cref="Dispose"/> is called, the currently held underlying instance is disposed.
/// The aggregate becomes disposed, after which the <see cref="Disposable"/> getter returns
/// <see langword="null"/> and the setter disposes the value it is given.
/// </para>
/// <para>
/// Setting the <see cref="Disposable"/> property does not automatically dispose the previous instance.
/// </para>
/// </remarks>
public sealed class DisposableAggregate : IDisposableAggregate, IDisposalStatus
{
    private static readonly IDisposable Sentinel = new DisposableEmpty();
    private IDisposable? _disposable;

    /// <summary>Gets a value indicating whether <see cref="Dispose"/> has been called at least once.</summary>
    public bool IsDisposed => ReferenceEquals(Volatile.Read(ref _disposable), Sentinel);

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableAggregate"/> class
    /// with no underlying <see cref="IDisposable"/> instance.
    /// </summary>
    public DisposableAggregate()
    {
        // nothing
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableAggregate"/> class
    /// with the specified underlying <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">
    /// The initial underlying <see cref="IDisposable"/> instance to aggregate,
    /// or <see langword="null"/> to create an empty aggregate.
    /// </param>
    public DisposableAggregate(IDisposable? disposable)
    {
        _disposable = disposable;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes the currently held underlying <see cref="IDisposable"/> instance, if any.
    /// This method is idempotent; subsequent calls will have no effect.
    /// </para>
    /// <para>
    /// After disposal, the <see cref="Disposable"/> getter returns <see langword="null"/>
    /// and the setter disposes the value it is given.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposable, Sentinel);
        disposable?.Dispose();
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

                var snapshot = Interlocked.CompareExchange(ref _disposable, value, current);
                if (ReferenceEquals(current, snapshot))
                    return;
                spinWait.SpinOnce();
            }
        }
    }
}
