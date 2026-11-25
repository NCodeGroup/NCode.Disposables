// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// A struct that runs an action with a state value when it is disposed, without allocating.
/// </summary>
/// <typeparam name="TState">The type of the state value that is passed to the action.</typeparam>
/// <remarks>
/// <para>
/// Every call to <see cref="Dispose"/>, on every copy of the value, runs the action. Dispose it
/// once, through a <see langword="using"/> statement or declaration.
/// </para>
/// <para>
/// The <see langword="default"/> value does nothing when it is disposed.
/// </para>
/// </remarks>
/// <param name="action">The action to run when the value is disposed.</param>
/// <param name="state">The state value that is passed to <paramref name="action"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
// ADR-1006: a value disposable is a non-idempotent struct that trades idempotency for zero allocation.
public readonly struct ValueDisposable<TState>(Action<TState> action, TState state) : IDisposable
{
    private Action<TState>? Action { get; } =
        action ?? throw new ArgumentNullException(nameof(action));

    private TState State { get; } = state;

    /// <summary>
    /// Runs the action with the state value.
    /// </summary>
    /// <remarks>
    /// This method does nothing for the <see langword="default"/> value.
    /// </remarks>
    public void Dispose() => Action?.Invoke(State);
}
