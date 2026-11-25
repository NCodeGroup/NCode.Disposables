// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableSingleAssignmentTests : UnitTestBase
{
    private const string AlreadyAssignedMessage =
        "The disposable is already assigned; a single-assignment disposable takes one resource.";

    #region IsAssigned Property Tests

    [Fact]
    public void IsAssigned_BeforeAssignment_ReturnsFalse()
    {
        var holder = new DisposableSingleAssignment();

        Assert.False(holder.IsAssigned);
    }

    [Fact]
    public void IsAssigned_AfterAssignment_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var holder = new DisposableSingleAssignment();

        holder.Disposable = mockDisposable.Object;

        Assert.True(holder.IsAssigned);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var holder = new DisposableSingleAssignment();

        Assert.False(holder.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var holder = new DisposableSingleAssignment();

        holder.Dispose();

        Assert.True(holder.IsDisposed);
    }

    #endregion

    #region Disposable Property Tests

    [Fact]
    public void Disposable_Set_HoldsValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var holder = new DisposableSingleAssignment();

        holder.Disposable = mockDisposable.Object;

        Assert.Same(mockDisposable.Object, holder.Disposable);
    }

    [Fact]
    public void Disposable_SetTwice_ThrowsInvalidOperationException()
    {
        var mockFirst = CreateStrictMock<IDisposable>();
        var mockSecond = CreateStrictMock<IDisposable>();
        var holder = new DisposableSingleAssignment { Disposable = mockFirst.Object };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            holder.Disposable = mockSecond.Object
        );

        Assert.Equal(AlreadyAssignedMessage, exception.Message);
        Assert.Same(mockFirst.Object, holder.Disposable);
    }

    [Fact]
    public void Disposable_SetNull_ThrowsArgumentNullException()
    {
        var holder = new DisposableSingleAssignment();

        var exception = Assert.Throws<ArgumentNullException>(() => holder.Disposable = null!);

        Assert.Equal("value", exception.ParamName);
        Assert.False(holder.IsAssigned);
    }

    [Fact]
    public void Disposable_SetAfterDispose_DisposesValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var holder = new DisposableSingleAssignment();
        holder.Dispose();

        holder.Disposable = mockDisposable.Object;

        Assert.Null(holder.Disposable);
        Assert.True(holder.IsAssigned);
    }

    [Fact]
    public void Disposable_SetTwiceAfterDispose_ThrowsInvalidOperationException()
    {
        var mockFirst = CreateStrictMock<IDisposable>();
        mockFirst.Setup(x => x.Dispose()).Verifiable();
        var mockSecond = CreateStrictMock<IDisposable>();
        var holder = new DisposableSingleAssignment { Disposable = mockFirst.Object };
        holder.Dispose();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            holder.Disposable = mockSecond.Object
        );

        Assert.Equal(AlreadyAssignedMessage, exception.Message);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WhenAssigned_DisposesValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var holder = new DisposableSingleAssignment { Disposable = mockDisposable.Object };

        holder.Dispose();

        Assert.Null(holder.Disposable);
    }

    [Fact]
    public void Dispose_BeforeAssignment_DoesNotThrow()
    {
        var holder = new DisposableSingleAssignment();

        var exception = Record.Exception(() => holder.Dispose());

        Assert.Null(exception);
        Assert.True(holder.IsDisposed);
    }

    [Fact]
    public void Dispose_Twice_DisposesOnce()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var holder = new DisposableSingleAssignment { Disposable = mockDisposable.Object };

        holder.Dispose();
        holder.Dispose();
    }

    [Fact]
    public async Task Dispose_ConcurrentWithSet_DisposesValueOnce()
    {
        const int iterations = 100;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var mockValue = CreateStrictMock<IDisposable>();
            mockValue.Setup(x => x.Dispose()).Verifiable(Times.Once());
            var holder = new DisposableSingleAssignment();

            var setTask = Task.Run(() => holder.Disposable = mockValue.Object);

            var disposeTask = Task.Run(() => holder.Dispose());

            await Task.WhenAll(setTask, disposeTask);

            Assert.True(holder.IsDisposed);
            Assert.True(holder.IsAssigned);
            Assert.Null(holder.Disposable);
        }
    }

    #endregion
}
