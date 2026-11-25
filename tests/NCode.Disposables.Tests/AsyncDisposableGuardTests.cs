// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableGuardTests
{
    private const string AlreadyReleasedMessage =
        "Ownership of the resource has already been released.";

    #region Constructor Tests

    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AsyncDisposableGuard<IAsyncDisposable>(null!)
        );

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Constructor_Value_HoldsValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, guard.Value);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WithoutRelease_DisposesValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        await guard.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_AfterRelease_DoesNotDisposeValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);
        guard.Release();

        await guard.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task DisposeAsync_Twice_DisposesOnce()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        await guard.DisposeAsync();
        await guard.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task AwaitUsingDeclaration_WhenScopeThrows_DisposesValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await Assert.ThrowsAsync<InvalidOperationException>(ThrowInScope);

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);

        async Task ThrowInScope()
        {
            await using var guard = new AsyncDisposableGuard<IAsyncDisposable>(
                mockDisposable.Object
            );
            throw new InvalidOperationException();
        }
    }

    [Fact]
    public async Task AwaitUsingDeclaration_WhenReleased_DoesNotDisposeValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        IAsyncDisposable released;
        await using (var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object))
        {
            released = guard.Release();
        }

        Assert.Same(mockDisposable.Object, released);
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Never);
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_WhenOwned_ReturnsValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        var result = guard.Release();

        Assert.Same(mockDisposable.Object, result);
    }

    [Fact]
    public void Release_Twice_ThrowsInvalidOperationException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);
        guard.Release();

        var exception = Assert.Throws<InvalidOperationException>(() => guard.Release());

        Assert.Equal(AlreadyReleasedMessage, exception.Message);
    }

    [Fact]
    public async Task Release_AfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);
        await guard.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => guard.Release());
    }

    [Fact]
    public async Task Release_AfterReleaseAndDispose_ThrowsInvalidOperationException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);
        guard.Release();
        await guard.DisposeAsync();

        Assert.Throws<InvalidOperationException>(() => guard.Release());
    }

    #endregion

    #region IsReleased Property Tests

    [Fact]
    public void IsReleased_BeforeRelease_ReturnsFalse()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        Assert.False(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterRelease_ReturnsTrue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        guard.Release();

        Assert.True(guard.IsReleased);
    }

    [Fact]
    public async Task IsReleased_AfterReleaseAndDispose_ReturnsTrue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);
        guard.Release();

        await guard.DisposeAsync();

        Assert.True(guard.IsReleased);
    }

    [Fact]
    public async Task IsReleased_AfterDisposeWithoutRelease_ReturnsFalse()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        await guard.DisposeAsync();

        Assert.False(guard.IsReleased);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDispose_ReturnsTrue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        await guard.DisposeAsync();

        Assert.True(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterReleaseWithoutDispose_ReturnsFalse()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);

        guard.Release();

        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterReleaseAndDispose_ReturnsTrue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var guard = new AsyncDisposableGuard<IAsyncDisposable>(mockDisposable.Object);
        guard.Release();

        await guard.DisposeAsync();

        Assert.True(guard.IsDisposed);
    }

    #endregion
}
