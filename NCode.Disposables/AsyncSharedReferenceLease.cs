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

internal sealed class AsyncSharedReferenceLease<T>(
    IAsyncSharedReference<T> owner,
    Func<ValueTask> onRelease
) : IAsyncSharedReference<T>
{
    private int _released;

    /// <inheritdoc />
    public T Value => owner.Value;

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
        {
            await onRelease();
        }
    }

    /// <inheritdoc />
    public IAsyncSharedReference<T> AddReference() =>
        owner.AddReference();

    /// <inheritdoc />
    public bool TryAddReference([MaybeNullWhen(false)] out IAsyncSharedReference<T> reference) =>
        owner.TryAddReference(out reference);
}