// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a no-operation implementation of <see cref="IDisposable"/> that performs no action
/// when <see cref="Dispose"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful as a null object pattern replacement for <see cref="IDisposable"/>,
/// allowing code to avoid null checks when a disposable is optional.
/// </para>
/// <para>
/// Use the <see cref="Singleton"/> property or <see cref="Disposable.Empty"/> to access
/// a shared instance rather than creating new instances.
/// </para>
/// </remarks>
public sealed class DisposableEmpty : IDisposable
{
    /// <summary>
    /// Gets a singleton instance of <see cref="DisposableEmpty"/> that can be shared
    /// across the application.
    /// </summary>
    /// <value>A shared singleton instance of <see cref="DisposableEmpty"/>.</value>
    /// <remarks>
    /// This singleton is also accessible via <see cref="Disposable.Empty"/>.
    /// </remarks>
    public static DisposableEmpty Singleton { get; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// This implementation performs no operation and returns immediately.
    /// </remarks>
    public void Dispose()
    {
        // nothing
    }
}
