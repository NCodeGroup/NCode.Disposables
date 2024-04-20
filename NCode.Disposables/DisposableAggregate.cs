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
/// Represents an <see cref="IDisposable"/> instance that contains (i.e.
/// aggregates) a property to another underlying <see cref="IDisposable"/>
/// instance.
/// </summary>
public interface IDisposableAggregate : IDisposable
{
    /// <summary>
    /// Sets or gets the underlying <see cref="IDisposable"/> instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the current <see cref="IDisposableAggregate"/> instance has already been disposed.</exception>
    IDisposable? Disposable { get; set; }
}

/// <summary>
/// Provides the implementation for <see cref="IDisposableAggregate"/>.
/// </summary>
public sealed class DisposableAggregate : IDisposableAggregate
{
    private static readonly IDisposable Sentinel = new DisposableEmpty();
    private IDisposable? _disposable;

    /// <summary>
    /// Initializes a new instance of <see cref="IDisposableAggregate"/> where
    /// it's underlying <see cref="IDisposable"/> instance is initially <c>null</c>.
    /// </summary>
    public DisposableAggregate()
    {
        // nothing
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IDisposableAggregate"/> using
    /// the specified underlying <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance that the <see cref="IDisposableAggregate"/> will contain (i.e. aggregate).</param>
    public DisposableAggregate(IDisposable? disposable)
    {
        _disposable = disposable;
    }

    /// <summary>
    /// Invokes the <see cref="Dispose"/> method on the underlying <see cref="IDisposable"/>
    /// instance only if it already hasn't been invoked. This method will
    /// guarantee that the underlying <see cref="Dispose"/> method will only
    /// be invoked once.
    /// </summary>
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

    /// <summary>
    /// Sets or gets the underlying <see cref="IDisposable"/> instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the current <see cref="IDisposableAggregate"/> instance has already been disposed.</exception>
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