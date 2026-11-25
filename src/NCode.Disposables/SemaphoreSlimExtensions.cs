// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides extension methods that acquire a <see cref="SemaphoreSlim"/> for the duration of a scope.
/// </summary>
public static class SemaphoreSlimExtensions
{
    /// <summary>
    /// Gets the cached delegate that releases the semaphore when a scope is disposed.
    /// </summary>
    // ADR-1006: one cached delegate means taking a lock allocates nothing.
    private static Action<SemaphoreSlim> ReleaseAction { get; } =
        static semaphore => semaphore.Release();

    /// <param name="semaphore">The <see cref="SemaphoreSlim"/> to acquire.</param>
    extension(SemaphoreSlim semaphore)
    {
        /// <summary>
        /// Blocks until the semaphore is acquired and returns a scope that releases it.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the wait for the semaphore.</param>
        /// <returns>A scope that releases the semaphore once when it is disposed.</returns>
        /// <remarks>
        /// Dispose the returned scope once, through a <see langword="using"/> statement or declaration.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The semaphore is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
        public ValueDisposable<SemaphoreSlim> Lock(CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            semaphore.Wait(cancellationToken);
            return new ValueDisposable<SemaphoreSlim>(ReleaseAction, semaphore);
        }

        /// <summary>
        /// Asynchronously waits until the semaphore is acquired and returns a scope that releases it.
        /// </summary>
        /// <param name="cancellationToken">A token that cancels the wait for the semaphore.</param>
        /// <returns>
        /// A <see cref="ValueTask{TResult}"/> that produces a scope that releases the semaphore once when it is disposed.
        /// </returns>
        /// <remarks>
        /// Dispose the returned scope once, through a <see langword="using"/> statement or declaration.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The semaphore is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> is canceled.</exception>
        public async ValueTask<ValueDisposable<SemaphoreSlim>> LockAsync(
            CancellationToken cancellationToken = default
        )
        {
            ArgumentNullException.ThrowIfNull(semaphore);

            await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new ValueDisposable<SemaphoreSlim>(ReleaseAction, semaphore);
        }
    }
}
