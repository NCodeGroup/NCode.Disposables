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

using System.Diagnostics;

namespace NCode.Disposables;

/// <summary>
/// Provides factory methods for creating <see cref="SharedReferenceLease{T}"/> instances
/// that manage shared resources using reference counting.
/// </summary>
/// <remarks>
/// <para>
/// Use this class to create shared references that allow multiple consumers to share access
/// to a single resource. The resource is automatically released when all leases are disposed.
/// </para>
/// <para>
/// This is the synchronous version. For asynchronous disposal, use <see cref="AsyncSharedReference"/>.
/// </para>
/// </remarks>
public static class SharedReference
{
    private static void Dispose<T>(T value)
        where T : IDisposable
    {
        value.Dispose();
    }

    /// <summary>
    /// Creates a new <see cref="SharedReferenceLease{T}"/> that shares the specified <see cref="IDisposable"/>
    /// resource using reference counting. The resource is automatically disposed when the last lease is released.
    /// </summary>
    /// <typeparam name="T">The type of the shared resource, which must implement <see cref="IDisposable"/>.</typeparam>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <returns>
    /// A new <see cref="SharedReferenceLease{T}"/> representing the initial lease on the shared resource.
    /// </returns>
    /// <remarks>
    /// The returned lease holds an initial reference to the resource. Additional references can be obtained
    /// by calling <see cref="SharedReferenceLease{T}.AddReference"/> on any active lease.
    /// </remarks>
    public static SharedReferenceLease<T> Create<T>(T value)
        where T : IDisposable
    {
        return Create(value, Dispose);
    }

    /// <summary>
    /// Creates a new <see cref="SharedReferenceLease{T}"/> that shares the specified resource using reference
    /// counting with a custom release callback. The callback is invoked when the last lease is released.
    /// </summary>
    /// <typeparam name="T">The type of the shared resource.</typeparam>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <param name="onRelease">
    /// The callback to invoke when the last lease is released to clean up the shared resource.
    /// </param>
    /// <returns>
    /// A new <see cref="SharedReferenceLease{T}"/> representing the initial lease on the shared resource.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Use this overload when the shared resource does not implement <see cref="IDisposable"/>
    /// or when custom cleanup logic is required.
    /// </para>
    /// <para>
    /// The returned lease holds an initial reference to the resource. Additional references can be obtained
    /// by calling <see cref="SharedReferenceLease{T}.AddReference"/> on any active lease.
    /// </para>
    /// </remarks>
    public static SharedReferenceLease<T> Create<T>(T value, Action<T> onRelease)
    {
        var owner = new SharedReferenceOwner<T>(value, onRelease);
        try
        {
            return owner.AddReference();
        }
        finally
        {
            var count = owner.ReleaseReference();
            Debug.Assert(count == 1);
        }
    }
}
