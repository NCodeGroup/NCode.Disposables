// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Represents a collection of <see cref="IDisposable"/> items that are automatically disposed
/// when the collection itself is disposed. Items are disposed in reverse order of their addition.
/// </summary>
/// <remarks>
/// <para>
/// This interface combines <see cref="IDisposable"/> and <see cref="ICollection{T}"/> to provide
/// a managed collection of disposable resources with automatic cleanup.
/// </para>
/// <para>
/// When the collection is disposed, all contained items are disposed in LIFO (last-in, first-out) order,
/// which is typically appropriate for resource cleanup scenarios where resources may have dependencies.
/// </para>
/// </remarks>
// ADR-1005: deleted in the next major version.
[Obsolete(
    "Use DisposableCollection, which Disposable.Collection returns. This interface is deleted in the next major version.",
    DiagnosticId = "NCDISP002"
)]
public interface IDisposableCollection : IDisposable, ICollection<IDisposable>
{
    // nothing
}
