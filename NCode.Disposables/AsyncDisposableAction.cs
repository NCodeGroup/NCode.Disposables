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
/// Provides an <see cref="IAsyncDisposable"/> implementation that invokes a specified
/// <see cref="Func{TResult}">Func&lt;ValueTask&gt;</see> delegate when <see cref="DisposeAsync"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful for wrapping cleanup logic in an <see cref="IAsyncDisposable"/> interface,
/// enabling use with <see langword="await using"/> statements and other disposal patterns.
/// </para>
/// <para>
/// By default, the action is invoked only once (idempotent behavior) even if <see cref="DisposeAsync"/>
/// is called multiple times. This behavior can be configured via the constructor.
/// </para>
/// <para>
/// Thread-safety: When idempotent mode is enabled, concurrent calls to <see cref="DisposeAsync"/>
/// are handled safely using atomic operations.
/// </para>
/// </remarks>
public sealed class AsyncDisposableAction : IAsyncDisposable
{
    private Func<ValueTask>? _action;
    private readonly bool _idempotent;

    /// <summary>
    /// Initializes a new instance of the <see cref="AsyncDisposableAction"/> class with the specified
    /// asynchronous dispose action and idempotency setting.
    /// </summary>
    /// <param name="action">The asynchronous delegate to invoke when <see cref="DisposeAsync"/> is called.</param>
    /// <param name="idempotent">
    /// <see langword="true"/> (the default) to ensure multiple calls to <see cref="DisposeAsync"/>
    /// will only invoke the action once; <see langword="false"/> to allow multiple invocations.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <see langword="null"/>.</exception>
    public AsyncDisposableAction(Func<ValueTask> action, bool idempotent = true)
    {
        ArgumentNullException.ThrowIfNull(action);

        _action = action;
        _idempotent = idempotent;
    }

    /// <inheritdoc />
    /// <remarks>
    /// When idempotent mode is enabled (the default), the action is invoked only on the first call
    /// to this method; subsequent calls return immediately without invoking the action again.
    /// When idempotent mode is disabled, the action is invoked on every call.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        var action = _idempotent ? Interlocked.Exchange(ref _action, null) : _action;
        if (action is not null)
            await action();
    }
}
