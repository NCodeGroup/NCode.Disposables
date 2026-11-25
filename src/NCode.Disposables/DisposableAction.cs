// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides an <see cref="IDisposable"/> implementation that invokes a specified
/// <see cref="Action"/> delegate when <see cref="Dispose"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful for wrapping cleanup logic in an <see cref="IDisposable"/> interface,
/// enabling use with <see langword="using"/> statements and other disposal patterns.
/// </para>
/// <para>
/// By default, the action is invoked only once (idempotent behavior) even if <see cref="Dispose"/>
/// is called multiple times. <see cref="Disposable.CreateRepeatable"/> creates an instance that
/// invokes the action on every call instead.
/// </para>
/// <para>
/// Thread-safety: Unless the instance is repeatable, concurrent calls to <see cref="Dispose"/>
/// are handled safely using atomic operations.
/// </para>
/// </remarks>
/// <param name="action">The delegate to invoke when <see cref="Dispose"/> is called.</param>
/// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
public sealed class DisposableAction(Action action) : IDisposable, IDisposalStatus
{
    private Action? _action = action ?? throw new ArgumentNullException(nameof(action));

    /// <summary>
    /// Holds 1 after disposal has been requested and 0 before; <see cref="IsDisposed"/> reads it.
    /// </summary>
    private int _disposed;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> invokes the action on every call
    /// instead of only the first.
    /// </summary>
    internal bool IsRepeatable { get; init; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// By default, the action is invoked only on the first call to this method; subsequent calls
    /// return immediately without invoking the action again. An instance created by
    /// <see cref="Disposable.CreateRepeatable"/> invokes the action on every call.
    /// </remarks>
    public void Dispose()
    {
        Volatile.Write(ref _disposed, 1);
        var action = IsRepeatable ? _action : Interlocked.Exchange(ref _action, null);
        action?.Invoke();
    }
}
