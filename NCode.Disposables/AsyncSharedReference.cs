﻿#region Copyright Preamble

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
/// Contains factory methods for creating <see cref="AsyncSharedReferenceLease{T}"/> instances.
/// </summary>
public static class AsyncSharedReference
{
    private static async ValueTask DisposeAsync<T>(T value)
        where T : IAsyncDisposable
    {
        await value.DisposeAsync();
    }

    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> instance that uses reference counting to share the
    /// specified <paramref name="value"/>. This variant will automatically dispose the resource when the last lease
    /// is disposed.
    /// </summary>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <typeparam name="T">The type of the shared resource.</typeparam>
    public static async ValueTask<AsyncSharedReferenceLease<T>> CreateAsync<T>(T value)
        where T : IAsyncDisposable
    {
        return await CreateAsync(value, DisposeAsync);
    }

    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> instance that uses reference counting to share the
    /// specified <paramref name="value"/>. This variant will call the specified <paramref name="onRelease"/> function
    /// when the last lease is disposed.
    /// </summary>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <param name="onRelease">The method to be called when the last lease is disposed.</param>
    /// <typeparam name="T">The type of the shared resource.</typeparam>
    public static async ValueTask<AsyncSharedReferenceLease<T>> CreateAsync<T>(T value, Func<T, ValueTask> onRelease)
    {
        var owner = new AsyncSharedReferenceOwner<T>(value, onRelease);
        try
        {
            return owner.AddReference();
        }
        finally
        {
            var count = await owner.ReleaseReferenceAsync();
            Debug.Assert(count == 1);
        }
    }
}