#region Copyright Preamble

// Copyright @ 2024 NCode Group
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

#endregion

namespace NCode.Disposables.Tests;

public class AsyncDisposableAdapterTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDisposable_CreatesInstance()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_WithIdempotentTrue_CreatesInstance()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object, idempotent: true);

        Assert.NotNull(adapter);
    }

    [Fact]
    public void Constructor_WithIdempotentFalse_CreatesInstance()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object, idempotent: false);

        Assert.NotNull(adapter);
    }

    [Fact]
    public async Task Constructor_DefaultIdempotent_IsTrue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WithValidDisposable_InvokesDispose()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        await adapter.DisposeAsync();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenIdempotent_CalledMultipleTimes_DisposesOnce()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object, idempotent: true);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotent_CalledMultipleTimes_DisposesEachTime()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object, idempotent: false);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        mockDisposable.Verify(x => x.Dispose(), Times.Exactly(3));
    }

    [Fact]
    public async Task DisposeAsync_WhenUnderlyingDisposeThrows_PropagatesException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.DisposeAsync());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_WhenIdempotentAndFirstDisposeThrows_SubsequentCallsDoNotThrow()
    {
        var disposeCount = 0;
        var disposable = new TestDisposable(() =>
        {
            disposeCount++;
            if (disposeCount == 1)
            {
                throw new InvalidOperationException("First dispose");
            }
        });

        var adapter = new AsyncDisposableAdapter(disposable, idempotent: true);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.DisposeAsync());
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotentAndDisposeThrows_SubsequentCallsAlsoThrow()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object, idempotent: false);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.DisposeAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await adapter.DisposeAsync());

        mockDisposable.Verify(x => x.Dispose(), Times.Exactly(2));
    }

    [Fact]
    public async Task DisposeAsync_ReturnsCompletedValueTask()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        var valueTask = adapter.DisposeAsync();

        Assert.True(valueTask.IsCompletedSuccessfully);
        await valueTask;
    }

    [Fact]
    public async Task DisposeAsync_WhenIdempotent_ConcurrentCalls_DisposesOnce()
    {
        var disposeCount = 0;
        var disposable = new TestDisposable(() => Interlocked.Increment(ref disposeCount));

        var adapter = new AsyncDisposableAdapter(disposable, idempotent: true);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(async () => await adapter.DisposeAsync());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotent_ConcurrentCalls_DisposesEachTime()
    {
        var disposeCount = 0;
        var disposable = new TestDisposable(() => Interlocked.Increment(ref disposeCount));

        var adapter = new AsyncDisposableAdapter(disposable, idempotent: false);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(async () => await adapter.DisposeAsync());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentCalls, disposeCount);
    }

    [Fact]
    public async Task DisposeAsync_CanBeUsedWithAwaitUsing()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        await using (new AsyncDisposableAdapter(mockDisposable.Object))
        {
            // Use the adapter within await using block
        }

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region Helper Classes

    private sealed class TestDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose() => disposeAction();
    }

    #endregion
}
