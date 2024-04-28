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
/// Provides an <see cref="IAsyncDisposable"/> implementation that will invoke a <see cref="Func{ValueTask}"/> when <see cref="DisposeAsync"/> is called.
/// </summary>
public sealed class AsyncDisposableAction : IAsyncDisposable
{
    private Func<ValueTask>? _action;

    /// <summary>
    /// Initializes a new instance of <see cref="AsyncDisposableAction"/> with the specified dispose action.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Func{ValueTask}"/> to invoke when <see cref="DisposeAsync"/> is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public AsyncDisposableAction(Func<ValueTask> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        _action = action;
    }

    /// <summary>
    /// Invokes the dispose action only if it already hasn't been invoked.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        var action = Interlocked.Exchange(ref _action, null);
        if (action is not null)
            await action();
    }
}