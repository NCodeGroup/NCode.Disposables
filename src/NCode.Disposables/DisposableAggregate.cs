#region Copyright Preamble

//
//    Copyright © 2017 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

#endregion

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
    /// <exception cref="ObjectDisposedException">
    /// The current <see cref="IDisposableAggregate"/> instance has already been disposed.
    /// </exception>
    /// <remarks>
    /// Setting this property does not dispose the previous instance. If disposal of the previous
    /// instance is required, it must be done explicitly before setting a new value.
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
/// The aggregate becomes disposed and any subsequent access to the <see cref="Disposable"/> property
/// will throw an <see cref="ObjectDisposedException"/>.
/// </para>
/// <para>
/// Setting the <see cref="Disposable"/> property does not automatically dispose the previous instance.
/// </para>
/// </remarks>
public sealed class DisposableAggregate : IDisposableAggregate
{
    private static readonly IDisposable Sentinel = new DisposableEmpty();
    private IDisposable? _disposable;

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
    /// After disposal, any access to the <see cref="Disposable"/> property will throw
    /// an <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposable, Sentinel);
        disposable?.Dispose();
    }

    private IDisposable? ThrowIfDisposed(IDisposable? disposable)
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
    /// </remarks>
    public IDisposable? Disposable
    {
        // Uses SpinLock pattern:
        // http://www.albahari.com/threading/part5.aspx#_SpinLock_and_SpinWait
        // http://www.adammil.net/blog/v111_Creating_High-Performance_Locks_and_Lock-free_Code_for_NET_.html

        get
        {
            var current = Volatile.Read(ref _disposable);
            return ThrowIfDisposed(current);
        }

        set
        {
            var spinWait = new SpinWait();
            while (true)
            {
                var current = ThrowIfDisposed(Volatile.Read(ref _disposable));
                var snapshot = Interlocked.CompareExchange(ref _disposable, value, current);
                if (ReferenceEquals(current, snapshot)) return;
                spinWait.SpinOnce();
            }
        }
    }
}
