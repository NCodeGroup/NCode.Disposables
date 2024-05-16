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

namespace NCode.Disposables.Tests;

public class SharedReferenceOwnerTests
{
    // ohh, the memories from the good old COM days...
    private static int FinalRelease<T>(SharedReferenceOwner<T> owner)
    {
        var count = 0;
        while (true)
        {
            ++count;
            if (owner.ReleaseReference() == 0)
            {
                return count;
            }
        }
    }

    [Fact]
    public void Value_Valid()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object o) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);
        Assert.Same(value, owner.Value);
        Assert.Equal(0, releaseCount);
    }

    [Fact]
    public void Value_Throws_AfterFinalRelease()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object o) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var finalReleaseCount = FinalRelease(owner);
        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);

        Assert.Throws<ObjectDisposedException>(() => owner.Value);
    }

    [Fact]
    public void AddReference_Valid()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object o) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        _ = owner.AddReference();
        _ = owner.AddReference();

        var finalReleaseCount = FinalRelease(owner);
        Assert.Equal(1, releaseCount);
        Assert.Equal(3, finalReleaseCount);

        Assert.Throws<ObjectDisposedException>(() => owner.AddReference());
    }

    [Fact]
    public void AddReference_Throws_AfterDispose()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object o) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var finalReleaseCount = FinalRelease(owner);
        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);

        Assert.Throws<ObjectDisposedException>(() => owner.AddReference());
    }

    [Fact]
    public void TryAddReference_Fails_AfterDispose()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object o) => { ++releaseCount; };

        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var finalReleaseCount = FinalRelease(owner);

        var result = owner.TryAddReference(out var newReference);
        Assert.False(result);
        Assert.False(newReference.IsActive);

        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
    }
}