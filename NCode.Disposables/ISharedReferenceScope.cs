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
/// Represents an <see cref="IDisposable"/> resource that uses reference
/// counting and only disposes the underlying resource when all the
/// references have been released (i.e. reference count is zero).
/// </summary>
/// <remarks>
/// The very first instance of <see cref="ISharedReferenceScope{T}"/> will be
/// initialized with a count of one (1) and additional references will
/// increment that count until they are disposed.
/// </remarks>
/// <typeparam name="T">The type of shared reference.</typeparam>
public interface ISharedReferenceScope<T> : ISharedReferenceProvider<T>, IDisposable
{
    /// <summary>
    /// Gets the value of the shared reference.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The reference count has reached zero (0)
    /// and the underlying resource has been disposed already.</exception>
    T Value { get; }
}