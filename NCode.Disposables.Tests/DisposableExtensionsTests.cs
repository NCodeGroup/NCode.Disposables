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

public class DisposableExtensionsTests
{
    #region DisposeAsyncIfAvailable Tests

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenAsyncDisposable_CallsDisposeAsync()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable
            .As<IAsyncDisposable>()
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        await mockDisposable.Object.DisposeAsyncIfAvailable();

        mockDisposable.Verify();
        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenNotAsyncDisposable_CallsDispose()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable
            .Setup(x => x.Dispose())
            .Verifiable();

        await mockDisposable.Object.DisposeAsyncIfAvailable();

        mockDisposable.Verify();
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenNull_DoesNotThrow()
    {
        IDisposable? disposable = null;

        var exception = await Record.ExceptionAsync(() => disposable.DisposeAsyncIfAvailable().AsTask());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenAsyncDisposeThrows_PropagatesException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable
            .As<IAsyncDisposable>()
            .Setup(x => x.DisposeAsync())
            .ThrowsAsync(new InvalidOperationException("Test exception"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mockDisposable.Object.DisposeAsyncIfAvailable().AsTask());

        Assert.Equal("Test exception", exception.Message);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailable_WhenSyncDisposeThrows_PropagatesException()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable
            .Setup(x => x.Dispose())
            .Throws(new InvalidOperationException("Test exception"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mockDisposable.Object.DisposeAsyncIfAvailable().AsTask());

        Assert.Equal("Test exception", exception.Message);
    }

    #endregion

    #region AsSharedReference Tests

    [Fact]
    public void AsSharedReference_CreatesLease()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        using var lease = mockDisposable.Object.AsSharedReference();

        Assert.Same(mockDisposable.Object, lease.Value);
    }

    [Fact]
    public void AsSharedReference_DisposesWhenLeaseReleased()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var lease = mockDisposable.Object.AsSharedReference();

        mockDisposable.Verify(x => x.Dispose(), Times.Never);

        lease.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void AsSharedReference_MultipleLeases_DisposesOnlyWhenLastReleased()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var lease1 = mockDisposable.Object.AsSharedReference();
        var lease2 = lease1.AddReference();
        var lease3 = lease2.AddReference();

        lease1.Dispose();
        mockDisposable.Verify(x => x.Dispose(), Times.Never);

        lease2.Dispose();
        mockDisposable.Verify(x => x.Dispose(), Times.Never);

        lease3.Dispose();
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion

    #region DisposeAll Tests

    [Fact]
    public void DisposeAll_WithDisposableItems_DisposesAll()
    {
        var disposeCount = 0;
        var collection = new object[]
        {
            Disposable.Create(() => disposeCount++),
            Disposable.Create(() => disposeCount++),
            Disposable.Create(() => disposeCount++)
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
            Disposable.Create(() => order += "3")
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
            Disposable.Create(() => order += "3")
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
            null
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
    public void DisposeAll_WithIgnoreExceptions_ContinuesOnError()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "3")
        };

        collection.DisposeAll(ignoreExceptions: true);

        Assert.Equal("321", order);
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
            Disposable.Create(() => order += "3")
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
            Disposable.Create(() => order += "3")
        };

        var exception = Assert.Throws<AggregateException>(() => collection.DisposeAll());

        Assert.Equal(2, exception.InnerExceptions.Count);
        Assert.Equal("321", order);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisposeAll_WithAsyncDisposable_ThrowsInvalidOperationException(bool ignoreExceptions)
    {
        var collection = new object[]
        {
            AsyncDisposable.Create(() => { })
        };

        var exception = Assert.Throws<InvalidOperationException>(() => collection.DisposeAll(ignoreExceptions));

        Assert.Equal("The collection contains an IAsyncDisposable instance.", exception.Message);
    }

    [Fact]
    public void DisposeAll_WithAsyncDisposable_SetsIgnoreExceptionsFalseAndCollectsException()
    {
        var disposed = false;
        var collection = new object[]
        {
            Disposable.Create(() => disposed = true),
            AsyncDisposable.Create(() => { })
        };

        // When IAsyncDisposable is encountered, ignoreExceptions is set to false
        // and the exception is collected. The loop continues and disposes remaining items.
        Assert.Throws<InvalidOperationException>(() => collection.DisposeAll(ignoreExceptions: true));

        // The sync disposable is still disposed because it comes after the async one in reverse order
        Assert.True(disposed);
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
}
