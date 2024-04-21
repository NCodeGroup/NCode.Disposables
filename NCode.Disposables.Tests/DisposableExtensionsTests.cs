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

public class DisposableExtensionsTests
{
    [Fact]
    public void DisposeAll_Valid()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => order += "3")
        };
        collection.DisposeAll();
        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_IgnoreExceptions()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "3")
        };
        collection.DisposeAll(ignoreExceptions: true);
        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_Full_ThrowSingle()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => order += "3")
        };
        Assert.Throws<InvalidOperationException>(() => { collection.DisposeAll(); });
        collection.DisposeAll();
        Assert.Equal("321", order);
    }

    [Fact]
    public void DisposeAll_Full_ThrowMultiple()
    {
        var order = string.Empty;
        var collection = new[]
        {
            Disposable.Create(() => order += "1"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "2"),
            Disposable.Create(() => throw new InvalidOperationException()),
            Disposable.Create(() => order += "3")
        };
        var exception = Assert.Throws<AggregateException>(() => { collection.DisposeAll(); });
        Assert.Equal(2, exception.InnerExceptions.Count);
        collection.DisposeAll();
        Assert.Equal("321", order);
    }
}