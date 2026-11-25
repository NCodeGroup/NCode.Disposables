// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides extension methods that enter a <see cref="ReaderWriterLockSlim"/> for the duration of a scope.
/// </summary>
public static class ReaderWriterLockSlimExtensions
{
    /// <summary>
    /// Gets the cached delegate that exits the read lock when a scope is disposed.
    /// </summary>
    // ADR-1006: one cached delegate per lock mode means taking a lock allocates nothing.
    private static Action<ReaderWriterLockSlim> ExitReadAction { get; } =
        static l => l.ExitReadLock();

    /// <summary>
    /// Gets the cached delegate that exits the upgradeable read lock when a scope is disposed.
    /// </summary>
    private static Action<ReaderWriterLockSlim> ExitUpgradeableReadAction { get; } =
        static l => l.ExitUpgradeableReadLock();

    /// <summary>
    /// Gets the cached delegate that exits the write lock when a scope is disposed.
    /// </summary>
    private static Action<ReaderWriterLockSlim> ExitWriteAction { get; } =
        static l => l.ExitWriteLock();

    /// <param name="readerWriterLock">The <see cref="ReaderWriterLockSlim"/> to enter.</param>
    extension(ReaderWriterLockSlim readerWriterLock)
    {
        /// <summary>
        /// Blocks until the lock is entered in read mode and returns a scope that exits it.
        /// </summary>
        /// <returns>A scope that exits the read lock once when it is disposed.</returns>
        /// <remarks>
        /// Dispose the returned scope once, through a <see langword="using"/> statement or declaration.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The lock is <see langword="null"/>.</exception>
        public ValueDisposable<ReaderWriterLockSlim> ReadLock()
        {
            ArgumentNullException.ThrowIfNull(readerWriterLock);

            readerWriterLock.EnterReadLock();
            return new ValueDisposable<ReaderWriterLockSlim>(ExitReadAction, readerWriterLock);
        }

        /// <summary>
        /// Blocks until the lock is entered in upgradeable read mode and returns a scope that exits it.
        /// </summary>
        /// <returns>A scope that exits the upgradeable read lock once when it is disposed.</returns>
        /// <remarks>
        /// Dispose the returned scope once, through a <see langword="using"/> statement or declaration.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The lock is <see langword="null"/>.</exception>
        public ValueDisposable<ReaderWriterLockSlim> UpgradeableReadLock()
        {
            ArgumentNullException.ThrowIfNull(readerWriterLock);

            readerWriterLock.EnterUpgradeableReadLock();
            return new ValueDisposable<ReaderWriterLockSlim>(
                ExitUpgradeableReadAction,
                readerWriterLock
            );
        }

        /// <summary>
        /// Blocks until the lock is entered in write mode and returns a scope that exits it.
        /// </summary>
        /// <returns>A scope that exits the write lock once when it is disposed.</returns>
        /// <remarks>
        /// Dispose the returned scope once, through a <see langword="using"/> statement or declaration.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The lock is <see langword="null"/>.</exception>
        public ValueDisposable<ReaderWriterLockSlim> WriteLock()
        {
            ArgumentNullException.ThrowIfNull(readerWriterLock);

            readerWriterLock.EnterWriteLock();
            return new ValueDisposable<ReaderWriterLockSlim>(ExitWriteAction, readerWriterLock);
        }
    }
}
