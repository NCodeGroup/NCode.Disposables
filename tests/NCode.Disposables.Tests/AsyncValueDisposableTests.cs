// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncValueDisposableTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_NullAction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AsyncValueDisposable<int>(null!, 1));
    }

    #endregion

    #region DisposeAsync Tests

    [Fact]
    public async Task DisposeAsync_InvokesActionWithState()
    {
        const int expectedState = 42;
        var received = new List<int>();
        var disposable = new AsyncValueDisposable<int>(
            state =>
            {
                received.Add(state);
                return ValueTask.CompletedTask;
            },
            expectedState
        );

        await disposable.DisposeAsync();

        var actual = Assert.Single(received);
        Assert.Equal(expectedState, actual);
    }

    [Fact]
    public async Task DisposeAsync_Twice_InvokesActionTwice()
    {
        const int expectedState = 7;
        var received = new List<int>();
        var disposable = new AsyncValueDisposable<int>(
            state =>
            {
                received.Add(state);
                return ValueTask.CompletedTask;
            },
            expectedState
        );

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        Assert.Equal([expectedState, expectedState], received);
    }

    [Fact]
    public async Task Default_DisposeAsync_DoesNothing()
    {
        AsyncValueDisposable<int> disposable = default;

        await disposable.DisposeAsync();
    }

    [Fact]
    public async Task AwaitUsingStatement_InvokesActionOnce()
    {
        const int expectedState = 5;
        var received = new List<int>();

        await using (
            new AsyncValueDisposable<int>(
                state =>
                {
                    received.Add(state);
                    return ValueTask.CompletedTask;
                },
                expectedState
            )
        )
        {
            Assert.Empty(received);
        }

        var actual = Assert.Single(received);
        Assert.Equal(expectedState, actual);
    }

    #endregion
}
