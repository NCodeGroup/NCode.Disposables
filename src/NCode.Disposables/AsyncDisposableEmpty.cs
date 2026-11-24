// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a no-operation implementation of <see cref="IAsyncDisposable"/> that performs no action
/// when <see cref="DisposeAsync"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful as a null object pattern replacement for <see cref="IAsyncDisposable"/>,
/// allowing code to avoid null checks when a disposable is optional.
/// </para>
/// <para>
/// Use the <see cref="Singleton"/> property or <see cref="AsyncDisposable.Empty"/> to access
/// a shared instance rather than creating new instances.
/// </para>
/// </remarks>
public sealed class AsyncDisposableEmpty : IAsyncDisposable
{
    /// <summary>
    /// Gets a singleton instance of <see cref="AsyncDisposableEmpty"/> that can be shared
    /// across the application.
    /// </summary>
    /// <value>A shared singleton instance of <see cref="AsyncDisposableEmpty"/>.</value>
    /// <remarks>
    /// This singleton is also accessible via <see cref="AsyncDisposable.Empty"/>.
    /// </remarks>
    public static AsyncDisposableEmpty Singleton { get; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// This implementation performs no operation and returns a completed <see cref="ValueTask"/> immediately.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
