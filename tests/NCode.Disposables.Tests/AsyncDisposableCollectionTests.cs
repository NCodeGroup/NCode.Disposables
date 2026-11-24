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
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyCollection()
    {
        var collection = new AsyncDisposableCollection();

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithIgnoreExceptionsFalse_CreatesEmptyCollection()
    {
        var collection = new AsyncDisposableCollection(ignoreExceptions: false);

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithIgnoreExceptionsTrue_CreatesEmptyCollection()
    {
        var collection = new AsyncDisposableCollection(ignoreExceptions: true);

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithCollection_CopiesElements()
    {
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        IAsyncDisposable[] items = [disposable1.Object, disposable2.Object];

        var collection = new AsyncDisposableCollection(items);

        Assert.Equal(2, collection.Count);
        Assert.Contains(disposable1.Object, collection);
        Assert.Contains(disposable2.Object, collection);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_CreatesEmptyCollection()
    {
        IAsyncDisposable[] items = [];

        var collection = new AsyncDisposableCollection(items);

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithNullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AsyncDisposableCollection(null!));
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_WithValidItem_AddsToCollection()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        collection.Add(disposable.Object);

        Assert.Single(collection);
        Assert.Contains(disposable.Object, collection);
    }

    [Fact]
    public void Add_MultipleItems_AddsAllToCollection()
    {
        var collection = new AsyncDisposableCollection();
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable3 = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);
        collection.Add(disposable3.Object);

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Add_WithNullItem_ThrowsArgumentNullException()
    {
        var collection = new AsyncDisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Add(null!));
    }

    [Fact]
    public void Add_SameItemTwice_AddsBothInstances()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        collection.Add(disposable.Object);
        collection.Add(disposable.Object);

        Assert.Equal(2, collection.Count);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ExistingItem_RemovesAndReturnsTrue()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var result = collection.Remove(disposable.Object);

        Assert.True(result);
        Assert.Empty(collection);
    }

    [Fact]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var result = collection.Remove(disposable.Object);

        Assert.False(result);
    }

    [Fact]
    public void Remove_WithNullItem_ThrowsArgumentNullException()
    {
        var collection = new AsyncDisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Remove(null!));
    }

    [Fact]
    public async Task Remove_DoesNotDisposeRemovedItem()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        collection.Remove(disposable.Object);
        await collection.DisposeAsync();

        disposable.Verify(x => x.DisposeAsync(), Times.Never);
    }

    [Fact]
    public void Remove_DuplicateItem_RemovesOnlyFirst()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);
        collection.Add(disposable.Object);

        var result = collection.Remove(disposable.Object);

        Assert.True(result);
        Assert.Single(collection);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_WithItems_RemovesAllItems()
    {
        var collection = new AsyncDisposableCollection();
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void Clear_EmptyCollection_Succeeds()
    {
        var collection = new AsyncDisposableCollection();

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public async Task Clear_DoesNotDisposeItems()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        collection.Clear();
        await collection.DisposeAsync();

        disposable.Verify(x => x.DisposeAsync(), Times.Never);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var result = collection.Contains(disposable.Object);

        Assert.True(result);
    }

    [Fact]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var result = collection.Contains(disposable.Object);

        Assert.False(result);
    }

    [Fact]
    public void Contains_WithNullItem_ThrowsArgumentNullException()
    {
        var collection = new AsyncDisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Contains(null!));
    }

    #endregion

    #region Count Tests

    [Fact]
    public void Count_EmptyCollection_ReturnsZero()
    {
        var collection = new AsyncDisposableCollection();

        Assert.Empty(collection);
    }

    [Fact]
    public void Count_WithItems_ReturnsCorrectCount()
    {
        var collection = new AsyncDisposableCollection();
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public async Task Count_AfterDispose_ReturnsZero()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable.Object);

        await collection.DisposeAsync();

        Assert.Empty(collection);
    }

    #endregion

    #region IsReadOnly Tests

    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        var collection = new AsyncDisposableCollection();

        Assert.False(collection.IsReadOnly);
    }

    #endregion

    #region CopyTo Tests

    [Fact]
    public void CopyTo_WithValidArray_CopiesElements()
    {
        var collection = new AsyncDisposableCollection();
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);
        var array = new IAsyncDisposable[2];

        collection.CopyTo(array, 0);

        Assert.Same(disposable1.Object, array[0]);
        Assert.Same(disposable2.Object, array[1]);
    }

    [Fact]
    public void CopyTo_WithOffset_CopiesAtCorrectPosition()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);
        var array = new IAsyncDisposable[3];

        collection.CopyTo(array, 1);

        Assert.Null(array[0]);
        Assert.Same(disposable.Object, array[1]);
        Assert.Null(array[2]);
    }

    [Fact]
    public void CopyTo_WithNullArray_ThrowsArgumentNullException()
    {
        var collection = new AsyncDisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
    }

    #endregion

    #region GetEnumerator Tests

    [Fact]
    public void GetEnumerator_EmptyCollection_ReturnsEmptyEnumerator()
    {
        var collection = new AsyncDisposableCollection();

        var items = collection.ToList();

        Assert.Empty(items);
    }

    [Fact]
    public void GetEnumerator_WithItems_EnumeratesAllItems()
    {
        var collection = new AsyncDisposableCollection();
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        var items = collection.ToList();

        Assert.Equal(2, items.Count);
        Assert.Same(disposable1.Object, items[0]);
        Assert.Same(disposable2.Object, items[1]);
    }

    [Fact]
    public void GetEnumerator_NonGeneric_EnumeratesAllItems()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var enumerable = (System.Collections.IEnumerable)collection;
        var items = enumerable.Cast<IAsyncDisposable>().ToList();

        Assert.Single(items);
        Assert.Same(disposable.Object, items[0]);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_EmptyCollection_Succeeds()
    {
        var collection = new AsyncDisposableCollection();

        await collection.DisposeAsync();

        Assert.Empty(collection);
    }

    [Fact]
    public async Task DisposeAsync_WithItems_DisposesAllItems()
    {
        var collection = new AsyncDisposableCollection();
        var disposable1 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable1.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        disposable2.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        await collection.DisposeAsync();

        disposable1.Verify(x => x.DisposeAsync(), Times.Once);
        disposable2.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_DisposesInReverseOrder()
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

        await collection.DisposeAsync();

        Assert.Equal("654321", order);
    }

    [Fact]
    public async Task DisposeAsync_ClearsCollectionAfterDispose()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable.Object);

        await collection.DisposeAsync();

        Assert.Empty(collection);
    }

    [Fact]
    public async Task DisposeAsync_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var collection = new AsyncDisposableCollection();
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        collection.Add(disposable.Object);

        await collection.DisposeAsync();
        await collection.DisposeAsync();
        await collection.DisposeAsync();

        disposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task DisposeAsync_WhenItemThrows_PropagatesException()
    {
        var collection = new AsyncDisposableCollection(ignoreExceptions: false);
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.DisposeAsync()).ThrowsAsync(new InvalidOperationException("Test exception"));
        collection.Add(disposable.Object);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.DisposeAsync());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_WithIgnoreExceptions_SuppressesExceptions()
    {
        var disposeCount = 0;
        var collection = new AsyncDisposableCollection(ignoreExceptions: true);
        var throwingDisposable = new TestAsyncDisposable(() =>
        {
            Interlocked.Increment(ref disposeCount);
            throw new InvalidOperationException("Test exception");
        });
        var normalDisposable = new TestAsyncDisposable(() =>
        {
            Interlocked.Increment(ref disposeCount);
            return ValueTask.CompletedTask;
        });
        collection.Add(normalDisposable);
        collection.Add(throwingDisposable);

        await collection.DisposeAsync();

        Assert.Equal(2, disposeCount);
        Assert.Empty(collection);
    }

    [Fact]
    public async Task DisposeAsync_WithIgnoreExceptions_DisposesAllEvenWithMultipleExceptions()
    {
        var disposeCount = 0;
        var collection = new AsyncDisposableCollection(ignoreExceptions: true);

        for (var i = 0; i < 5; i++)
        {
            var disposable = new TestAsyncDisposable(() =>
            {
                Interlocked.Increment(ref disposeCount);
                throw new InvalidOperationException($"Exception {disposeCount}");
            });
            collection.Add(disposable);
        }

        await collection.DisposeAsync();

        Assert.Equal(5, disposeCount);
    }

    [Fact]
    public async Task DisposeAsync_ConcurrentCalls_DisposesOnlyOnce()
    {
        var disposeCount = 0;
        var collection = new AsyncDisposableCollection();
        var disposable = new TestAsyncDisposable(() =>
        {
            Interlocked.Increment(ref disposeCount);
            return ValueTask.CompletedTask;
        });
        collection.Add(disposable);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(async () => await collection.DisposeAsync());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, disposeCount);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIAsyncDisposableCollection()
    {
        var collection = new AsyncDisposableCollection();

        Assert.IsAssignableFrom<IAsyncDisposableCollection>(collection);
    }

    [Fact]
    public void ImplementsIAsyncDisposable()
    {
        var collection = new AsyncDisposableCollection();

        Assert.IsAssignableFrom<IAsyncDisposable>(collection);
    }

    [Fact]
    public void ImplementsICollectionOfIAsyncDisposable()
    {
        var collection = new AsyncDisposableCollection();

        Assert.IsAssignableFrom<ICollection<IAsyncDisposable>>(collection);
    }

    #endregion

    #region Helper Classes

    private sealed class TestAsyncDisposable(Func<ValueTask> disposeAction) : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => disposeAction();
    }

    #endregion
}
