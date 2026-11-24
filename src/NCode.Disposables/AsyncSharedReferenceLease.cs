// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

namespace NCode.Disposables;

/// <summary>
/// Represents a lease for a shared reference that will release the underlying resource when the reference count
/// reaches zero (0). The lease is not idempotent safe and consumers must take care to not dispose the same lease
/// multiple times otherwise the underlying resource will be released prematurely. One solution for consumers is to
/// assign the lease to <c>default</c> after disposing it.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
/// <remarks>
/// Initializes a new instance of the <see cref="AsyncSharedReferenceLease{T}"/> struct.
/// </remarks>
/// <param name="owner">The <see cref="AsyncSharedReferenceOwner{T}"/> instance that owns the shared reference.</param>
public readonly struct AsyncSharedReferenceLease<T>(IAsyncSharedReferenceOwner<T> owner) : IAsyncDisposable
{
    internal readonly IAsyncSharedReferenceOwner<T>? OwnerOrNull = owner;

    private IAsyncSharedReferenceOwner<T> Owner =>
        !IsActive
            ? throw new InvalidOperationException("The lease for the shared reference is not active.")
            : OwnerOrNull;

    /// <summary>
    /// Gets a value indicating whether the lease is active.
    /// </summary>
    [MemberNotNullWhen(true, nameof(OwnerOrNull))]
    public bool IsActive => OwnerOrNull != null;

    /// <summary>
    /// Gets the value of the shared resource.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this lease is not active.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reference count has reached zero (0)
    /// and the underlying resource has been released already.</exception>
    public T Value => Owner.Value;

    /// <summary>
    /// Increments the reference count and returns a disposable resource that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when this lease is not active.</exception>
    /// <exception cref="ObjectDisposedException">Thrown when the reference count has reached zero (0)
    /// and the underlying resource has been released already.</exception>
    public AsyncSharedReferenceLease<T> AddReference()
    {
        return Owner.AddReference();
    }

    /// <summary>
    /// Attempts to increment the reference count and outputs a disposable resource that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <param name="reference">Destination for the <see cref="AsyncSharedReferenceLease{T}"/> instance
    /// if the original reference count is greater than zero (0).</param>
    /// <returns><c>true</c> if the original reference count was greater than zero (0) and
    /// a new shared reference was successfully created with an incremented reference count.</returns>
    public bool TryAddReference(out AsyncSharedReferenceLease<T> reference)
    {
        if (OwnerOrNull?.TryAddReference(out reference) ?? false)
        {
            return true;
        }

        reference = default;
        return false;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Unfortunately, because we are a struct, there is nothing we can do to prevent the consumer from
        // calling dispose multiple times. We can only check if the lease is active and release the reference.
        if (IsActive)
        {
            await OwnerOrNull.ReleaseReferenceAsync();
        }
    }
}
