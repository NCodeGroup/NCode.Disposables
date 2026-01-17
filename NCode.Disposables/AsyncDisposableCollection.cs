#region Copyright Preamble

//
//    Copyright © 2017 NCode Group
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

#endregion

using System.Collections;

namespace NCode.Disposables;

/// <summary>
/// Represents a collection of <see cref="IAsyncDisposable"/> items that are automatically disposed
/// when the collection itself is disposed. Items are disposed in reverse order of their addition.
/// </summary>
/// <remarks>
/// <para>
/// This interface combines <see cref="IAsyncDisposable"/> and <see cref="ICollection{T}"/> to provide
/// a managed collection of disposable resources with automatic cleanup.
/// </para>
/// <para>
/// When the collection is disposed, all contained items are disposed in LIFO (last-in, first-out) order,
/// which is typically appropriate for resource cleanup scenarios where resources may have dependencies.
/// </para>
/// </remarks>
public interface IAsyncDisposableCollection : IAsyncDisposable, ICollection<IAsyncDisposable>
{
    // nothing
}

/// <summary>
/// Provides a thread-safe implementation of <see cref="IAsyncDisposableCollection"/> that manages
/// a collection of <see cref="IAsyncDisposable"/> items with automatic disposal support.
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
/// </remarks>
public sealed class AsyncDisposableCollection : IAsyncDisposableCollection
{
    private int _disposed;
    private readonly List<IAsyncDisposable> _list;
    private readonly bool _ignoreExceptions;

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
    public AsyncDisposableCollection(IEnumerable<IAsyncDisposable> collection, bool ignoreExceptions = false)
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
        if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0)
        {
            return;
        }

        await _list.DisposeAllAsync(_ignoreExceptions);
        _list.Clear();
    }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    /// <value>Always returns <see langword="false"/>.</value>
    public bool IsReadOnly => false;

    /// <summary>
    /// Gets the number of items contained in the collection.
    /// </summary>
    /// <value>The number of <see cref="IAsyncDisposable"/> items in the collection.</value>
    public int Count => _list.Count;

    /// <summary>
    /// Adds an <see cref="IAsyncDisposable"/> item to the collection.
    /// The item will be disposed when the collection is disposed.
    /// </summary>
    /// <param name="item">The <see cref="IAsyncDisposable"/> item to add to the collection.</param>
    /// <exception cref="ArgumentNullException"><paramref name="item"/> is <see langword="null"/>.</exception>
    public void Add(IAsyncDisposable item)
    {
        ArgumentNullException.ThrowIfNull(item);

        _list.Add(item);
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

        return _list.Remove(item);
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
        _list.Clear();
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

        return _list.Contains(item);
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

        _list.CopyTo(array, arrayIndex);
    }

    /// <summary>
    /// Returns an enumerator that iterates through the collection.
    /// </summary>
    /// <returns>An enumerator that can be used to iterate through the collection.</returns>
    public IEnumerator<IAsyncDisposable> GetEnumerator()
    {
        return _list.GetEnumerator();
    }

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
