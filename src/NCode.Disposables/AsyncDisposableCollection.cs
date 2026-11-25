// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Collections;

namespace NCode.Disposables;

/// <summary>
/// Provides a thread-safe collection of <see cref="IAsyncDisposable"/> items that disposes them when it is disposed.
/// </summary>
/// <remarks>
/// <para>
/// When the collection is disposed via <see cref="DisposeAsync"/>, all contained items are disposed
/// in reverse order of their addition (LIFO order). The disposal operation is idempotent.
/// </para>
/// <para>
/// The collection can optionally be configured to ignore exceptions thrown during individual item disposal,
/// allowing all items to be disposed even if some throw exceptions.
/// </para>
/// <para>
/// Every member is thread-safe. Disposal runs at most once, even when several threads dispose the collection at once.
/// Enumeration walks a snapshot of the items taken when <see cref="GetEnumerator"/> is called, so changing the
/// collection while enumerating does not throw.
/// </para>
/// <para>
/// <see cref="Add"/> after the collection is disposed throws <see cref="ObjectDisposedException"/> and does not take the item.
/// <see cref="AddAsync"/> after the collection is disposed disposes the added item instead of storing it.
/// </para>
/// </remarks>
#pragma warning disable NCDISP002 // implements the deprecated interface until it is deleted
public sealed class AsyncDisposableCollection : IAsyncDisposableCollection, IDisposalStatus
#pragma warning restore NCDISP002
{
    private int _disposed;
    private readonly List<IAsyncDisposable> _list;
    private readonly bool _ignoreExceptions;

    /// <summary>
    /// Gets the lock that guards the item list and the disposed flag.
    /// </summary>
    private Lock Gate { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableCollection"/> class with an empty collection.
    /// </summary>
    /// <param name="ignoreExceptions">
    /// <see langword="true"/> to suppress exceptions thrown by individual items during disposal and continue
    /// disposing remaining items; <see langword="false"/> (the default) to propagate exceptions.
    /// </param>
    public AsyncDisposableCollection(bool ignoreExceptions = false)
    {
        _list = [];
        _ignoreExceptions = ignoreExceptions;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableCollection"/> class with elements
    /// copied from the specified collection.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new collection.</param>
    /// <param name="ignoreExceptions">
    /// <see langword="true"/> to suppress exceptions thrown by individual items during disposal and continue
    /// disposing remaining items; <see langword="false"/> (the default) to propagate exceptions.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <see langword="null"/>.</exception>
    public AsyncDisposableCollection(
        IEnumerable<IAsyncDisposable> collection,
        bool ignoreExceptions = false
    )
    {
        _list = [.. collection];
        _ignoreExceptions = ignoreExceptions;
    }

    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// Disposes all items in the collection in reverse order of their addition (LIFO order),
    /// then clears the collection. This method is idempotent; subsequent calls have no effect.
    /// </para>
    /// <para>
    /// If <c>ignoreExceptions</c> was set to <see langword="true"/> in the constructor, exceptions
    /// thrown by individual items are suppressed, allowing all items to be disposed.
    /// </para>
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        IAsyncDisposable[] items;
        lock (Gate)
        {
            if (_disposed != 0)
            {
                return;
            }

            Volatile.Write(ref _disposed, 1);
            items = [.. _list];
            _list.Clear();
        }

        if (_ignoreExceptions)
        {
            await items.DisposeAllIgnoringExceptionsAsync().ConfigureAwait(false);
        }
        else
        {
            await items.DisposeAllAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Gets a value indicating whether <see cref="DisposeAsync"/> has been called at least once.
    /// </summary>
    public bool IsDisposed => Volatile.Read(ref _disposed) != 0;

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    /// <value>Always returns <see langword="false"/>.</value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets the number of items contained in the collection.
    /// </summary>
    /// <value>The number of <see cref="IAsyncDisposable"/> items in the collection.</value>
    public int Count
    {
        get
        {
            lock (Gate)
            {
                return _list.Count;
            }
        }
    }

    /// <summary>
    /// Adds an <see cref="IAsyncDisposable"/> item to the collection.
    /// The item will be disposed when the collection is disposed.
    /// </summary>
    /// <param name="item">The <see cref="IAsyncDisposable"/> item to add to the collection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The collection is already disposed.</exception>
    /// <remarks>
    /// If the collection is already disposed, the item is not added and is not disposed.
    /// Use <see cref="AddAsync"/> to have the item disposed in that case.
    /// </remarks>
    public void Add(IAsyncDisposable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // ADR-1004: the synchronous Add can't await a disposal, so it throws after disposal and doesn't take the item.
        lock (Gate)
        {
            ObjectDisposedException.ThrowIf(_disposed != 0, this);

            _list.Add(item);
        }
    }

    /// <summary>
    /// Adds an <see cref="IAsyncDisposable"/> item to the collection.
    /// The item will be disposed when the collection is disposed.
    /// </summary>
    /// <param name="item">The <see cref="IAsyncDisposable"/> item to add to the collection.</param>
    /// <returns>
    /// A task that is already completed when the item is added to the collection, or that completes when the item
    /// is disposed because the collection is already disposed.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// If the collection is already disposed, the item is disposed immediately and is not added to the collection.
    /// </remarks>
    public ValueTask AddAsync(IAsyncDisposable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        // ADR-1004: an item added after disposal is disposed instead of stored.
        lock (Gate)
        {
            if (_disposed == 0)
            {
                _list.Add(item);
                return ValueTask.CompletedTask;
            }
        }

        return item.DisposeAsync();
    }

    /// <summary>
    /// Removes the first occurrence of the specified item from the collection without disposing it.
    /// </summary>
    /// <param name="item">The <see cref="IAsyncDisposable"/> item to remove from the collection.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="item"/> was successfully removed from the collection;
    /// otherwise, <see langword="false"/>. This method also returns <see langword="false"/> if
    /// <paramref name="item"/> was not found in the collection.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// The removed item is not disposed. If disposal is required, it must be done explicitly after removal.
    /// </remarks>
    public bool Remove(IAsyncDisposable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (Gate)
        {
            return _list.Remove(item);
        }
    }

    /// <summary>
    /// Removes all items from the collection without disposing them.
    /// </summary>
    /// <remarks>
    /// The removed items are not disposed. If disposal is required, it must be done explicitly.
    /// To dispose all items and clear the collection, use <see cref="DisposeAsync"/> instead.
    /// </remarks>
    public void Clear()
    {
        lock (Gate)
        {
            _list.Clear();
        }
    }

    /// <summary>
    /// Determines whether the collection contains the specified item.
    /// </summary>
    /// <param name="item">The <see cref="IAsyncDisposable"/> item to locate in the collection.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="item"/> is found in the collection;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    public bool Contains(IAsyncDisposable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        lock (Gate)
        {
            return _list.Contains(item);
        }
    }

    /// <summary>
    /// Copies the elements of the collection to an array, starting at the specified array index.
    /// </summary>
    /// <param name="array">
    /// The one-dimensional array that is the destination of the elements copied from the collection.
    /// The array must have zero-based indexing.
    /// </param>
    /// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
    /// <exception cref="ArgumentNullException"><paramref name="array"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
    /// <exception cref="ArgumentException">
    /// The number of elements in the source collection is greater than the available space
    /// from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
    /// </exception>
    public void CopyTo(IAsyncDisposable[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array);

        lock (Gate)
        {
            _list.CopyTo(array, arrayIndex);
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through a snapshot of the collection.</returns>
    /// <remarks>
    /// Each call copies the items into a new array under the lock, so enumeration costs one allocation and a copy of every item.
    /// </remarks>
    public IEnumerator<IAsyncDisposable> GetEnumerator()
    {
        IAsyncDisposable[] snapshot;
        lock (Gate)
        {
            snapshot = [.. _list];
        }

        return ((IEnumerable<IAsyncDisposable>)snapshot).GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
