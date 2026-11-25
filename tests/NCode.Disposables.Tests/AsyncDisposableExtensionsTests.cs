// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public sealed class AsyncDisposableExtensionsTests : IDisposable
{
    private readonly MockRepository _mocks = new(MockBehavior.Strict);

    public void Dispose()
    {
        _mocks.Verify();
    }

    #region DisposeAllAsync Tests

    [Fact]
    public async Task DisposeAllAsync_EmptyCollection_Succeeds()
    {
        var collection = Array.Empty<object>();

        await collection.DisposeAllAsync();
    }

    [Fact]
    public async Task DisposeAllAsync_WithAsyncDisposables_DisposesAll()
    {
        var disposeCount = 0;
        var collection = new IAsyncDisposable[]
        {
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                return ValueTask.CompletedTask;
            }),
        };

        await collection.DisposeAllAsync();

        Assert.Equal(3, disposeCount);
    }

    [Fact]
    public async Task DisposeAllAsync_WithSyncDisposables_DisposesAll()
    {
        var disposeCount = 0;
        var collection = new IDisposable[]
        {
            Disposable.Create(() => disposeCount++),
            Disposable.Create(() => disposeCount++),
            Disposable.Create(() => disposeCount++),
        };

        await collection.DisposeAllAsync();

        Assert.Equal(3, disposeCount);
    }

    [Fact]
    public async Task DisposeAllAsync_WithMixedDisposables_DisposesAll()
    {
        var order = string.Empty;
        var collection = new object[]
        {
            Disposable.Create(() => order += "1"),
            AsyncDisposable.Create(() =>
            {
                order += "2";
                return ValueTask.CompletedTask;
            }),
            Disposable.Create(() => order += "3"),
        };

        await collection.DisposeAllAsync();

        Assert.Equal("321", order);
    }

    [Fact]
    public async Task DisposeAllAsync_WithNonDisposables_SkipsNonDisposables()
    {
        var order = string.Empty;
        var collection = new object[]
        {
            new(),
            AsyncDisposable.Create(() =>
            {
                order += "1";
                return ValueTask.CompletedTask;
            }),
            new(),
            AsyncDisposable.Create(() =>
            {
                order += "2";
                return ValueTask.CompletedTask;
            }),
            new(),
            AsyncDisposable.Create(() =>
            {
                order += "3";
                return ValueTask.CompletedTask;
            }),
        };

        await collection.DisposeAllAsync();

        Assert.Equal("321", order);
    }

    [Fact]
    public async Task DisposeAllAsync_WithNullItems_SkipsNullItems()
    {
        var order = string.Empty;
        var collection = new object?[]
        {
            null,
            AsyncDisposable.Create(() =>
            {
                order += "1";
                return ValueTask.CompletedTask;
            }),
            null,
            AsyncDisposable.Create(() =>
            {
                order += "2";
                return ValueTask.CompletedTask;
            }),
            null,
        };

        await collection.DisposeAllAsync();

        Assert.Equal("21", order);
    }

    [Fact]
    public async Task DisposeAllAsync_DisposesInReverseOrder()
    {
        var order = string.Empty;

        const int count = 6;
        var collection = new IAsyncDisposable[count];
        for (var i = 1; i <= count; i++)
        {
            var local = i;
            collection[i - 1] = AsyncDisposable.Create(() =>
            {
                order += local;
                return ValueTask.CompletedTask;
            });
        }

        await collection.DisposeAllAsync();

        Assert.Equal("654321", order);
    }

    [Fact]
    public async Task DisposeAllAsync_WithItemImplementingBothInterfaces_CallsDisposeAsyncOnly()
    {
        var mockDisposable = _mocks.Create<IAsyncDisposable>();
        _ = mockDisposable.As<IDisposable>();
        mockDisposable
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable(Times.Once());
        var collection = new object[] { mockDisposable.Object };

        await collection.DisposeAllAsync();
    }

    #endregion

    #region DisposeAllAsync Exception Handling Tests

    [Fact]
    public async Task DisposeAllAsync_SingleException_ThrowsOriginalException()
    {
        var collection = new[]
        {
            AsyncDisposable.Create(() => ValueTask.CompletedTask),
            AsyncDisposable.Create(() => throw new InvalidOperationException("Test exception")),
            AsyncDisposable.Create(() => ValueTask.CompletedTask),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await collection.DisposeAllAsync()
        );

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAllAsync_MultipleExceptions_ThrowsAggregateException()
    {
        var collection = new[]
        {
            AsyncDisposable.Create(() => ValueTask.CompletedTask),
            AsyncDisposable.Create(() => throw new InvalidOperationException("Exception 1")),
            AsyncDisposable.Create(() => ValueTask.CompletedTask),
            AsyncDisposable.Create(() => throw new ArgumentException("Exception 2")),
            AsyncDisposable.Create(() => ValueTask.CompletedTask),
        };

        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            await collection.DisposeAllAsync()
        );

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e is ArgumentException);
        Assert.Contains(exception.InnerExceptions, e => e is InvalidOperationException);
    }

    [Fact]
    public async Task DisposeAllAsync_WhenItemsThrow_CollectsExceptions()
    {
        var disposeCount = 0;
        var collection = new[]
        {
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                throw new InvalidOperationException();
            }),
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                throw new InvalidOperationException();
            }),
            AsyncDisposable.Create(() =>
            {
                disposeCount++;
                return ValueTask.CompletedTask;
            }),
        };

        await Assert.ThrowsAsync<AggregateException>(async () =>
            await collection.DisposeAllAsync()
        );

        Assert.Equal(5, disposeCount);
    }

    [Fact]
    public async Task DisposeAllAsync_ExceptionInSyncDisposable_PropagatesException()
    {
        var collection = new object[]
        {
            Disposable.Create(() => { }),
            Disposable.Create(() => throw new InvalidOperationException("Sync exception")),
            Disposable.Create(() => { }),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await collection.DisposeAllAsync()
        );

        Assert.Equal("Sync exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAllAsync_ContinuesDisposingAfterException()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() =>
            {
                order += "1";
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() =>
            {
                order += "2";
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() =>
            {
                order += "3";
                return ValueTask.CompletedTask;
            }),
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await collection.DisposeAllAsync()
        );

        Assert.Equal("321", order);
    }

    #endregion

    #region DisposeAllIgnoringExceptionsAsync Tests

    [Fact]
    public async Task DisposeAllIgnoringExceptionsAsync_WhenItemsThrow_DisposesEveryItem()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() =>
            {
                order += "1";
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() =>
            {
                order += "2";
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() =>
            {
                order += "3";
                return ValueTask.CompletedTask;
            }),
        };

        await collection.DisposeAllIgnoringExceptionsAsync();

        Assert.Equal("321", order);
    }

    #endregion

    #region AsAsyncSharedReference Tests

    [Fact]
    public void AsAsyncSharedReference_ReturnsLease()
    {
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var lease = disposable.Object.AsAsyncSharedReference();

        Assert.Same(disposable.Object, lease.Value);
    }

    [Fact]
    public async Task AsAsyncSharedReference_DisposingLease_DisposesUnderlying()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var lease = mockDisposable.Object.AsAsyncSharedReference();
        await lease.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task AsAsyncSharedReference_MultipleLeases_DisposesOnLastRelease()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var lease1 = mockDisposable.Object.AsAsyncSharedReference();
        var lease2 = lease1.AddReference();

        await lease1.DisposeAsync();
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Never);

        await lease2.DisposeAsync();
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task AsAsyncSharedReference_WithTypeImplementingBothInterfaces_DisposesAsynchronously()
    {
        var resource = new DualDisposable();

        var lease = resource.AsAsyncSharedReference();
        await lease.DisposeAsync();

        Assert.True(resource.DisposeAsyncCalled);
        Assert.False(resource.DisposeCalled);
    }

    #endregion

    #region AsSharedReference Tests

    // ADR-1002: the name AsSharedReference belongs to the synchronous extension alone.
    [Fact]
    public void AsSharedReference_IsNotDeclared()
    {
        var methods = typeof(AsyncDisposableExtensions).GetMethods();

        Assert.DoesNotContain(methods, method => method.Name == "AsSharedReference");
    }

    #endregion

    #region Deprecated Overload Tests

#pragma warning disable NCDISP003 // tests the deprecated overloads until they are deleted

    [Fact]
    public async Task DisposeAllAsync_WithIgnoreExceptionsTrue_IgnoresFailures()
    {
        var log = new List<string>();
        var collection = new[]
        {
            AsyncDisposable.Create(() =>
            {
                log.Add("first");
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
        };

        await collection.DisposeAllAsync(ignoreExceptions: true);

        Assert.Equal(["first"], log);
    }

    [Fact]
    public async Task DisposeAllAsync_WithIgnoreExceptionsFalse_RethrowsFailure()
    {
        var log = new List<string>();
        var collection = new[]
        {
            AsyncDisposable.Create(() =>
            {
                log.Add("first");
                return ValueTask.CompletedTask;
            }),
            AsyncDisposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await collection.DisposeAllAsync(ignoreExceptions: false)
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
