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
/// Provides a no-operation implementation of <see cref="IAsyncDisposable"/> that performs no action
/// when <see cref="DisposeAsync"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful as a null object pattern replacement for <see cref="IAsyncDisposable"/>,
/// allowing code to avoid null checks when a disposable is optional.
/// </para>
/// <para>
/// Use the <see cref="Singleton"/> property or <see cref="AsyncDisposable.Empty"/> to access
/// a shared instance rather than creating new instances.
/// </para>
/// </remarks>
public sealed class AsyncDisposableEmpty : IAsyncDisposable
{
    /// <summary>
    /// Gets a singleton instance of <see cref="AsyncDisposableEmpty"/> that can be shared
    /// across the application.
    /// </summary>
    /// <value>A shared singleton instance of <see cref="AsyncDisposableEmpty"/>.</value>
    /// <remarks>
    /// This singleton is also accessible via <see cref="AsyncDisposable.Empty"/>.
    /// </remarks>
    public static AsyncDisposableEmpty Singleton { get; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// This implementation performs no operation and returns a completed <see cref="ValueTask"/> immediately.
    /// </remarks>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
