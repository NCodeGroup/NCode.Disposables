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
/// Provides extension methods for <see cref="IAsyncDisposable"/> instances and collections
/// containing disposable items.
/// </summary>
public static class AsyncDisposableExtensions
{
    /// <param name="value">The underlying resource to be shared.</param>
    /// <typeparam name="T">The type of the shared resource, which must implement <see cref="IAsyncDisposable"/>.</typeparam>
    extension<T>(T value) where T : IAsyncDisposable
    {
        /// <summary>
        /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> that shares the specified <see cref="IAsyncDisposable"/>
        /// resource using reference counting. The resource is automatically disposed asynchronously when the last lease is released.
        /// </summary>
        /// <returns>
        /// A new <see cref="AsyncSharedReferenceLease{T}"/> representing the initial lease on the shared resource.
        /// </returns>
        /// <remarks>
        /// This is a convenience extension method that wraps <see cref="AsyncSharedReference.Create{T}(T)"/>.
        /// The returned lease holds an initial reference to the resource. Additional references can be obtained
        /// by calling <see cref="AsyncSharedReferenceLease{T}.AddReference"/> on any active lease.
        /// </remarks>
        public AsyncSharedReferenceLease<T> AsSharedReference()
        {
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
        /// <param name="ignoreExceptions">
        /// <see langword="true"/> to suppress exceptions thrown by individual items during disposal and continue
        /// disposing remaining items; <see langword="false"/> (the default) to collect and throw exceptions.
        /// </param>
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
        /// When <paramref name="ignoreExceptions"/> is <see langword="false"/> and multiple items throw exceptions,
        /// an <see cref="AggregateException"/> containing all exceptions is thrown. If only one item throws,
        /// that exception is rethrown directly with its original stack trace preserved.
        /// </para>
        /// </remarks>
        /// <exception cref="AggregateException">
        /// Multiple items threw exceptions during disposal and <paramref name="ignoreExceptions"/> is <see langword="false"/>.
        /// </exception>
        public async ValueTask DisposeAllAsync(bool ignoreExceptions = false)
        {
            List<Exception>? exceptions = null;

            foreach (var item in collection.Reverse())
            {
                try
                {
                    switch (item)
                    {
                        case IAsyncDisposable asyncDisposable:
                            await asyncDisposable.DisposeAsync();
                            break;

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
