// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableGuardTests
{
    private const string AlreadyReleasedMessage =
        "Ownership of the resource has already been released.";

    #region Constructor Tests

    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DisposableGuard<IDisposable>(null!));
    }

    [Fact]
    public void Constructor_Value_HoldsValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, guard.Value);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithoutRelease_DisposesValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_AfterRelease_DoesNotDisposeValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        guard.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public void Dispose_Twice_DisposesOnce()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();
        guard.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void UsingDeclaration_WhenScopeThrows_DisposesValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        Assert.Throws<InvalidOperationException>(ThrowInScope);

        mockDisposable.Verify(x => x.Dispose(), Times.Once);

        void ThrowInScope()
        {
            using var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
            throw new InvalidOperationException();
        }
    }

    [Fact]
    public void UsingDeclaration_WhenReleased_DoesNotDisposeValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        IDisposable released;
        using (var guard = new DisposableGuard<IDisposable>(mockDisposable.Object))
        {
            released = guard.Release();
        }

        Assert.Same(mockDisposable.Object, released);
        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_ReturnsValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        var result = guard.Release();

        Assert.Same(mockDisposable.Object, result);
    }

    [Fact]
    public void Release_Twice_ThrowsInvalidOperationException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        var exception = Assert.Throws<InvalidOperationException>(() => guard.Release());

        Assert.Equal(AlreadyReleasedMessage, exception.Message);
    }

    [Fact]
    public void Release_AfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Dispose();

        Assert.Throws<ObjectDisposedException>(() => guard.Release());
    }

    [Fact]
    public void Release_AfterReleaseAndDispose_ThrowsInvalidOperationException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();
        guard.Dispose();

        Assert.Throws<InvalidOperationException>(() => guard.Release());
    }

    #endregion

    #region IsReleased Property Tests

    [Fact]
    public void IsReleased_BeforeRelease_ReturnsFalse()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        Assert.False(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterRelease_ReturnsTrue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Release();

        Assert.True(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterReleaseAndDispose_ReturnsTrue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        guard.Dispose();

        Assert.True(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterDisposeWithoutRelease_ReturnsFalse()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();

        Assert.False(guard.IsReleased);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();

        Assert.True(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterReleaseWithoutDispose_ReturnsFalse()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Release();

        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterReleaseAndDispose_ReturnsTrue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        guard.Dispose();

        Assert.True(guard.IsDisposed);
    }

    #endregion
}
