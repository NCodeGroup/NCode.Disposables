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

namespace NCode.Disposables;

/// <summary>
/// Represents the owner of a shared resource that uses reference counting for lifetime management.
/// When the last lease is released (reference count reaches zero), the underlying resource is
/// disposed by invoking the configured release callback.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
/// <remarks>
/// <para>
/// This interface provides the core reference counting mechanism for shared resources.
/// Consumers should obtain leases via <see cref="AddReference"/> or <see cref="TryAddReference"/>
/// and dispose those leases when done using the resource.
/// </para>
/// <para>
/// All operations are thread-safe using lock-free atomic operations.
/// </para>
/// </remarks>
public interface ISharedReferenceOwner<T>
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
    /// A new <see cref="SharedReferenceLease{T}"/> that decrements the reference count when disposed.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// The reference count has reached zero and the underlying resource has been released.
    /// </exception>
    /// <remarks>
    /// The caller is responsible for disposing the returned lease when access to the shared resource
    /// is no longer needed.
    /// </remarks>
    SharedReferenceLease<T> AddReference();

    /// <summary>
    /// Attempts to increment the reference count and obtain a new lease to the shared resource.
    /// </summary>
    /// <param name="reference">
    /// When this method returns <see langword="true"/>, contains a new <see cref="SharedReferenceLease{T}"/>
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
    bool TryAddReference(out SharedReferenceLease<T> reference);

    /// <summary>
    /// Decrements the reference count and releases the underlying resource if the count reaches zero.
    /// </summary>
    /// <returns>The new reference count after decrementing.</returns>
    /// <remarks>
    /// This method is typically called internally by <see cref="SharedReferenceLease{T}"/> when disposed.
    /// Direct calls to this method should be rare in normal usage patterns.
    /// </remarks>
    int ReleaseReference();
}

/// <summary>
/// Provides a thread-safe implementation of <see cref="ISharedReferenceOwner{T}"/> that manages
/// a shared resource using reference counting with lock-free atomic operations.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
/// <param name="value">The underlying resource to be shared.</param>
/// <param name="onRelease">
/// The callback to invoke when the reference count reaches zero to release the underlying resource.
/// </param>
/// <remarks>
/// <para>
/// This class is the composition root for shared reference management. The initial reference count is 1,
/// representing the initial lease returned to the caller.
/// </para>
/// <para>
/// All reference counting operations use lock-free spin-wait patterns for thread-safety and performance.
/// </para>
/// </remarks>
public sealed class SharedReferenceOwner<T>(T value, Action<T> onRelease) : ISharedReferenceOwner<T>
{
    private int _count = 1;

    /// <inheritdoc />
    /// <value>The underlying shared resource.</value>
    public T Value
    {
        get
        {
            if (Volatile.Read(ref _count) == 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return value;
        }
    }

    /// <inheritdoc />
    public SharedReferenceLease<T> AddReference()
    {
        if (!TryAddReference(out var reference))
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        return reference;
    }

    /// <inheritdoc />
    public bool TryAddReference(out SharedReferenceLease<T> reference)
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
                reference = new SharedReferenceLease<T>(this);
                return true;
            }

            spinWait.SpinOnce();
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Uses a lock-free spin-wait pattern to atomically decrement the reference count.
    /// When the count reaches zero, the <c>onRelease</c> callback is invoked to release the resource.
    /// Subsequent calls after the count reaches zero return 0 without invoking the callback again.
    /// </remarks>
    public int ReleaseReference()
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
                    onRelease(value);
                }

                return newCount;
            }

            spinWait.SpinOnce();
        }
    }
}
