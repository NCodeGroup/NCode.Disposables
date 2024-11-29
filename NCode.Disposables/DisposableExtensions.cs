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
/// Provides various extension methods for <see cref="IDisposable"/> instances.
/// </summary>
public static class DisposableExtensions
{
    /// <summary>
    /// Asynchronously disposes the specified <see cref="IDisposable"/> instance if it implements <see cref="IAsyncDisposable"/>
    /// otherwise it will dispose it synchronously.
    /// </summary>
    /// <param name="disposable">The <see cref="IDisposable"/> instance to dispose.</param>
    public static async ValueTask DisposeAsyncIfAvailable(this IDisposable? disposable)
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

    /// <summary>
    /// Creates a new <see cref="SharedReferenceLease{T}"/> instance that uses reference counting to share the
    /// specified <paramref name="value"/>. This variant will automatically dispose the resource when the last
    /// lease is disposed.
    /// </summary>
    /// <param name="value">The underlying resource to be shared.</param>
    /// <typeparam name="T">The type of the shared resource.</typeparam>
    public static SharedReferenceLease<T> AsSharedReference<T>(this T value)
        where T : IDisposable
    {
        return SharedReference.Create(value);
    }

    /// <summary>
    /// Provides a safe way to dispose of a collection of items which may contain <see cref="IDisposable"/> instances.
    /// This method will throw an <see cref="InvalidOperationException"/> if the collection contains an <see cref="IAsyncDisposable"/> instance
    /// irrespective of the <paramref name="ignoreExceptions"/> parameter.
    /// </summary>
    /// <param name="collection">The collection of items to dispose.</param>
    /// <param name="ignoreExceptions"><c>true</c> to ignore any exceptions thrown while disposing individual items.</param>
    /// <exception cref="InvalidOperationException">Thrown when the collection contains an <see cref="IAsyncDisposable"/> instance.</exception>
    public static void DisposeAll(
        this IEnumerable<object?> collection,
        bool ignoreExceptions = false)
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