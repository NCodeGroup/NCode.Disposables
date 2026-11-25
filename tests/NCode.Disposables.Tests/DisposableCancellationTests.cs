// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableCancellationTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullSource_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DisposableCancellation(null!)
        );

        Assert.Equal("source", exception.ParamName);
    }

    #endregion

    #region Token Property Tests

    [Fact]
    public void Token_BeforeDispose_IsNotCancelled()
    {
        using var cancellation = new DisposableCancellation();

        Assert.False(cancellation.Token.IsCancellationRequested);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        using var cancellation = new DisposableCancellation();

        Assert.False(cancellation.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var cancellation = new DisposableCancellation();

        cancellation.Dispose();

        Assert.True(cancellation.IsDisposed);
    }

    [Fact]
    public void IsDisposed_WhenCallbackThrows_ReturnsTrue()
    {
        var cancellation = new DisposableCancellation();
        var token = cancellation.Token;
        token.Register(() => throw new InvalidOperationException("Test exception"));

        Assert.Throws<AggregateException>(() => cancellation.Dispose());

        Assert.True(cancellation.IsDisposed);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WhenNotDisposed_CancelsToken()
    {
        var cancellation = new DisposableCancellation();
        var token = cancellation.Token;

        cancellation.Dispose();

        Assert.True(token.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_OwnedSource_DisposesSource()
    {
        var cancellation = new DisposableCancellation();

        cancellation.Dispose();

        Assert.Throws<ObjectDisposedException>(() => cancellation.Token.WaitHandle);
    }

    [Fact]
    public void Dispose_CallerSource_CancelsSource()
    {
        using var source = new CancellationTokenSource();
        var cancellation = new DisposableCancellation(source);

        cancellation.Dispose();

        Assert.True(source.IsCancellationRequested);
    }

    [Fact]
    public void Dispose_CallerSource_DoesNotDisposeSource()
    {
        using var source = new CancellationTokenSource();
        var cancellation = new DisposableCancellation(source);

        cancellation.Dispose();

        var exception = Record.Exception(() => source.Token.WaitHandle);
        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_Twice_CancelsOnce()
    {
        using var source = new CancellationTokenSource();
        var cancellation = new DisposableCancellation(source);
        var count = 0;
        using var registration = cancellation.Token.Register(() => count++);

        cancellation.Dispose();
        cancellation.Dispose();

        Assert.Equal(1, count);
    }

    [Fact]
    public void Dispose_WhenCallbackThrows_ThrowsAggregateException()
    {
        var cancellation = new DisposableCancellation();
        var token = cancellation.Token;
        token.Register(() => throw new InvalidOperationException("Test exception"));

        var exception = Assert.Throws<AggregateException>(() => cancellation.Dispose());

        var inner = Assert.Single(exception.InnerExceptions);
        Assert.IsType<InvalidOperationException>(inner);
    }

    [Fact]
    public void Dispose_WhenCallbackThrows_DisposesOwnedSource()
    {
        var cancellation = new DisposableCancellation();
        var token = cancellation.Token;
        token.Register(() => throw new InvalidOperationException("Test exception"));

        Assert.Throws<AggregateException>(() => cancellation.Dispose());

        Assert.Throws<ObjectDisposedException>(() => token.WaitHandle);
    }

    #endregion
}
