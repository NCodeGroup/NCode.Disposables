// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableSerialTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_HoldsNull()
    {
        var serial = new AsyncDisposableSerial();

        Assert.Null(serial.Disposable);
        Assert.False(serial.IsDisposed);
    }

    [Fact]
    public void Constructor_WithDisposable_HoldsIt()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var serial = new AsyncDisposableSerial(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var serial = new AsyncDisposableSerial();

        Assert.False(serial.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDispose_ReturnsTrue()
    {
        var serial = new AsyncDisposableSerial();

        await serial.DisposeAsync();

        Assert.True(serial.IsDisposed);
    }

    #endregion

    #region SetDisposableAsync Tests

    [Fact]
    public async Task SetDisposableAsync_Set_DisposesPrevious()
    {
        var mockFirst = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockFirst.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var mockSecond = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var serial = new AsyncDisposableSerial(mockFirst.Object);

        await serial.SetDisposableAsync(mockSecond.Object);

        Assert.Same(mockSecond.Object, serial.Disposable);
        mockFirst.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task SetDisposableAsync_SetSameInstance_DoesNotDisposeIt()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var serial = new AsyncDisposableSerial(mockDisposable.Object);

        await serial.SetDisposableAsync(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    [Fact]
    public async Task SetDisposableAsync_SetNull_DisposesPreviousAndHoldsNull()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var serial = new AsyncDisposableSerial(mockDisposable.Object);

        await serial.SetDisposableAsync(null);

        Assert.Null(serial.Disposable);
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task SetDisposableAsync_SetAfterDispose_DisposesValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var serial = new AsyncDisposableSerial();
        await serial.DisposeAsync();

        await serial.SetDisposableAsync(mockDisposable.Object);

        Assert.Null(serial.Disposable);
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task SetDisposableAsync_GetAfterDispose_ReturnsNull()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var serial = new AsyncDisposableSerial(mockDisposable.Object);
        await serial.DisposeAsync();

        Assert.Null(serial.Disposable);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_DisposesCurrent()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var serial = new AsyncDisposableSerial(mockDisposable.Object);

        await serial.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_Twice_DisposesOnce()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var serial = new AsyncDisposableSerial(mockDisposable.Object);

        await serial.DisposeAsync();
        await serial.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentWithSet_DisposesEveryValueOnce()
    {
        const int iterations = 100;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var mockInitial = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockInitial.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
            var mockValue = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockValue.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
            var serial = new AsyncDisposableSerial(mockInitial.Object);

            var setTask = Task.Run(() => serial.SetDisposableAsync(mockValue.Object).AsTask());

            var disposeTask = Task.Run(() => serial.DisposeAsync().AsTask());

            await Task.WhenAll(setTask, disposeTask);

            Assert.True(serial.IsDisposed);
            Assert.Null(serial.Disposable);
            mockInitial.Verify(x => x.DisposeAsync(), Times.Once);
            mockValue.Verify(x => x.DisposeAsync(), Times.Once);
        }
    }

    #endregion
}
