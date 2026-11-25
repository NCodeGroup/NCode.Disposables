// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/scope-a-lock.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class ScopeALockSamples
{
    #region Cancel The Wait Samples

    [Fact]
    public async Task SiteScopeALockCancelledSample_Runs()
    {
        // begin-snippet: SiteScopeALockCancelled
        using var semaphore = new SemaphoreSlim(0, 1); // another scope holds it
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        var acquired = false;
        try
        {
            using (await semaphore.LockAsync(cancelled.Token))
            {
                acquired = true;
            }
        }
        catch (OperationCanceledException)
        {
            // the wait was cancelled: no scope, so nothing to release
        }

        // acquired == false, semaphore.CurrentCount == 0
        // end-snippet

        Assert.False(acquired);
        Assert.Equal(0, semaphore.CurrentCount);
    }

    #endregion

    #region Scope A Reader-Writer Lock Samples

    [Fact]
    public void SiteScopeALockUpgradeableSample_Runs()
    {
        // begin-snippet: SiteScopeALockUpgradeable
        using var cacheLock = new ReaderWriterLockSlim();
        var writeHeld = false;
        using (cacheLock.UpgradeableReadLock())
        {
            // read, and decide to write
            using (cacheLock.WriteLock())
            {
                writeHeld = cacheLock.IsWriteLockHeld; // true
            }
        }
        // cacheLock.IsUpgradeableReadLockHeld == false
        // end-snippet

        Assert.True(writeHeld);
        Assert.False(cacheLock.IsUpgradeableReadLockHeld);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteScopeALockDoubleReleaseSample_Runs()
    {
        // begin-snippet: SiteScopeALockDoubleRelease
        using var semaphore = new SemaphoreSlim(1, 1);
        var scope = semaphore.Lock();
        scope.Dispose(); // releases the semaphore

        var failed = false;
        try
        {
            scope.Dispose(); // releases it again
        }
        catch (SemaphoreFullException)
        {
            failed = true; // the count would pass its maximum, 1
        }
        // end-snippet

        Assert.True(failed);
    }

    #endregion
}
