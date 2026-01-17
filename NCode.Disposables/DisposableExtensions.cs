#region Copyright Preamble

//
//    Copyright @ 2023 NCode Group
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

#endregion

using System.Runtime.ExceptionServices;

namespace NCode.Disposables;

/// <summary>
/// Provides extension methods for <see cref="IDisposable"/> instances and collections
/// containing disposable items.
/// </summary>
public static class DisposableExtensions
{
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
    extension<T>(T value) where T : IDisposable
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
        /// Note: <see cref="InvalidOperationException"/> thrown for <see cref="IAsyncDisposable"/> items is never ignored.
        /// </param>
        /// <remarks>
        /// <para>
        /// Items are disposed in reverse order (LIFO), which is typically appropriate for resource cleanup
        /// scenarios where resources may have dependencies.
        /// </para>
        /// <para>
        /// <b>Important:</b> This method does not support <see cref="IAsyncDisposable"/> items. If the collection
        /// contains any <see cref="IAsyncDisposable"/> items, an <see cref="InvalidOperationException"/> is thrown.
        /// Use <see cref="AsyncDisposableExtensions.DisposeAllAsync"/> for collections that may contain async disposables.
        /// </para>
        /// <para>
        /// When <paramref name="ignoreExceptions"/> is <see langword="false"/> and multiple items throw exceptions,
        /// an <see cref="AggregateException"/> containing all exceptions is thrown. If only one item throws,
        /// that exception is rethrown directly with its original stack trace preserved.
        /// </para>
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// The collection contains an <see cref="IAsyncDisposable"/> instance.
        /// </exception>
        /// <exception cref="AggregateException">
        /// Multiple items threw exceptions during disposal and <paramref name="ignoreExceptions"/> is <see langword="false"/>.
        /// </exception>
        public void DisposeAll(bool ignoreExceptions = false)
        {
            List<Exception>? exceptions = null;

            foreach (var item in collection.Reverse())
            {
                try
                {
                    switch (item)
                    {
                        case IAsyncDisposable:
                            ignoreExceptions = false;
                            throw new InvalidOperationException("The collection contains an IAsyncDisposable instance.");

                        case IDisposable disposable:
                            disposable.Dispose();
                            break;
                    }
                }
                catch (Exception exception)
                {
                    if (ignoreExceptions) continue;
                    exceptions ??= [];
                    exceptions.Add(exception);
                }
            }

            if (exceptions == null) return;

            if (exceptions.Count > 1)
            {
                throw new AggregateException(exceptions);
            }

            ExceptionDispatchInfo.Throw(exceptions[0]);
        }
    }
}
