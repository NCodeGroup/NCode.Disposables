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
using Moq;
using Xunit;

namespace NCode.Disposables.Test
{
  public class DisposableContextTests
  {
    [Fact]
    public void Dispose_Sync()
    {
      var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
      context.Setup(_ => _.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>())).Callback((SendOrPostCallback c, object s) => c(s));

      var item = new Mock<IDisposable>(MockBehavior.Strict);
      item.Setup(_ => _.Dispose());

      var disposable = new DisposableContext(item.Object, context.Object);
      disposable.Dispose();

      context.Verify(_ => _.Send(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
      item.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void Dispose_Async()
    {
      var context = new Mock<SynchronizationContext>(MockBehavior.Strict);
      context.Setup(_ => _.OperationStarted());
      context.Setup(_ => _.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>())).Callback((SendOrPostCallback c, object s) => c(s));
      context.Setup(_ => _.OperationCompleted());

      var item = new Mock<IDisposable>(MockBehavior.Strict);
      item.Setup(_ => _.Dispose());

      var disposable = new DisposableContext(item.Object, context.Object, true);
      disposable.Dispose();

      context.Verify(_ => _.OperationStarted(), Times.Once);
      context.Verify(_ => _.Post(It.IsAny<SendOrPostCallback>(), It.IsAny<object>()), Times.Once);
      context.Verify(_ => _.OperationCompleted(), Times.Once);
      item.Verify(_ => _.Dispose(), Times.Once);
    }

  }
}
