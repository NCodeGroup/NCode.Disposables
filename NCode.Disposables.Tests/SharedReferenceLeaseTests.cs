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
    public void Default_Valid()
    {
        var lease = new SharedReferenceLease<object>();
        Assert.False(lease.IsActive);
        Assert.Null(lease.OwnerOrNull);
        Assert.Throws<InvalidOperationException>(() => lease.Value);
        Assert.Throws<InvalidOperationException>(() => lease.AddReference());

        var result = lease.TryAddReference(out var newLease);
        Assert.False(result);
        Assert.False(newLease.IsActive);
        Assert.Null(newLease.OwnerOrNull);

        lease.Dispose();
        lease.Dispose();
        lease.Dispose();
    }

    [Fact]
    public void IsActive_Valid()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);
        Assert.True(lease.IsActive);
    }

    [Fact]
    public void Value_Valid()
    {
        var value = new object();

        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner
            .Setup(x => x.Value)
            .Returns(value)
            .Verifiable();

        var lease = new SharedReferenceLease<object>(mockOwner.Object);
        Assert.Same(value, lease.Value);

        mockOwner.Verify();
    }

    [Fact]
    public void Value_ThrowsWhenInactive()
    {
        var lease = new SharedReferenceLease<object>();
        var exception = Assert.Throws<InvalidOperationException>(() => lease.Value);
        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void AddReference_Valid()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);

        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        Assert.Same(mockOwner.Object, newReference.OwnerOrNull);

        var lease = new SharedReferenceLease<object>(mockOwner.Object);
        Assert.Same(mockOwner.Object, lease.OwnerOrNull);

        mockOwner
            .Setup(x => x.AddReference())
            .Returns(newReference)
            .Verifiable();

        var reference = lease.AddReference();
        Assert.Same(mockOwner.Object, reference.OwnerOrNull);

        mockOwner.Verify();
    }

    [Fact]
    public void AddReference_ThrowsWhenInactive()
    {
        var lease = new SharedReferenceLease<object>();
        var exception = Assert.Throws<InvalidOperationException>(() => lease.AddReference());
        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void TryAddReference_Valid()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);

        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        Assert.Same(mockOwner.Object, newReference.OwnerOrNull);

        mockOwner
            .Setup(x => x.TryAddReference(out newReference))
            .Returns(true)
            .Verifiable();

        var lease = new SharedReferenceLease<object>(mockOwner.Object);
        Assert.Same(mockOwner.Object, lease.OwnerOrNull);

        var result = lease.TryAddReference(out var reference);
        Assert.True(result);
        Assert.Same(mockOwner.Object, reference.OwnerOrNull);

        mockOwner.Verify();
    }

    [Fact]
    public void Dispose_Valid()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner
            .Setup(x => x.ReleaseReference())
            .Returns(0)
            .Verifiable();

        var lease = new SharedReferenceLease<object>(mockOwner.Object);
        lease.Dispose();

        mockOwner.Verify();
    }
}