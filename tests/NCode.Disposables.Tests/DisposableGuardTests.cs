// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

namespace NCode.Disposables.Tests;

public class DisposableGuardTests : UnitTestBase
{
    private const string AlreadyReleasedMessage =
        "Ownership of the resource has already been released.";

    #region Constructor Tests

    [Fact]
    public void Constructor_Null_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new DisposableGuard<IDisposable>(null!)
        );

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void Constructor_Value_HoldsValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();

        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        Assert.Same(mockDisposable.Object, guard.Value);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_WithoutRelease_DisposesValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();
    }

    [Fact]
    public void Dispose_AfterRelease_DoesNotDisposeValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        guard.Dispose();

        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    [Fact]
    public void Dispose_Twice_DisposesOnce()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();
        guard.Dispose();
    }

    [Fact]
    public void UsingDeclaration_WhenScopeThrows_DisposesValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable(Times.Once());

        Assert.Throws<InvalidOperationException>(ThrowInScope);

        void ThrowInScope()
        {
            using var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
            throw new InvalidOperationException();
        }
    }

    [Fact]
    public void UsingDeclaration_WhenReleased_DoesNotDisposeValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();

        IDisposable released;
        using (var guard = new DisposableGuard<IDisposable>(mockDisposable.Object))
        {
            released = guard.Release();
        }

        Assert.Same(mockDisposable.Object, released);
        mockDisposable.Verify(x => x.Dispose(), Times.Never);
    }

    #endregion

    #region Release Tests

    [Fact]
    public void Release_WhenOwned_ReturnsValue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        var result = guard.Release();

        Assert.Same(mockDisposable.Object, result);
    }

    [Fact]
    public void Release_Twice_ThrowsInvalidOperationException()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        var exception = Assert.Throws<InvalidOperationException>(() => guard.Release());

        Assert.Equal(AlreadyReleasedMessage, exception.Message);
    }

    [Fact]
    public void Release_AfterDispose_ThrowsObjectDisposedException()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Dispose();

        Assert.Throws<ObjectDisposedException>(() => guard.Release());
    }

    [Fact]
    public void Release_AfterReleaseAndDispose_ThrowsInvalidOperationException()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();
        guard.Dispose();

        Assert.Throws<InvalidOperationException>(() => guard.Release());
    }

    [Fact]
    public async Task Release_WhileDisposeRuns_ThrowsObjectDisposedException()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var gate = new ManualResetEventSlim();
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable
            .Setup(x => x.Dispose())
            .Callback(() =>
            {
                entered.SetResult();
                var released = gate.Wait(TimeSpan.FromSeconds(30));
                Assert.True(released);
            })
            .Verifiable(Times.Once());
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        var disposeTask = Task.Run(() => guard.Dispose());
        await entered.Task;

        Assert.Throws<ObjectDisposedException>(() => guard.Release());
        Assert.False(disposeTask.IsCompleted);

        gate.Set();
        await disposeTask;
    }

    #endregion

    #region IsReleased Property Tests

    [Fact]
    public void IsReleased_BeforeRelease_ReturnsFalse()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        Assert.False(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterRelease_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Release();

        Assert.True(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterReleaseAndDispose_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        guard.Dispose();

        Assert.True(guard.IsReleased);
    }

    [Fact]
    public void IsReleased_AfterDisposeWithoutRelease_ReturnsFalse()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();

        Assert.False(guard.IsReleased);
    }

    #endregion

    #region IsDisposed Property Tests

    [Fact]
    public void IsDisposed_BeforeDispose_ReturnsFalse()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterDispose_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        mockDisposable.Setup(x => x.Dispose()).Verifiable();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Dispose();

        Assert.True(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterReleaseWithoutDispose_ReturnsFalse()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);

        guard.Release();

        Assert.False(guard.IsDisposed);
    }

    [Fact]
    public void IsDisposed_AfterReleaseAndDispose_ReturnsTrue()
    {
        var mockDisposable = CreateStrictMock<IDisposable>();
        var guard = new DisposableGuard<IDisposable>(mockDisposable.Object);
        guard.Release();

        guard.Dispose();

        Assert.True(guard.IsDisposed);
    }

    #endregion
}
