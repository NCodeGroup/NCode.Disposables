// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// A disposable that only records that it has been disposed.
/// </summary>
/// <remarks>
/// Thread-safety: <see cref="IsDisposed"/> and <see cref="Dispose"/> use volatile memory access,
/// so they can be called concurrently from multiple threads.
/// </remarks>
public sealed class DisposableFlag : IDisposable, IDisposalStatus
{
    private int _disposed;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// Calling this method more than once has no further effect.
    /// </remarks>
    public void Dispose() => Volatile.Write(ref _disposed, 1);
}
