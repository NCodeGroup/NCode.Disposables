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
/// Provides the implementation of an <see cref="IDisposable"/> resource that
/// will invoke the <see cref="Dispose"/> method of an underlying resource using
/// an asynchronous or synchronous operation from a <see cref="SynchronizationContext"/>.
/// </summary>
public sealed class DisposableContext : IDisposable
{
    private IDisposable? _disposable;
    private readonly SynchronizationContext _context;
    private readonly bool _async;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableContext"/> class
    /// with an underlying <see cref="IDisposable"/> resource that will be
    /// disposed using an asynchronous or synchronous operation from a
    /// <see cref="SynchronizationContext"/>.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to synchronize.</param>
    /// <param name="context">The <see cref="SynchronizationContext"/> to invoke the operation.</param>
    /// <param name="async"><c>true</c> to asynchronously invoke the operation; otherwise, <c>false</c> to synchronously invoke the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
    public DisposableContext(IDisposable disposable, SynchronizationContext context, bool async = false)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        _disposable = disposable;
        _context = context;
        _async = async;
    }

    /// <summary>
    /// Invokes the <see cref="Dispose"/> method of the underlying resource by using a <see cref="SynchronizationContext"/>
    /// only if it already hasn't been invoked.
    /// </summary>
    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposable, null);
        if (disposable == null) return;

        if (_async)
        {
            _context.OperationStarted();
            _context.Post(AsynchronousCallback, disposable);
        }
        else
        {
            _context.Send(SynchronousCallback, disposable);
        }
    }

    private static void SynchronousCallback(object? state)
    {
        var disposable = (IDisposable?)state;
        disposable?.Dispose();
    }

    private void AsynchronousCallback(object? state)
    {
        try
        {
            SynchronousCallback(state);
        }
        finally
        {
            _context.OperationCompleted();
        }
    }
}