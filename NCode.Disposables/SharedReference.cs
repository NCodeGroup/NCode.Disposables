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
/// Contains factory methods for creating <see cref="ISharedReferenceScope{T}"/> instances.
/// </summary>
public static class SharedReference
{
    private static void Dispose<T>(T value)
        where T : IDisposable
    {
        value.Dispose();
    }

    /// <summary>
    /// Creates a new <see cref="ISharedReferenceScope{T}"/> instance that uses reference counting to share the specified value.
    /// This variant will automatically dispose the value when the last reference is released.
    /// </summary>
    /// <param name="value">The underlying value to be shared.</param>
    /// <typeparam name="T">The type of the shared value.</typeparam>
    public static ISharedReferenceScope<T> Create<T>(T value)
        where T : IDisposable
    {
        return new SharedReferenceOwner<T>(value, Dispose);
    }

    /// <summary>
    /// Creates a new <see cref="ISharedReferenceScope{T}"/> instance that uses reference counting to share the specified value.
    /// This variant will call the specified <paramref name="onRelease"/> action when the last reference is released.
    /// </summary>
    /// <param name="value">The underlying value to be shared.</param>
    /// <param name="onRelease">The method to be called when the last reference is released.</param>
    /// <typeparam name="T">The type of the shared value.</typeparam>
    public static ISharedReferenceScope<T> Create<T>(T value, Action<T> onRelease)
    {
        return new SharedReferenceOwner<T>(value, onRelease);
    }
}