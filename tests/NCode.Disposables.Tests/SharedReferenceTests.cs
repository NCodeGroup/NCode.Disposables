// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class SharedReferenceTests
{
    #region Create Tests

    [Fact]
    public void Create_WithNullValue_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SharedReference.Create<IDisposable>(null!)
        );

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Create_WithNullOnRelease_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            SharedReference.Create(new object(), null!)
        );

        Assert.Equal("onRelease", exception.ParamName);
    }

    [Fact]
    public void Create_WithNullValueAndOnRelease_PassesNullToOnRelease()
    {
        object? released = new object();
        var lease = SharedReference.Create<object?>(null, value => released = value);

        lease.Dispose();

        Assert.Null(released);
    }

    #endregion
}
