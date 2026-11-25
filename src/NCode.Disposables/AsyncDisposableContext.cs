// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides an <see cref="IAsyncDisposable"/> wrapper that invokes the <see cref="IAsyncDisposable.DisposeAsync"/>
/// method of an underlying resource using a <see cref="SynchronizationContext"/>, enabling disposal on a specific
/// thread or context (such as a UI thread).
/// </summary>
/// <remarks>
/// <para>
/// This class is useful when a resource must be disposed on a specific thread, such as UI controls
/// that must be disposed on the UI thread, or when coordinating disposal with other context-sensitive operations.
/// </para>
/// <para>
/// The disposal operation is idempotent; multiple calls to <see cref="DisposeAsync"/> will only dispose
/// the underlying resource once.
/// </para>
/// <para>
/// Thread-safety: Concurrent calls to <see cref="DisposeAsync"/> are handled safely using atomic operations.
/// </para>
/// </remarks>
/// <param name="disposable">The underlying <see cref="IAsyncDisposable"/> instance to wrap.</param>
/// <param name="context">The <see cref="SynchronizationContext"/> used to invoke the disposal operation.</param>
/// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
/// <exception cref="ArgumentNullException"><paramref name="context"/> is <see langword="null"/>.</exception>
public sealed class AsyncDisposableContext(
    IAsyncDisposable disposable,
    SynchronizationContext context
) : IAsyncDisposable, IDisposalStatus
{
    /// <summary>
    /// The underlying instance, or <see langword="null"/> once disposal has started.
    /// </summary>
    private IAsyncDisposable? _disposable = disposable
        ?? throw new ArgumentNullException(nameof(disposable));

    /// <summary>
    /// Gets the context the disposal is posted to.
    /// </summary>
    private SynchronizationContext Context { get; } =
        context ?? throw new ArgumentNullException(nameof(context));

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposable) is null;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Posts the underlying resource's <see cref="IAsyncDisposable.DisposeAsync"/> call to the configured
    /// <see cref="SynchronizationContext"/> and completes when the disposal has completed on that context.
    /// The disposal is bracketed by <see cref="SynchronizationContext.OperationStarted"/> and
    /// <see cref="SynchronizationContext.OperationCompleted"/>. Any exception thrown by the underlying
    /// resource is propagated to the caller.
    /// </para>
    /// <para>
    /// This method is idempotent; subsequent calls have no effect.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        var disposable = Interlocked.Exchange(ref _disposable, null);
        if (disposable is null)
            return;

        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );

        Context.OperationStarted();
        Context.Post(_ => _ = DisposeOnContextAsync(disposable, completion), null);

        await completion.Task.ConfigureAwait(false);
    }

    /// <summary>
    /// Disposes <paramref name="disposable"/> on the context and reports the outcome through <paramref name="completion"/>.
    /// </summary>
    /// <param name="disposable">The instance to dispose.</param>
    /// <param name="completion">The source that receives the outcome of the disposal.</param>
    /// <returns>A task that completes when the disposal has finished.</returns>
    // Task, not ValueTask: the caller discards it (CA2012), and every exception reaches completion.
    private async Task DisposeOnContextAsync(
        IAsyncDisposable disposable,
        TaskCompletionSource completion
    )
    {
        try
        {
            await disposable.DisposeAsync().ConfigureAwait(false);
            completion.SetResult();
        }
        catch (Exception exception)
        {
            completion.SetException(exception);
        }
        finally
        {
            Context.OperationCompleted();
        }
    }
}
