﻿#region Copyright Preamble

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
/// Provides an <see cref="IDisposable"/> implementation that will invoke an <see cref="Action"/> when <see cref="Dispose"/> is called.
/// </summary>
public sealed class DisposableAction : IDisposable
{
    private Action? _action;
    private readonly bool _idempotent;

    /// <summary>
    /// Initializes a new instance of <see cref="DisposableAction"/> with the specified dispose action.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="Dispose"/> is called.</param>
    /// <param name="idempotent">Specifies if the adapter should be idempotent where multiple calls to <c>Dispose</c>
    /// will only dispose the underlying instance once. Default is <c>true</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public DisposableAction(Action action, bool idempotent = true)
    {
        ArgumentNullException.ThrowIfNull(action);

        _action = action;
        _idempotent = idempotent;
    }

    /// <summary>
    /// Invokes the dispose action only if it already hasn't been invoked.
    /// </summary>
    public void Dispose()
    {
        var action = _idempotent ? Interlocked.Exchange(ref _action, null) : _action;
        action?.Invoke();
    }
}