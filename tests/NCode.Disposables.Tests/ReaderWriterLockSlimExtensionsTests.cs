// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class ReaderWriterLockSlimExtensionsTests
{
    #region ReadLock Tests

    [Fact]
    public void ReadLock_WhenLockIsFree_EntersReadLock()
    {
        using var readerWriterLock = new ReaderWriterLockSlim();

        using var scope = readerWriterLock.ReadLock();

        Assert.True(readerWriterLock.IsReadLockHeld);
    }

    [Fact]
    public void ReadLock_Dispose_ExitsReadLock()
    {
        using var readerWriterLock = new ReaderWriterLockSlim();
        var scope = readerWriterLock.ReadLock();

        scope.Dispose();

        Assert.False(readerWriterLock.IsReadLockHeld);
    }

    [Fact]
    public void ReadLock_NullLock_ThrowsArgumentNullException()
    {
        ReaderWriterLockSlim readerWriterLock = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => readerWriterLock.ReadLock());

        Assert.Equal("readerWriterLock", exception.ParamName);
    }

    #endregion

    #region UpgradeableReadLock Tests

    [Fact]
    public void UpgradeableReadLock_WhenLockIsFree_EntersUpgradeableReadLock()
    {
        using var readerWriterLock = new ReaderWriterLockSlim();

        using var scope = readerWriterLock.UpgradeableReadLock();

        Assert.True(readerWriterLock.IsUpgradeableReadLockHeld);
    }

    [Fact]
    public void UpgradeableReadLock_WhenHeld_AllowsNestedWriteLock()
    {
        using var readerWriterLock = new ReaderWriterLockSlim();
        using var upgradeable = readerWriterLock.UpgradeableReadLock();

        using var write = readerWriterLock.WriteLock();

        Assert.True(readerWriterLock.IsWriteLockHeld);
    }

    [Fact]
    public void UpgradeableReadLock_NullLock_ThrowsArgumentNullException()
    {
        ReaderWriterLockSlim readerWriterLock = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            readerWriterLock.UpgradeableReadLock()
        );

        Assert.Equal("readerWriterLock", exception.ParamName);
    }

    #endregion

    #region WriteLock Tests

    [Fact]
    public void WriteLock_WhenLockIsFree_EntersWriteLock()
    {
        using var readerWriterLock = new ReaderWriterLockSlim();

        using var scope = readerWriterLock.WriteLock();

        Assert.True(readerWriterLock.IsWriteLockHeld);
    }

    [Fact]
    public void WriteLock_Dispose_ExitsWriteLock()
    {
        using var readerWriterLock = new ReaderWriterLockSlim();
        var scope = readerWriterLock.WriteLock();

        scope.Dispose();

        Assert.False(readerWriterLock.IsWriteLockHeld);
    }

    [Fact]
    public void WriteLock_NullLock_ThrowsArgumentNullException()
    {
        ReaderWriterLockSlim readerWriterLock = null!;

        var exception = Assert.Throws<ArgumentNullException>(() => readerWriterLock.WriteLock());

        Assert.Equal("readerWriterLock", exception.ParamName);
    }

    #endregion
}
