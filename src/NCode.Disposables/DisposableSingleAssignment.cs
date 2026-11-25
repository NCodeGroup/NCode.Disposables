// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe holder for a single <see cref="IDisposable"/> instance that can be assigned only once.
/// </summary>
/// <remarks>
/// <para>
/// All operations on the <see cref="Disposable"/> property are thread-safe using lock-free atomic operations.
/// </para>
/// <para>
/// The <see cref="Disposable"/> property accepts one non-null assignment. A second assignment throws
/// <see cref="InvalidOperationException"/>, whether it happens before or after disposal.
/// </para>
/// <para>
/// When <see cref="Dispose"/> is called, the assigned instance is disposed.
/// The holder becomes disposed, after which the <see cref="Disposable"/> getter returns
/// <see langword="null"/> and a value assigned for the first time is disposed at once.
/// </para>
/// </remarks>
public sealed class DisposableSingleAssignment : IDisposable, IDisposalStatus
{
    /// <summary>
    /// The message of the exception a second assignment throws.
    /// </summary>
    private const string AlreadyAssignedMessage =
        "The disposable is already assigned; a single-assignment disposable takes one resource.";

    /// <summary>
    /// Gets the marker stored in place of the assigned instance once the holder is disposed.
    /// </summary>
    private static IDisposable Sentinel { get; } = new DisposableEmpty();

    /// <summary>
    /// One once an assignment has been made, zero before.
    /// </summary>
    private int _assigned;

    /// <summary>
    /// The assigned instance, <see langword="null"/> before assignment, or <see cref="Sentinel"/> once the holder is disposed.
    /// </summary>
    private IDisposable? _disposable;

    /// <summary>
    /// Gets a value indicating whether <see cref="Dispose"/> has been called at least once.
    /// </summary>
    public bool IsDisposed
    {
        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel);
        }
    }

    /// <summary>
    /// Gets a value indicating whether a disposable has been assigned.
    /// </summary>
    public bool IsAssigned => Volatile.Read(ref _assigned) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes the assigned <see cref="IDisposable"/> instance, if any.
    /// This method is idempotent; subsequent calls will have no effect.
    /// </para>
    /// <para>
    /// After disposal, the <see cref="Disposable"/> getter returns <see langword="null"/>
    /// and a value assigned for the first time is disposed at once.
    /// </para>
    /// </remarks>
    public void Dispose()
    {
        var previous = Interlocked.Exchange(ref _disposable, Sentinel);
        if (!ReferenceEquals(previous, Sentinel))
            previous?.Dispose();
    }

    /// <summary>
    /// Gets or sets the assigned <see cref="IDisposable"/> instance.
    /// </summary>
    /// <value>
    /// The assigned instance, or <see langword="null"/> when none is assigned or the holder is disposed.
    /// </value>
    /// <remarks>
    /// <para>
    /// The getter returns the assigned instance using a volatile read for thread-safety.
    /// </para>
    /// <para>
    /// The setter accepts one non-null value. If the holder is already disposed, the value is disposed at once.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">The setter is given <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A value has already been assigned.</exception>
    [DisallowNull]
    public IDisposable? Disposable
    {
        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel) ? null : current;
        }
        set
        {
            ArgumentNullException.ThrowIfNull(value);

            if (Interlocked.Exchange(ref _assigned, 1) != 0)
                throw new InvalidOperationException(AlreadyAssignedMessage);

            var previous = Interlocked.CompareExchange(ref _disposable, value, null);
            if (ReferenceEquals(previous, Sentinel))
            {
                // ADR-1004
                value.Dispose();
            }
        }
    }
}
