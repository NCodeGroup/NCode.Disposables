// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableSingleAssignmentTests
{
    private const string AlreadyAssignedMessage =
        "The disposable is already assigned; a single-assignment disposable takes one resource.";

    #region IsAssigned Property Tests

    [Fact]
    public void IsAssigned_BeforeAssignment_ReturnsFalse()
    {
        var holder = new AsyncDisposableSingleAssignment();

        Assert.False(holder.IsAssigned);
    }

    [Fact]
    public async Task IsAssigned_AfterAssignment_ReturnsTrue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var holder = new AsyncDisposableSingleAssignment();

        await holder.SetDisposableAsync(mockDisposable.Object);

        Assert.True(holder.IsAssigned);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var holder = new AsyncDisposableSingleAssignment();

        Assert.False(holder.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDispose_ReturnsTrue()
    {
        var holder = new AsyncDisposableSingleAssignment();

        await holder.DisposeAsync();

        Assert.True(holder.IsDisposed);
    }

    #endregion

    #region SetDisposableAsync Tests

    [Fact]
    public async Task SetDisposableAsync_Set_HoldsValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var holder = new AsyncDisposableSingleAssignment();

        await holder.SetDisposableAsync(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, holder.Disposable);
    }

    [Fact]
    public async Task SetDisposableAsync_SetTwice_ThrowsInvalidOperationException()
    {
        var mockFirst = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var mockSecond = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var holder = new AsyncDisposableSingleAssignment();
        await holder.SetDisposableAsync(mockFirst.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await holder.SetDisposableAsync(mockSecond.Object)
        );

        Assert.Equal(AlreadyAssignedMessage, exception.Message);
        Assert.Same(mockFirst.Object, holder.Disposable);
    }

    [Fact]
    public async Task SetDisposableAsync_SetNull_ThrowsArgumentNullException()
    {
        var holder = new AsyncDisposableSingleAssignment();

        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await holder.SetDisposableAsync(null!)
        );

        Assert.False(holder.IsAssigned);
    }

    [Fact]
    public async Task SetDisposableAsync_SetAfterDispose_DisposesValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var holder = new AsyncDisposableSingleAssignment();
        await holder.DisposeAsync();

        await holder.SetDisposableAsync(mockDisposable.Object);

        Assert.Null(holder.Disposable);
        Assert.True(holder.IsAssigned);
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task SetDisposableAsync_SetTwiceAfterDispose_ThrowsInvalidOperationException()
    {
        var mockFirst = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockFirst.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var mockSecond = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var holder = new AsyncDisposableSingleAssignment();
        await holder.SetDisposableAsync(mockFirst.Object);
        await holder.DisposeAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await holder.SetDisposableAsync(mockSecond.Object)
        );

        Assert.Equal(AlreadyAssignedMessage, exception.Message);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_DisposesAssignedValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var holder = new AsyncDisposableSingleAssignment();
        await holder.SetDisposableAsync(mockDisposable.Object);

        await holder.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
        Assert.Null(holder.Disposable);
    }

    [Fact]
    public async Task DisposeAsync_BeforeAssignment_DoesNotThrow()
    {
        var holder = new AsyncDisposableSingleAssignment();

        var exception = await Record.ExceptionAsync(async () => await holder.DisposeAsync());

        Assert.Null(exception);
        Assert.True(holder.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_Twice_DisposesOnce()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var holder = new AsyncDisposableSingleAssignment();
        await holder.SetDisposableAsync(mockDisposable.Object);

        await holder.DisposeAsync();
        await holder.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentWithSet_DisposesValueOnce()
    {
        const int iterations = 100;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var mockValue = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockValue.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
            var holder = new AsyncDisposableSingleAssignment();

            var setTask = Task.Run(async () => await holder.SetDisposableAsync(mockValue.Object));

            var disposeTask = Task.Run(async () => await holder.DisposeAsync());

            await Task.WhenAll(setTask, disposeTask);

            Assert.True(holder.IsDisposed);
            Assert.True(holder.IsAssigned);
            Assert.Null(holder.Disposable);
            mockValue.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }

    #endregion
}
