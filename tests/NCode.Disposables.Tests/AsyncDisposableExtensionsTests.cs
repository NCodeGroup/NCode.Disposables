#region Copyright Preamble

// Copyright @ 2024 NCode Group
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

#endregion

namespace NCode.Disposables.Tests;

public class AsyncDisposableExtensionsTests
{
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
            AsyncDisposable.Create(() => { disposeCount++; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => { disposeCount++; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => { disposeCount++; return ValueTask.CompletedTask; })
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
            Disposable.Create(() => disposeCount++)
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
            AsyncDisposable.Create(() => { order += "2"; return ValueTask.CompletedTask; }),
            Disposable.Create(() => order += "3")
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
            AsyncDisposable.Create(() => { order += "1"; return ValueTask.CompletedTask; }),
            new(),
            AsyncDisposable.Create(() => { order += "2"; return ValueTask.CompletedTask; }),
            new(),
            AsyncDisposable.Create(() => { order += "3"; return ValueTask.CompletedTask; })
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
            AsyncDisposable.Create(() => { order += "1"; return ValueTask.CompletedTask; }),
            null,
            AsyncDisposable.Create(() => { order += "2"; return ValueTask.CompletedTask; }),
            null
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
            collection[i - 1] = AsyncDisposable.Create(() => { order += local; return ValueTask.CompletedTask; });
        }

        await collection.DisposeAllAsync();

        Assert.Equal("654321", order);
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
            AsyncDisposable.Create(() => ValueTask.CompletedTask)
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await collection.DisposeAllAsync());

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
            AsyncDisposable.Create(() => ValueTask.CompletedTask)
        };

        var exception = await Assert.ThrowsAsync<AggregateException>(
            async () => await collection.DisposeAllAsync());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Contains(exception.InnerExceptions, e => e is ArgumentException);
        Assert.Contains(exception.InnerExceptions, e => e is InvalidOperationException);
    }

    [Fact]
    public async Task DisposeAllAsync_WithIgnoreExceptions_SuppressesAllExceptions()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() => { order += "1"; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => { order += "2"; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => { order += "3"; return ValueTask.CompletedTask; })
        };

        await collection.DisposeAllAsync(ignoreExceptions: true);

        Assert.Equal("321", order);
    }

    [Fact]
    public async Task DisposeAllAsync_WithIgnoreExceptionsFalse_CollectsExceptions()
    {
        var disposeCount = 0;
        var collection = new[]
        {
            AsyncDisposable.Create(() => { disposeCount++; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => { disposeCount++; throw new InvalidOperationException(); }),
            AsyncDisposable.Create(() => { disposeCount++; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => { disposeCount++; throw new InvalidOperationException(); }),
            AsyncDisposable.Create(() => { disposeCount++; return ValueTask.CompletedTask; })
        };

        await Assert.ThrowsAsync<AggregateException>(
            async () => await collection.DisposeAllAsync(ignoreExceptions: false));

        Assert.Equal(5, disposeCount);
    }

    [Fact]
    public async Task DisposeAllAsync_ExceptionInSyncDisposable_PropagatesException()
    {
        var collection = new object[]
        {
            Disposable.Create(() => { }),
            Disposable.Create(() => throw new InvalidOperationException("Sync exception")),
            Disposable.Create(() => { })
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await collection.DisposeAllAsync());

        Assert.Equal("Sync exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAllAsync_ContinuesDisposingAfterException()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() => { order += "1"; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => { order += "2"; return ValueTask.CompletedTask; }),
            AsyncDisposable.Create(() => { order += "3"; return ValueTask.CompletedTask; })
        };

        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await collection.DisposeAllAsync());

        Assert.Equal("321", order);
    }

    #endregion

    #region AsSharedReference Tests

    [Fact]
    public void AsSharedReference_ReturnsLease()
    {
        var disposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var lease = disposable.Object.AsSharedReference();

        Assert.Same(disposable.Object, lease.Value);
    }

    [Fact]
    public async Task AsSharedReference_DisposingLease_DisposesUnderlying()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var lease = mockDisposable.Object.AsSharedReference();
        await lease.DisposeAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task AsSharedReference_MultipleLeases_DisposesOnLastRelease()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var lease1 = mockDisposable.Object.AsSharedReference();
        var lease2 = lease1.AddReference();

        await lease1.DisposeAsync();
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Never);

        await lease2.DisposeAsync();
        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    #endregion
}
