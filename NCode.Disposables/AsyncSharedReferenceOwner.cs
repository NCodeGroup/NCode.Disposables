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

using System.ComponentModel;

namespace NCode.Disposables;

/// <summary>
/// Represents the owner of a shared resource that uses reference counting for lifetime management
/// with asynchronous release support. When the last lease is released (reference count reaches zero),
/// the underlying resource is disposed by invoking the configured release callback asynchronously.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
/// <remarks>
/// <para>
/// This interface provides the core reference counting mechanism for shared resources with async disposal.
/// Consumers should obtain leases via <see cref="AddReference"/> or <see cref="TryAddReference"/>
/// and dispose those leases when done using the resource.
/// </para>
/// <para>
/// All operations are thread-safe using lock-free atomic operations.
/// </para>
/// </remarks>
public interface IAsyncSharedReferenceOwner<T>
{
    /// <summary>
    /// Gets the value of the shared resource.
    /// </summary>
    /// <value>The underlying shared resource.</value>
    /// <exception cref="ObjectDisposedException">
    /// The reference count has reached zero and the underlying resource has been released.
    /// </exception>
    T Value { get; }

    /// <summary>
    /// Increments the reference count and returns a new lease that holds a reference to the shared resource.
    /// </summary>
    /// <returns>
    /// A new <see cref="AsyncSharedReferenceLease{T}"/> that decrements the reference count when disposed.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The reference count has reached zero and the underlying resource has been released.
    /// </exception>
    /// <remarks>
    /// The caller is responsible for disposing the returned lease when access to the shared resource
    /// is no longer needed.
    /// </remarks>
    AsyncSharedReferenceLease<T> AddReference();

    /// <summary>
    /// Attempts to increment the reference count and obtain a new lease to the shared resource.
    /// </summary>
    /// <param name="reference">
    /// When this method returns <see langword="true"/>, contains a new <see cref="AsyncSharedReferenceLease{T}"/>
    /// that holds a reference to the shared resource; otherwise, the default value.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the reference count was greater than zero and a new lease was
    /// successfully created; <see langword="false"/> if the resource has already been released.
    /// </returns>
    /// <remarks>
    /// Use this method when you need to safely check if a shared resource is still available
    /// without risking an <see cref="ObjectDisposedException"/>.
    /// </remarks>
    bool TryAddReference(out AsyncSharedReferenceLease<T> reference);

    /// <summary>
    /// Decrements the reference count and asynchronously releases the underlying resource if the count reaches zero.
    /// </summary>
    /// <returns>A task that completes with the new reference count after decrementing.</returns>
    /// <remarks>
    /// This method is typically called internally by <see cref="AsyncSharedReferenceLease{T}"/> when disposed.
    /// Direct calls to this method should be rare in normal usage patterns.
    /// </remarks>
    ValueTask<int> ReleaseReferenceAsync();
}

/// <summary>
/// Provides a thread-safe implementation of <see cref="IAsyncSharedReferenceOwner{T}"/> that manages
/// a shared resource using reference counting with lock-free atomic operations and asynchronous release.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
/// <param name="value">The underlying resource to be shared.</param>
/// <param name="onRelease">
/// The asynchronous callback to invoke when the reference count reaches zero to release the underlying resource.
/// </param>
/// <remarks>
/// <para>
/// This class is the composition root for shared reference management with async disposal. The initial
/// reference count is 1, representing the initial lease returned to the caller.
/// </para>
/// <para>
/// All reference counting operations use lock-free spin-wait patterns for thread-safety and performance.
/// </para>
/// </remarks>
public sealed class AsyncSharedReferenceOwner<T>(T value, Func<T, ValueTask> onRelease) : IAsyncSharedReferenceOwner<T>
{
    private int _count = 1;

    /// <inheritdoc />
    /// <value>The underlying shared resource.</value>
    public T Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _count) == 0, this);

            return value;
        }
    }

    /// <inheritdoc />
    public AsyncSharedReferenceLease<T> AddReference()
    {
        ObjectDisposedException.ThrowIf(!TryAddReference(out var reference), this);

        return reference;
    }

    /// <inheritdoc />
    public bool TryAddReference(out AsyncSharedReferenceLease<T> reference)
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var count = Volatile.Read(ref _count);
            if (count == 0)
            {
                reference = default;
                return false;
            }

            var newCount = count + 1;
            var prevCount = Interlocked.CompareExchange(ref _count, newCount, count);
            if (count == prevCount)
            {
                reference = new AsyncSharedReferenceLease<T>(this);
                return true;
            }

            spinWait.SpinOnce();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses a lock-free spin-wait pattern to atomically decrement the reference count.
    /// When the count reaches zero, the <c>onRelease</c> callback is invoked asynchronously to release the resource.
    /// Subsequent calls after the count reaches zero return 0 without invoking the callback again.
    /// </remarks>
    public async ValueTask<int> ReleaseReferenceAsync()
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var count = Volatile.Read(ref _count);
            if (count == 0)
            {
                return 0;
            }

            var newCount = count - 1;
            var prevCount = Interlocked.CompareExchange(ref _count, newCount, count);
            if (count == prevCount)
            {
                if (newCount == 0)
                {
                    await onRelease(value);
                }

                return newCount;
            }

            spinWait.SpinOnce();
        }
    }

    /// <summary>
    /// Decrements the reference count without invoking the release callback, even when the count reaches zero.
    /// </summary>
    /// <returns>The new reference count after decrementing.</returns>
    /// <remarks>
    /// <para>
    /// <b>Warning:</b> This is an internal API and should not be used directly by consumers.
    /// It is used internally for specific scenarios where the release callback should not be invoked.
    /// </para>
    /// <para>
    /// Using this method incorrectly can lead to resource leaks since the <c>onRelease</c> callback
    /// will not be invoked when the reference count reaches zero.
    /// </para>
    /// </remarks>
    [Browsable(false)]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int UnsafeReleaseReference()
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var count = Volatile.Read(ref _count);
            if (count == 0)
            {
                return 0;
            }

            var newCount = count - 1;
            var prevCount = Interlocked.CompareExchange(ref _count, newCount, count);
            if (count == prevCount)
            {
                return newCount;
            }

            spinWait.SpinOnce();
        }
    }
}
