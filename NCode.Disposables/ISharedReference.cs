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
/// Represents an <see cref="IDisposable"/> resource that uses reference
/// counting and only disposes the underlying resource when all the
/// references have been released (i.e. reference count is zero).
/// </summary>
/// <remarks>
/// The very first instance of <see cref="ISharedReference{T}"/> will be
/// initialized with a count of one (1) and additional references will
/// increment that count until they are disposed.
/// </remarks>
/// <typeparam name="T">The type of shared reference.</typeparam>
public interface ISharedReference<T> : IDisposable
{
    /// <summary>
    /// Gets the value of the shared reference.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The reference count has reached zero (0)
    /// and the underlying resource has been disposed already.</exception>
    T Value { get; }

    /// <summary>
    /// Increments the reference count and returns a disposable resource that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The reference count has reached zero (0)
    /// and the underlying resource has been disposed already.</exception>
    ISharedReference<T> AddReference();

    /// <summary>
    /// Attempts to increment the reference count and outputs a disposable resource that
    /// can be used to decrement the newly incremented reference count.
    /// </summary>
    /// <param name="reference">Destination for the <see cref="ISharedReference{T}"/> instance
    /// if the original reference count is greater than zero (0).</param>
    /// <returns><c>true</c> if the original reference count was greater than zero (0) and
    /// a new shared reference was successfully created with an incremented reference count.</returns>
    bool TryAddReference([MaybeNullWhen(false)] out ISharedReference<T> reference);
}