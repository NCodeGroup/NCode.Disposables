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
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesInactiveLease()
    {
        var lease = new SharedReferenceLease<object>();

        Assert.False(lease.IsActive);
        Assert.Null(lease.OwnerOrNull);
    }

    [Fact]
    public void Constructor_WithOwner_CreatesActiveLease()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);

        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Same(mockOwner.Object, lease.OwnerOrNull);
    }

    #endregion

    #region IsActive Tests

    [Fact]
    public void IsActive_DefaultLease_ReturnsFalse()
    {
        var lease = new SharedReferenceLease<object>();

        Assert.False(lease.IsActive);
    }

    [Fact]
    public void IsActive_WithOwner_ReturnsTrue()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);

        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        Assert.True(lease.IsActive);
    }

    #endregion

    #region Value Tests

    [Fact]
    public void Value_WithActiveLease_ReturnsOwnerValue()
    {
        var value = new object();
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Returns(value);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var result = lease.Value;

        Assert.Same(value, result);
        mockOwner.Verify(x => x.Value, Times.Once);
    }

    [Fact]
    public void Value_WithInactiveLease_ThrowsInvalidOperationException()
    {
        var lease = new SharedReferenceLease<object>();

        var exception = Assert.Throws<InvalidOperationException>(() => lease.Value);

        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void Value_WithValueType_ReturnsCorrectValue()
    {
        const int value = 42;
        var mockOwner = new Mock<ISharedReferenceOwner<int>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Returns(value);
        var lease = new SharedReferenceLease<int>(mockOwner.Object);

        var result = lease.Value;

        Assert.Equal(value, result);
    }

    #endregion

    #region AddReference Tests

    [Fact]
    public void AddReference_WithActiveLease_ReturnsNewLease()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        mockOwner.Setup(x => x.AddReference()).Returns(newReference);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var reference = lease.AddReference();

        Assert.Same(mockOwner.Object, reference.OwnerOrNull);
        mockOwner.Verify(x => x.AddReference(), Times.Once);
    }

    [Fact]
    public void AddReference_WithInactiveLease_ThrowsInvalidOperationException()
    {
        var lease = new SharedReferenceLease<object>();

        var exception = Assert.Throws<InvalidOperationException>(() => lease.AddReference());

        Assert.Equal("The lease for the shared reference is not active.", exception.Message);
    }

    [Fact]
    public void AddReference_CalledMultipleTimes_ReturnsNewLeaseEachTime()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        mockOwner.Setup(x => x.AddReference()).Returns(newReference);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var reference1 = lease.AddReference();
        var reference2 = lease.AddReference();
        var reference3 = lease.AddReference();

        Assert.True(reference1.IsActive);
        Assert.True(reference2.IsActive);
        Assert.True(reference3.IsActive);
        mockOwner.Verify(x => x.AddReference(), Times.Exactly(3));
    }

    #endregion

    #region TryAddReference Tests

    [Fact]
    public void TryAddReference_WithActiveLease_ReturnsTrueAndNewLease()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        mockOwner
            .Setup(x => x.TryAddReference(out newReference))
            .Returns(true);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var result = lease.TryAddReference(out var reference);

        Assert.True(result);
        Assert.Same(mockOwner.Object, reference.OwnerOrNull);
    }

    [Fact]
    public void TryAddReference_WithInactiveLease_ReturnsFalseAndDefaultLease()
    {
        var lease = new SharedReferenceLease<object>();

        var result = lease.TryAddReference(out var reference);

        Assert.False(result);
        Assert.False(reference.IsActive);
        Assert.Null(reference.OwnerOrNull);
    }

    [Fact]
    public void TryAddReference_WhenOwnerReturnsFalse_ReturnsFalseAndDefaultLease()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        var outRef = new SharedReferenceLease<object>();
        mockOwner
            .Setup(x => x.TryAddReference(out outRef))
            .Returns(false);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var result = lease.TryAddReference(out var reference);

        Assert.False(result);
        Assert.False(reference.IsActive);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithActiveLease_ReleasesReference()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        lease.Dispose();

        mockOwner.Verify(x => x.ReleaseReference(), Times.Once);
    }

    [Fact]
    public void Dispose_WithInactiveLease_DoesNothing()
    {
        var lease = new SharedReferenceLease<object>();

        var exception = Record.Exception(() => lease.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_ReleasesReferenceMultipleTimes()
    {
        // Note: This is the documented behavior - struct leases are NOT idempotent-safe
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        lease.Dispose();
        lease.Dispose();
        lease.Dispose();

        mockOwner.Verify(x => x.ReleaseReference(), Times.Exactly(3));
    }

    [Fact]
    public void Dispose_ReturnsRemainingReferenceCount()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(5);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        lease.Dispose();

        mockOwner.Verify(x => x.ReleaseReference(), Times.Once);
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingStatement()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);

        using (var lease = new SharedReferenceLease<object>(mockOwner.Object))
        {
            Assert.True(lease.IsActive);
        }

        mockOwner.Verify(x => x.ReleaseReference(), Times.Once);
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingDeclaration()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);

        void TestMethod()
        {
            using var lease = new SharedReferenceLease<object>(mockOwner.Object);
            Assert.True(lease.IsActive);
        }

        TestMethod();

        mockOwner.Verify(x => x.ReleaseReference(), Times.Once);
    }

    #endregion

    #region Default Struct Behavior Tests

    [Fact]
    public void Default_AllOperationsBehaveCorrectly()
    {
        var lease = new SharedReferenceLease<object>();

        Assert.False(lease.IsActive);
        Assert.Null(lease.OwnerOrNull);
        Assert.Throws<InvalidOperationException>(() => lease.Value);
        Assert.Throws<InvalidOperationException>(() => lease.AddReference());

        var result = lease.TryAddReference(out var newLease);
        Assert.False(result);
        Assert.False(newLease.IsActive);

        // Dispose should be safe on default
        var exception = Record.Exception(() => lease.Dispose());
        Assert.Null(exception);
    }

    [Fact]
    public void Default_CanAssignToDefault()
    {
        var mockOwner = new Mock<ISharedReferenceOwner<object>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        Assert.True(lease.IsActive);

        lease.Dispose();
        lease = default;

        Assert.False(lease.IsActive);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposable()
    {
        var lease = new SharedReferenceLease<object>();

        Assert.IsAssignableFrom<IDisposable>(lease);
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void GenericType_WorksWithValueTypes()
    {
        const int value = 42;
        var mockOwner = new Mock<ISharedReferenceOwner<int>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Returns(value);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);

        using var lease = new SharedReferenceLease<int>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Equal(value, lease.Value);
    }

    [Fact]
    public void GenericType_WorksWithReferenceTypes()
    {
        const string value = "test";
        var mockOwner = new Mock<ISharedReferenceOwner<string>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Returns(value);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);

        using var lease = new SharedReferenceLease<string>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Equal(value, lease.Value);
    }

    [Fact]
    public void GenericType_WorksWithDisposableTypes()
    {
        var mockValue = new Mock<IDisposable>(MockBehavior.Strict);
        var mockOwner = new Mock<ISharedReferenceOwner<IDisposable>>(MockBehavior.Strict);
        mockOwner.Setup(x => x.Value).Returns(mockValue.Object);
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0);

        using var lease = new SharedReferenceLease<IDisposable>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Same(mockValue.Object, lease.Value);
    }

    #endregion
}
