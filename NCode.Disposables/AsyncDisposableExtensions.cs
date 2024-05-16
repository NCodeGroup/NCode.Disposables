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
/// Provides various extension methods for <see cref="IAsyncDisposable"/> instances.
/// </summary>
public static class AsyncDisposableExtensions
{
    /// <summary>
    /// Creates a new <see cref="AsyncSharedReferenceLease{T}"/> instance that uses reference counting to share the
    /// specified <paramref name="value"/>. This variant will automatically dispose the resource when the last lease
    /// is disposed.
    /// </summary>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <typeparam name="T">The type of the shared resource.</typeparam>
    public static AsyncSharedReferenceLease<T> AsSharedReference<T>(this T value)
        where T : IAsyncDisposable
    {
        return AsyncSharedReference.Create(value);
    }

    /// <summary>
    /// Provides a safe way to dispose of a collection of <see cref="IAsyncDisposable"/> instances.
    /// </summary>
    /// <param name="collection">The collection of <see cref="IAsyncDisposable"/> instances.</param>
    /// <param name="ignoreExceptions"><c>true</c> to ignore any exceptions thrown while disposing individual items.</param>
    public static async ValueTask DisposeAllAsync(
        this IEnumerable<IAsyncDisposable?> collection,
        bool ignoreExceptions = false)
    {
        List<Exception>? exceptions = null;

        foreach (var item in collection.Reverse())
        {
            try
            {
                if (item is not null)
                    await item.DisposeAsync();
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