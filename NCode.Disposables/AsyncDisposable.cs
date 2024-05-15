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

namespace NCode.Disposables;

/// <summary>
/// Contains factory methods for various <see cref="IAsyncDisposable"/> implementations.
/// </summary>
public static class AsyncDisposable
{
    /// <summary>
    /// Returns a singleton instance of <see cref="IAsyncDisposable"/> that performs
    /// nothing when <see cref="IAsyncDisposable.DisposeAsync"/> is called.
    /// </summary>
    public static IAsyncDisposable Empty => AsyncDisposableEmpty.Singleton;

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposable"/> that adds asynchronous disposal support
    /// to an existing <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    public static IAsyncDisposable Adapt(IDisposable disposable)
    {
        return new AsyncDisposableAdapter(disposable);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposable"/> that adds asynchronous disposal support
    /// to an existing <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    /// <param name="idempotent">Specifies if the adapter should be idempotent where multiple calls to <c>DisposeAsync</c>
    /// will only dispose the underlying instance once. Default is <c>true</c>.</param>
    public static IAsyncDisposable Adapt(IDisposable disposable, bool idempotent)
    {
        return new AsyncDisposableAdapter(disposable, idempotent);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposableAggregate"/>
    /// that contains (i.e. aggregates) a property to another <see cref="IAsyncDisposable"/>
    /// instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IAsyncDisposable"/> instance that the <see cref="IAsyncDisposableAggregate"/> will contain (i.e. aggregate).</param>
    public static IAsyncDisposableAggregate Aggregate(IAsyncDisposable? disposable)
    {
        return new AsyncDisposableAggregate(disposable);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncSharedReference{T}"/>
    /// which contains an <see cref="IAsyncDisposable"/> resource that uses reference
    /// counting and only disposes the underlying resource when all the
    /// references have been released (i.e. reference count is zero).
    /// </summary>
    /// <param name="value">The underlying <see cref="IAsyncDisposable"/> instance that this <see cref="IAsyncSharedReference{T}"/> will contain.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static IAsyncSharedReference<T> Shared<T>(T value)
        where T : IAsyncDisposable
    {
        ArgumentNullException.ThrowIfNull(value);

        return AsyncSharedReference.Create(value);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncSharedReference{T}"/>
    /// which contains an <see cref="IAsyncDisposable"/> resource that uses reference
    /// counting and only disposes the underlying resource when all the
    /// references have been released (i.e. reference count is zero).
    /// </summary>
    /// <param name="value">The underlying value that the <see cref="IAsyncSharedReference{T}"/> will contain.</param>
    /// <param name="onRelease">The callback method used to release/cleanup the shared reference.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static IAsyncSharedReference<T> Shared<T>(T value, Func<T, ValueTask> onRelease)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(onRelease);

        return AsyncSharedReference.Create(value, onRelease);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposableCollection"/>
    /// which contains other <see cref="IAsyncDisposable"/> items that will be
    /// disposed when the collection itself is disposed. The collection is
    /// initialized from a variable argument array of <see cref="IAsyncDisposable"/>
    /// items.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="IAsyncDisposableCollection"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    public static IAsyncDisposableCollection Collection(params IAsyncDisposable[] collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return Collection(collection.AsEnumerable());
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposableCollection"/>
    /// which contains other <see cref="IAsyncDisposable"/> items that will be
    /// disposed when the collection itself is disposed. The collection is
    /// initialized from a variable argument array of <see cref="IAsyncDisposable"/>
    /// items.
    /// </summary>
    /// <param name="ignoreExceptions"><c>true</c> to ignore any exceptions thrown while disposing individual items.</param>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="IAsyncDisposableCollection"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    public static IAsyncDisposableCollection Collection(bool ignoreExceptions, params IAsyncDisposable[] collection)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return Collection(collection.AsEnumerable(), ignoreExceptions);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposableCollection"/>
    /// which contains other <see cref="IAsyncDisposable"/> items that will be
    /// disposed when the collection itself is disposed. The collection is
    /// initialized from an enumeration of <see cref="IAsyncDisposable"/> items.
    /// </summary>
    /// <param name="collection">The collection whose elements are copied to the new <see cref="IAsyncDisposableCollection"/>.</param>
    /// <param name="ignoreExceptions"><c>true</c> to ignore any exceptions thrown while disposing individual items.</param>
    /// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
    public static IAsyncDisposableCollection Collection(IEnumerable<IAsyncDisposable> collection,
        bool ignoreExceptions = false)
    {
        ArgumentNullException.ThrowIfNull(collection);

        return new AsyncDisposableCollection(collection, ignoreExceptions);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposable"/> that
    /// will invoke an <see cref="Func{ValueTask}"/> when <see cref="IAsyncDisposable.DisposeAsync"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Func{ValueTask}"/> to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IAsyncDisposable Create(Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(action);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposable"/> that
    /// will invoke an <see cref="Func{ValueTask}"/> when <see cref="IAsyncDisposable.DisposeAsync"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Func{ValueTask}"/> to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="idempotent">Specifies if the adapter should be idempotent where multiple calls to <c>DisposeAsync</c>
    /// will only dispose the underlying instance once. Default is <c>true</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IAsyncDisposable Create(Func<ValueTask> action, bool idempotent)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(action, idempotent);
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IAsyncDisposable.DisposeAsync"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IAsyncDisposable Create(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() =>
        {
            action();
            return ValueTask.CompletedTask;
        });
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IAsyncDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IAsyncDisposable.DisposeAsync"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IAsyncDisposable.DisposeAsync"/> is called.</param>
    /// <param name="idempotent">Specifies if the adapter should be idempotent where multiple calls to <c>DisposeAsync</c>
    /// will only dispose the underlying instance once. Default is <c>true</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IAsyncDisposable Create(Action action, bool idempotent)
    {
        ArgumentNullException.ThrowIfNull(action);

        return new AsyncDisposableAction(() =>
        {
            action();
            return ValueTask.CompletedTask;
        }, idempotent);
    }
}