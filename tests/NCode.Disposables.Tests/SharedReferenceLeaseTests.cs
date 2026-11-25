// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class SharedReferenceLeaseTests : UnitTestBase
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();

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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();

        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        Assert.True(lease.IsActive);
    }

    #endregion

    #region Value Tests

    [Fact]
    public void Value_WithActiveLease_ReturnsOwnerValue()
    {
        var value = new object();
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.Value).Returns(value).Verifiable(Times.Once());
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var result = lease.Value;

        Assert.Same(value, result);
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<int>>();
        mockOwner.Setup(x => x.Value).Returns(value).Verifiable();
        var lease = new SharedReferenceLease<int>(mockOwner.Object);

        var result = lease.Value;

        Assert.Equal(value, result);
    }

    #endregion

    #region AddReference Tests

    [Fact]
    public void AddReference_WithActiveLease_ReturnsNewLease()
    {
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        mockOwner.Setup(x => x.AddReference()).Returns(newReference).Verifiable(Times.Once());
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        var reference = lease.AddReference();

        Assert.Same(mockOwner.Object, reference.OwnerOrNull);
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        mockOwner.Setup(x => x.AddReference()).Returns(newReference).Verifiable();
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        var newReference = new SharedReferenceLease<object>(mockOwner.Object);
        mockOwner.Setup(x => x.TryAddReference(out newReference)).Returns(true).Verifiable();
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        var outRef = new SharedReferenceLease<object>();
        mockOwner.Setup(x => x.TryAddReference(out outRef)).Returns(false).Verifiable();
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable(Times.Once());
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        lease.Dispose();
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable();
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        lease.Dispose();
        lease.Dispose();
        lease.Dispose();

        mockOwner.Verify(x => x.ReleaseReference(), Times.Exactly(3));
    }

    [Fact]
    public void Dispose_ReturnsRemainingReferenceCount()
    {
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(5).Verifiable(Times.Once());
        var lease = new SharedReferenceLease<object>(mockOwner.Object);

        lease.Dispose();
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingStatement()
    {
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable(Times.Once());

        using (var lease = new SharedReferenceLease<object>(mockOwner.Object))
        {
            Assert.True(lease.IsActive);
        }
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingDeclaration()
    {
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable(Times.Once());

        void TestMethod()
        {
            using var lease = new SharedReferenceLease<object>(mockOwner.Object);
            Assert.True(lease.IsActive);
        }

        TestMethod();
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<object>>();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable();
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
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<int>>();
        mockOwner.Setup(x => x.Value).Returns(value).Verifiable();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable();

        using var lease = new SharedReferenceLease<int>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Equal(value, lease.Value);
    }

    [Fact]
    public void GenericType_WorksWithReferenceTypes()
    {
        const string value = "test";
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<string>>();
        mockOwner.Setup(x => x.Value).Returns(value).Verifiable();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable();

        using var lease = new SharedReferenceLease<string>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Equal(value, lease.Value);
    }

    [Fact]
    public void GenericType_WorksWithDisposableTypes()
    {
        var mockValue = CreateStrictMock<IDisposable>();
        var mockOwner = CreateStrictMock<ISharedReferenceOwner<IDisposable>>();
        mockOwner.Setup(x => x.Value).Returns(mockValue.Object).Verifiable();
        mockOwner.Setup(x => x.ReleaseReference()).Returns(0).Verifiable();

        using var lease = new SharedReferenceLease<IDisposable>(mockOwner.Object);

        Assert.True(lease.IsActive);
        Assert.Same(mockValue.Object, lease.Value);
    }

    #endregion
}
