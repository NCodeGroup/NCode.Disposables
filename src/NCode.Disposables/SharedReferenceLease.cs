#region Copyright Preamble

// Copyright @ 2024 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

#endregion

using System.Diagnostics.CodeAnalysis;

namespace NCode.Disposables;

/// <summary>
/// Represents a lease for a shared reference that uses reference counting for lifetime management.
/// When disposed, the lease decrements the reference count, and the underlying resource is released
/// when the count reaches zero.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
/// <remarks>
/// <para>
/// This is a <see langword="struct"/> for performance reasons, but this means it is <b>not idempotent-safe</b>.
/// Consumers must take care not to dispose the same lease multiple times, as this will cause the reference
/// count to be decremented multiple times, potentially releasing the resource prematurely.
/// </para>
/// <para>
/// A recommended pattern is to assign the lease to <see langword="default"/> after disposing it:
/// <code>
/// lease.Dispose();
/// lease = default;
/// </code>
/// </para>
/// <para>
/// Use <see cref="IsActive"/> to check if a lease is valid before accessing the shared resource.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="SharedReferenceLease{T}"/> struct
/// with the specified owner.
/// </remarks>
/// <param name="owner">The <see cref="ISharedReferenceOwner{T}"/> instance that owns the shared reference.</param>
public readonly struct SharedReferenceLease<T>(ISharedReferenceOwner<T> owner) : IDisposable
{
    internal readonly ISharedReferenceOwner<T>? OwnerOrNull = owner;

    private ISharedReferenceOwner<T> Owner =>
        !IsActive
            ? throw new InvalidOperationException("The lease for the shared reference is not active.")
            : OwnerOrNull;

    /// <summary>
    /// Gets a value indicating whether the lease is active and can be used to access the shared resource.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the lease is active and associated with a shared reference owner;
    /// <see langword="false"/> if the lease is the default value or has been invalidated.
    /// </value>
    /// <remarks>
    /// A lease becomes inactive when it is the <see langword="default"/> value of the struct.
    /// Always check this property before accessing <see cref="Value"/> or calling <see cref="AddReference"/>
    /// to avoid <see cref="InvalidOperationException"/>.
    /// </remarks>
    [MemberNotNullWhen(true, nameof(OwnerOrNull))]
    public bool IsActive => OwnerOrNull != null;

    /// <summary>
    /// Gets the value of the shared resource.
    /// </summary>
    /// <value>The underlying shared resource.</value>
    /// <exception cref="InvalidOperationException">The lease is not active (default value).</exception>
    /// <exception cref="ObjectDisposedException">
    /// The reference count has reached zero and the underlying resource has been released.
    /// </exception>
    public T Value => Owner.Value;

    /// <summary>
    /// Increments the reference count and returns a new lease that holds a reference to the shared resource.
    /// </summary>
    /// <returns>
    /// A new <see cref="SharedReferenceLease{T}"/> that decrements the reference count when disposed.
    /// </returns>
    /// <exception cref="InvalidOperationException">The lease is not active (default value).</exception>
    /// <exception cref="ObjectDisposedException">
    /// The reference count has reached zero and the underlying resource has been released.
    /// </exception>
    /// <remarks>
    /// The caller is responsible for disposing the returned lease when access to the shared resource
    /// is no longer needed.
    /// </remarks>
    public SharedReferenceLease<T> AddReference()
    {
        return Owner.AddReference();
    }

    /// <summary>
    /// Attempts to increment the reference count and obtain a new lease to the shared resource.
    /// </summary>
    /// <param name="reference">
    /// When this method returns <see langword="true"/>, contains a new <see cref="SharedReferenceLease{T}"/>
    /// that holds a reference to the shared resource; otherwise, the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the lease is active, the reference count was greater than zero,
    /// and a new lease was successfully created; <see langword="false"/> otherwise.
    /// </returns>
    /// <remarks>
    /// This method is safe to call on an inactive lease (default value) and will return <see langword="false"/>
    /// without throwing an exception. Use this method when you need to safely check if a shared resource
    /// is still available.
    /// </remarks>
    public bool TryAddReference(out SharedReferenceLease<T> reference)
    {
        if (OwnerOrNull?.TryAddReference(out reference) ?? false)
        {
            return true;
        }

        reference = default;
        return false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Decrements the reference count on the shared resource. When the reference count reaches zero,
    /// the underlying resource is released.
    /// </para>
    /// <para>
    /// <b>Warning:</b> Because this is a <see langword="struct"/>, there is no built-in protection against
    /// calling <see cref="Dispose"/> multiple times on the same lease. Each call will decrement the
    /// reference count, potentially causing premature release of the resource. Consider assigning the
    /// lease to <see langword="default"/> after disposing.
    /// </para>
    /// <para>
    /// This method is safe to call on an inactive lease (default value) and will have no effect.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        // Unfortunately, because we are a struct, there is nothing we can do to prevent the consumer from
        // calling dispose multiple times. We can only check if the lease is active and release the reference.
        if (IsActive)
        {
            OwnerOrNull.ReleaseReference();
        }
    }
}
