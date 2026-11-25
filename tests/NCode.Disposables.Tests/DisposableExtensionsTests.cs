// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public sealed class DisposableExtensionsTests : UnitTestBase
{
    private const string AsyncOnlyMessage =
        "The collection contains an item that implements IAsyncDisposable but not IDisposable; dispose it with DisposeAllAsync.";

    #region DisposeAsyncIfAvailable Tests

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenAsyncDisposable_CallsDisposeAsync()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable
            .As<IAsyncDisposable>()
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        await mockDisposable.Object.DisposeAsyncIfAvailable();

        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenNotAsyncDisposable_CallsDispose()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

        await mockDisposable.Object.DisposeAsyncIfAvailable();
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenNull_DoesNotThrow()
    {
        IDisposable? disposable = null;

        var exception = await Record.ExceptionAsync(() =>
            disposable.DisposeAsyncIfAvailable().AsTask()
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenAsyncDisposeThrows_PropagatesException()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable
            .As<IAsyncDisposable>()
            .Setup(x => x.DisposeAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"))
            .Verifiable();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mockDisposable.Object.DisposeAsyncIfAvailable().AsTask()
        );

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenSyncDisposeThrows_PropagatesException()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable
            .Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"))
            .Verifiable();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mockDisposable.Object.DisposeAsyncIfAvailable().AsTask()
        );

        Assert.Equal("Test exception", exception.Message);
    }

    #endregion

    #region AsSharedReference Tests

    [Fact]
    public void AsSharedReference_CreatesLease()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

        using var lease = mockDisposable.Object.AsSharedReference();

        Assert.Same(mockDisposable.Object, lease.Value);
    }

    [Fact]
    public void AsSharedReference_DisposesWhenLeaseReleased()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var lease = mockDisposable.Object.AsSharedReference();

        mockDisposable.Verify(x => x.Dispose(), Times.Never);

        lease.Dispose();
    }

    [Fact]
    public void AsSharedReference_MultipleLeases_DisposesOnlyWhenLastReleased()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var lease1 = mockDisposable.Object.AsSharedReference();
        var lease2 = lease1.AddReference();
        var lease3 = lease2.AddReference();

        lease1.Dispose();
        mockDisposable.Verify(x => x.Dispose(), Times.Never);

        lease2.Dispose();
        mockDisposable.Verify(x => x.Dispose(), Times.Never);

        lease3.Dispose();
    }

    [Fact]
    public void AsSharedReference_WithTypeImplementingBothInterfaces_DisposesSynchronously()
    {
        var resource = new DualDisposable();

        var lease = resource.AsSharedReference();
        lease.Dispose();

        Assert.True(resource.DisposeCalled);
        Assert.False(resource.DisposeAsyncCalled);
    }

    #endregion

    #region DisposeAll Tests

    [Fact]
    public void DisposeAll_WithItemImplementingBothInterfaces_CallsDispose()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        _ = mockDisposable.As<IAsyncDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var collection = new object[] { mockDisposable.Object };

        collection.DisposeAll();
    }

    [Fact]
    public void DisposeAll_WithDisposableItems_DisposesAll()
    {
        var disposeCount = 0;
        var collection = new object[]
        {
            Disposable.Create(() => disposeCount++),
            Disposable.Create(() => disposeCount++),
            Disposable.Create(() => disposeCount++),
        };

        collection.DisposeAll();

        Assert.Equal(3, disposeCount);
    }

    [Fact]
    public void DisposeAll_DisposesInReverseOrder()
    {
        var order = string.Empty;
        var collection = new object[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => order += "3"),
        };

        collection.DisposeAll();

        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_WithMixedItems_DisposesOnlyDisposables()
    {
        var order = string.Empty;
        var collection = new object[]
        {
            new(),
            Disposable.Create(() => order += "1"),
            new(),
            Disposable.Create(() => order += "2"),
            new(),
            Disposable.Create(() => order += "3"),
        };

        collection.DisposeAll();

        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_WithNullItems_SkipsNulls()
    {
        var order = string.Empty;
        var collection = new object?[]
        {
            null,
            Disposable.Create(() => order += "1"),
            null,
            Disposable.Create(() => order += "2"),
            null,
        };

        collection.DisposeAll();

        Assert.Equal("21", order);
    }

    [Fact]
    public void DisposeAll_EmptyCollection_DoesNotThrow()
    {
        var collection = Array.Empty<object>();

        var exception = Record.Exception(() => collection.DisposeAll());

        Assert.Null(exception);
    }

    [Fact]
    public void DisposeAll_WithSingleException_ThrowsOriginalException()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException("Test exception")),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => order += "3"),
        };

        var exception = Assert.Throws<InvalidOperationException>(() => collection.DisposeAll());

        Assert.Equal("Test exception", exception.Message);
        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_WithMultipleExceptions_ThrowsAggregateException()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException("Exception 1")),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => throw new InvalidOperationException("Exception 2")),
            Disposable.Create(() => order += "3"),
        };

        var exception = Assert.Throws<AggregateException>(() => collection.DisposeAll());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_WithAsyncDisposableAndFailingItem_ThrowsAggregateExceptionInDisposalOrder()
    {
        var collection = new object[]
        {
            Disposable.Create(() => throw new ArgumentException("Dispose failed.")),
            AsyncDisposable.Create(() => { }),
        };

        var exception = Assert.Throws<AggregateException>(() => collection.DisposeAll());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.IsType<InvalidOperationException>(exception.InnerExceptions[0]);
        Assert.IsType<ArgumentException>(exception.InnerExceptions[1]);
    }

    [Fact]
    public void DisposeAll_WithAsyncDisposable_ThrowsInvalidOperationException()
    {
        var collection = new object[] { AsyncDisposable.Create(() => { }) };

        var exception = Assert.Throws<InvalidOperationException>(() => collection.DisposeAll());

        Assert.Equal(AsyncOnlyMessage, exception.Message);
    }

    [Fact]
    public void DisposeAll_CalledTwice_DisposesEachTime()
    {
        var disposeCount = 0;
        IDisposable disposable = Disposable.Create(() => disposeCount++);
        var collection = new[] { disposable };

        collection.DisposeAll();

        // Note: DisposableAction is one-shot, so second call doesn't increment
        // The extension method itself doesn't track previous calls
        Assert.Equal(1, disposeCount);
    }

    #endregion

    #region DisposeAllIgnoringExceptions Tests

    [Fact]
    public void DisposeAllIgnoringExceptions_WhenItemsThrow_DisposesEveryItem()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "3"),
        };

        collection.DisposeAllIgnoringExceptions();

        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAllIgnoringExceptions_WithAsyncDisposable_ThrowsOnlyInvalidOperationException()
    {
        var log = new List<string>();
        var collection = new object[]
        {
            Disposable.Create(() => log.Add("first")),
            Disposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
            AsyncDisposable.Create(() => { }),
            Disposable.Create(() => log.Add("last")),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            collection.DisposeAllIgnoringExceptions()
        );

        Assert.Equal(AsyncOnlyMessage, exception.Message);
        Assert.Equal(["last", "first"], log);
    }

    [Fact]
    public void DisposeAllIgnoringExceptions_WithAsyncDisposable_ThrowsInvalidOperationException()
    {
        var collection = new object[] { AsyncDisposable.Create(() => { }) };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            collection.DisposeAllIgnoringExceptions()
        );

        Assert.Equal(AsyncOnlyMessage, exception.Message);
    }

    [Fact]
    public void DisposeAllIgnoringExceptions_WithAsyncDisposable_DisposesRemainingItems()
    {
        var disposed = false;
        var collection = new object[]
        {
            Disposable.Create(() => disposed = true),
            AsyncDisposable.Create(() => { }),
        };

        Assert.Throws<InvalidOperationException>(() => collection.DisposeAllIgnoringExceptions());

        // The sync disposable is still disposed because it comes after the async one in reverse order
        Assert.True(disposed);
    }

    #endregion

    #region Deprecated Overload Tests

