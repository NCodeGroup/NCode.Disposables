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
/// Provides an <see cref="IDisposable"/> wrapper that invokes the <see cref="IDisposable.Dispose"/> method
/// of an underlying resource using a <see cref="SynchronizationContext"/>, enabling disposal on a specific
/// thread or context (such as a UI thread).
/// </summary>
/// <remarks>
/// <para>
/// This class is useful when a resource must be disposed on a specific thread, such as UI controls
/// that must be disposed on the UI thread, or when coordinating disposal with other context-sensitive operations.
/// </para>
/// <para>
/// The disposal operation is idempotent; multiple calls to <see cref="Dispose"/> will only dispose
/// the underlying resource once.
/// </para>
/// <para>
/// Thread-safety: Concurrent calls to <see cref="Dispose"/> are handled safely using atomic operations.
/// </para>
/// </remarks>
public sealed class DisposableContext : IDisposable
{
    private IDisposable? _disposable;
    private readonly SynchronizationContext _context;
    private readonly bool _async;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableContext"/> class that wraps an
    /// underlying <see cref="IDisposable"/> resource for disposal via a <see cref="SynchronizationContext"/>.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to wrap.</param>
    /// <param name="context">The <see cref="SynchronizationContext"/> used to invoke the disposal operation.</param>
    /// <param name="async">
    /// <see langword="true"/> to invoke disposal asynchronously via <see cref="SynchronizationContext.Post"/>;
    /// <see langword="false"/> (the default) to invoke disposal synchronously via <see cref="SynchronizationContext.Send"/>.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// When <paramref name="async"/> is <see langword="true"/>, the disposal uses
    /// <see cref="SynchronizationContext.OperationStarted"/> and <see cref="SynchronizationContext.OperationCompleted"/>
    /// to properly track the asynchronous operation.
    /// </remarks>
    public DisposableContext(IDisposable disposable, SynchronizationContext context, bool async = false)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        _disposable = disposable;
        _context = context;
        _async = async;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Invokes the underlying resource's <see cref="IDisposable.Dispose"/> method via the configured
    /// <see cref="SynchronizationContext"/>. This method is idempotent; subsequent calls have no effect.
    /// </para>
    /// <para>
    /// When async mode is enabled, disposal is posted to the context and this method returns immediately
    /// without waiting for the disposal to complete. When sync mode is used, this method blocks until
    /// the disposal completes on the target context.
    /// </para>
    /// </remarks>
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
