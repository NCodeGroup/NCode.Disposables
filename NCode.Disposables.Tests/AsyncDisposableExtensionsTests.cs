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

namespace NCode.Disposables.Tests;

public class AsyncDisposableExtensionsTests
{
    [Fact]
    public async Task DisposeAll_Valid()
    {
        var order = string.Empty;
        var collection = new object[]
        {
            new(),
            AsyncDisposable.Create(() => order += "1"),
            new(),
            AsyncDisposable.Create(() => order += "2"),
            new(),
            AsyncDisposable.Create(() => order += "3")
        };
        await collection.DisposeAllAsync();
        Assert.Equal("321", order);
    }

    [Fact]
    public async Task DisposeAll_IgnoreExceptions()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() => order += "1"),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => order += "2"),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => order += "3")
        };
        await collection.DisposeAllAsync(ignoreExceptions: true);
        Assert.Equal("321", order);
    }

    [Fact]
    public async Task DisposeAll_Full_ThrowSingle()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() => order += "1"),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => order += "2"),
            AsyncDisposable.Create(() => order += "3")
        };
        await Assert.ThrowsAsync<InvalidOperationException>(async () => await collection.DisposeAllAsync());
        await collection.DisposeAllAsync();
        Assert.Equal("321", order);
    }

    [Fact]
    public async Task DisposeAll_Full_ThrowMultiple()
    {
        var order = string.Empty;
        var collection = new[]
        {
            AsyncDisposable.Create(() => order += "1"),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => order += "2"),
            AsyncDisposable.Create(() => throw new InvalidOperationException()),
            AsyncDisposable.Create(() => order += "3")
        };
        var exception = await Assert.ThrowsAsync<AggregateException>(async () => await collection.DisposeAllAsync());
        Assert.Equal(2, exception.InnerExceptions.Count);
        await collection.DisposeAllAsync();
        Assert.Equal("321", order);
    }
}