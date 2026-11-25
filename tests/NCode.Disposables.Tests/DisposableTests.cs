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
    public void SingleAssignment_WhenCalled_ReturnsUnassignedHolder()
    {
        using var singleAssignment = Disposable.SingleAssignment();

        Assert.False(singleAssignment.IsAssigned);
        Assert.Null(singleAssignment.Disposable);
        Assert.False(singleAssignment.IsDisposed);
    }

    #endregion

    #region Guard Tests

    [Fact]
    public void Guard_WithValue_ReturnsGuardOverValue()
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
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Disposable.Guard<IDisposable>(null!)
        );

        Assert.Equal("value", exception.ParamName);
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
    public void Flag_WhenCalled_ReturnsUndisposedFlag()
    {
        var flag = Disposable.Flag();

        Assert.False(flag.IsDisposed);
    }

    #endregion

    #region CreateValue Tests

    [Fact]
    public void CreateValue_WhenDisposed_InvokesAction()
    {
        const int expectedState = 42;
        var actualState = 0;

        var value = Disposable.CreateValue(state => actualState = state, expectedState);
        value.Dispose();

        Assert.Equal(expectedState, actualState);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void Collection_WithEnumerable_WhenItemThrows_DisposesEveryItemAndRethrows()
    {
        var log = new List<string>();
        IEnumerable<IDisposable> items =
        [
            Disposable.Create(() => log.Add("first")),
            Disposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
            Disposable.Create(() => log.Add("third")),
        ];
        var collection = Disposable.Collection(items);

        Assert.Throws<InvalidOperationException>(collection.Dispose);

        Assert.Equal(["third", "first"], log);
    }

    #endregion

    #region CollectionIgnoringExceptions Tests

    [Fact]
    public void CollectionIgnoringExceptions_WhenItemThrows_DisposesEveryItemWithoutThrowing()
    {
        var log = new List<string>();
        var collection = Disposable.CollectionIgnoringExceptions(
            Disposable.Create(() => log.Add("first")),
            Disposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
            Disposable.Create(() => log.Add("third"))
        );

        collection.Dispose();

        Assert.Equal(["third", "first"], log);
    }

    [Fact]
    public void CollectionIgnoringExceptions_WithEnumerable_WhenItemThrows_DisposesEveryItemWithoutThrowing()
    {
        var log = new List<string>();
        IEnumerable<IDisposable> items =
        [
            Disposable.Create(() => log.Add("first")),
            Disposable.Create(() => throw new InvalidOperationException("Dispose failed.")),
            Disposable.Create(() => log.Add("third")),
        ];
        var collection = Disposable.CollectionIgnoringExceptions(items);

        collection.Dispose();

        Assert.Equal(["third", "first"], log);
    }

    [Fact]
    public void CollectionIgnoringExceptions_NullCollection_ThrowsArgumentNullException()
    {
        var arrayException = Assert.Throws<ArgumentNullException>(() =>
            Disposable.CollectionIgnoringExceptions((IDisposable[])null!)
        );
        var enumerableException = Assert.Throws<ArgumentNullException>(() =>
            Disposable.CollectionIgnoringExceptions((IEnumerable<IDisposable>)null!)
        );

        Assert.Equal("collection", arrayException.ParamName);
        Assert.Equal("collection", enumerableException.ParamName);
    }

    #endregion
}
