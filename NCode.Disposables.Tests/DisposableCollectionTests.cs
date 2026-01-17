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

public class DisposableCollectionTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesEmptyCollection()
    {
        var collection = new DisposableCollection();

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithIgnoreExceptionsFalse_CreatesEmptyCollection()
    {
        var collection = new DisposableCollection(ignoreExceptions: false);

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithIgnoreExceptionsTrue_CreatesEmptyCollection()
    {
        var collection = new DisposableCollection(ignoreExceptions: true);

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithCollection_CopiesElements()
    {
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        IDisposable[] items = [disposable1.Object, disposable2.Object];

        var collection = new DisposableCollection(items);

        Assert.Equal(2, collection.Count);
        Assert.Contains(disposable1.Object, collection);
        Assert.Contains(disposable2.Object, collection);
    }

    [Fact]
    public void Constructor_WithEmptyCollection_CreatesEmptyCollection()
    {
        IDisposable[] items = [];

        var collection = new DisposableCollection(items);

        Assert.Empty(collection);
    }

    [Fact]
    public void Constructor_WithNullCollection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DisposableCollection(null!));
    }

    #endregion

    #region Add Tests

    [Fact]
    public void Add_WithValidItem_AddsToCollection()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        collection.Add(disposable.Object);

        Assert.Single(collection);
        Assert.Contains(disposable.Object, collection);
    }

    [Fact]
    public void Add_MultipleItems_AddsAllToCollection()
    {
        var collection = new DisposableCollection();
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable3 = new Mock<IDisposable>(MockBehavior.Strict);

        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);
        collection.Add(disposable3.Object);

        Assert.Equal(3, collection.Count);
    }

    [Fact]
    public void Add_WithNullItem_ThrowsArgumentNullException()
    {
        var collection = new DisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Add(null!));
    }

    [Fact]
    public void Add_SameItemTwice_AddsBothInstances()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        collection.Add(disposable.Object);
        collection.Add(disposable.Object);

        Assert.Equal(2, collection.Count);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void Remove_ExistingItem_RemovesAndReturnsTrue()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var result = collection.Remove(disposable.Object);

        Assert.True(result);
        Assert.Empty(collection);
    }

    [Fact]
    public void Remove_NonExistingItem_ReturnsFalse()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        var result = collection.Remove(disposable.Object);

        Assert.False(result);
    }

    [Fact]
    public void Remove_WithNullItem_ThrowsArgumentNullException()
    {
        var collection = new DisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Remove(null!));
    }

    [Fact]
    public void Remove_DoesNotDisposeRemovedItem()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        collection.Remove(disposable.Object);
        collection.Dispose();

        disposable.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public void Remove_DuplicateItem_RemovesOnlyFirst()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
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
        var collection = new DisposableCollection();
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void Clear_EmptyCollection_Succeeds()
    {
        var collection = new DisposableCollection();

        collection.Clear();

        Assert.Empty(collection);
    }

    [Fact]
    public void Clear_DoesNotDisposeItems()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        collection.Clear();
        collection.Dispose();

        disposable.Verify(x => x.Dispose(), Times.Never);
    }

    #endregion

    #region Contains Tests

    [Fact]
    public void Contains_ExistingItem_ReturnsTrue()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var result = collection.Contains(disposable.Object);

        Assert.True(result);
    }

    [Fact]
    public void Contains_NonExistingItem_ReturnsFalse()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);

        var result = collection.Contains(disposable.Object);

        Assert.False(result);
    }

    [Fact]
    public void Contains_WithNullItem_ThrowsArgumentNullException()
    {
        var collection = new DisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.Contains(null!));
    }

    #endregion

    #region Count Tests

    [Fact]
    public void Count_EmptyCollection_ReturnsZero()
    {
        var collection = new DisposableCollection();

        Assert.Empty(collection);
    }

    [Fact]
    public void Count_WithItems_ReturnsCorrectCount()
    {
        var collection = new DisposableCollection();
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        Assert.Equal(2, collection.Count);
    }

    [Fact]
    public void Count_AfterDispose_ReturnsZero()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.Dispose());
        collection.Add(disposable.Object);

        collection.Dispose();

        Assert.Empty(collection);
    }

    #endregion

    #region IsReadOnly Tests

    [Fact]
    public void IsReadOnly_ReturnsFalse()
    {
        var collection = new DisposableCollection();

        Assert.False(collection.IsReadOnly);
    }

    #endregion

    #region CopyTo Tests

    [Fact]
    public void CopyTo_WithValidArray_CopiesElements()
    {
        var collection = new DisposableCollection();
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);
        var array = new IDisposable[2];

        collection.CopyTo(array, 0);

        Assert.Same(disposable1.Object, array[0]);
        Assert.Same(disposable2.Object, array[1]);
    }

    [Fact]
    public void CopyTo_WithOffset_CopiesAtCorrectPosition()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);
        var array = new IDisposable[3];

        collection.CopyTo(array, 1);

        Assert.Null(array[0]);
        Assert.Same(disposable.Object, array[1]);
        Assert.Null(array[2]);
    }

    [Fact]
    public void CopyTo_WithNullArray_ThrowsArgumentNullException()
    {
        var collection = new DisposableCollection();

        Assert.Throws<ArgumentNullException>(() => collection.CopyTo(null!, 0));
    }

    #endregion

    #region GetEnumerator Tests

    [Fact]
    public void GetEnumerator_EmptyCollection_ReturnsEmptyEnumerator()
    {
        var collection = new DisposableCollection();

        var items = collection.ToList();

        Assert.Empty(items);
    }

    [Fact]
    public void GetEnumerator_WithItems_EnumeratesAllItems()
    {
        var collection = new DisposableCollection();
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
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
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        collection.Add(disposable.Object);

        var enumerable = (System.Collections.IEnumerable)collection;
        var items = enumerable.Cast<IDisposable>().ToList();

        Assert.Single(items);
        Assert.Same(disposable.Object, items[0]);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_EmptyCollection_Succeeds()
    {
        var collection = new DisposableCollection();

        collection.Dispose();

        Assert.Empty(collection);
    }

    [Fact]
    public void Dispose_WithItems_DisposesAllItems()
    {
        var collection = new DisposableCollection();
        var disposable1 = new Mock<IDisposable>(MockBehavior.Strict);
        var disposable2 = new Mock<IDisposable>(MockBehavior.Strict);
        disposable1.Setup(x => x.Dispose());
        disposable2.Setup(x => x.Dispose());
        collection.Add(disposable1.Object);
        collection.Add(disposable2.Object);

        collection.Dispose();

        disposable1.Verify(x => x.Dispose(), Times.Once);
        disposable2.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_DisposesInReverseOrder()
    {
        var order = string.Empty;
        var collection = new DisposableCollection();

        const int count = 6;
        for (var i = 1; i <= count; ++i)
        {
            var local = i;
            var disposable = new Mock<IDisposable>(MockBehavior.Strict);
            disposable.Setup(x => x.Dispose()).Callback(() => order += local);
            collection.Add(disposable.Object);
        }

        collection.Dispose();

        Assert.Equal("654321", order);
    }

    [Fact]
    public void Dispose_ClearsCollectionAfterDispose()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.Dispose());
        collection.Add(disposable.Object);

        collection.Dispose();

        Assert.Empty(collection);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DisposesOnlyOnce()
    {
        var collection = new DisposableCollection();
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.Dispose());
        collection.Add(disposable.Object);

        collection.Dispose();
        collection.Dispose();
        collection.Dispose();

        disposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_WhenItemThrows_PropagatesException()
    {
        var collection = new DisposableCollection(ignoreExceptions: false);
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        disposable.Setup(x => x.Dispose()).Throws(new InvalidOperationException("Test exception"));
        collection.Add(disposable.Object);

        var exception = Assert.Throws<InvalidOperationException>(() => collection.Dispose());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public void Dispose_WithIgnoreExceptions_SuppressesExceptions()
    {
        var disposeCount = 0;
        var collection = new DisposableCollection(ignoreExceptions: true);
        var throwingDisposable = new TestDisposable(() =>
        {
            Interlocked.Increment(ref disposeCount);
            throw new InvalidOperationException("Test exception");
        });
        var normalDisposable = new TestDisposable(() => Interlocked.Increment(ref disposeCount));
        collection.Add(normalDisposable);
        collection.Add(throwingDisposable);

        collection.Dispose();

        Assert.Equal(2, disposeCount);
        Assert.Empty(collection);
    }

    [Fact]
    public void Dispose_WithIgnoreExceptions_DisposesAllEvenWithMultipleExceptions()
    {
        var disposeCount = 0;
        var collection = new DisposableCollection(ignoreExceptions: true);

        for (var i = 0; i < 5; i++)
        {
            var disposable = new TestDisposable(() =>
            {
                Interlocked.Increment(ref disposeCount);
                throw new InvalidOperationException($"Exception {disposeCount}");
            });
            collection.Add(disposable);
        }

        collection.Dispose();

        Assert.Equal(5, disposeCount);
    }

    [Fact]
    public async Task Dispose_ConcurrentCalls_DisposesOnlyOnce()
    {
        var disposeCount = 0;
        var collection = new DisposableCollection();
        var disposable = new TestDisposable(() => Interlocked.Increment(ref disposeCount));
        collection.Add(disposable);

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => collection.Dispose());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingStatement()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        using (var collection = new DisposableCollection())
        {
            collection.Add(mockDisposable.Object);
        }

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposableCollection()
    {
        var collection = new DisposableCollection();

        Assert.IsAssignableFrom<IDisposableCollection>(collection);
    }

    [Fact]
    public void ImplementsIDisposable()
    {
        var collection = new DisposableCollection();

        Assert.IsAssignableFrom<IDisposable>(collection);
    }

    [Fact]
    public void ImplementsICollectionOfIDisposable()
    {
        var collection = new DisposableCollection();

        Assert.IsAssignableFrom<ICollection<IDisposable>>(collection);
    }

    #endregion

    #region Helper Classes

    private sealed class TestDisposable(Action disposeAction) : IDisposable
    {
        public void Dispose() => disposeAction();
    }

    #endregion
}
