// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class AsyncSharedReferenceTests
{
    #region Create Tests

    [Fact]
    public void Create_WithNullValue_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncSharedReference.Create<IAsyncDisposable>(null!)
        );

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_WithNullOnRelease_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            AsyncSharedReference.Create(new object(), null!)
        );

        Assert.Equal("onRelease", exception.ParamName);
    }

    [Fact]
    public async Task Create_WithNullValueAndOnRelease_PassesNullToOnRelease()
    {
        object? released = new object();
        var lease = AsyncSharedReference.Create<object?>(null, value =>
        {
            released = value;
            return ValueTask.CompletedTask;
        });

        await lease.DisposeAsync();

        Assert.Null(released);
    }

    #endregion
}
