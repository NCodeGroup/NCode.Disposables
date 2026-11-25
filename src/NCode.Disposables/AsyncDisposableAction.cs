// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides an <see cref="IAsyncDisposable"/> implementation that invokes a specified
/// <see cref="Func{TResult}">Func&lt;ValueTask&gt;</see> delegate when <see cref="DisposeAsync"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful for wrapping cleanup logic in an <see cref="IAsyncDisposable"/> interface,
/// enabling use with <see langword="await using"/> statements and other disposal patterns.
/// </para>
/// <para>
/// By default, the action is invoked only once (idempotent behavior) even if <see cref="DisposeAsync"/>
/// is called multiple times. <see cref="AsyncDisposable.CreateRepeatable(Func{ValueTask})"/> creates an
/// instance that invokes the action on every call instead.
/// </para>
/// <para>
/// Thread-safety: Unless the instance is repeatable, concurrent calls to <see cref="DisposeAsync"/>
/// are handled safely using atomic operations.
/// </para>
/// </remarks>
public sealed class AsyncDisposableAction : IAsyncDisposable, IDisposalStatus
{
    private Func<ValueTask>? _action;

    /// <summary>
    /// Holds 1 after disposal has been requested and 0 before; <see cref="IsDisposed"/> reads it.
    /// </summary>
    private int _disposed;

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> invokes the action on every call
    /// instead of only the first.
    /// </summary>
    internal bool IsRepeatable { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableAction"/> class with the specified
    /// asynchronous dispose action, which is invoked only once.
    /// </summary>
    /// <param name="action">The asynchronous delegate to invoke when <see cref="DisposeAsync"/> is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public AsyncDisposableAction(Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        _action = action;
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// By default, the action is invoked only on the first call to this method; subsequent calls
    /// return immediately without invoking the action again. An instance created by
    /// <see cref="AsyncDisposable.CreateRepeatable(Func{ValueTask})"/> invokes the action on every call.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        Volatile.Write(ref _disposed, 1);

        var action = IsRepeatable ? _action : Interlocked.Exchange(ref _action, null);
        if (action is not null)
            await action().ConfigureAwait(false);
    }
}
