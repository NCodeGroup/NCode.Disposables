// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableSerialTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_HoldsNull()
    {
        var serial = new DisposableSerial();

        Assert.Null(serial.Disposable);
        Assert.False(serial.IsDisposed);
    }

    [Fact]
    public void Constructor_WithDisposable_HoldsIt()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        var serial = new DisposableSerial(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var serial = new DisposableSerial();

        Assert.False(serial.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var serial = new DisposableSerial();

        serial.Dispose();

        Assert.True(serial.IsDisposed);
    }

    #endregion

    #region Disposable Property Tests

    [Fact]
    public void Disposable_Set_DisposesPrevious()
    {
        var mockFirst = new Mock<IDisposable>(MockBehavior.Strict);
        mockFirst.Setup(x => x.Dispose());
        var mockSecond = new Mock<IDisposable>(MockBehavior.Strict);
        var serial = new DisposableSerial(mockFirst.Object);

        serial.Disposable = mockSecond.Object;

        Assert.Same(mockSecond.Object, serial.Disposable);
        mockFirst.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Disposable_SetSameInstance_DoesNotDisposeIt()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Disposable = mockDisposable.Object;

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    [Fact]
    public void Disposable_SetNull_DisposesPreviousAndHoldsNull()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Disposable = null;

        Assert.Null(serial.Disposable);
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Disposable_SetAfterDispose_DisposesValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var serial = new DisposableSerial();
        serial.Dispose();

        serial.Disposable = mockDisposable.Object;

        Assert.Null(serial.Disposable);
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Disposable_GetAfterDispose_ReturnsNull()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var serial = new DisposableSerial(mockDisposable.Object);
        serial.Dispose();

        Assert.Null(serial.Disposable);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DisposesCurrent()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Twice_DisposesOnce()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Dispose();
        serial.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Dispose_ConcurrentWithSet_DisposesEveryValueOnce()
    {
        const int iterations = 100;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var mockInitial = new Mock<IDisposable>(MockBehavior.Strict);
            mockInitial.Setup(x => x.Dispose());
            var mockValue = new Mock<IDisposable>(MockBehavior.Strict);
            mockValue.Setup(x => x.Dispose());
            var serial = new DisposableSerial(mockInitial.Object);

            var setTask = Task.Run(() => serial.Disposable = mockValue.Object);

            var disposeTask = Task.Run(() => serial.Dispose());

            await Task.WhenAll(setTask, disposeTask);

            Assert.True(serial.IsDisposed);
            Assert.Null(serial.Disposable);
            mockInitial.Verify(x => x.Dispose(), Times.Once);
            mockValue.Verify(x => x.Dispose(), Times.Once);
        }
    }

    #endregion
}
