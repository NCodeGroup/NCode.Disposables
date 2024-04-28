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

public class AsyncDisposableAggregateTests
{
    [Fact]
    public async Task DisposeAsync_Nothing_Once()
    {
        var aggregate = new AsyncDisposableAggregate();
        await aggregate.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Nothing_Multiple()
    {
        var aggregate = new AsyncDisposableAggregate();
        await aggregate.DisposeAsync();
        await aggregate.DisposeAsync();
        await aggregate.DisposeAsync();
    }

    [Fact]
    public async Task Set_Get()
    {
        var aggregate = new AsyncDisposableAggregate();

        var action = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        action.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        aggregate.Disposable = action.Object;

        Interlocked.MemoryBarrier();
        await Task.Yield();

        var get = aggregate.Disposable;
        Assert.Same(action.Object, get);
        action.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Set_SetNull_GetNull()
    {
        var aggregate = new AsyncDisposableAggregate();

        var action = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        action.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        aggregate.Disposable = action.Object;

        Interlocked.MemoryBarrier();
        await Task.Yield( /* do something, anything, etc... */);

        aggregate.Disposable = null;

        Interlocked.MemoryBarrier();
        await Task.Yield( /* do something, anything, etc... */);

        var get = aggregate.Disposable;
        Assert.Null(get);
        action.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public async Task Set_Dispose()
    {
        var aggregate = new AsyncDisposableAggregate();

        var action = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        action.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        aggregate.Disposable = action.Object;

        await aggregate.DisposeAsync();
        action.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Set_Dispose_Multiple()
    {
        var aggregate = new AsyncDisposableAggregate();

        var action = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        action.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        aggregate.Disposable = action.Object;

        await aggregate.DisposeAsync();
        await aggregate.DisposeAsync();
        await aggregate.DisposeAsync();
        action.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Set_Dispose_Get()
    {
        var aggregate = new AsyncDisposableAggregate();

        var action = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        action.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        aggregate.Disposable = action.Object;

        await aggregate.DisposeAsync();
        action.Verify(x => x.DisposeAsync(), Times.Once);

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
    }

    [Fact]
    public async Task Set_Dispose_Set()
    {
        var aggregate = new AsyncDisposableAggregate();

        var action = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        action.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        aggregate.Disposable = action.Object;

        await aggregate.DisposeAsync();
        action.Verify(x => x.DisposeAsync(), Times.Once);

        Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable = new AsyncDisposableEmpty());
    }
}