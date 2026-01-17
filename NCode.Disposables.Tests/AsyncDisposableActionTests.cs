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

public class AsyncDisposableActionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullAction_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => new AsyncDisposableAction(null!));

        Assert.Equal("action", exception.ParamName);
    }

    [Fact]
    public void Constructor_WithValidAction_CreatesInstance()
    {
        var action = () => ValueTask.CompletedTask;

        var disposable = new AsyncDisposableAction(action);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void Constructor_WithIdempotentTrue_CreatesInstance()
    {
        var action = () => ValueTask.CompletedTask;

        var disposable = new AsyncDisposableAction(action, idempotent: true);

        Assert.NotNull(disposable);
    }

    [Fact]
    public void Constructor_WithIdempotentFalse_CreatesInstance()
    {
        var action = () => ValueTask.CompletedTask;

        var disposable = new AsyncDisposableAction(action, idempotent: false);

        Assert.NotNull(disposable);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WhenCalled_InvokesAction()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action);

        await disposable.DisposeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeAsync_WhenIdempotentAndCalledMultipleTimes_InvokesActionOnlyOnce()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action, idempotent: true);

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotentAndCalledMultipleTimes_InvokesActionEachTime()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action, idempotent: false);

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        Assert.Equal(3, count);
    }

    [Fact]
    public async Task DisposeAsync_WhenActionThrows_PropagatesException()
    {
        const string expectedMessage = "Test exception";
        var action = () =>
        {
            throw new InvalidOperationException(expectedMessage);
#pragma warning disable CS0162 // Unreachable code detected
            return ValueTask.CompletedTask;
#pragma warning restore CS0162 // Unreachable code detected
        };
        var disposable = new AsyncDisposableAction(action);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await disposable.DisposeAsync());

        Assert.Equal(expectedMessage, exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_WhenActionThrowsAndIdempotent_DoesNotInvokeAgain()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            throw new InvalidOperationException("Test exception");
#pragma warning disable CS0162 // Unreachable code detected
            return ValueTask.CompletedTask;
#pragma warning restore CS0162 // Unreachable code detected
        };
        var disposable = new AsyncDisposableAction(action, idempotent: true);

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await disposable.DisposeAsync());

        await disposable.DisposeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeAsync_WhenCalledConcurrently_InvokesActionOnlyOnce()
    {
        var count = 0;
        var action = () =>
        {
            Interlocked.Increment(ref count);
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action, idempotent: true);

        var tasks = Enumerable.Range(0, 100)
            .Select(_ => disposable.DisposeAsync().AsTask())
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeAsync_WhenNotIdempotentAndCalledConcurrently_InvokesActionMultipleTimes()
    {
        var count = 0;
        var action = () =>
        {
            Interlocked.Increment(ref count);
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action, idempotent: false);
        const int taskCount = 100;

        var tasks = Enumerable.Range(0, taskCount)
            .Select(_ => disposable.DisposeAsync().AsTask())
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(taskCount, count);
    }

    [Fact]
    public async Task DisposeAsync_WithAsyncAction_AwaitsCompletion()
    {
        var completed = false;
        var action = async () =>
        {
            await Task.Delay(10);
            completed = true;
            return;
        };
        Func<ValueTask> valueTaskAction = async () => await action();
        var disposable = new AsyncDisposableAction(valueTaskAction);

        await disposable.DisposeAsync();

        Assert.True(completed);
    }

    [Fact]
    public async Task DisposeAsync_DefaultIdempotentIsTrue_InvokesActionOnlyOnce()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action);

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        Assert.Equal(1, count);
    }

    #endregion
}
