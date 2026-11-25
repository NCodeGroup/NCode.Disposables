// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableTests
{
    #region Aggregate Tests

    [Fact]
    public void Aggregate_Default_HoldsNull()
    {
        using var aggregate = Disposable.Aggregate();

        Assert.Null(aggregate.Disposable);
        Assert.False(aggregate.IsDisposed);
    }

    #endregion

    #region Serial Tests

    [Fact]
    public void Serial_Default_HoldsNull()
    {
        using var serial = Disposable.Serial();

        Assert.Null(serial.Disposable);
        Assert.False(serial.IsDisposed);
    }

    [Fact]
    public void Serial_WithDisposable_HoldsIt()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        using var serial = Disposable.Serial(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, serial.Disposable);
    }

    #endregion

    #region SingleAssignment Tests

    [Fact]
    public void SingleAssignment_ReturnsUnassigned()
    {
        using var singleAssignment = Disposable.SingleAssignment();

        Assert.False(singleAssignment.IsAssigned);
        Assert.Null(singleAssignment.Disposable);
        Assert.False(singleAssignment.IsDisposed);
    }

    #endregion

    #region Guard Tests

    [Fact]
    public void Guard_ReturnsGuardOverValue()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        using var guard = Disposable.Guard(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, guard.Value);
        Assert.False(guard.IsReleased);
        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public void Guard_Null_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Disposable.Guard<IDisposable>(null!));
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public void Cancellation_Default_TokenNotCancelled()
    {
        using var cancellation = Disposable.Cancellation();

        Assert.True(cancellation.Token.CanBeCanceled);
        Assert.False(cancellation.Token.IsCancellationRequested);
        Assert.False(cancellation.IsDisposed);
    }

    [Fact]
    public void Cancellation_WithSource_UsesSourceToken()
    {
        using var source = new CancellationTokenSource();

        using var cancellation = Disposable.Cancellation(source);

        Assert.Equal(source.Token, cancellation.Token);
    }

    #endregion

    #region Flag Tests

    [Fact]
    public void Flag_ReturnsUndisposedFlag()
    {
        var flag = Disposable.Flag();

        Assert.False(flag.IsDisposed);
    }

    #endregion

    #region CreateValue Tests

    [Fact]
    public void CreateValue_DisposeInvokesAction()
    {
        const int expectedState = 42;
        var actualState = 0;

        var value = Disposable.CreateValue(state => actualState = state, expectedState);
        value.Dispose();

        Assert.Equal(expectedState, actualState);
    }

    #endregion
}
