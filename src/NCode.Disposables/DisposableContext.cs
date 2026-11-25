// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

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
public sealed class DisposableContext : IDisposable, IDisposalStatus
{
    private IDisposable? _disposable;
    private readonly SynchronizationContext _context;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> posts the disposal to the context
    /// instead of sending it.
    /// </summary>
    internal bool IsPosting { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposable) is null;

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableContext"/> class that wraps an
    /// underlying <see cref="IDisposable"/> resource for disposal via a <see cref="SynchronizationContext"/>,
    /// which is invoked synchronously via <see cref="SynchronizationContext.Send"/>.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to wrap.</param>
    /// <param name="context">The <see cref="SynchronizationContext"/> used to invoke the disposal operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
    public DisposableContext(IDisposable disposable, SynchronizationContext context)
    {
        ArgumentNullException.ThrowIfNull(disposable);
        ArgumentNullException.ThrowIfNull(context);

        _disposable = disposable;
        _context = context;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Invokes the underlying resource's <see cref="IDisposable.Dispose"/> method via the configured
    /// <see cref="SynchronizationContext"/>. This method is idempotent; subsequent calls have no effect.
    /// </para>
    /// <para>
    /// A context from <see cref="Disposable.PostingContext"/> posts the disposal to the context and this
    /// method returns immediately without waiting for the disposal to complete. Any other context sends
    /// the disposal, and this method blocks until the disposal completes on the target context.
    /// </para>
    /// <para>
    /// When the context's <see cref="SynchronizationContext.Post"/> throws before the disposal runs, this method calls
    /// <see cref="SynchronizationContext.OperationCompleted"/> and rethrows the exception. The underlying resource
    /// is not disposed, because it is disposed only on the context.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        var disposable = Interlocked.Exchange(ref _disposable, null);
        if (disposable == null)
            return;

        if (IsPosting)
        {
            var callbackStarted = false;
            _context.OperationStarted();
            try
            {
                _context.Post(
                    _ =>
                    {
                        Volatile.Write(ref callbackStarted, true);
                        AsynchronousCallback(disposable);
                    },
                    null
                );
            }
            catch when (!Volatile.Read(ref callbackStarted))
            {
                // The context refused the callback, so the operation it was told about ends here.
                _context.OperationCompleted();
                throw;
            }
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
