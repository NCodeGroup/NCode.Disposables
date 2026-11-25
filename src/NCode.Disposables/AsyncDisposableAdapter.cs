// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides an adapter that adds asynchronous disposal support to an existing <see cref="IDisposable"/> instance,
/// enabling it to be used with <see langword="await using"/> statements and other <see cref="IAsyncDisposable"/> patterns.
/// </summary>
/// <remarks>
/// <para>
/// This adapter wraps a synchronous <see cref="IDisposable"/> and exposes it as an <see cref="IAsyncDisposable"/>.
/// The underlying <see cref="IDisposable.Dispose"/> method is called synchronously when <see cref="DisposeAsync"/> is invoked.
/// </para>
/// <para>
/// By default, the adapter is idempotent, meaning multiple calls to <see cref="DisposeAsync"/> will only
/// dispose the underlying instance once. <see cref="AsyncDisposable.AdaptRepeatable(IDisposable)"/> creates an
/// adapter that disposes the underlying instance on every call instead.
/// </para>
/// <para>
/// Thread-safety: Unless the adapter is repeatable, concurrent calls to <see cref="DisposeAsync"/>
/// are handled safely using atomic operations.
/// </para>
/// </remarks>
public sealed class AsyncDisposableAdapter : IAsyncDisposable, IDisposalStatus
{
    private IDisposable? _disposable;

    /// <summary>
    /// Holds 1 after disposal has been requested and 0 before; <see cref="IsDisposed"/> reads it.
    /// </summary>
    private int _disposed;

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> disposes the underlying instance on every call
    /// instead of only the first.
    /// </summary>
    internal bool IsRepeatable { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableAdapter"/> class that wraps the specified
    /// <see cref="IDisposable"/>, which is disposed only once.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    public AsyncDisposableAdapter(IDisposable disposable)
    {
        _disposable = disposable;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableAdapter"/> class that wraps the specified
    /// <see cref="IDisposable"/> with the specified idempotency setting.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    /// <param name="idempotent">
    /// <see langword="true"/> (the default) to ensure multiple calls to <see cref="DisposeAsync"/>
    /// will only dispose the underlying instance once; <see langword="false"/> to allow multiple disposals.
    /// </param>
    // Callers that pass only the disposable bind to the constructor without the flag, so this one keeps its default until it is deleted.
#pragma warning disable RS0027 // optional parameter on an overload with fewer parameters than another
    public AsyncDisposableAdapter(IDisposable disposable, bool idempotent = true)
#pragma warning restore RS0027
        : this(disposable)
    {
        IsRepeatable = !idempotent;
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// Disposes the underlying <see cref="IDisposable"/> instance synchronously.
    /// By default, the underlying instance is disposed only on the first call; subsequent calls
    /// return immediately without disposing again. An adapter created by
    /// <see cref="AsyncDisposable.AdaptRepeatable(IDisposable)"/> disposes the underlying instance on every call.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);

        var disposable = IsRepeatable ? _disposable : Interlocked.Exchange(ref _disposable, null);
        disposable?.Dispose();
        return ValueTask.CompletedTask;
    }
}
