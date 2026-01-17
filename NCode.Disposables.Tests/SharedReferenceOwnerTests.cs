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

public class SharedReferenceOwnerTests
{
    #region Helper Methods

    // ohh, the memories from the good old COM days...
    private static int FinalRelease<T>(SharedReferenceOwner<T> owner)
    {
        var count = 0;
        while (true)
        {
            ++count;
            if (owner.ReleaseReference() == 0)
            {
                return count;
            }
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidArguments_CreatesOwner()
    {
        var value = new object();
        var onRelease = (object _) => { };

        var owner = new SharedReferenceOwner<object>(value, onRelease);

        Assert.Same(value, owner.Value);
    }

    [Fact]
    public void Constructor_InitialReferenceCountIsOne()
    {
        var releaseCount = 0;
        var value = new object();
        var onRelease = (object _) => { releaseCount++; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        // First release should decrement to zero
        var remaining = owner.ReleaseReference();

        Assert.Equal(0, remaining);
        Assert.Equal(1, releaseCount);
    }

    #endregion

    #region Value Tests

    [Fact]
    public void Value_ReturnsUnderlyingValue()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var result = owner.Value;

        Assert.Same(value, result);
        Assert.Equal(0, releaseCount);
    }

    [Fact]
    public void Value_AfterRelease_ThrowsObjectDisposedException()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var finalReleaseCount = FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
        Assert.Throws<ObjectDisposedException>(() => owner.Value);
    }

    [Fact]
    public void Value_WithValueType_ReturnsCorrectValue()
    {
        const int value = 42;
        var releaseCount = 0;
        var onRelease = (int _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<int>(value, onRelease);

        var result = owner.Value;

        Assert.Equal(value, result);
    }

    [Fact]
    public void Value_CanBeAccessedMultipleTimes()
    {
        var value = new object();
        var onRelease = (object _) => { };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

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
    public void AddReference_IncrementsReferenceCount()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        _ = owner.AddReference();
        _ = owner.AddReference();

        var finalReleaseCount = FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(3, finalReleaseCount);
    }

    [Fact]
    public void AddReference_ReturnsNewLeaseWithSameOwner()
    {
        var value = new object();
        var onRelease = (object _) => { };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var lease = owner.AddReference();

        Assert.True(lease.IsActive);
        Assert.Same(owner, lease.OwnerOrNull);
    }

    [Fact]
    public void AddReference_AfterRelease_ThrowsObjectDisposedException()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var finalReleaseCount = FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
        Assert.Throws<ObjectDisposedException>(() => owner.AddReference());
    }

    [Fact]
    public void AddReference_MultipleReferences_AllActive()
    {
        var value = new object();
        var onRelease = (object _) => { };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var lease1 = owner.AddReference();
        var lease2 = owner.AddReference();
        var lease3 = owner.AddReference();

        Assert.True(lease1.IsActive);
        Assert.True(lease2.IsActive);
        Assert.True(lease3.IsActive);
        Assert.Same(value, lease1.Value);
        Assert.Same(value, lease2.Value);
        Assert.Same(value, lease3.Value);
    }

    #endregion

    #region TryAddReference Tests

    [Fact]
    public void TryAddReference_WhenActive_ReturnsTrueAndLease()
    {
        var value = new object();
        var onRelease = (object _) => { };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var result = owner.TryAddReference(out var lease);

        Assert.True(result);
        Assert.True(lease.IsActive);
        Assert.Same(owner, lease.OwnerOrNull);
    }

    [Fact]
    public void TryAddReference_AfterRelease_ReturnsFalseAndDefaultLease()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        var finalReleaseCount = FinalRelease(owner);

        var result = owner.TryAddReference(out var newReference);

        Assert.False(result);
        Assert.False(newReference.IsActive);
        Assert.Equal(1, releaseCount);
        Assert.Equal(1, finalReleaseCount);
    }

    [Fact]
    public void TryAddReference_IncrementsReferenceCount()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { ++releaseCount; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        _ = owner.TryAddReference(out _);
        _ = owner.TryAddReference(out _);

        var finalReleaseCount = FinalRelease(owner);

        Assert.Equal(1, releaseCount);
        Assert.Equal(3, finalReleaseCount);
    }

    #endregion

    #region ReleaseReference Tests

    [Fact]
    public void ReleaseReference_DecrementsReferenceCount()
    {
        var value = new object();
        var onRelease = (object _) => { };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        _ = owner.AddReference();
        _ = owner.AddReference();

        var remaining1 = owner.ReleaseReference();
        var remaining2 = owner.ReleaseReference();
        var remaining3 = owner.ReleaseReference();

        Assert.Equal(2, remaining1);
        Assert.Equal(1, remaining2);
        Assert.Equal(0, remaining3);
    }

    [Fact]
    public void ReleaseReference_WhenCountReachesZero_InvokesOnRelease()
    {
        var value = new object();
        var releaseCount = 0;
        object? releasedValue = null;
        var onRelease = (object v) =>
        {
            releaseCount++;
            releasedValue = v;
        };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        owner.ReleaseReference();

        Assert.Equal(1, releaseCount);
        Assert.Same(value, releasedValue);
    }

