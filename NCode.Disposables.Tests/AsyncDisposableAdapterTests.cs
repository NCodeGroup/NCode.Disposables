﻿#region Copyright Preamble

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

namespace NCode.Disposables.Tests;

public class AsyncDisposableAdapterTests
{
    [Fact]
    public async Task AdaptNonAsync()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);
        await adapter.DisposeAsync();
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeOnce()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object);
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        mockDisposable.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public async Task DisposeMultipl()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.Dispose());

        var adapter = new AsyncDisposableAdapter(mockDisposable.Object, idempotent: false);
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        await adapter.DisposeAsync();
        mockDisposable.Verify(x => x.Dispose(), Times.Exactly(3));
    }
}