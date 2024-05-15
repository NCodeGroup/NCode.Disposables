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
/// Provides the ability add asynchronous disposal support to an existing <see cref="IDisposable"/> instance.
/// </summary>
public sealed class AsyncDisposableAdapter : IAsyncDisposable
{
    private IDisposable? _disposable;
    private readonly bool _idempotent;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncDisposableAdapter"/> with the specified <see cref="IDisposable"/> instance.
    /// </summary>
    /// <param name="disposable">The underlying <see cref="IDisposable"/> instance to adapt.</param>
    /// <param name="idempotent">Specifies if the adapter should be idempotent where multiple calls to <c>DisposeAsync</c>
    /// will only dispose the underlying instance once. Default is <c>true</c>.</param>
    public AsyncDisposableAdapter(IDisposable disposable, bool idempotent = true)
    {
        _disposable = disposable;
        _idempotent = idempotent;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        var disposable = _idempotent ? Interlocked.Exchange(ref _disposable, null) : _disposable;
        disposable?.Dispose();
        return ValueTask.CompletedTask;
    }
}