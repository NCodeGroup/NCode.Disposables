// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableContextTests : UnitTestBase
{
    private Mock<SynchronizationContext> CreateContext(List<string>? callOrder = null)
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.OperationStarted())
            .Callback(() => callOrder?.Add("OperationStarted"))
            .Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback(
                (SendOrPostCallback c, object s) =>
                {
                    callOrder?.Add("Post");
                    c(s);
                }
            )
            .Verifiable(Times.Once());
        context
            .Setup(x => x.OperationCompleted())
            .Callback(() => callOrder?.Add("OperationCompleted"))
            .Verifiable(Times.Once());
        return context;
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        var disposable = CreateStrictMock<IAsyncDisposable>();

        var result = new AsyncDisposableContext(disposable.Object, context.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_WithNullDisposable_ThrowsArgumentNullException()
    {
        var context = CreateStrictMock<SynchronizationContext>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AsyncDisposableContext(null!, context.Object)
        );

        Assert.Equal("disposable", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var disposable = CreateStrictMock<IAsyncDisposable>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AsyncDisposableContext(disposable.Object, null!)
        );

        Assert.Equal("context", exception.ParamName);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WhenNotDisposed_PostsToContext()
    {
        var context = CreateContext();

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable();

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        await disposable.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotDisposed_DisposesUnderlying()
    {
        var context = CreateContext();

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable(Times.Once());

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        await disposable.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhenNotDisposed_CallsOperationStartedAndCompleted()
    {
        var callOrder = new List<string>();
        var context = CreateContext(callOrder);

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync())
            .Callback(() => callOrder.Add("DisposeAsync"))
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        await disposable.DisposeAsync();

        Assert.Equal(["OperationStarted", "Post", "DisposeAsync", "OperationCompleted"], callOrder);
    }

    [Fact]
    public async Task DisposeAsync_WhenUnderlyingThrows_PropagatesException()
    {
        var context = CreateContext();

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"))
            .Verifiable();

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await disposable.DisposeAsync()
        );

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_WhenPostThrows_CompletesOperationAndPropagatesException()
    {
        const string message = "Post failed";

        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable(Times.Once());
        context.Setup(x => x.OperationCompleted()).Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Throws(new InvalidOperationException(message))
            .Verifiable();

        var item = CreateStrictMock<IAsyncDisposable>();

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await disposable.DisposeAsync()
        );

        Assert.Equal(message, exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_BeforePostedCallbackRuns_DoesNotComplete()
    {
        SendOrPostCallback? posted = null;
        object? postedState = null;

        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable();
        context.Setup(x => x.OperationCompleted()).Verifiable();
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback(
                (SendOrPostCallback c, object s) =>
                {
                    posted = c;
                    postedState = s;
                }
            )
            .Verifiable();

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable(Times.Once());

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        var disposeTask = disposable.DisposeAsync().AsTask();

        Assert.False(disposeTask.IsCompleted);
        item.Verify(x => x.DisposeAsync(), Times.Never);
        Assert.NotNull(posted);

        posted(postedState);

        await disposeTask;
    }

    [Fact]
    public async Task DisposeAsync_Twice_DisposesOnce()
    {
        var context = CreateContext();

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable(Times.Once());

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDisposeAsync_ReturnsFalse()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        var item = CreateStrictMock<IAsyncDisposable>();

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        Assert.False(disposable.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDisposeAsync_ReturnsTrue()
    {
        var context = CreateContext();

        var item = CreateStrictMock<IAsyncDisposable>();
        item.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask).Verifiable();

        var disposable = new AsyncDisposableContext(item.Object, context.Object);

        await disposable.DisposeAsync();

        Assert.True(disposable.IsDisposed);
    }

    #endregion
}
