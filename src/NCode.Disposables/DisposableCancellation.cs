// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides an <see cref="IDisposable"/> implementation that cancels a <see cref="CancellationTokenSource"/>
/// when <see cref="Dispose"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful for tying the lifetime of cancellable work to a <see langword="using"/> scope.
/// The <see cref="Token"/> is cancelled when the scope ends.
/// </para>
/// <para>
/// When created with the parameterless constructor, the <see cref="CancellationTokenSource"/> is created
/// by this instance and is cancelled and then disposed by <see cref="Dispose"/>. When created from a
/// <see cref="CancellationTokenSource"/> supplied by the caller, that source is only cancelled; the caller
/// remains responsible for disposing it.
/// </para>
/// <para>
/// The disposal operation is idempotent; multiple calls to <see cref="Dispose"/> will only cancel
/// the source once.
/// </para>
/// <para>
/// Thread-safety: Concurrent calls to <see cref="Dispose"/> are handled safely using atomic operations.
/// </para>
/// </remarks>
public sealed class DisposableCancellation : IDisposable, IDisposalStatus
{
    /// <summary>
    /// Holds 1 after disposal has started and 0 before.
    /// </summary>
    private int _disposed;

    /// <summary>
    /// Gets the source that is cancelled on disposal.
    /// </summary>
    private CancellationTokenSource Source { get; }

    /// <summary>
    /// Gets a value indicating whether this instance created <see cref="Source"/> and disposes it after cancelling it.
    /// </summary>
    private bool OwnsSource { get; }

    /// <summary>
    /// Gets the <see cref="CancellationToken"/> that is cancelled when <see cref="Dispose"/> is called.
    /// </summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableCancellation"/> class with a new
    /// <see cref="CancellationTokenSource"/> that this instance cancels and then disposes.
    /// </summary>
    public DisposableCancellation()
        : this(new CancellationTokenSource(), ownsSource: true) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="DisposableCancellation"/> class that cancels
    /// the specified <see cref="CancellationTokenSource"/> without disposing it.
    /// </summary>
    /// <param name="source">The <see cref="CancellationTokenSource"/> to cancel when <see cref="Dispose"/> is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
    /// <remarks>The caller remains responsible for disposing <paramref name="source"/>.</remarks>
    public DisposableCancellation(CancellationTokenSource source)
        : this(source, ownsSource: false) { }

    /// <summary>
    /// Initializes a new instance that cancels <paramref name="source"/> and, when <paramref name="ownsSource"/>
    /// is <see langword="true"/>, disposes it.
    /// </summary>
    /// <param name="source">The <see cref="CancellationTokenSource"/> to cancel when <see cref="Dispose"/> is called.</param>
    /// <param name="ownsSource"><see langword="true"/> to dispose <paramref name="source"/> after cancelling it; otherwise, <see langword="false"/>.</param>
    private DisposableCancellation(CancellationTokenSource source, bool ownsSource)
    {
        ArgumentNullException.ThrowIfNull(source);

        Source = source;
        OwnsSource = ownsSource;
        Token = source.Token;
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Cancels the <see cref="CancellationTokenSource"/>. A source created by this instance is then disposed;
    /// a source supplied by the caller is not disposed. This method is idempotent; subsequent calls have no effect.
    /// </para>
    /// <para>
    /// If a callback registered on the <see cref="Token"/> throws, the exception propagates from this method
    /// as an <see cref="AggregateException"/> after a source created by this instance has been disposed.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            Source.Cancel();
        }
        finally
        {
            if (OwnsSource)
                Source.Dispose();
        }
    }
}
