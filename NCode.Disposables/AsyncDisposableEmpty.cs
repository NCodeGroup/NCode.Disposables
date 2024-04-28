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
/// Provides an implementation of <see cref="IAsyncDisposable"/> that is empty and performs nothing (i.e. nop) when <see cref="DisposeAsync"/> is called.
/// </summary>
public sealed class AsyncDisposableEmpty : IAsyncDisposable
{
    /// <summary>
    /// Contains a singleton instance of <see cref="IAsyncDisposable"/> that performs nothing when <see cref="DisposeAsync"/> is called.
    /// </summary>
    public static AsyncDisposableEmpty Singleton { get; } = new();

    /// <summary>
    /// This specific implementation of <see cref="IAsyncDisposable"/> is empty and performs nothing.
    /// </summary>
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}