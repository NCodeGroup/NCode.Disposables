// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableContextTests : UnitTestBase
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        var disposable = CreateStrictMock<IDisposable>();

        var result = new DisposableContext(disposable.Object, context.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_WithNullDisposable_ThrowsArgumentNullException()
    {
        var context = CreateStrictMock<SynchronizationContext>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DisposableContext(null!, context.Object)
        );

        Assert.Equal("disposable", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var disposable = CreateStrictMock<IDisposable>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DisposableContext(disposable.Object, null!)
        );

        Assert.Equal("context", exception.ParamName);
    }

    #endregion

    #region Dispose Synchronous Tests

    [Fact]
    public void Dispose_Sync_UsesSendMethod()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var disposable = new DisposableContext(item.Object, context.Object);

        disposable.Dispose();
    }

    [Fact]
    public void Dispose_Sync_DoesNotCallOperationStartedOrCompleted()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable();

        var disposable = new DisposableContext(item.Object, context.Object);

        disposable.Dispose();

        context.Verify(x => x.OperationStarted(), Times.Never);
        context.Verify(x => x.OperationCompleted(), Times.Never);
    }

    #endregion

    #region Dispose Asynchronous Tests

    [Fact]
    public void Dispose_Async_UsesPostMethod()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable();
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable(Times.Once());
        context.Setup(x => x.OperationCompleted()).Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        disposable.Dispose();
    }

    [Fact]
    public void Dispose_Async_CallsOperationStartedBeforePost()
    {
        var callOrder = new List<string>();

        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.OperationStarted())
            .Callback(() => callOrder.Add("OperationStarted"))
            .Verifiable();
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback(
                (SendOrPostCallback c, object s) =>
                {
                    callOrder.Add("Post");
                    c(s);
                }
            )
            .Verifiable();
        context
            .Setup(x => x.OperationCompleted())
            .Callback(() => callOrder.Add("OperationCompleted"))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Callback(() => callOrder.Add("Dispose")).Verifiable();

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        disposable.Dispose();

        Assert.Equal(["OperationStarted", "Post", "Dispose", "OperationCompleted"], callOrder);
    }

    [Fact]
    public void Dispose_Async_CallsOperationCompleted()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();
        context.Setup(x => x.OperationCompleted()).Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable();

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        disposable.Dispose();
    }

    [Fact]
    public void Dispose_Async_CallsOperationCompletedEvenWhenDisposeThrows()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();
        context.Setup(x => x.OperationCompleted()).Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"))
            .Verifiable();

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());
    }

    [Fact]
    public void Dispose_Posting_WhenPostThrows_CompletesOperationAndPropagatesException()
    {
        const string message = "Post failed";

        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Throws(new InvalidOperationException(message))
            .Verifiable();
        context.Setup(x => x.OperationCompleted()).Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal(message, exception.Message);
        Assert.True(disposable.IsDisposed);
    }

    #endregion

    #region Idempotent Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var disposable = new DisposableContext(item.Object, context.Object);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();
    }

    [Fact]
    public async Task Dispose_ConcurrentCalls_DisposesOnlyOnce()
    {
        var disposeCount = 0;

        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose())
            .Callback(() => Interlocked.Increment(ref disposeCount))
            .Verifiable();

        var disposable = new DisposableContext(item.Object, context.Object);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => disposable.Dispose());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_Async_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable(Times.Once());
        context.Setup(x => x.OperationCompleted()).Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void Dispose_Sync_WhenDisposeThrows_PropagatesException()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"))
            .Verifiable();

        var disposable = new DisposableContext(item.Object, context.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void Dispose_Async_WhenDisposeThrows_PropagatesException()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context.Setup(x => x.OperationStarted()).Verifiable(Times.Once());
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();
        context.Setup(x => x.OperationCompleted()).Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"))
            .Verifiable();

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    #endregion

    #region Using Statement Tests

    [Fact]
    public void DisposableContext_CanBeUsedWithUsingStatement()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable(Times.Once());

        using (var disposable = new DisposableContext(item.Object, context.Object))
        {
            Assert.NotNull(disposable);
        }
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        var item = CreateStrictMock<IDisposable>();

        var disposable = new DisposableContext(item.Object, context.Object);

        Assert.False(disposable.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable();

        var disposable = new DisposableContext(item.Object, context.Object);

        disposable.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposable()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        var item = CreateStrictMock<IDisposable>();

        var disposable = new DisposableContext(item.Object, context.Object);

        Assert.IsAssignableFrom<IDisposable>(disposable);
    }

    #endregion
}
