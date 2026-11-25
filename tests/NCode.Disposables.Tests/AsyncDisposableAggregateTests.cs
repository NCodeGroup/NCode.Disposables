// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableAggregateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyAggregate()
    {
        var aggregate = new AsyncDisposableAggregate();

        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public void Constructor_WithDisposable_SetsDisposable()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, aggregate.Disposable);
    }

    [Fact]
    public void Constructor_WithNull_CreatesEmptyAggregate()
    {
        var aggregate = new AsyncDisposableAggregate(null);

        Assert.Null(aggregate.Disposable);
    }

    #endregion

    #region Disposable Property Tests

    [Fact]
    public void Disposable_Get_ReturnsCurrentValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        var result = aggregate.Disposable;

        Assert.Same(mockDisposable.Object, result);
    }

    [Fact]
    public void Disposable_Set_UpdatesValue()
    {
        var mockDisposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var aggregate = new AsyncDisposableAggregate(mockDisposable1.Object);

        aggregate.Disposable = mockDisposable2.Object;

        Assert.Same(mockDisposable2.Object, aggregate.Disposable);
    }

    [Fact]
    public void Disposable_SetNull_ClearsValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        aggregate.Disposable = null;

        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public void Disposable_SetDoesNotDisposePreviousInstance()
    {
        var mockDisposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var aggregate = new AsyncDisposableAggregate(mockDisposable1.Object);

        aggregate.Disposable = mockDisposable2.Object;

        mockDisposable1.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Disposable_GetAfterDispose_ReturnsNull()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await aggregate.DisposeAsync();

        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public async Task Disposable_SetAfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await aggregate.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() =>
            aggregate.Disposable = new AsyncDisposableEmpty()
        );
    }

    #endregion

    #region SetDisposableAsync Tests

    [Fact]
    public async Task SetDisposableAsync_BeforeDispose_ReplacesWithoutDisposingPrevious()
    {
        var mockDisposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var aggregate = new AsyncDisposableAggregate(mockDisposable1.Object);

        await aggregate.SetDisposableAsync(mockDisposable2.Object);

        Assert.Same(mockDisposable2.Object, aggregate.Disposable);
        mockDisposable1.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task SetDisposableAsync_AfterDispose_DisposesValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate();
        await aggregate.DisposeAsync();

        await aggregate.SetDisposableAsync(mockDisposable.Object);

        Assert.Null(aggregate.Disposable);
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task SetDisposableAsync_NullAfterDispose_DoesNothing()
    {
        var aggregate = new AsyncDisposableAggregate();
        await aggregate.DisposeAsync();

        var exception = await Record.ExceptionAsync(async () =>
            await aggregate.SetDisposableAsync(null)
        );

        Assert.Null(exception);
        Assert.True(aggregate.IsDisposed);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var aggregate = new AsyncDisposableAggregate();

        Assert.False(aggregate.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDispose_ReturnsTrue()
    {
        var aggregate = new AsyncDisposableAggregate();

        await aggregate.DisposeAsync();

        Assert.True(aggregate.IsDisposed);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WithNoDisposable_Succeeds()
    {
        var aggregate = new AsyncDisposableAggregate();

        await aggregate.DisposeAsync();

        Assert.True(aggregate.IsDisposed);
        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public async Task DisposeAsync_WithDisposable_DisposesUnderlyingInstance()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await aggregate.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await aggregate.DisposeAsync();
        await aggregate.DisposeAsync();
        await aggregate.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WithNullDisposable_DoesNotThrow()
    {
        var aggregate = new AsyncDisposableAggregate(null);

        var exception = await Record.ExceptionAsync(async () => await aggregate.DisposeAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_WhenUnderlyingDisposeThrows_PropagatesException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable
            .Setup(x => x.DisposeAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await aggregate.DisposeAsync()
        );

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_AfterExceptionFromUnderlying_MarksAsDisposed()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable
            .Setup(x => x.DisposeAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"));
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await aggregate.DisposeAsync()
        );

        Assert.True(aggregate.IsDisposed);
        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public async Task DisposeAsync_DisposesCurrentValueAtTimeOfCall()
    {
        var mockDisposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable2.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable1.Object);

        aggregate.Disposable = mockDisposable2.Object;
        await aggregate.DisposeAsync();

        mockDisposable1.Verify(x => x.DisposeAsync(), Times.Never);
        mockDisposable2.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentCalls_DisposesOnlyOnce()
    {
        var disposeCount = 0;
        var disposable = new TestAsyncDisposable(() =>
        {
            Interlocked.Increment(ref disposeCount);
            return ValueTask.CompletedTask;
        });
        var aggregate = new AsyncDisposableAggregate(disposable);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(async () => await aggregate.DisposeAsync());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, disposeCount);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Disposable_ConcurrentGetAndSet_ThreadSafe()
    {
        var aggregate = new AsyncDisposableAggregate();
        const int iterations = 1000;
        var disposables = new IAsyncDisposable[iterations];

        for (var i = 0; i < iterations; i++)
        {
            disposables[i] = new AsyncDisposableEmpty();
        }

        var setTasks = new Task[iterations];
        var getTasks = new Task<IAsyncDisposable?>[iterations];

        for (var i = 0; i < iterations; i++)
        {
            var index = i;
            setTasks[i] = Task.Run(() => aggregate.Disposable = disposables[index]);
            getTasks[i] = Task.Run(() => aggregate.Disposable);
        }

        await Task.WhenAll(setTasks);
        await Task.WhenAll(getTasks);

        var finalValue = aggregate.Disposable;
        Assert.Contains(finalValue, disposables);
    }

    [Fact]
    public async Task Disposable_ConcurrentSetWithDispose_HandlesRaceCondition()
    {
        const int iterations = 100;

        for (var iteration = 0; iteration < iterations; iteration++)
        {
            var aggregate = new AsyncDisposableAggregate();
            var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

            var setTask = Task.Run(() =>
            {
                try
                {
                    aggregate.Disposable = mockDisposable.Object;
                }
                catch (ObjectDisposedException)
                {
                    // Expected if dispose happens first
                }
            });

            var disposeTask = Task.Run(async () => await aggregate.DisposeAsync());

            await Task.WhenAll(setTask, disposeTask);

            Assert.True(aggregate.IsDisposed);
            Assert.Null(aggregate.Disposable);
        }
    }

    #endregion

    #region IAsyncDisposableAggregate Interface Tests

#pragma warning disable NCDISP002 // implements the deprecated interface until it is deleted
    [Fact]
    public void ImplementsIAsyncDisposableAggregate()
    {
        var aggregate = new AsyncDisposableAggregate();

        Assert.IsAssignableFrom<IAsyncDisposableAggregate>(aggregate);
    }
#pragma warning restore NCDISP002

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        var aggregate = new AsyncDisposableAggregate();

        Assert.IsAssignableFrom<IAsyncDisposable>(aggregate);
    }

    #endregion

    #region Helper Classes

    private sealed class TestAsyncDisposable(Func<ValueTask> disposeAction) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => disposeAction();
    }

    #endregion
}
