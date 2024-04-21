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

public class SharedReferenceLeaseTests
{
    [Fact]
    public void Value_Valid()
    {
        var value = new object();
        var onRelease = () => { };
        var mockOwner = new Mock<ISharedReferenceScope<object>>(MockBehavior.Strict);
        mockOwner
            .Setup(x => x.Value)
            .Returns(value)
            .Verifiable();
        var lease = new SharedReferenceLease<object>(mockOwner.Object, onRelease);
        Assert.Same(value, lease.Value);
        mockOwner.Verify();
    }

    [Fact]
    public void Dispose_Once()
    {
        var releaseCount = 0;
        var onRelease = () => { ++releaseCount; };
        var mockOwner = new Mock<ISharedReferenceScope<object>>(MockBehavior.Strict);
        var lease = new SharedReferenceLease<object>(mockOwner.Object, onRelease);
        lease.Dispose();
        lease.Dispose();
        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public void AddReference_Valid()
    {
        var onRelease = () => { };
        var mockOwner = new Mock<ISharedReferenceScope<object>>(MockBehavior.Strict);
        var mockReference = new Mock<ISharedReferenceScope<object>>(MockBehavior.Strict);
        mockOwner
            .Setup(x => x.AddReference())
            .Returns(mockReference.Object)
            .Verifiable();
        var lease = new SharedReferenceLease<object>(mockOwner.Object, onRelease);
        var reference = lease.AddReference();
        Assert.Same(mockReference.Object, reference);
        mockOwner.Verify();
    }

    [Fact]
    public void TryAddReference_Valid()
    {
        var onRelease = () => { };
        var mockOwner = new Mock<ISharedReferenceScope<object>>(MockBehavior.Strict);
        var mockReference = new Mock<ISharedReferenceScope<object>>(MockBehavior.Strict);
        ISharedReferenceScope<object>? newReference = mockReference.Object;
        mockOwner
            .Setup(x => x.TryAddReference(out newReference))
            .Returns(true)
            .Verifiable();
        var lease = new SharedReferenceLease<object>(mockOwner.Object, onRelease);
        var result = lease.TryAddReference(out var reference);
        Assert.True(result);
        Assert.Same(mockReference.Object, reference);
        mockOwner.Verify();
    }
}