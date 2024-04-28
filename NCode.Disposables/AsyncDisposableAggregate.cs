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
/// Represents an <see cref="IAsyncDisposable"/> instance that contains (i.e.
/// aggregates) a property to another underlying <see cref="IAsyncDisposable"/>
/// instance.
/// </summary>
public interface IAsyncDisposableAggregate : IAsyncDisposable
{
    /// <summary>
    /// Sets or gets the underlying <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the current <see cref="IAsyncDisposableAggregate"/> instance has already been disposed.</exception>
    IAsyncDisposable? Disposable { get; set; }
}

/// <summary>
/// Provides the implementation for <see cref="IAsyncDisposableAggregate"/>.
/// </summary>
public sealed class AsyncDisposableAggregate : IAsyncDisposableAggregate
{
    private static readonly IAsyncDisposable Sentinel = new AsyncDisposableEmpty();
    private IAsyncDisposable? _disposable;

    /// <summary>
    /// Initializes a new instance of <see cref="IAsyncDisposableAggregate"/> where
    /// it's underlying <see cref="IAsyncDisposable"/> instance is initially <c>null</c>.
    /// </summary>
    public AsyncDisposableAggregate()
    {
        // nothing
    }

    /// <summary>
    /// Initializes a new instance of <see cref="IAsyncDisposableAggregate"/> using
    /// the specified underlying <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IAsyncDisposable"/> instance that the <see cref="IAsyncDisposableAggregate"/> will contain (i.e. aggregate).</param>
    public AsyncDisposableAggregate(IAsyncDisposable? disposable)
    {
        _disposable = disposable;
    }

    /// <summary>
    /// Invokes the <see cref="DisposeAsync"/> method on the underlying <see cref="IAsyncDisposable"/>
    /// instance only if it already hasn't been invoked. This method will
    /// guarantee that the underlying <see cref="DisposeAsync"/> method will only
    /// be invoked once.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var disposable = Interlocked.Exchange(ref _disposable, Sentinel);
        if (disposable is not null)
            await disposable.DisposeAsync();
    }

    private IAsyncDisposable? ThrowIfDisposed(IAsyncDisposable? disposable)
    {
        var isDisposed = ReferenceEquals(disposable, Sentinel);
        ObjectDisposedException.ThrowIf(isDisposed, this);
        return disposable;
    }

    /// <summary>
    /// Sets or gets the underlying <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the current <see cref="IAsyncDisposableAggregate"/> instance has already been disposed.</exception>
    public IAsyncDisposable? Disposable
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