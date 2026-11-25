// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Represents an <see cref="IAsyncDisposable"/> container that aggregates and manages
/// a reference to another underlying <see cref="IAsyncDisposable"/> instance, allowing
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
    "Use AsyncDisposableAggregate, which AsyncDisposable.Aggregate returns. This interface is deleted in the next major version.",
    DiagnosticId = "NCDISP002"
)]
public interface IAsyncDisposableAggregate : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the underlying <see cref="IAsyncDisposable"/> instance managed by this aggregate.
    /// </summary>
    /// <value>
    /// The current underlying <see cref="IAsyncDisposable"/> instance, or <see langword="null"/> if no instance is set.
    /// </value>
    /// <exception cref="ObjectDisposedException">
    /// The setter was called after the current <see cref="IAsyncDisposableAggregate"/> instance has been disposed.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Setting this property does not dispose the previous instance. If disposal of the previous
    /// instance is required, it must be done explicitly before setting a new value.
    /// </para>
    /// <para>
    /// After the aggregate has been disposed, the getter returns <see langword="null"/>
    /// and the setter throws an <see cref="ObjectDisposedException"/> without taking the value.
    /// </para>
    /// </remarks>
    IAsyncDisposable? Disposable { get; set; }
}
