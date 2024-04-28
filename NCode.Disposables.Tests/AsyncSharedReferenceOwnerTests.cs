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

public class AsyncSharedReferenceOwnerTests
{
    [Fact]
    public void Value_Valid()
    {
        var value = new object();
        var onRelease = (object o) => ValueTask.CompletedTask;
        var reference = new AsyncSharedReferenceOwner<object>(value, onRelease);
        Assert.Same(value, reference.Value);
    }

    [Fact]
    public async Task Value_Throws_AfterDispose()
    {
        var value = new object();
        var onRelease = (object o) => ValueTask.CompletedTask;
        var reference = new AsyncSharedReferenceOwner<object>(value, onRelease);
        await reference.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => reference.Value);
    }

    [Fact]
    public async Task Value_Throws_AfterSharedDispose()
    {
        var value = new object();
        var onRelease = (object o) => ValueTask.CompletedTask;
        var reference0 = new AsyncSharedReferenceOwner<object>(value, onRelease);
        Assert.Same(value, reference0.Value);
        var reference1 = reference0.AddReference();
        Assert.Same(value, reference1.Value);
        await reference0.DisposeAsync();
        Assert.Same(value, reference0.Value);
        await reference1.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => reference0.Value);
        Assert.Throws<ObjectDisposedException>(() => reference1.Value);
    }

    [Fact]
    public async Task Dispose_Once()
    {
        var value = new object();
        var disposeCount = 0;
        var onRelease = (object o) =>
        {
            ++disposeCount;
            return ValueTask.CompletedTask;
        };
        var reference = new AsyncSharedReferenceOwner<object>(value, onRelease);
        await reference.DisposeAsync();
        await reference.DisposeAsync();
        Assert.Equal(1, disposeCount);
    }

    [Fact]
    public async Task AddReference_Throws_AfterDispose()
    {
        var value = new object();
        var onRelease = (object o) => ValueTask.CompletedTask;
        var reference = new AsyncSharedReferenceOwner<object>(value, onRelease);
        await reference.DisposeAsync();
        Assert.Throws<ObjectDisposedException>(() => reference.AddReference());
    }

    [Fact]
    public async Task TryAddReference_Fails_AfterDispose()
    {
        var value = new object();
        var onRelease = (object o) => ValueTask.CompletedTask;
        var reference0 = new AsyncSharedReferenceOwner<object>(value, onRelease);
        await reference0.DisposeAsync();
        var result = reference0.TryAddReference(out var reference1);
        Assert.False(result);
        Assert.Null(reference1);
    }
}