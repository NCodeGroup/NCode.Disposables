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

public class AsyncDisposableEmptyTests
{
    #region Singleton Tests

    [Fact]
    public void Singleton_ReturnsNonNullInstance()
    {
        var instance = AsyncDisposableEmpty.Singleton;

        Assert.NotNull(instance);
    }

    [Fact]
    public void Singleton_ReturnsSameInstance()
    {
        var first = AsyncDisposableEmpty.Singleton;
        var second = AsyncDisposableEmpty.Singleton;

        Assert.Same(first, second);
    }

    [Fact]
    public void AsyncDisposableEmpty_ReturnsAsyncDisposableEmptyType()
    {
        var instance = AsyncDisposableEmpty.Singleton;

        Assert.IsType<AsyncDisposableEmpty>(instance);
    }

    #endregion

    #region AsyncDisposable.Empty Tests

    [Fact]
    public void AsyncDisposable_Empty_ReturnsNonNullInstance()
    {
        var instance = AsyncDisposable.Empty;

        Assert.NotNull(instance);
    }

    [Fact]
    public void AsyncDisposable_Empty_ReturnsSameInstanceAsSingleton()
    {
        var empty = AsyncDisposable.Empty;
        var singleton = AsyncDisposableEmpty.Singleton;

        Assert.Same(singleton, empty);
    }

    [Fact]
    public void AsyncDisposable_Empty_ReturnsSameInstanceOnMultipleCalls()
    {
        var first = AsyncDisposable.Empty;
        var second = AsyncDisposable.Empty;

        Assert.Same(first, second);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_Succeeds()
    {
        var disposable = new AsyncDisposableEmpty();

        await disposable.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_Succeeds()
    {
        var disposable = new AsyncDisposableEmpty();

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_ReturnsCompletedValueTask()
    {
        var disposable = new AsyncDisposableEmpty();

        var valueTask = disposable.DisposeAsync();

        Assert.True(valueTask.IsCompletedSuccessfully);
        await valueTask;
    }

    [Fact]
    public async Task DisposeAsync_OnSingleton_Succeeds()
    {
        var singleton = AsyncDisposableEmpty.Singleton;

        await singleton.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_OnSingleton_CalledMultipleTimes_Succeeds()
    {
        var singleton = AsyncDisposableEmpty.Singleton;

        await singleton.DisposeAsync();
        await singleton.DisposeAsync();
        await singleton.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_CanBeUsedWithAwaitUsing()
    {
        await using (new AsyncDisposableEmpty())
        {
            // Use the empty disposable within await using block
        }
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentCalls_AllSucceed()
    {
        var disposable = new AsyncDisposableEmpty();

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(async () => await disposable.DisposeAsync());
        }

        await Task.WhenAll(tasks);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        var disposable = new AsyncDisposableEmpty();

        Assert.IsAssignableFrom<IAsyncDisposable>(disposable);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesNewInstance()
    {
        var instance1 = new AsyncDisposableEmpty();
        var instance2 = new AsyncDisposableEmpty();

        Assert.NotSame(instance1, instance2);
    }

    #endregion
}
