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

public class AsyncDisposableCollectionTests
{
    [Fact]
    public async Task Dispose_Count_IsEmpty()
    {
        var collection = new AsyncDisposableCollection();
        await collection.DisposeAsync();

        var count = collection.Count;
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task Add_Dispose()
    {
        var collection = new AsyncDisposableCollection();

        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable.Object);

        await collection.DisposeAsync();
        disposable.Verify(x => x.DisposeAsync(), Times.Once);
        Assert.Empty(collection);
    }

    [Fact]
    public async Task Add_Remove_Dispose()
    {
        var collection = new AsyncDisposableCollection();

        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable.Object);

        var contains = collection.Remove(disposable.Object);
        Assert.True(contains);

        await collection.DisposeAsync();
        disposable.Verify(x => x.DisposeAsync(), Times.Never);
        Assert.Empty(collection);
    }

    [Fact]
    public async Task Add_Clear_Dispose()
    {
        var collection = new AsyncDisposableCollection();

        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable.Object);
        collection.Clear();

        await collection.DisposeAsync();
        disposable.Verify(x => x.DisposeAsync(), Times.Never);
        Assert.Empty(collection);
    }

    [Fact]
    public void Add_Contains()
    {
        var collection = new AsyncDisposableCollection();

        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var contains = collection.Contains(disposable.Object);
        Assert.True(contains);
        Assert.Single(collection);
    }

    [Fact]
    public async Task Dispose_Reverse()
    {
        var order = string.Empty;
        var collection = new AsyncDisposableCollection();

        const int count = 6;
        for (var i = 1; i <= count; ++i)
        {
            var local = i;
            var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
            disposable.Setup(x => x.DisposeAsync()).Callback(() => order += local).Returns(ValueTask.CompletedTask);
            collection.Add(disposable.Object);
        }

        Assert.Equal(count, collection.Count);
        await collection.DisposeAsync();

        Assert.Empty(collection);
        Assert.Equal("654321", order);
    }
}