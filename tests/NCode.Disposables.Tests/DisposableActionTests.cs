// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

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
        var disposable = new DisposableAction(() => count++);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Dispose_WhenNotIdempotent_CalledMultipleTimes_InvokesActionEachTime()
    {
        var count = 0;
        var disposable = Disposable.CreateRepeatable(() => count++);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(3, count);
    }

    [Fact]
    public void Dispose_WhenActionThrows_PropagatesException()
    {
        var disposable = new DisposableAction(() =>
            throw new InvalidOperationException("Test exception")
        );

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
        });

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_WhenNotIdempotentAndActionThrows_SubsequentCallsAlsoThrow()
    {
        var disposeCount = 0;
        var disposable = Disposable.CreateRepeatable(() =>
        {
            disposeCount++;
            throw new InvalidOperationException("Test exception");
        });

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
        var disposable = new DisposableAction(() => Interlocked.Increment(ref disposeCount));

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
        var disposable = Disposable.CreateRepeatable(() => Interlocked.Increment(ref disposeCount));

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

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var disposable = new DisposableAction(() => { });

        Assert.False(disposable.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var disposable = new DisposableAction(() => { });

        disposable.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void IsDisposed_NonIdempotentAfterDispose_ReturnsTrue()
    {
        var disposable = Disposable.CreateRepeatable(() => { });

        disposable.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void IsDisposed_WhenActionThrows_ReturnsTrue()
    {
        var disposable = new DisposableAction(() =>
            throw new InvalidOperationException("Test exception")
        );

        Assert.Throws<InvalidOperationException>(() => disposable.Dispose());

        Assert.True(disposable.IsDisposed);
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

    #region Deprecated Overload Tests

#pragma warning disable NCDISP003 // tests the deprecated overloads until they are deleted

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
    public void Constructor_WithIdempotentFalse_InvokesActionEachTime()
    {
        var count = 0;
        var disposable = new DisposableAction(() => count++, idempotent: false);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(3, count);
    }

    [Fact]
    public void Create_WithIdempotentFalse_InvokesActionEachTime()
    {
        var count = 0;
        var disposable = Disposable.Create(() => count++, idempotent: false);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(3, count);
    }

#pragma warning restore NCDISP003

    #endregion
}
