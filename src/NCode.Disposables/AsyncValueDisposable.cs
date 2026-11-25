// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// A struct that runs an asynchronous action with a state value when it is disposed, without allocating.
/// </summary>
/// <typeparam name="TState">The type of the state value that is passed to the action.</typeparam>
/// <remarks>
/// <para>
/// Every call to <see cref="DisposeAsync"/>, on every copy of the value, runs the action. Dispose it
/// once, through an <see langword="await"/> <see langword="using"/> statement or declaration.
/// </para>
/// <para>
/// The <see langword="default"/> value does nothing when it is disposed.
/// </para>
/// </remarks>
/// <param name="action">The asynchronous action to run when the value is disposed.</param>
/// <param name="state">The state value that is passed to <paramref name="action"/>.</param>
/// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
// ADR-1006: a value disposable is a non-idempotent struct that trades idempotency for zero allocation.
public readonly struct AsyncValueDisposable<TState>(Func<TState, ValueTask> action, TState state)
    : IAsyncDisposable
{
    /// <summary>
    /// Gets the action that runs with <see cref="State"/> when the value is disposed, or <see langword="null"/> for the <see langword="default"/> value.
    /// </summary>
    private Func<TState, ValueTask>? Callback { get; } =
        action ?? throw new ArgumentNullException(nameof(action));

    /// <summary>
    /// Gets the state value that is passed to <see cref="Callback"/>.
    /// </summary>
    private TState State { get; } = state;

    /// <summary>
    /// Runs the asynchronous action with the state value.
    /// </summary>
    /// <remarks>
    /// This method does nothing for the <see langword="default"/> value.
    /// </remarks>
    /// <returns>A task that represents the asynchronous dispose operation.</returns>
    public ValueTask DisposeAsync() => Callback is null ? ValueTask.CompletedTask : Callback(State);
}
