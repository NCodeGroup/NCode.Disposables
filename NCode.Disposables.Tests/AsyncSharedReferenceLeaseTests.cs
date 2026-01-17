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

public class AsyncSharedReferenceLeaseTests
{
    #region Default Value Tests

    [Fact]
    public void Default_IsActive_ReturnsFalse()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        Assert.False(lease.IsActive);
    }

    [Fact]
    public void Default_OwnerOrNull_ReturnsNull()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        Assert.Null(lease.OwnerOrNull);
    }

    [Fact]
    public void Default_Value_ThrowsInvalidOperationException()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var exception = Assert.Throws<InvalidOperationException>(() => lease.Value);

        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void Default_AddReference_ThrowsInvalidOperationException()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var exception = Assert.Throws<InvalidOperationException>(() => lease.AddReference());

        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void Default_TryAddReference_ReturnsFalse()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var result = lease.TryAddReference(out var newLease);

        Assert.False(result);
        Assert.False(newLease.IsActive);
        Assert.Null(newLease.OwnerOrNull);
    }

    [Fact]
    public async Task Default_DisposeAsync_Succeeds()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        await lease.DisposeAsync();
    }

    [Fact]
    public async Task Default_DisposeAsync_CalledMultipleTimes_Succeeds()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        await lease.DisposeAsync();
        await lease.DisposeAsync();
        await lease.DisposeAsync();
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithOwner_SetsOwner()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);

        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        Assert.Same(mockOwner.Object, lease.OwnerOrNull);
    }

    [Fact]
    public void Constructor_WithOwner_IsActiveReturnsTrue()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);

        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        Assert.True(lease.IsActive);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_WithOwner_ReturnsTrue()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);

        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        Assert.True(lease.IsActive);
    }

    [Fact]
    public void IsActive_WithoutOwner_ReturnsFalse()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        Assert.False(lease.IsActive);
    }

    #endregion

    #region Value Tests

    [Fact]
    public void Value_WhenActive_ReturnsOwnerValue()
    {
        var value = new object();
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Returns(value);
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        var result = lease.Value;

        Assert.Same(value, result);
        mockOwner.Verify(x => x.Value, Times.Once);
    }

    [Fact]
    public void Value_WhenInactive_ThrowsInvalidOperationException()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var exception = Assert.Throws<InvalidOperationException>(() => lease.Value);

        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void Value_WhenOwnerThrowsObjectDisposedException_PropagatesException()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Throws(new ObjectDisposedException("Test"));
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        Assert.Throws<ObjectDisposedException>(() => lease.Value);
    }

    #endregion

    #region AddReference Tests

    [Fact]
    public void AddReference_WhenActive_ReturnsNewLease()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        var newReference = new AsyncSharedReferenceLease<object>(mockOwner.Object);
        mockOwner.Setup(x => x.AddReference()).Returns(newReference);
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        var reference = lease.AddReference();

        Assert.Same(mockOwner.Object, reference.OwnerOrNull);
        mockOwner.Verify(x => x.AddReference(), Times.Once);
    }

    [Fact]
    public void AddReference_WhenInactive_ThrowsInvalidOperationException()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var exception = Assert.Throws<InvalidOperationException>(() => lease.AddReference());

        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void AddReference_WhenOwnerThrowsObjectDisposedException_PropagatesException()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.AddReference()).Throws(new ObjectDisposedException("Test"));
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        Assert.Throws<ObjectDisposedException>(() => lease.AddReference());
    }

    #endregion

    #region TryAddReference Tests

    [Fact]
    public void TryAddReference_WhenActiveAndOwnerReturnsTrue_ReturnsTrueWithNewLease()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        var newReference = new AsyncSharedReferenceLease<object>(mockOwner.Object);
        mockOwner
            .Setup(x => x.TryAddReference(out newReference))
            .Returns(true);
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        var result = lease.TryAddReference(out var reference);

        Assert.True(result);
        Assert.Same(mockOwner.Object, reference.OwnerOrNull);
    }

    [Fact]
    public void TryAddReference_WhenActiveAndOwnerReturnsFalse_ReturnsFalseWithDefault()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        var defaultReference = default(AsyncSharedReferenceLease<object>);
        mockOwner
            .Setup(x => x.TryAddReference(out defaultReference))
            .Returns(false);
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        var result = lease.TryAddReference(out var reference);

        Assert.False(result);
        Assert.False(reference.IsActive);
    }

    [Fact]
    public void TryAddReference_WhenInactive_ReturnsFalseWithDefault()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var result = lease.TryAddReference(out var reference);

        Assert.False(result);
        Assert.False(reference.IsActive);
        Assert.Null(reference.OwnerOrNull);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WhenActive_CallsOwnerReleaseReferenceAsync()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReferenceAsync()).ReturnsAsync(0);
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        await lease.DisposeAsync();

        mockOwner.Verify(x => x.ReleaseReferenceAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenInactive_DoesNotThrow()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        var exception = await Record.ExceptionAsync(async () => await lease.DisposeAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_CanBeUsedWithAwaitUsing()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReferenceAsync()).ReturnsAsync(0);

        await using (new AsyncSharedReferenceLease<object>(mockOwner.Object))
        {
            // Use the lease within await using block
        }

        mockOwner.Verify(x => x.ReleaseReferenceAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_CallsOwnerEachTime()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReferenceAsync()).ReturnsAsync(0);
        var lease = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        await lease.DisposeAsync();
        await lease.DisposeAsync();
        await lease.DisposeAsync();

        mockOwner.Verify(x => x.ReleaseReferenceAsync(), Times.Exactly(3));
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        var lease = new AsyncSharedReferenceLease<object>();

        Assert.IsAssignableFrom<IAsyncDisposable>(lease);
    }

    #endregion

    #region Struct Behavior Tests

    [Fact]
    public void Struct_IsValueType()
    {
        Assert.True(typeof(AsyncSharedReferenceLease<object>).IsValueType);
    }

    [Fact]
    public void Struct_CopyRetainsOwner()
    {
        var mockOwner = new Mock<IAsyncSharedReferenceOwner<object>>(MockBehavior.Strict);
        var lease1 = new AsyncSharedReferenceLease<object>(mockOwner.Object);

        var lease2 = lease1;

        Assert.Same(lease1.OwnerOrNull, lease2.OwnerOrNull);
    }

    [Fact]
    public void Struct_DefaultEqualsDefault()
    {
        var lease1 = new AsyncSharedReferenceLease<object>();
        var lease2 = new AsyncSharedReferenceLease<object>();

        Assert.Equal(lease1.OwnerOrNull, lease2.OwnerOrNull);
        Assert.Equal(lease1.IsActive, lease2.IsActive);
    }

    #endregion
}
