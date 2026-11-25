// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableAdapterTests : UnitTestBase
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDisposable_CreatesInstance()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        Assert.NotNull(adapter);
    }

    [Fact]
    public async Task Constructor_DefaultIdempotent_IsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WithValidDisposable_InvokesDispose()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenIdempotent_CalledMultipleTimes_DisposesOnce()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotent_CalledMultipleTimes_DisposesEachTime()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

        var adapter = AsyncDisposable.AdaptRepeatable(mockDisposable.Object);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        mockDisposable.Verify(x => x.Dispose(), Times.Exactly(3));
    }

    [Fact]
    public async Task DisposeAsync_WhenUnderlyingDisposeThrows_PropagatesException()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable
            .Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"))
            .Verifiable();

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await adapter.DisposeAsync()
        );

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

        var adapter = new AsyncDisposableAdapter(disposable);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await adapter.DisposeAsync()
        );
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotentAndDisposeThrows_SubsequentCallsAlsoThrow()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable
            .Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"))
            .Verifiable();

        var adapter = AsyncDisposable.AdaptRepeatable(mockDisposable.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await adapter.DisposeAsync()
        );
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await adapter.DisposeAsync()
        );

        mockDisposable.Verify(x => x.Dispose(), Times.Exactly(2));
    }

    [Fact]
    public async Task DisposeAsync_ReturnsCompletedValueTask()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

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

        var adapter = new AsyncDisposableAdapter(disposable);

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

        var adapter = AsyncDisposable.AdaptRepeatable(disposable);

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
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        await using (new AsyncDisposableAdapter(mockDisposable.Object))
        {
            // Use the adapter within await using block
        }
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        Assert.False(adapter.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDispose_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);

        await adapter.DisposeAsync();

        Assert.True(adapter.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_NonIdempotentAfterDispose_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

        var adapter = AsyncDisposable.AdaptRepeatable(mockDisposable.Object);

        await adapter.DisposeAsync();

        Assert.True(adapter.IsDisposed);
    }

    #endregion

    #region Helper Classes

    private sealed class TestDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose() => disposeAction();
    }

    #endregion
}
