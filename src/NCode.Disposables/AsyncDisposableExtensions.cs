// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.ExceptionServices;

namespace NCode.Disposables;

/// <summary>
/// Provides extension methods for <see cref="IAsyncDisposable"/> instances and collections
/// containing disposable items.
/// </summary>
public static class AsyncDisposableExtensions
{
    /// <param name="value">The underlying resource to be shared.</param>
    /// <typeparam name="T">The type of the shared resource, which must implement <see cref="IAsyncDisposable"/>.</typeparam>
    extension<T>(T value)
        where T : IAsyncDisposable
    {
        /// <summary>
        /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> that shares the specified
        /// <see cref="IAsyncDisposable"/> resource using reference counting. The resource is automatically disposed
        /// asynchronously when the last lease is released.
        /// </summary>
        /// <returns>
        /// A new <see cref="AsyncSharedReferenceLease{T}"/> representing the initial lease on the shared resource.
        /// </returns>
        /// <remarks>
        /// This is a convenience extension method that wraps <see cref="AsyncSharedReference.Create{T}(T)"/>.
        /// The returned lease holds an initial reference to the resource. Additional references can be obtained
        /// by calling <see cref="AsyncSharedReferenceLease{T}.AddReference"/> on any active lease.
        /// A type that also implements <see cref="IDisposable"/>, such as a <see cref="System.IO.Stream"/>, gets an
        /// asynchronous lease too; its <c>AsSharedReference()</c> returns a synchronous lease.
        /// </remarks>
        /// <exception cref="ArgumentNullException">The resource is <see langword="null"/>.</exception>
        public AsyncSharedReferenceLease<T> AsAsyncSharedReference()
        {
            ArgumentNullException.ThrowIfNull(value);

            return AsyncSharedReference.Create(value);
        }
    }

    /// <param name="collection">The collection of items to dispose. Items that are <see langword="null"/> or do not
    /// implement a disposable interface are skipped.</param>
    extension(IEnumerable<object?> collection)
    {
        /// <summary>
        /// Disposes all items in a collection that implement <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>,
        /// processing items in reverse order.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        /// <remarks>
        /// <para>
        /// Items are disposed in reverse order (LIFO), which is typically appropriate for resource cleanup
        /// scenarios where resources may have dependencies.
        /// </para>
        /// <para>
        /// Items implementing <see cref="IAsyncDisposable"/> are disposed asynchronously via
        /// <see cref="IAsyncDisposable.DisposeAsync"/>. Items implementing only <see cref="IDisposable"/>
        /// are disposed synchronously via <see cref="IDisposable.Dispose"/>.
        /// </para>
        /// <para>
        /// When multiple items throw exceptions, an <see cref="AggregateException"/> containing all exceptions
        /// is thrown. If only one item throws, that exception is rethrown directly with its original stack trace
        /// preserved.
        /// </para>
        /// </remarks>
        /// <exception cref="AggregateException">Multiple items threw exceptions during disposal.</exception>
        public ValueTask DisposeAllAsync() =>
            DisposeAllAsyncCore(collection, ignoreExceptions: false);

        /// <summary>
        /// Disposes all items in a collection that implement <see cref="IAsyncDisposable"/> or <see cref="IDisposable"/>,
        /// processing items in reverse order and ignoring the exceptions that items throw.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        /// <remarks>
        /// <para>
        /// Items are disposed in reverse order (LIFO), which is typically appropriate for resource cleanup
        /// scenarios where resources may have dependencies.
        /// </para>
        /// <para>
        /// Items implementing <see cref="IAsyncDisposable"/> are disposed asynchronously via
        /// <see cref="IAsyncDisposable.DisposeAsync"/>. Items implementing only <see cref="IDisposable"/>
        /// are disposed synchronously via <see cref="IDisposable.Dispose"/>.
        /// </para>
        /// <para>
        /// Every exception that an item throws during disposal is ignored, so the remaining items are still disposed.
        /// </para>
        /// </remarks>
        public ValueTask DisposeAllIgnoringExceptionsAsync() =>
            DisposeAllAsyncCore(collection, ignoreExceptions: true);
    }

    /// <summary>
    /// Disposes every item of <paramref name="collection"/>, in reverse order, preferring
    /// <see cref="IAsyncDisposable.DisposeAsync"/> over <see cref="IDisposable.Dispose"/>.
    /// </summary>
    /// <param name="collection">
    /// The items to dispose; items that are <see langword="null"/> or implement neither interface are skipped.
    /// </param>
    /// <param name="ignoreExceptions">
    /// <see langword="true"/> to ignore the exceptions items' disposal throws; <see langword="false"/> to collect them.
    /// </param>
    /// <returns>A task that completes when every item has been disposed.</returns>
    /// <remarks>
    /// <para>
    /// Every item is visited even after one throws.
    /// </para>
    /// <para>
    /// After the loop, one collected exception is rethrown with its original stack trace, and several are thrown
    /// as an <see cref="AggregateException"/>.
    /// </para>
    /// </remarks>
    private static async ValueTask DisposeAllAsyncCore(
        IEnumerable<object?> collection,
        bool ignoreExceptions
    )
    {
        List<Exception>? exceptions = null;

        foreach (var item in collection.Reverse())
        {
            try
            {
                switch (item)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;

                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
            catch (Exception exception)
            {
                if (ignoreExceptions)
                    continue;
                exceptions ??= [];
                exceptions.Add(exception);
            }
        }

        if (exceptions == null)
            return;

        if (exceptions.Count > 1)
        {
            throw new AggregateException(exceptions);
        }

        ExceptionDispatchInfo.Throw(exceptions[0]);
    }
}
