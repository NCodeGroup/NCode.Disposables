// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class ValueDisposableTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new ValueDisposable<int>(null!, 1)
        );

        Assert.Equal("action", exception.ParamName);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithAction_InvokesActionWithState()
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
    public void Dispose_WhenDefault_DoesNotThrow()
    {
        ValueDisposable<int> disposable = default;

        var exception = Record.Exception(disposable.Dispose);

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_InUsingStatement_InvokesActionOnce()
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
