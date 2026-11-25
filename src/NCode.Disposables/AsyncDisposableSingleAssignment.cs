// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe holder for a single <see cref="IAsyncDisposable"/> instance that can be assigned only once.
/// </summary>
/// <remarks>
/// <para>
/// All operations on the holder are thread-safe using lock-free atomic operations.
/// </para>
/// <para>
/// <see cref="SetDisposableAsync"/> accepts one non-null assignment. A second assignment throws
/// <see cref="InvalidOperationException"/>, whether it happens before or after disposal.
/// </para>
/// <para>
/// When <see cref="DisposeAsync"/> is called, the assigned instance is disposed.
/// The holder becomes disposed, after which the <see cref="Disposable"/> property returns
/// <see langword="null"/> and a value assigned for the first time is disposed at once.
/// </para>
/// </remarks>
public sealed class AsyncDisposableSingleAssignment : IAsyncDisposable, IDisposalStatus
{
    private const string AlreadyAssignedMessage =
        "The disposable is already assigned; a single-assignment disposable takes one resource.";

    private static IAsyncDisposable Sentinel { get; } = new AsyncDisposableEmpty();

    private int _assigned;
    private IAsyncDisposable? _disposable;

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => ReferenceEquals(Volatile.Read(ref _disposable), Sentinel);

    /// <summary>
    /// Gets a value indicating whether a disposable has been assigned.
    /// </summary>
    public bool IsAssigned => Volatile.Read(ref _assigned) != 0;

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes the assigned <see cref="IAsyncDisposable"/> instance, if any.
    /// This method is idempotent; subsequent calls will have no effect.
    /// </para>
    /// <para>
    /// After disposal, the <see cref="Disposable"/> property returns <see langword="null"/>
    /// and a value assigned for the first time is disposed at once.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        var previous = Interlocked.Exchange(ref _disposable, Sentinel);
        if (!ReferenceEquals(previous, Sentinel) && previous is not null)
            await previous.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Gets the assigned <see cref="IAsyncDisposable"/> instance.
    /// </summary>
    /// <value>
    /// The assigned instance, or <see langword="null"/> when none is assigned or the holder is disposed.
    /// </value>
    /// <remarks>
    /// The getter returns the assigned instance using a volatile read for thread-safety.
    /// </remarks>
    public IAsyncDisposable? Disposable
    {
        get
        {
            var current = Volatile.Read(ref _disposable);
            return ReferenceEquals(current, Sentinel) ? null : current;
        }
    }

    /// <summary>
    /// Assigns the <see cref="IAsyncDisposable"/> instance that the holder takes.
    /// </summary>
    /// <param name="disposable">The <see cref="IAsyncDisposable"/> instance to hold.</param>
    /// <returns>
    /// A task that completes when <paramref name="disposable"/> has been disposed
    /// if the holder was already disposed, or a completed task otherwise.
    /// </returns>
    /// <remarks>
    /// The holder accepts one non-null value. If the holder is already disposed,
    /// this method returns the result of <see cref="IAsyncDisposable.DisposeAsync"/> on <paramref name="disposable"/>.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">A value has already been assigned.</exception>
    public ValueTask SetDisposableAsync(IAsyncDisposable disposable)
    {
        ArgumentNullException.ThrowIfNull(disposable);

        if (Interlocked.Exchange(ref _assigned, 1) != 0)
            throw new InvalidOperationException(AlreadyAssignedMessage);

        var previous = Interlocked.CompareExchange(ref _disposable, disposable, null);

        // ADR-1004
        return ReferenceEquals(previous, Sentinel)
            ? disposable.DisposeAsync()
            : ValueTask.CompletedTask;
    }
}
