// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncDisposableTests
{
    #region Aggregate Tests

    [Fact]
    public async Task Aggregate_Default_HoldsNull()
    {
        await using var aggregate = AsyncDisposable.Aggregate();

        Assert.Null(aggregate.Disposable);
        Assert.False(aggregate.IsDisposed);
    }

    #endregion

    #region Serial Tests

    [Fact]
    public async Task Serial_Default_HoldsNull()
    {
        await using var serial = AsyncDisposable.Serial();

        Assert.Null(serial.Disposable);
        Assert.False(serial.IsDisposed);
    }

    [Fact]
    public async Task Serial_WithDisposable_HoldsIt()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var serial = AsyncDisposable.Serial(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    #endregion

    #region SingleAssignment Tests

    [Fact]
    public async Task SingleAssignment_WhenCalled_ReturnsUnassignedHolder()
    {
        await using var singleAssignment = AsyncDisposable.SingleAssignment();

        Assert.False(singleAssignment.IsAssigned);
        Assert.Null(singleAssignment.Disposable);
        Assert.False(singleAssignment.IsDisposed);
    }

    #endregion

    #region Guard Tests

    [Fact]
    public async Task Guard_WithValue_ReturnsGuardOverValue()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        await using var guard = AsyncDisposable.Guard(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, guard.Value);
        Assert.False(guard.IsReleased);
        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public void Guard_Null_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncDisposable.Guard<IAsyncDisposable>(null!)
        );

        Assert.Equal("value", exception.ParamName);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task Cancellation_Default_TokenNotCancelled()
    {
        await using var cancellation = AsyncDisposable.Cancellation();

        Assert.True(cancellation.Token.CanBeCanceled);
        Assert.False(cancellation.Token.IsCancellationRequested);
        Assert.False(cancellation.IsDisposed);
    }

    [Fact]
    public async Task Cancellation_WithSource_UsesSourceToken()
    {
        using var source = new CancellationTokenSource();

        await using var cancellation = AsyncDisposable.Cancellation(source);

        Assert.Equal(source.Token, cancellation.Token);
    }

    [Fact]
    public void Cancellation_NullSource_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncDisposable.Cancellation(null!)
        );

        Assert.Equal("source", exception.ParamName);
    }

    #endregion

    #region Context Tests

    [Fact]
    public async Task Context_WithDisposableAndContext_ReturnsContextDisposable()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        var context = new SynchronizationContext();

        await using var contextDisposable = AsyncDisposable.Context(mockDisposable.Object, context);

        Assert.False(contextDisposable.IsDisposed);
    }

    [Fact]
    public void Context_NullContext_ThrowsArgumentNullException()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);

        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncDisposable.Context(mockDisposable.Object, null!)
        );

        Assert.Equal("context", exception.ParamName);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_OneArgument_PassesArgument()
    {
        const int expectedArg = 42;
        var actualArg = 0;

        var disposable = AsyncDisposable.Create(
            (int arg) =>
            {
                actualArg = arg;
                return ValueTask.CompletedTask;
            },
            expectedArg
        );
        await disposable.DisposeAsync();

        Assert.Equal(expectedArg, actualArg);
    }

    [Fact]
    public async Task Create_EightArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = AsyncDisposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) =>
            {
                actual.AddRange([a1, a2, a3, a4, a5, a6, a7, a8]);
                return ValueTask.CompletedTask;
            },
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8
        );
        await disposable.DisposeAsync();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], actual);
    }

    [Fact]
    public void Create_OneArgumentNullAction_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncDisposable.Create<int>(null!, 0)
        );

        Assert.Equal("action", exception.ParamName);
    }

    [Fact]
    public async Task Create_OneArgument_IsIdempotent()
    {
        var count = 0;

        var disposable = AsyncDisposable.Create(
            (int _) =>
            {
                count++;
                return ValueTask.CompletedTask;
            },
            0
        );
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        Assert.Equal(1, count);
    }

    #endregion

    #region CreateValue Tests

    [Fact]
    public async Task CreateValue_WhenDisposed_InvokesAction()
    {
        const int expectedState = 42;
        var actualState = 0;

        var value = AsyncDisposable.CreateValue(
            state =>
            {
                actualState = state;
                return ValueTask.CompletedTask;
            },
            expectedState
        );
        await value.DisposeAsync();

        Assert.Equal(expectedState, actualState);
    }

    [Fact]
    public void CreateValue_NullAction_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncDisposable.CreateValue<int>(null!, 0)
        );

        Assert.Equal("action", exception.ParamName);
    }

    #endregion
}
