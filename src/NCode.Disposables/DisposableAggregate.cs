// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe holder that manages
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
#pragma warning disable NCDISP002 // implements the deprecated interface until it is deleted
public sealed class DisposableAggregate : IDisposableAggregate, IDisposalStatus
#pragma warning restore NCDISP002
{
    private static readonly IDisposable Sentinel = new DisposableEmpty();
    private IDisposable? _disposable;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
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
