// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Reports whether a disposable has been disposed.
/// </summary>
public interface IDisposalStatus
{
    /// <summary>
    /// Gets a value indicating whether the instance has been disposed.
    /// </summary>
    /// <remarks>
    /// <see langword="true"/> once disposal has started, even when it threw.
    /// </remarks>
    bool IsDisposed { get; }
}
