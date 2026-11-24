#region Copyright Preamble

// Copyright @ 2024 NCode Group
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

namespace NCode.Disposables;

/// <summary>
/// Provides an adapter that adds asynchronous disposal support to an existing <see cref="IDisposable"/> instance,
/// enabling it to be used with <see langword="await using"/> statements and other <see cref="IAsyncDisposable"/> patterns.
/// </summary>
/// <remarks>
/// <para>
/// This adapter wraps a synchronous <see cref="IDisposable"/> and exposes it as an <see cref="IAsyncDisposable"/>.
/// The underlying <see cref="IDisposable.Dispose"/> method is called synchronously when <see cref="DisposeAsync"/> is invoked.
/// </para>
/// <para>
/// By default, the adapter is idempotent, meaning multiple calls to <see cref="DisposeAsync"/> will only
/// dispose the underlying instance once. This behavior can be configured via the constructor.
/// </para>
/// <para>
/// Thread-safety: When idempotent mode is enabled, concurrent calls to <see cref="DisposeAsync"/>
/// are handled safely using atomic operations.
/// </para>
/// </remarks>
/// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
/// <param name="idempotent">
/// <see langword="true"/> (the default) to ensure multiple calls to <see cref="DisposeAsync"/>
/// will only dispose the underlying instance once; <see langword="false"/> to allow multiple disposals.
/// </param>
public sealed class AsyncDisposableAdapter(IDisposable disposable, bool idempotent = true) : IAsyncDisposable
{
    private IDisposable? _disposable = disposable;
    private readonly bool _idempotent = idempotent;

    /// <inheritdoc />
    /// <remarks>
    /// Disposes the underlying <see cref="IDisposable"/> instance synchronously.
    /// When idempotent mode is enabled (the default), the underlying instance is disposed only on the first call;
    /// subsequent calls return immediately without disposing again.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        var disposable = _idempotent ? Interlocked.Exchange(ref _disposable, null) : _disposable;
        disposable?.Dispose();
        return ValueTask.CompletedTask;
    }
}
