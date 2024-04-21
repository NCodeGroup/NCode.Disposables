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

internal sealed class SharedReferenceOwner<T>(
    T value,
    Action<T> onRelease
) : ISharedReference<T>
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
    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
        {
            Release();
        }
    }

    private void Release()
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
                    onRelease(value);
                }

                return;
            }

            spinWait.SpinOnce();
        }
    }

    /// <inheritdoc />
    public ISharedReference<T> AddReference()
    {
        if (!TryAddReference(out var reference))
        {
            throw new ObjectDisposedException(GetType().FullName);
        }

        return reference;
    }

    /// <inheritdoc />
    public bool TryAddReference([MaybeNullWhen(false)] out ISharedReference<T> reference)
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
                reference = new SharedReferenceLease<T>(this, Release);
                return true;
            }

            spinWait.SpinOnce();
        }
    }
}