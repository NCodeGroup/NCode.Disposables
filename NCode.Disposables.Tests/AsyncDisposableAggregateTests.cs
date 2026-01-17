#region Copyright Preamble

//
//    Copyright © 2017 NCode Group
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
//

#endregion

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
    public async Task Disposable_GetAfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await aggregate.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
    }

    [Fact]
    public async Task Disposable_SetAfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await aggregate.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable = new AsyncDisposableEmpty());
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WithNoDisposable_Succeeds()
    {
        var aggregate = new AsyncDisposableAggregate();

        await aggregate.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
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
        mockDisposable.Setup(x => x.DisposeAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await aggregate.DisposeAsync());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_AfterExceptionFromUnderlying_MarksAsDisposed()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));
        var aggregate = new AsyncDisposableAggregate(mockDisposable.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await aggregate.DisposeAsync());

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
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

            Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
        }
    }

    #endregion

    #region IAsyncDisposableAggregate Interface Tests

    [Fact]
    public void ImplementsIAsyncDisposableAggregate()
    {
        var aggregate = new AsyncDisposableAggregate();

        Assert.IsAssignableFrom<IAsyncDisposableAggregate>(aggregate);
    }

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
