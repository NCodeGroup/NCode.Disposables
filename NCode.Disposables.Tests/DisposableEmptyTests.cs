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

namespace NCode.Disposables.Tests;

public class DisposableEmptyTests
{
    #region Singleton Tests

    [Fact]
    public void Singleton_ReturnsNonNullInstance()
    {
        var instance = DisposableEmpty.Singleton;

        Assert.NotNull(instance);
    }

    [Fact]
    public void Singleton_ReturnsSameInstance()
    {
        var first = DisposableEmpty.Singleton;
        var second = DisposableEmpty.Singleton;

        Assert.Same(first, second);
    }

    [Fact]
    public void Singleton_IsDisposableEmptyType()
    {
        var instance = DisposableEmpty.Singleton;

        Assert.IsType<DisposableEmpty>(instance);
    }

    #endregion

    #region Disposable.Empty Tests

    [Fact]
    public void DisposableEmpty_ReturnsNonNullInstance()
    {
        var instance = Disposable.Empty;

        Assert.NotNull(instance);
    }

    [Fact]
    public void DisposableEmpty_ReturnsSameInstanceAsSingleton()
    {
        var empty = Disposable.Empty;
        var singleton = DisposableEmpty.Singleton;

        Assert.Same(singleton, empty);
    }

    [Fact]
    public void DisposableEmpty_MultipleCalls_ReturnsSameInstance()
    {
        var first = Disposable.Empty;
        var second = Disposable.Empty;
        var third = Disposable.Empty;

        Assert.Same(first, second);
        Assert.Same(second, third);
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_CreatesNewInstance()
    {
        var instance = new DisposableEmpty();

        Assert.NotNull(instance);
    }

    [Fact]
    public void Constructor_CreatesDistinctInstances()
    {
        var first = new DisposableEmpty();
        var second = new DisposableEmpty();

        Assert.NotSame(first, second);
    }

    #endregion

    #region Dispose Tests

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var disposable = new DisposableEmpty();

        var exception = Record.Exception(() => disposable.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var disposable = new DisposableEmpty();

        var exception = Record.Exception(() =>
        {
            disposable.Dispose();
            disposable.Dispose();
            disposable.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_OnSingleton_DoesNotThrow()
    {
        var singleton = DisposableEmpty.Singleton;

        var exception = Record.Exception(() => singleton.Dispose());

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_OnSingleton_MultipleTimes_DoesNotThrow()
    {
        var singleton = DisposableEmpty.Singleton;

        var exception = Record.Exception(() =>
        {
            singleton.Dispose();
            singleton.Dispose();
            singleton.Dispose();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_CanBeUsedWithUsingStatement()
    {
        var exception = Record.Exception(() =>
        {
            using var disposable = new DisposableEmpty();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Dispose_SingletonCanBeUsedWithUsingStatement()
    {
        var exception = Record.Exception(() =>
        {
            using var disposable = DisposableEmpty.Singleton;
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task Dispose_ConcurrentCalls_DoesNotThrow()
    {
        var disposable = new DisposableEmpty();

        const int concurrentCalls = 100;
        var tasks = new Task[concurrentCalls];

        for (var i = 0; i < concurrentCalls; i++)
        {
            tasks[i] = Task.Run(() => disposable.Dispose());
        }

        var exception = await Record.ExceptionAsync(() => Task.WhenAll(tasks));

        Assert.Null(exception);
    }

    #endregion

    #region Interface Tests

    [Fact]
    public void ImplementsIDisposable()
    {
        var instance = new DisposableEmpty();

        Assert.IsAssignableFrom<IDisposable>(instance);
    }

    [Fact]
    public void Singleton_ImplementsIDisposable()
    {
        var singleton = DisposableEmpty.Singleton;

        Assert.IsAssignableFrom<IDisposable>(singleton);
    }

    #endregion
}