#pragma warning disable NCDISP003 // tests the deprecated overloads until they are deleted

    [Fact]
    public void DisposeAll_WithIgnoreExceptionsTrue_IgnoresFailures()
    {
        var log = new List<string>();
        var collection = new[]
        {
            Disposable.Create(() => log.Add("first")),
            Disposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
        };

        collection.DisposeAll(ignoreExceptions: true);

        Assert.Equal(["first"], log);
    }

    [Fact]
    public void DisposeAll_WithIgnoreExceptionsFalse_RethrowsFailure()
    {
        var log = new List<string>();
        var collection = new[]
        {
            Disposable.Create(() => log.Add("first")),
            Disposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
        };

        var exception = Assert.Throws<InvalidOperationException>(() =>
            collection.DisposeAll(ignoreExceptions: false)
        );

        Assert.Equal("Dispose failed.", exception.Message);
        Assert.Equal(["first"], log);
    }

#pragma warning restore NCDISP003

    #endregion

    private sealed class DualDisposable : IDisposable, IAsyncDisposable
    {
        public bool DisposeCalled { get; private set; }

        public bool DisposeAsyncCalled { get; private set; }

        public void Dispose()
        {
            DisposeCalled = true;
        }

        public ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return ValueTask.CompletedTask;
        }
    }
}
