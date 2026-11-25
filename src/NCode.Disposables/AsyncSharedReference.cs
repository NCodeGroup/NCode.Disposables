// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;

namespace NCode.Disposables;

/// <summary>
/// Provides factory methods for creating <see cref="AsyncSharedReferenceLease{T}"/> instances
/// that manage shared resources using reference counting with asynchronous release support.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to create shared references that allow multiple consumers to share access
/// to a single resource. The resource is automatically released asynchronously when all leases are disposed.
/// </para>
/// <para>
/// This is the asynchronous version. For synchronous disposal, use <see cref="SharedReference"/>.
/// </para>
/// </remarks>
public static class AsyncSharedReference
{
    private static async ValueTask DisposeAsync<T>(T value)
        where T : IAsyncDisposable
    {
        await value.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> that shares the specified <see cref="IAsyncDisposable"/>
    /// resource using reference counting. The resource is automatically disposed asynchronously when the last lease is released.
    /// </summary>
    /// <typeparam name="T">The type of the shared resource, which must implement <see cref="IAsyncDisposable"/>.</typeparam>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <returns>
    /// A new <see cref="AsyncSharedReferenceLease{T}"/> representing the initial lease on the shared resource.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The returned lease holds an initial reference to the resource. Additional references can be obtained
    /// by calling <see cref="AsyncSharedReferenceLease{T}.AddReference"/> on any active lease.
    /// </remarks>
    public static AsyncSharedReferenceLease<T> Create<T>(T value)
        where T : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(value);

        return Create(value, DisposeAsync);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> that shares the specified resource using reference
    /// counting with a custom asynchronous release callback. The callback is invoked when the last lease is released.
    /// </summary>
    /// <typeparam name="T">The type of the shared resource.</typeparam>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <param name="onRelease">
    /// The asynchronous callback to invoke when the last lease is released to clean up the shared resource.
    /// </param>
    /// <returns>
    /// A new <see cref="AsyncSharedReferenceLease{T}"/> representing the initial lease on the shared resource.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="onRelease"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// <para>
    /// Use this overload when the shared resource does not implement <see cref="IAsyncDisposable"/>
    /// or when custom asynchronous cleanup logic is required.
    /// </para>
    /// <para>
    /// The returned lease holds an initial reference to the resource. Additional references can be obtained
    /// by calling <see cref="AsyncSharedReferenceLease{T}.AddReference"/> on any active lease.
    /// </para>
    /// </remarks>
    public static AsyncSharedReferenceLease<T> Create<T>(T value, Func<T, ValueTask> onRelease)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onRelease);

        var owner = new AsyncSharedReferenceOwner<T>(value, onRelease);
        try
        {
            return owner.AddReference();
        }
        finally
        {
            var count = owner.UnsafeReleaseReference();
            Debug.Assert(count == 1);
        }
    }
}
