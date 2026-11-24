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

public class DisposableAggregateTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyAggregate()
    {
        var aggregate = new DisposableAggregate();

        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public void Constructor_WithDisposable_SetsDisposable()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        var aggregate = new DisposableAggregate(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, aggregate.Disposable);
    }

    [Fact]
    public void Constructor_WithNull_CreatesEmptyAggregate()
    {
        var aggregate = new DisposableAggregate(null);

        Assert.Null(aggregate.Disposable);
    }

    #endregion

    #region Disposable Property Tests

    [Fact]
    public void Disposable_Get_ReturnsCurrentValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        var result = aggregate.Disposable;

        Assert.Same(mockDisposable.Object, result);
    }

    [Fact]
    public void Disposable_Set_UpdatesValue()
    {
        var mockDisposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        var aggregate = new DisposableAggregate(mockDisposable1.Object);

        aggregate.Disposable = mockDisposable2.Object;

        Assert.Same(mockDisposable2.Object, aggregate.Disposable);
    }

    [Fact]
    public void Disposable_SetNull_ClearsValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        aggregate.Disposable = null;

        Assert.Null(aggregate.Disposable);
    }

    [Fact]
    public void Disposable_SetDoesNotDisposePreviousInstance()
    {
        var mockDisposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        var aggregate = new DisposableAggregate(mockDisposable1.Object);

        aggregate.Disposable = mockDisposable2.Object;

        mockDisposable1.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public void Disposable_GetAfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        aggregate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
    }

    [Fact]
    public void Disposable_SetAfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        aggregate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable = new DisposableEmpty());
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithNoDisposable_Succeeds()
    {
        var aggregate = new DisposableAggregate();

        aggregate.Dispose();

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
    }

    [Fact]
    public void Dispose_WithDisposable_DisposesUnderlyingInstance()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        aggregate.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        aggregate.Dispose();
        aggregate.Dispose();
        aggregate.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WithNullDisposable_DoesNotThrow()
    {
        var aggregate = new DisposableAggregate(null);

        var exception = Record.Exception(() => aggregate.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_WhenUnderlyingDisposeThrows_PropagatesException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => aggregate.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void Dispose_AfterExceptionFromUnderlying_MarksAsDisposed()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));
        var aggregate = new DisposableAggregate(mockDisposable.Object);

        Assert.Throws<InvalidOperationException>(() => aggregate.Dispose());

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
    }

    [Fact]
    public void Dispose_DisposesCurrentValueAtTimeOfCall()
    {
        var mockDisposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var mockDisposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable2.Setup(x => x.Dispose());
        var aggregate = new DisposableAggregate(mockDisposable1.Object);

        aggregate.Disposable = mockDisposable2.Object;
        aggregate.Dispose();

        mockDisposable1.Verify(x => x.Dispose(), Times.Never);
        mockDisposable2.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingStatement()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        using (new DisposableAggregate(mockDisposable.Object))
        {
            // Use the aggregate within using block
        }

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Dispose_ConcurrentCalls_DisposesOnlyOnce()
    {
        var disposeCount = 0;
        var disposable = new TestDisposable(() => Interlocked.Increment(ref disposeCount));
        var aggregate = new DisposableAggregate(disposable);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => aggregate.Dispose());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, disposeCount);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task Disposable_ConcurrentGetAndSet_ThreadSafe()
    {
        var aggregate = new DisposableAggregate();
        const int iterations = 1000;
        var disposables = new IDisposable[iterations];

        for (var i = 0; i < iterations; i++)
        {
            disposables[i] = new DisposableEmpty();
        }

        var setTasks = new Task[iterations];
        var getTasks = new Task<IDisposable?>[iterations];

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
            var aggregate = new DisposableAggregate();
            var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
            mockDisposable.Setup(x => x.Dispose());

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

            var disposeTask = Task.Run(() => aggregate.Dispose());

            await Task.WhenAll(setTask, disposeTask);

            Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
        }
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposableAggregate()
    {
        var aggregate = new DisposableAggregate();

        Assert.IsAssignableFrom<IDisposableAggregate>(aggregate);
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        var aggregate = new DisposableAggregate();

        Assert.IsAssignableFrom<IDisposable>(aggregate);
    }

    #endregion

    #region Helper Classes

    private sealed class TestDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose() => disposeAction();
    }

    #endregion
}
