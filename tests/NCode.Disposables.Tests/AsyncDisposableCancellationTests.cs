// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableCancellationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new AsyncDisposableCancellation(null!)
        );

        Assert.Equal("source", exception.ParamName);
    }

    #endregion

    #region Token Property Tests

    [Fact]
    public async Task Token_BeforeDispose_IsNotCancelled()
    {
        await using var cancellation = new AsyncDisposableCancellation();

        Assert.False(cancellation.Token.IsCancellationRequested);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public async Task IsDisposed_BeforeDispose_ReturnsFalse()
    {
        await using var cancellation = new AsyncDisposableCancellation();

        Assert.False(cancellation.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_AfterDispose_ReturnsTrue()
    {
        var cancellation = new AsyncDisposableCancellation();

        await cancellation.DisposeAsync();

        Assert.True(cancellation.IsDisposed);
    }

    [Fact]
    public async Task IsDisposed_WhenCallbackThrows_ReturnsTrue()
    {
        var cancellation = new AsyncDisposableCancellation();
        var token = cancellation.Token;
        token.Register(() => throw new InvalidOperationException("Test exception"));

        await Assert.ThrowsAsync<AggregateException>(async () => await cancellation.DisposeAsync());

        Assert.True(cancellation.IsDisposed);
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_WhenNotDisposed_CancelsToken()
    {
        var cancellation = new AsyncDisposableCancellation();
        var token = cancellation.Token;

        await cancellation.DisposeAsync();

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposeAsync_OwnedSource_DisposesSource()
    {
        var cancellation = new AsyncDisposableCancellation();

        await cancellation.DisposeAsync();

        Assert.Throws<ObjectDisposedException>(() => cancellation.Token.WaitHandle);
    }

    [Fact]
    public async Task DisposeAsync_CallerSource_CancelsSource()
    {
        using var source = new CancellationTokenSource();
        var cancellation = new AsyncDisposableCancellation(source);

        await cancellation.DisposeAsync();

        Assert.True(source.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposeAsync_CallerSource_DoesNotDisposeSource()
    {
        using var source = new CancellationTokenSource();
        var cancellation = new AsyncDisposableCancellation(source);

        await cancellation.DisposeAsync();

        _ = source.Token.WaitHandle;
    }

    [Fact]
    public async Task DisposeAsync_Twice_CancelsOnce()
    {
        using var source = new CancellationTokenSource();
        var cancellation = new AsyncDisposableCancellation(source);
        var count = 0;
        using var registration = cancellation.Token.Register(() => count++);

        await cancellation.DisposeAsync();
        await cancellation.DisposeAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task DisposeAsync_WhenCallbackThrows_ThrowsAggregateException()
    {
        var cancellation = new AsyncDisposableCancellation();
        var token = cancellation.Token;
        token.Register(() => throw new InvalidOperationException("Test exception"));

        var exception = await Assert.ThrowsAsync<AggregateException>(async () =>
            await cancellation.DisposeAsync()
        );

        var inner = Assert.Single(exception.InnerExceptions);
        Assert.IsType<InvalidOperationException>(inner);
    }

    [Fact]
    public async Task DisposeAsync_WhenCallbackThrows_DisposesOwnedSource()
    {
        var cancellation = new AsyncDisposableCancellation();
        var token = cancellation.Token;
        token.Register(() => throw new InvalidOperationException("Test exception"));

        await Assert.ThrowsAsync<AggregateException>(async () => await cancellation.DisposeAsync());

        Assert.Throws<ObjectDisposedException>(() => token.WaitHandle);
    }

    #endregion
}
