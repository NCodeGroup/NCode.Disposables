// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableTests : UnitTestBase
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
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

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
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();

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

    #region Shared Tests

    [Fact]
    public void Shared_WithDisposable_DisposesItWhenTheLastLeaseIsReleased()
    {
        var count = 0;
        var resource = Disposable.CreateRepeatable(() => count++);
        var lease = Disposable.Shared(resource);
        var second = lease.AddReference();

        lease.Dispose();
        var countAfterFirstRelease = count;
        second.Dispose();

        Assert.Equal(0, countAfterFirstRelease);
        Assert.Equal(1, count);
    }

    [Fact]
    public void Shared_WithOnRelease_ReleasesTheValueWhenTheLastLeaseIsReleased()
    {
        const string value = "resource";
        var released = new List<string>();
        var lease = Disposable.Shared(value, released.Add);
        var second = lease.AddReference();

        lease.Dispose();
        var releasedAfterFirstRelease = released.Count;
        second.Dispose();

        Assert.Equal(0, releasedAfterFirstRelease);
        Assert.Equal([value], released);
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
        var first = Disposable.Create(() => log.Add("first"));
        var failing = Disposable.Create(() =>
            throw new InvalidOperationException("Dispose failed.")
        );
        var third = Disposable.Create(() => log.Add("third"));
        var collection = Disposable.CollectionIgnoringExceptions(first, failing, third);

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

    #region Create Tests

    [Fact]
    public void Create_WhenDisposedThreeTimes_InvokesActionOnce()
    {
        var count = 0;
        var disposable = Disposable.Create(() => count++);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(1, count);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void Create_FuncWithOneArgument_PassesArgument()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1) =>
            {
                actual.Add(a1);
                return actual.Count;
            },
            1
        );
        disposable.Dispose();

        Assert.Equal([1], actual);
    }

    [Fact]
    public void Create_TwoArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create((int a1, int a2) => actual.AddRange([a1, a2]), 1, 2);
        disposable.Dispose();

        Assert.Equal([1, 2], actual);
    }

    [Fact]
    public void Create_FuncWithTwoArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2) =>
            {
                actual.AddRange([a1, a2]);
                return actual.Count;
            },
            1,
            2
        );
        disposable.Dispose();

        Assert.Equal([1, 2], actual);
    }

    [Fact]
    public void Create_ThreeArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3) => actual.AddRange([a1, a2, a3]),
            1,
            2,
            3
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3], actual);
    }

    [Fact]
    public void Create_FuncWithThreeArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3) =>
            {
                actual.AddRange([a1, a2, a3]);
                return actual.Count;
            },
            1,
            2,
            3
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3], actual);
    }

    [Fact]
    public void Create_FourArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4) => actual.AddRange([a1, a2, a3, a4]),
            1,
            2,
            3,
            4
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4], actual);
    }

    [Fact]
    public void Create_FuncWithFourArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4) =>
            {
                actual.AddRange([a1, a2, a3, a4]);
                return actual.Count;
            },
            1,
            2,
            3,
            4
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4], actual);
    }

    [Fact]
    public void Create_FiveArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5) => actual.AddRange([a1, a2, a3, a4, a5]),
            1,
            2,
            3,
            4,
            5
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5], actual);
    }

    [Fact]
    public void Create_FuncWithFiveArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5) =>
            {
                actual.AddRange([a1, a2, a3, a4, a5]);
                return actual.Count;
            },
            1,
            2,
            3,
            4,
            5
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5], actual);
    }

    [Fact]
    public void Create_SixArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6) =>
                actual.AddRange([a1, a2, a3, a4, a5, a6]),
            1,
            2,
            3,
            4,
            5,
            6
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5, 6], actual);
    }

    [Fact]
    public void Create_FuncWithSixArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6) =>
            {
                actual.AddRange([a1, a2, a3, a4, a5, a6]);
                return actual.Count;
            },
            1,
            2,
            3,
            4,
            5,
            6
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5, 6], actual);
    }

    [Fact]
    public void Create_SevenArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6, int a7) =>
                actual.AddRange([a1, a2, a3, a4, a5, a6, a7]),
            1,
            2,
            3,
            4,
            5,
            6,
            7
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], actual);
    }

    [Fact]
    public void Create_FuncWithSevenArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6, int a7) =>
            {
                actual.AddRange([a1, a2, a3, a4, a5, a6, a7]);
                return actual.Count;
            },
            1,
            2,
            3,
            4,
            5,
            6,
            7
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7], actual);
    }

    [Fact]
    public void Create_EightArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) =>
                actual.AddRange([a1, a2, a3, a4, a5, a6, a7, a8]),
            1,
            2,
            3,
            4,
            5,
            6,
            7,
            8
        );
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], actual);
    }

    [Fact]
    public void Create_FuncWithEightArguments_PassesArguments()
    {
        var actual = new List<int>();

        var disposable = Disposable.Create(
            (int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8) =>
            {
                actual.AddRange([a1, a2, a3, a4, a5, a6, a7, a8]);
                return actual.Count;
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
        disposable.Dispose();

        Assert.Equal([1, 2, 3, 4, 5, 6, 7, 8], actual);
    }

    #endregion

    #region CreateRepeatable Tests

    [Fact]
    public void CreateRepeatable_WhenDisposedThreeTimes_InvokesActionEachTime()
    {
        var count = 0;
        var disposable = Disposable.CreateRepeatable(() => count++);

        disposable.Dispose();
        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal(3, count);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void CreateRepeatable_NullAction_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            Disposable.CreateRepeatable(null!)
        );

        Assert.Equal("action", exception.ParamName);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void Context_WhenDisposed_SendsDisposal()
    {
        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback((SendOrPostCallback c, object s) => c(s))
            .Verifiable(Times.Once());

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Verifiable(Times.Once());

        var disposable = Disposable.Context(item.Object, context.Object);

        disposable.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    #endregion

    #region PostingContext Tests

    [Fact]
    public void PostingContext_WhenDisposed_PostsBetweenOperationStartedAndCompleted()
    {
        var callOrder = new List<string>();

        var context = CreateStrictMock<SynchronizationContext>();
        context
            .Setup(x => x.OperationStarted())
            .Callback(() => callOrder.Add("OperationStarted"))
            .Verifiable();
        context
            .Setup(x => x.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()))
            .Callback(
                (SendOrPostCallback c, object s) =>
                {
                    callOrder.Add("Post");
                    c(s);
                }
            )
            .Verifiable();
        context
            .Setup(x => x.OperationCompleted())
            .Callback(() => callOrder.Add("OperationCompleted"))
            .Verifiable();

        var item = CreateStrictMock<IDisposable>();
        item.Setup(x => x.Dispose()).Callback(() => callOrder.Add("Dispose")).Verifiable();

        var disposable = Disposable.PostingContext(item.Object, context.Object);

        disposable.Dispose();

        Assert.Equal(["OperationStarted", "Post", "Dispose", "OperationCompleted"], callOrder);
        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void PostingContext_NullDisposable_ThrowsArgumentNullException()
    {
        var context = CreateStrictMock<SynchronizationContext>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            Disposable.PostingContext(null!, context.Object)
        );

        Assert.Equal("disposable", exception.ParamName);
    }

    [Fact]
    public void PostingContext_NullContext_ThrowsArgumentNullException()
    {
        var item = CreateStrictMock<IDisposable>();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            Disposable.PostingContext(item.Object, null!)
        );

        Assert.Equal("context", exception.ParamName);
    }

    #endregion
}
