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
    #region Helper Methods

    private static async ValueTask<int> FinalRelease<T>(AsyncSharedReferenceOwner<T> owner)
    {
        var count = 0;
        while (true)
        {
            ++count;
            if (await owner.ReleaseReferenceAsync() == 0)
            {
                return count;
            }
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_SetsInitialReferenceCountToOne()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        Assert.Same(value, owner.Value);
    }

    [Fact]
    public void Constructor_WithNullValue_Succeeds()
    {
        var owner = new AsyncSharedReferenceOwner<object?>(null, _ => ValueTask.CompletedTask);

        Assert.Null(owner.Value);
    }

    #endregion

    #region Value Tests

    [Fact]
    public void Value_ReturnsUnderlyingValue()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        Assert.Same(value, owner.Value);
        Assert.Equal(0, releaseCount);
    }

    [Fact]
    public async Task Value_AfterFinalRelease_ThrowsObjectDisposedException()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        var finalReleaseCount = await FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
        Assert.Throws<ObjectDisposedException>(() => owner.Value);
    }

    [Fact]
    public void Value_CanBeAccessedMultipleTimes()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        var result1 = owner.Value;
        var result2 = owner.Value;
        var result3 = owner.Value;

        Assert.Same(value, result1);
        Assert.Same(value, result2);
        Assert.Same(value, result3);
    }

    #endregion

    #region AddReference Tests

    [Fact]
    public void AddReference_ReturnsActiveLease()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        var lease = owner.AddReference();

        Assert.True(lease.IsActive);
        Assert.Same(owner, lease.OwnerOrNull);
    }

    [Fact]
    public async Task AddReference_IncrementsReferenceCount()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        _ = owner.AddReference();
        _ = owner.AddReference();

        var finalReleaseCount = await FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(3, finalReleaseCount);
    }

    [Fact]
    public async Task AddReference_AfterFinalRelease_ThrowsObjectDisposedException()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        var finalReleaseCount = await FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
        Assert.Throws<ObjectDisposedException>(() => owner.AddReference());
    }

    [Fact]
    public void AddReference_MultipleCalls_AllReturnActiveLease()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        var lease1 = owner.AddReference();
        var lease2 = owner.AddReference();
        var lease3 = owner.AddReference();

        Assert.True(lease1.IsActive);
        Assert.True(lease2.IsActive);
        Assert.True(lease3.IsActive);
    }

    #endregion

    #region TryAddReference Tests

    [Fact]
    public void TryAddReference_WhenActive_ReturnsTrueWithActiveLease()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        var result = owner.TryAddReference(out var lease);

        Assert.True(result);
        Assert.True(lease.IsActive);
        Assert.Same(owner, lease.OwnerOrNull);
    }

    [Fact]
    public async Task TryAddReference_AfterFinalRelease_ReturnsFalseWithDefaultLease()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        var finalReleaseCount = await FinalRelease(owner);

        var result = owner.TryAddReference(out var newReference);

        Assert.False(result);
        Assert.False(newReference.IsActive);
        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
    }

    [Fact]
    public void TryAddReference_MultipleCalls_AllSucceed()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        var result1 = owner.TryAddReference(out var lease1);
        var result2 = owner.TryAddReference(out var lease2);

        Assert.True(result1);
        Assert.True(result2);
        Assert.True(lease1.IsActive);
        Assert.True(lease2.IsActive);
    }

    #endregion

    #region ReleaseReferenceAsync Tests

    [Fact]
    public async Task ReleaseReferenceAsync_DecrementsReferenceCount()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        _ = owner.AddReference();
        var countAfterRelease = await owner.ReleaseReferenceAsync();

        Assert.Equal(1, countAfterRelease);
    }

    [Fact]
    public async Task ReleaseReferenceAsync_WhenCountReachesZero_InvokesReleaseCallback()
    {
        var value = new object();
        var releaseCount = 0;
        object? releasedValue = null;
        var owner = new AsyncSharedReferenceOwner<object>(value, v =>
        {
            releaseCount++;
            releasedValue = v;
            return ValueTask.CompletedTask;
        });

        var countAfterRelease = await owner.ReleaseReferenceAsync();

        Assert.Equal(0, countAfterRelease);
        Assert.Equal(1, releaseCount);
        Assert.Same(value, releasedValue);
    }

    [Fact]
    public async Task ReleaseReferenceAsync_CalledAfterZero_ReturnsZeroWithoutCallingCallback()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        await owner.ReleaseReferenceAsync();
        var count1 = await owner.ReleaseReferenceAsync();
        var count2 = await owner.ReleaseReferenceAsync();

        Assert.Equal(0, count1);
        Assert.Equal(0, count2);
        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public async Task ReleaseReferenceAsync_WithMultipleReferences_CallbackOnlyOnFinal()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        _ = owner.AddReference();
        _ = owner.AddReference();

        await owner.ReleaseReferenceAsync();
        Assert.Equal(0, releaseCount);

        await owner.ReleaseReferenceAsync();
        Assert.Equal(0, releaseCount);

        await owner.ReleaseReferenceAsync();
        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public async Task ReleaseReferenceAsync_ReleaseCallbackThrows_PropagatesException()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
            throw new InvalidOperationException("Release failed"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await owner.ReleaseReferenceAsync());

        Assert.Equal("Release failed", exception.Message);
    }

    #endregion

    #region UnsafeReleaseReference Tests

    [Fact]
    public void UnsafeReleaseReference_DecrementsWithoutCallback()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        var count = owner.UnsafeReleaseReference();

        Assert.Equal(0, count);
        Assert.Equal(0, releaseCount);
    }

    [Fact]
    public void UnsafeReleaseReference_AfterZero_ReturnsZero()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        owner.UnsafeReleaseReference();
        var count = owner.UnsafeReleaseReference();

        Assert.Equal(0, count);
    }

    [Fact]
    public void UnsafeReleaseReference_DoesNotPreventObjectDisposedException()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        owner.UnsafeReleaseReference();

        Assert.Throws<ObjectDisposedException>(() => owner.Value);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task AddReference_ConcurrentCalls_AllSucceed()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            Interlocked.Increment(ref releaseCount);
            return ValueTask.CompletedTask;
        });

        const int concurrentCalls = 100;
        var leases = new AsyncSharedReferenceLease<object>[concurrentCalls];
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            var index = i;
            tasks[i] = Task.Run(() => leases[index] = owner.AddReference());
        }

        await Task.WhenAll(tasks);

        Assert.All(leases, l => Assert.True(l.IsActive));

        var finalReleaseCount = await FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(concurrentCalls + 1, finalReleaseCount);
    }

    [Fact]
    public async Task ReleaseReferenceAsync_ConcurrentCalls_CallbackOnlyOnce()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            Interlocked.Increment(ref releaseCount);
            return ValueTask.CompletedTask;
        });

        const int additionalReferences = 99;
        for (var i = 0; i < additionalReferences; i++)
        {
            _ = owner.AddReference();
        }

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = owner.ReleaseReferenceAsync().AsTask();
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, releaseCount);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIAsyncSharedReferenceOwner()
    {
        var value = new object();
        var owner = new AsyncSharedReferenceOwner<object>(value, _ => ValueTask.CompletedTask);

        Assert.IsAssignableFrom<IAsyncSharedReferenceOwner<object>>(owner);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task FullLifecycle_CreateAddReleaseDispose()
    {
        var value = new object();
        var releaseCount = 0;
        var owner = new AsyncSharedReferenceOwner<object>(value, _ =>
        {
            releaseCount++;
            return ValueTask.CompletedTask;
        });

        var lease1 = owner.AddReference();
        var lease2 = owner.AddReference();

        Assert.Same(value, lease1.Value);
        Assert.Same(value, lease2.Value);

        await lease1.DisposeAsync();
        Assert.Equal(0, releaseCount);

        await lease2.DisposeAsync();
        Assert.Equal(0, releaseCount);

        await owner.ReleaseReferenceAsync();
        Assert.Equal(1, releaseCount);

        Assert.Throws<ObjectDisposedException>(() => owner.Value);
    }

    [Fact]
    public async Task WithAsyncDisposableValue_DisposesUnderlyingResource()
    {
        var mockDisposable = new Mock<IAsyncDisposable>(MockBehavior.Strict);
        mockDisposable.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);

        var owner = new AsyncSharedReferenceOwner<IAsyncDisposable>(
            mockDisposable.Object,
            async v => await v.DisposeAsync());

        await owner.ReleaseReferenceAsync();

        mockDisposable.Verify(x => x.DisposeAsync(), Times.Once);
    }

    #endregion
}
