// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;

namespace NCode.Disposables;

/// <summary>
/// Provides extension methods for <see cref="IDisposable"/> instances and collections
/// containing disposable items.
/// </summary>
[SuppressMessage(
    "Naming",
    "CA1708:Identifiers should differ by more than case",
    Justification = "False positive: the analyzer compares the compiler-generated names of the C# 14 extension blocks."
)]
public static class DisposableExtensions
{
    // ADR-1001: an item that implements both interfaces is disposed through IDisposable.
    private const string AsyncOnlyMessage =
        "The collection contains an item that implements IAsyncDisposable but not IDisposable; dispose it with DisposeAllAsync.";

    /// <param name="disposable">The <see cref="IDisposable"/> instance to dispose, or <see langword="null"/>.</param>
    extension(IDisposable? disposable)
    {
        /// <summary>
        /// Asynchronously disposes the specified instance if it implements <see cref="IAsyncDisposable"/>;
        /// otherwise, disposes it synchronously via <see cref="IDisposable.Dispose"/>.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> that represents the asynchronous dispose operation.</returns>
        /// <remarks>
        /// <para>
        /// This method provides a unified way to dispose resources that may or may not support
        /// asynchronous disposal. If the instance is <see langword="null"/>, no action is taken.
        /// </para>
        /// <para>
        /// If the instance implements <see cref="IAsyncDisposable"/>, <see cref="IAsyncDisposable.DisposeAsync"/>
        /// is called. Otherwise, <see cref="IDisposable.Dispose"/> is called synchronously.
        /// </para>
        /// </remarks>
        public async ValueTask DisposeAsyncIfAvailable()
        {
            if (disposable is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                disposable?.Dispose();
            }
        }
    }

    /// <param name="value">The underlying resource to be shared.</param>
    /// <typeparam name="T">The type of the shared resource, which must implement <see cref="IDisposable"/>.</typeparam>
    extension<T>(T value)
        where T : IDisposable
    {
        /// <summary>
        /// Creates a new <see cref="SharedReferenceLease{T}"/> that shares the specified <see cref="IDisposable"/>
        /// resource using reference counting. The resource is automatically disposed when the last lease is released.
        /// </summary>
        /// <returns>
        /// A new <see cref="SharedReferenceLease{T}"/> representing the initial lease on the shared resource.
        /// </returns>
        /// <remarks>
        /// This is a convenience extension method that wraps <see cref="SharedReference.Create{T}(T)"/>.
        /// The returned lease holds an initial reference to the resource. Additional references can be obtained
        /// by calling <see cref="SharedReferenceLease{T}.AddReference"/> on any active lease.
        /// For a type that also implements <see cref="IAsyncDisposable"/>, the call is ambiguous with the
        /// asynchronous extension of the same name: call <see cref="SharedReference.Create{T}(T)"/> for a lease
        /// that disposes the resource synchronously, or <c>AsAsyncSharedReference()</c> for one that disposes it
        /// asynchronously.
        /// </remarks>
        public SharedReferenceLease<T> AsSharedReference()
        {
            return SharedReference.Create(value);
        }
    }

    /// <param name="collection">The collection of items to dispose. Items that are <see langword="null"/> or do not
    /// implement <see cref="IDisposable"/> are skipped.</param>
    extension(IEnumerable<object?> collection)
    {
        /// <summary>
        /// Disposes all items in a collection that implement <see cref="IDisposable"/>, processing items in reverse order.
        /// </summary>
        /// <param name="ignoreExceptions">
        /// <see langword="true"/> to suppress exceptions thrown by individual items during disposal and continue
        /// disposing remaining items; <see langword="false"/> (the default) to collect and throw exceptions.
        /// Note: The <see cref="InvalidOperationException"/> for an item that implements only
        /// <see cref="IAsyncDisposable"/> is never ignored.
        /// </param>
        /// <remarks>
        /// <para>
        /// Items are disposed in reverse order (LIFO), which is typically appropriate for resource cleanup
        /// scenarios where resources may have dependencies.
        /// </para>
        /// <para>
        /// <b>Important:</b> An item that implements both <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/>
        /// is disposed through <see cref="IDisposable.Dispose"/>. An item that implements only
        /// <see cref="IAsyncDisposable"/> is not disposed, and once the other items are disposed an
        /// <see cref="InvalidOperationException"/> is thrown for it. Use
        /// <see cref="AsyncDisposableExtensions.DisposeAllAsync"/> for such items.
        /// </para>
        /// <para>
        /// When <paramref name="ignoreExceptions"/> is <see langword="false"/> and multiple items throw exceptions,
        /// an <see cref="AggregateException"/> containing all exceptions is thrown. If only one item throws,
        /// that exception is rethrown directly with its original stack trace preserved.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The collection contains an item that implements <see cref="IAsyncDisposable"/> but not
        /// <see cref="IDisposable"/>.
        /// </exception>
        /// <exception cref="AggregateException">
        /// Multiple items threw exceptions during disposal and <paramref name="ignoreExceptions"/> is <see langword="false"/>.
        /// </exception>
        public void DisposeAll(bool ignoreExceptions = false)
        {
            List<Exception>? exceptions = null;

            foreach (var item in collection.Reverse())
            {
                switch (item)
                {
                    case IDisposable disposable:
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception exception)
                        {
                            if (!ignoreExceptions)
                            {
                                exceptions ??= [];
                                exceptions.Add(exception);
                            }
                        }

                        break;

                    case IAsyncDisposable:
                        exceptions ??= [];
                        exceptions.Add(new InvalidOperationException(AsyncOnlyMessage));
                        break;
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
}
