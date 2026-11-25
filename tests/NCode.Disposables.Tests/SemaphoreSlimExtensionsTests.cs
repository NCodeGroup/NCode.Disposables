// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class SemaphoreSlimExtensionsTests
{
    #region Lock Tests

    [Fact]
    public void Lock_AcquiresSemaphore()
    {
        using var semaphore = new SemaphoreSlim(1, 1);

        using var scope = semaphore.Lock();

        Assert.Equal(0, semaphore.CurrentCount);
    }

    [Fact]
    public void Lock_Dispose_ReleasesSemaphore()
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        var scope = semaphore.Lock();

        scope.Dispose();

        Assert.Equal(1, semaphore.CurrentCount);
    }

    [Fact]
    public void Lock_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAny<OperationCanceledException>(() => semaphore.Lock(cts.Token));
        Assert.Equal(1, semaphore.CurrentCount);
    }

    [Fact]
    public void Lock_NullSemaphore_ThrowsArgumentNullException()
    {
        SemaphoreSlim semaphore = null!;

        Assert.Throws<ArgumentNullException>(() => semaphore.Lock());
    }

    #endregion

    #region LockAsync Tests

    [Fact]
    public async Task LockAsync_AcquiresSemaphore()
    {
        using var semaphore = new SemaphoreSlim(1, 1);

        using var scope = await semaphore.LockAsync();

        Assert.Equal(0, semaphore.CurrentCount);
    }

    [Fact]
    public async Task LockAsync_WhenCancelled_ThrowsOperationCanceledException()
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await semaphore.LockAsync(cts.Token)
        );

        Assert.Equal(1, semaphore.CurrentCount);
    }

    [Fact]
    public async Task LockAsync_WhileHeld_WaitsUntilReleased()
    {
        using var semaphore = new SemaphoreSlim(1, 1);
        var first = await semaphore.LockAsync();

        var secondTask = semaphore.LockAsync().AsTask();

        Assert.False(secondTask.IsCompleted);

        first.Dispose();
        using var second = await secondTask;

        Assert.Equal(0, semaphore.CurrentCount);
    }

    #endregion
}
