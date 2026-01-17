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
/// Provides a no-operation implementation of <see cref="IDisposable"/> that performs no action
/// when <see cref="Dispose"/> is called.
/// </summary>
/// <remarks>
/// <para>
/// This class is useful as a null object pattern replacement for <see cref="IDisposable"/>,
/// allowing code to avoid null checks when a disposable is optional.
/// </para>
/// <para>
/// Use the <see cref="Singleton"/> property or <see cref="Disposable.Empty"/> to access
/// a shared instance rather than creating new instances.
/// </para>
/// </remarks>
public sealed class DisposableEmpty : IDisposable
{
    /// <summary>
    /// Gets a singleton instance of <see cref="DisposableEmpty"/> that can be shared
    /// across the application.
    /// </summary>
    /// <value>A shared singleton instance of <see cref="DisposableEmpty"/>.</value>
    /// <remarks>
    /// This singleton is also accessible via <see cref="Disposable.Empty"/>.
    /// </remarks>
    public static DisposableEmpty Singleton { get; } = new();

    /// <inheritdoc />
    /// <remarks>
    /// This implementation performs no operation and returns immediately.
    /// </remarks>
    public void Dispose()
    {
        // nothing
    }
}
