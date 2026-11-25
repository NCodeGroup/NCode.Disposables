// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Represents an <see cref="IDisposable"/> container that aggregates and manages
/// a reference to another underlying <see cref="IDisposable"/> instance, allowing
/// the underlying instance to be replaced or cleared during the aggregate's lifetime.
/// </summary>
/// <remarks>
/// <para>
/// This interface enables scenarios where the disposable resource needs to be swapped
/// or updated dynamically, while still providing proper disposal semantics.
/// </para>
/// <para>
/// When the aggregate is disposed, it will also dispose the currently held underlying instance.
/// </para>
/// </remarks>
// ADR-1005: deleted in the next major version.
[Obsolete(
    "Use DisposableAggregate, which Disposable.Aggregate returns. This interface is deleted in the next major version.",
    DiagnosticId = "NCDISP002"
)]
public interface IDisposableAggregate : IDisposable
{
    /// <summary>
    /// Gets or sets the underlying <see cref="IDisposable"/> instance managed by this aggregate.
    /// </summary>
    /// <value>
    /// The current underlying <see cref="IDisposable"/> instance, or <see langword="null"/> if no instance is set.
    /// </value>
    /// <remarks>
    /// <para>
    /// Setting this property does not dispose the previous instance. If disposal of the previous
    /// instance is required, it must be done explicitly before setting a new value.
    /// </para>
    /// <para>
    /// After the aggregate has been disposed, the getter returns <see langword="null"/>
    /// and the setter disposes the value it is given.
    /// </para>
    /// </remarks>
    IDisposable? Disposable { get; set; }
}
