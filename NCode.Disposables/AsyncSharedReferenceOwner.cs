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
/// Represents the composition root for a resource that can be shared using reference counting. When the last lease
/// is disposed, the underlying resource is released by calling the configured <c>onRelease</c> function.
/// </summary>
/// <typeparam name="T">The type of the shared resource.</typeparam>
public interface IAsyncSharedReferenceOwner<T>
{
    /// <summary>
    /// Gets the value of the shared resource.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the reference count has reached zero (0)
    /// and the underlying resource has been released already.</exception>
    T Value { get; }

    /// <summary>
    /// Increments the reference count and returns a disposable lease that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the reference count has reached zero (0)
    /// and the underlying resource has been released already.</exception>
    AsyncSharedReferenceLease<T> AddReference();

    /// <summary>
    /// Attempts to increment the reference count and outputs a disposable lease that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <param name="reference">Destination for the <see cref="AsyncSharedReferenceLease{T}"/> instance
    /// if the original reference count is greater than zero (0).</param>
    /// <returns><c>true</c> if the original reference count was greater than zero (0) and
    /// a new shared reference was successfully created with an incremented reference count.</returns>
    bool TryAddReference(out AsyncSharedReferenceLease<T> reference);

    /// <summary>
    /// Decrements the reference count and releases the underlying resource when the reference count reaches zero (0).
    /// </summary>
    ValueTask<int> ReleaseReferenceAsync();
}

/// <summary>
/// Represents the composition root for a resource that can be shared using reference counting. When the last lease
/// is disposed, the underlying resource is released by calling the specified <paramref name="onRelease"/> function.
/// </summary>
/// <param name="value">The underlying resource to be shared.</param>
/// <param name="onRelease">The method to be called when the last lease is released.</param>
/// <typeparam name="T">The type of the shared resource.</typeparam>
public sealed class AsyncSharedReferenceOwner<T>(T value, Func<T, ValueTask> onRelease) : IAsyncSharedReferenceOwner<T>
{
    private int _count = 1;

    /// <summary>
    /// Gets the value of the shared resource.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the reference count has reached zero (0)
    /// and the underlying resource has been released already.</exception>
    public T Value
    {
        get
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _count) == 0, this);

            return value;
        }
    }

    /// <summary>
    /// Increments the reference count and returns a disposable lease that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when the reference count has reached zero (0)
    /// and the underlying resource has been released already.</exception>
    public AsyncSharedReferenceLease<T> AddReference()
    {
        ObjectDisposedException.ThrowIf(!TryAddReference(out var reference), this);

        return reference;
    }

    /// <summary>
    /// Attempts to increment the reference count and outputs a disposable lease that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <param name="reference">Destination for the <see cref="AsyncSharedReferenceLease{T}"/> instance
    /// if the original reference count is greater than zero (0).</param>
    /// <returns><c>true</c> if the original reference count was greater than zero (0) and
    /// a new shared reference was successfully created with an incremented reference count.</returns>
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

    /// <summary>
    /// Decrements the reference count and releases the underlying resource when the reference count reaches zero (0).
    /// </summary>
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
    /// This is an internal API and should not be used directly.
    /// Decrements the reference count but does not release the underlying resource when the reference count reaches zero (0).
    /// </summary>
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