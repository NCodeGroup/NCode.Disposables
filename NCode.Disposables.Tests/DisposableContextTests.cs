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

public class DisposableContextTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_CreatesInstance()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        var result = new DisposableContext(disposable.Object, context.Object);

        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_WithAsyncFalse_CreatesInstance()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        var result = new DisposableContext(disposable.Object, context.Object, async: false);

        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_WithAsyncTrue_CreatesInstance()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        var result = new DisposableContext(disposable.Object, context.Object, async: true);

        Assert.NotNull(result);
    }

    [Fact]
    public void Constructor_WithNullDisposable_ThrowsArgumentNullException()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DisposableContext(null!, context.Object));

        Assert.Equal("disposable", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithNullContext_ThrowsArgumentNullException()
    {
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DisposableContext(disposable.Object, null!));

        Assert.Equal("context", exception.ParamName);
    }

    #endregion

    #region Dispose Synchronous Tests

    [Fact]
    public void Dispose_Sync_UsesSendMethod()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object);

        disposable.Dispose();

        context.Verify(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
        item.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Sync_DefaultParameter_UsesSendMethod()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object, async: false);

        disposable.Dispose();

        context.Verify(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
        item.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Sync_DoesNotCallOperationStartedOrCompleted()
    {
        var operationStartedCalled = false;
        var operationCompletedCalled = false;

        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));
        context
            .Setup(x => x.OperationStarted())
            .Callback(() => operationStartedCalled = true);
        context
            .Setup(x => x.OperationCompleted())
            .Callback(() => operationCompletedCalled = true);

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object, async: false);

        disposable.Dispose();

        Assert.False(operationStartedCalled);
        Assert.False(operationCompletedCalled);
    }

    #endregion

    #region Dispose Asynchronous Tests

    [Fact]
    public void Dispose_Async_UsesPostMethod()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context.Setup(x => x.OperationStarted());
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));
        context.Setup(x => x.OperationCompleted());

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object, async: true);

        disposable.Dispose();

        context.Verify(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
        item.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Async_CallsOperationStartedBeforePost()
    {
        var callOrder = new List<string>();

        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context.Setup(x => x.OperationStarted()).Callback(() => callOrder.Add("OperationStarted"));
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) =>
            {
                callOrder.Add("Post");
                c(s);
            });
        context.Setup(x => x.OperationCompleted()).Callback(() => callOrder.Add("OperationCompleted"));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose()).Callback(() => callOrder.Add("Dispose"));

        var disposable = new DisposableContext(item.Object, context.Object, async: true);

        disposable.Dispose();

        Assert.Equal(["OperationStarted", "Post", "Dispose", "OperationCompleted"], callOrder);
    }

    [Fact]
    public void Dispose_Async_CallsOperationCompleted()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context.Setup(x => x.OperationStarted());
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));
        context.Setup(x => x.OperationCompleted());

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object, async: true);

        disposable.Dispose();

        context.Verify(x => x.OperationStarted(), Times.Once);
        context.Verify(x => x.OperationCompleted(), Times.Once);
    }

    [Fact]
    public void Dispose_Async_CallsOperationCompletedEvenWhenDisposeThrows()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context.Setup(x => x.OperationStarted());
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));
        context.Setup(x => x.OperationCompleted());

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));

        var disposable = new DisposableContext(item.Object, context.Object, async: true);

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        context.Verify(x => x.OperationStarted(), Times.Once);
        context.Verify(x => x.OperationCompleted(), Times.Once);
    }

    #endregion

    #region Idempotent Dispose Tests

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        context.Verify(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
        item.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Dispose_ConcurrentCalls_DisposesOnlyOnce()
    {
        var disposeCount = 0;

        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose()).Callback(() => Interlocked.Increment(ref disposeCount));

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
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context.Setup(x => x.OperationStarted());
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));
        context.Setup(x => x.OperationCompleted());

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        var disposable = new DisposableContext(item.Object, context.Object, async: true);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        context.Verify(x => x.OperationStarted(), Times.Once);
        context.Verify(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
        context.Verify(x => x.OperationCompleted(), Times.Once);
        item.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public void Dispose_Sync_WhenDisposeThrows_PropagatesException()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));

        var disposable = new DisposableContext(item.Object, context.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void Dispose_Async_WhenDisposeThrows_PropagatesException()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context.Setup(x => x.OperationStarted());
        context.Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));
        context.Setup(x => x.OperationCompleted());

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));

        var disposable = new DisposableContext(item.Object, context.Object, async: true);

        var exception = Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    #endregion

    #region Using Statement Tests

    [Fact]
    public void DisposableContext_CanBeUsedWithUsingStatement()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s));

        var item = new Mock<IDisposable>(MockBehavior.Strict);
        item.Setup(x => x.Dispose());

        using (var disposable = new DisposableContext(item.Object, context.Object))
        {
            Assert.NotNull(disposable);
        }

        item.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposable()
    {
        var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
        var item = new Mock<IDisposable>(MockBehavior.Strict);

        var disposable = new DisposableContext(item.Object, context.Object);

        Assert.IsAssignableFrom<IDisposable>(disposable);
    }

    #endregion
}
