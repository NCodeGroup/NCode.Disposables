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

using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace NCode.Disposables.Test
{
  public class DisposableAggregateTests
  {
    [Fact]
    public void Dispose_Nothing_Once()
    {
      var aggregate = new DisposableAggregate();
      aggregate.Dispose();
    }

    [Fact]
    public void Dispose_Nothing_Multiple()
    {
      var aggregate = new DisposableAggregate();
      aggregate.Dispose();
      aggregate.Dispose();
      aggregate.Dispose();
    }

    [Fact]
    public async Task Set_Get()
    {
      var aggregate = new DisposableAggregate();

      var action = new Mock<IDisposable>(MockBehavior.Strict);
      action.Setup(_ => _.Dispose());
      aggregate.Disposable = action.Object;

      Interlocked.MemoryBarrier();
      await Task.Yield();

      var get = aggregate.Disposable;
      Assert.Same(action.Object, get);
      action.Verify(_ => _.Dispose(), Times.Never);
    }

    [Fact]
    public async Task Set_SetNull_GetNull()
    {
      var aggregate = new DisposableAggregate();

      var action = new Mock<IDisposable>(MockBehavior.Strict);
      action.Setup(_ => _.Dispose());
      aggregate.Disposable = action.Object;

      Interlocked.MemoryBarrier();
      await Task.Yield(/* do something, anything, etc... */);

      aggregate.Disposable = null;

      Interlocked.MemoryBarrier();
      await Task.Yield(/* do something, anything, etc... */);

      var get = aggregate.Disposable;
      Assert.Null(get);
      action.Verify(_ => _.Dispose(), Times.Never);
    }

    [Fact]
    public void Set_Dispose()
    {
      var aggregate = new DisposableAggregate();

      var action = new Mock<IDisposable>(MockBehavior.Strict);
      action.Setup(_ => _.Dispose());
      aggregate.Disposable = action.Object;

      aggregate.Dispose();
      action.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void Set_Dispose_Multiple()
    {
      var aggregate = new DisposableAggregate();

      var action = new Mock<IDisposable>(MockBehavior.Strict);
      action.Setup(_ => _.Dispose());
      aggregate.Disposable = action.Object;

      aggregate.Dispose();
      aggregate.Dispose();
      aggregate.Dispose();
      action.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void Set_Dispose_Get()
    {
      var aggregate = new DisposableAggregate();

      var action = new Mock<IDisposable>(MockBehavior.Strict);
      action.Setup(_ => _.Dispose());
      aggregate.Disposable = action.Object;

      aggregate.Dispose();
      action.Verify(_ => _.Dispose(), Times.Once);

      Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable);
    }

    [Fact]
    public void Set_Dispose_Set()
    {
      var aggregate = new DisposableAggregate();

      var action = new Mock<IDisposable>(MockBehavior.Strict);
      action.Setup(_ => _.Dispose());
      aggregate.Disposable = action.Object;

      aggregate.Dispose();
      action.Verify(_ => _.Dispose(), Times.Once);

      Assert.Throws<ObjectDisposedException>(() => aggregate.Disposable = new DisposableEmpty());
    }

  }
}
