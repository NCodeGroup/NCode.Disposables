// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableFlagTests
{
    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var flag = new DisposableFlag();

        Assert.False(flag.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var flag = new DisposableFlag();

        flag.Dispose();

        Assert.True(flag.IsDisposed);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_Twice_StaysDisposed()
    {
        var flag = new DisposableFlag();

        flag.Dispose();
        flag.Dispose();

        Assert.True(flag.IsDisposed);
    }

    #endregion
}
