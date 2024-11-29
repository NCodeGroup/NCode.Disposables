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
    public async Task DisposeIfAvailable_WhenAvailable()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable
            .As<IAsyncDisposable>()
            .Setup(x => x.DisposeAsync())
            .Returns(ValueTask.CompletedTask)
            .Verifiable();

        await mockDisposable.Object.DisposeAsyncIfAvailable();

        mockDisposable.Verify();
        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public async Task DisposeIfAvailable_WhenNotAvailable()
    {
        var mockDisposable = new Mock<IDisposable>(MockBehavior.Strict);
        mockDisposable
            .Setup(x => x.Dispose())
            .Verifiable();

        await mockDisposable.Object.DisposeAsyncIfAvailable();

        mockDisposable.Verify();
    }

    [Fact]
    public void DisposeAll_Valid()
    {
        var order = string.Empty;
        var collection = new object[]
        {
            new(),
            Disposable.Create(() => order += "1"),
            new(),
            Disposable.Create(() => order += "2"),
            new(),
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
        Assert.Throws<InvalidOperationException>(() => collection.DisposeAll());
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
        var exception = Assert.Throws<AggregateException>(() => collection.DisposeAll());
        Assert.Equal(2, exception.InnerExceptions.Count);
        collection.DisposeAll();
        Assert.Equal("321", order);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DisposeAll_Async(bool ignoreExceptions)
    {
        var collection = new object[]
        {
            AsyncDisposable.Create(() => { })
        };
        var exception = Assert.Throws<InvalidOperationException>(() => collection.DisposeAll(ignoreExceptions));
        Assert.Equal("The collection contains an IAsyncDisposable instance.", exception.Message);
    }
}