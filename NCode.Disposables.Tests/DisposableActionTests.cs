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

public class DisposableActionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidAction_CreatesInstance()
    {
        var action = () => { };

        var disposable = new DisposableAction(action);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void Constructor_WithNullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DisposableAction(null!));
    }

    [Fact]
    public void Constructor_WithIdempotentTrue_CreatesInstance()
    {
        var action = () => { };

        var disposable = new DisposableAction(action, idempotent: true);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void Constructor_WithIdempotentFalse_CreatesInstance()
    {
        var action = () => { };

        var disposable = new DisposableAction(action, idempotent: false);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void Constructor_DefaultIdempotent_IsTrue()
    {
        var count = 0;
        var disposable = new DisposableAction(() => count++);

        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, count);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_InvokesAction()
    {
        var count = 0;
        var disposable = new DisposableAction(() => count++);

        disposable.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Dispose_WhenIdempotent_CalledMultipleTimes_InvokesActionOnce()
    {
        var count = 0;
        var disposable = new DisposableAction(() => count++, idempotent: true);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Dispose_WhenNotIdempotent_CalledMultipleTimes_InvokesActionEachTime()
    {
        var count = 0;
        var disposable = new DisposableAction(() => count++, idempotent: false);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(3, count);
    }

    [Fact]
    public void Dispose_WhenActionThrows_PropagatesException()
    {
        var disposable = new DisposableAction(() => throw new InvalidOperationException("Test exception"));

        var exception = Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void Dispose_WhenIdempotentAndFirstDisposeThrows_SubsequentCallsDoNotThrow()
    {
        var disposeCount = 0;
        var disposable = new DisposableAction(() =>
        {
            disposeCount++;
            if (disposeCount == 1)
            {
                throw new InvalidOperationException("First dispose");
            }
        }, idempotent: true);

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_WhenNotIdempotentAndActionThrows_SubsequentCallsAlsoThrow()
    {
        var disposeCount = 0;
        var disposable = new DisposableAction(() =>
        {
            disposeCount++;
            throw new InvalidOperationException("Test exception");
        }, idempotent: false);

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());
        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.Equal(2, disposeCount);
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingStatement()
    {
        var count = 0;

        using (new DisposableAction(() => count++))
        {
            // Use the disposable within using block
        }

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task Dispose_WhenIdempotent_ConcurrentCalls_InvokesActionOnce()
    {
        var disposeCount = 0;
        var disposable = new DisposableAction(() => Interlocked.Increment(ref disposeCount), idempotent: true);

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
    public async Task Dispose_WhenNotIdempotent_ConcurrentCalls_InvokesActionEachTime()
    {
        var disposeCount = 0;
        var disposable = new DisposableAction(() => Interlocked.Increment(ref disposeCount), idempotent: false);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => disposable.Dispose());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(concurrentCalls, disposeCount);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposable()
    {
        var disposable = new DisposableAction(() => { });

        Assert.IsAssignableFrom<IDisposable>(disposable);
    }

    #endregion
}