    [Fact]
    public void ReleaseReference_OnlyInvokesOnReleaseOnce()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { releaseCount++; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        owner.ReleaseReference();
        owner.ReleaseReference();
        owner.ReleaseReference();

        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public void ReleaseReference_AfterCountReachesZero_ReturnsZero()
    {
        var value = new object();
        var onRelease = (object _) => { };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        owner.ReleaseReference();

        var result1 = owner.ReleaseReference();
        var result2 = owner.ReleaseReference();

        Assert.Equal(0, result1);
        Assert.Equal(0, result2);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task AddReference_ConcurrentCalls_AllSucceed()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { Interlocked.Increment(ref releaseCount); };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        const int concurrentCalls = 100;
        var tasks = new Task<SharedReferenceLease<object>>[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => owner.AddReference());
        }

        var leases = await Task.WhenAll(tasks);

        Assert.All(leases, lease => Assert.True(lease.IsActive));

        // Release all references (100 from concurrent calls + 1 initial)
        var finalReleaseCount = FinalRelease(owner);

        Assert.Equal(concurrentCalls + 1, finalReleaseCount);
        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public async Task ReleaseReference_ConcurrentCalls_OnlyInvokesOnReleaseOnce()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { Interlocked.Increment(ref releaseCount); };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        // Add many references first
        const int refCount = 50;
        for (var i = 0; i < refCount; i++)
        {
            owner.AddReference();
        }

        // Release all concurrently (refCount + 1 initial)
        const int totalReleases = refCount + 1;
        var tasks = new Task[totalReleases];

        for (var i = 0; i < totalReleases; i++)
        {
            tasks[i] = Task.Run(() => owner.ReleaseReference());
        }

        await Task.WhenAll(tasks);

        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public async Task TryAddReference_ConcurrentWithRelease_EitherSucceedsOrFails()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { Interlocked.Increment(ref releaseCount); };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        // Add some references
        const int initialRefs = 10;
        for (var i = 0; i < initialRefs; i++)
        {
            owner.AddReference();
        }

        var successCount = 0;
        var failureCount = 0;

        var addTasks = new Task[50];
        var releaseTasks = new Task[initialRefs + 1];

        for (var i = 0; i < addTasks.Length; i++)
        {
            addTasks[i] = Task.Run(() =>
            {
                if (owner.TryAddReference(out var lease))
                {
                    Interlocked.Increment(ref successCount);
                    lease.Dispose();
                }
                else
                {
                    Interlocked.Increment(ref failureCount);
                }
            });
        }

        for (var i = 0; i < releaseTasks.Length; i++)
        {
            releaseTasks[i] = Task.Run(() => owner.ReleaseReference());
        }

        await Task.WhenAll(addTasks);
        await Task.WhenAll(releaseTasks);

        // All concurrent TryAddReference either succeeded or failed - no exceptions
        Assert.Equal(50, successCount + failureCount);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsISharedReferenceOwner()
    {
        var value = new object();
        var onRelease = (object _) => { };

        var owner = new SharedReferenceOwner<object>(value, onRelease);

        Assert.IsAssignableFrom<ISharedReferenceOwner<object>>(owner);
    }

    #endregion

    #region Generic Type Tests

    [Fact]
    public void GenericType_WorksWithValueTypes()
    {
        const int value = 42;
        var releaseCount = 0;
        var onRelease = (int _) => { releaseCount++; };
        var owner = new SharedReferenceOwner<int>(value, onRelease);

        Assert.Equal(value, owner.Value);

        owner.ReleaseReference();

        Assert.Equal(1, releaseCount);
    }

    [Fact]
    public void GenericType_WorksWithDisposableTypes()
    {
        var mockValue = new Mock<IDisposable>(MockBehavior.Strict);
        mockValue.Setup(x => x.Dispose());
        var onRelease = (IDisposable d) => d.Dispose();
        var owner = new SharedReferenceOwner<IDisposable>(mockValue.Object, onRelease);

        Assert.Same(mockValue.Object, owner.Value);

        owner.ReleaseReference();

        mockValue.Verify(x => x.Dispose(), Times.Once);
    }

    [Fact]
    public void GenericType_WorksWithStrings()
    {
        const string value = "test string";
        string? releasedValue = null;
        var onRelease = (string s) => { releasedValue = s; };
        var owner = new SharedReferenceOwner<string>(value, onRelease);

        Assert.Equal(value, owner.Value);

        owner.ReleaseReference();

        Assert.Equal(value, releasedValue);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Integration_FullLifecycle()
    {
        var value = new object();
        var releaseCount = 0;
        var onRelease = (object _) => { releaseCount++; };
        var owner = new SharedReferenceOwner<object>(value, onRelease);

        // Create some leases
        var lease1 = owner.AddReference();
        var lease2 = owner.AddReference();
        var lease3 = owner.AddReference();

        // All leases can access the value
        Assert.Same(value, owner.Value);
        Assert.Same(value, lease1.Value);
        Assert.Same(value, lease2.Value);
        Assert.Same(value, lease3.Value);

        // Dispose leases one by one
        lease1.Dispose();
        Assert.Equal(0, releaseCount);

        lease2.Dispose();
        Assert.Equal(0, releaseCount);

        lease3.Dispose();
        Assert.Equal(0, releaseCount);

        // Original reference still holds
        Assert.Same(value, owner.Value);

        // Release original reference
        owner.ReleaseReference();

        Assert.Equal(1, releaseCount);
        Assert.Throws<ObjectDisposedException>(() => owner.Value);
    }

    [Fact]
    public void Integration_SharedReferenceCreate_WorksEndToEnd()
    {
        var value = new Mock<IDisposable>(MockBehavior.Strict);
        value.Setup(x => x.Dispose());

        var lease1 = SharedReference.Create(value.Object);
        var lease2 = lease1.AddReference();
        var lease3 = lease2.AddReference();

        Assert.Same(value.Object, lease1.Value);
        Assert.Same(value.Object, lease2.Value);
        Assert.Same(value.Object, lease3.Value);

        lease1.Dispose();
        value.Verify(x => x.Dispose(), Times.Never);

        lease2.Dispose();
        value.Verify(x => x.Dispose(), Times.Never);

        lease3.Dispose();
        value.Verify(x => x.Dispose(), Times.Once);
    }

    #endregion
}
