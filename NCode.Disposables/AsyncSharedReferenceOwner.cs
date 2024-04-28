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
using System.Diagnostics.CodeAnalysis;

namespace NCode.Disposables;

internal sealed class AsyncSharedReferenceOwner<T>(
    T value,
    Func<T, ValueTask> onRelease
) : IAsyncSharedReference<T>
{
    private int _released;
    private ImmutableCounter _counter = new();

    /// <inheritdoc />
    public T Value
    {
        get
        {
            if (Volatile.Read(ref _counter).Count == 0)
            {
                throw new ObjectDisposedException(GetType().FullName);
            }

            return value;
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
        {
            await ReleaseAsync();
        }
    }

    private async ValueTask ReleaseAsync()
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var counter = Volatile.Read(ref _counter);
            if (counter.Count == 0)
            {
                Debug.Fail("We should never get here.");
                return;
            }

            var newCounter = counter.Decrement();
            var prevCounter = Interlocked.CompareExchange(ref _counter, newCounter, counter);
            if (ReferenceEquals(counter, prevCounter))
            {
                if (newCounter.Count == 0)
                {
                    await onRelease(value);
                }

                return;
            }

            spinWait.SpinOnce();
        }
    }

    /// <inheritdoc />
    public IAsyncSharedReference<T> AddReference()
    {
        if (!TryAddReference(out var reference))
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        return reference;
    }

    /// <inheritdoc />
    public bool TryAddReference([MaybeNullWhen(false)] out IAsyncSharedReference<T> reference)
    {
        var spinWait = new SpinWait();
        while (true)
        {
            var counter = Volatile.Read(ref _counter);
            if (counter.Count == 0)
            {
                reference = default;
                return false;
            }

            var newCounter = counter.Increment();
            var prevCounter = Interlocked.CompareExchange(ref _counter, newCounter, counter);
            if (ReferenceEquals(counter, prevCounter))
            {
                reference = new AsyncSharedReferenceLease<T>(this, ReleaseAsync);
                return true;
            }

            spinWait.SpinOnce();
        }
    }
}