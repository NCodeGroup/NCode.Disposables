// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class ValueDisposableTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ValueDisposable<int>(null!, 1));
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_InvokesActionWithState()
    {
        const int expectedState = 42;
        var received = new List<int>();
        var disposable = new ValueDisposable<int>(received.Add, expectedState);

        disposable.Dispose();

        var actual = Assert.Single(received);
        Assert.Equal(expectedState, actual);
    }

    [Fact]
    public void Dispose_Twice_InvokesActionTwice()
    {
        const int expectedState = 7;
        var received = new List<int>();
        var disposable = new ValueDisposable<int>(received.Add, expectedState);

        disposable.Dispose();
        disposable.Dispose();

        Assert.Equal([expectedState, expectedState], received);
    }

    [Fact]
    public void Default_Dispose_DoesNothing()
    {
        ValueDisposable<int> disposable = default;

        disposable.Dispose();
    }

    [Fact]
    public void UsingStatement_InvokesActionOnce()
    {
        const int expectedState = 5;
        var received = new List<int>();

        using (new ValueDisposable<int>(received.Add, expectedState))
        {
            Assert.Empty(received);
        }

        var actual = Assert.Single(received);
        Assert.Equal(expectedState, actual);
    }

    #endregion
}
