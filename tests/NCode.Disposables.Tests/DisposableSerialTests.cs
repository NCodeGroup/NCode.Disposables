// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableSerialTests : UnitTestBase
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
        var mockDisposable = CreateStrictMock<IDisposable>();

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
        var mockFirst = CreateStrictMock<IDisposable>();
        mockFirst.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var mockSecond = CreateStrictMock<IDisposable>();
        var serial = new DisposableSerial(mockFirst.Object);

        serial.Disposable = mockSecond.Object;

        Assert.Same(mockSecond.Object, serial.Disposable);
    }

    [Fact]
    public void Disposable_SetSameInstance_DoesNotDisposeIt()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Disposable = mockDisposable.Object;

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    [Fact]
    public void Disposable_SetNull_DisposesPrevious()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Disposable = null;
    }

    [Fact]
    public void Disposable_SetNull_HoldsNull()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Disposable = null;

        Assert.Null(serial.Disposable);
    }

    [Fact]
    public void Disposable_SetAfterDispose_DisposesValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var serial = new DisposableSerial();
        serial.Dispose();

        serial.Disposable = mockDisposable.Object;

        Assert.Null(serial.Disposable);
    }

    [Fact]
    public void Disposable_GetAfterDispose_ReturnsNull()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();
        var serial = new DisposableSerial(mockDisposable.Object);
        serial.Dispose();

        Assert.Null(serial.Disposable);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WhenHoldingValue_DisposesIt()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Dispose();
    }

    [Fact]
    public void Dispose_Twice_DisposesOnce()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var serial = new DisposableSerial(mockDisposable.Object);

        serial.Dispose();
        serial.Dispose();
    }

    [Fact]
    public async Task Dispose_WhileSetDisposesPrevious_DisposesNewValue()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new ManualResetEventSlim();
        var mockInitial = CreateStrictMock<IDisposable>();
        mockInitial
            .Setup(x => x.Dispose())
            .Callback(() =>
            {
                entered.SetResult();
                var released = release.Wait(TimeSpan.FromSeconds(30));
                Assert.True(released);
            })
            .Verifiable();
        var mockValue = CreateStrictMock<IDisposable>();
        mockValue.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var serial = new DisposableSerial(mockInitial.Object);

        var setTask = Task.Run(() => serial.Disposable = mockValue.Object);
        await entered.Task;

        serial.Dispose();

        Assert.False(setTask.IsCompleted);

        release.Set();
        await setTask;
    }

    [Fact]
    public async Task Dispose_ConcurrentWithSet_DisposesEveryValueOnce()
    {
        const int iterations = 100;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var mockInitial = CreateStrictMock<IDisposable>();
            mockInitial.Setup(x => x.Dispose()).Verifiable(Times.Once());
            var mockValue = CreateStrictMock<IDisposable>();
            mockValue.Setup(x => x.Dispose()).Verifiable(Times.Once());
            var serial = new DisposableSerial(mockInitial.Object);

            var setTask = Task.Run(() => serial.Disposable = mockValue.Object);

            var disposeTask = Task.Run(() => serial.Dispose());

            await Task.WhenAll(setTask, disposeTask);

            Assert.True(serial.IsDisposed);
            Assert.Null(serial.Disposable);
        }
    }

    #endregion
}
