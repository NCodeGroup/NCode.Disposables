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
using Moq;
using Xunit;

namespace NCode.Disposables.Test
{
  public class DisposableReferenceCounterTests
  {
    [Fact]
    public void Dispose_Once()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var counter = new DisposableReferenceCounter(disposable.Object);
      counter.Dispose();
      counter.Dispose();
      counter.Dispose();

      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void AddReference_Fail_Dispose_M()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);

      main.Dispose();

      Assert.Throws<ObjectDisposedException>(() => main.AddReference());
    }

    [Fact]
    public void AddReference_Fail_Dispose_MO()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();

      main.Dispose();
      other.Dispose();

      Assert.Throws<ObjectDisposedException>(() => main.AddReference());
    }

    [Fact]
    public void AddReference_Fail_Dispose_OM()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();

      other.Dispose();
      main.Dispose();

      Assert.Throws<ObjectDisposedException>(() => main.AddReference());
    }

    [Fact]
    public void AddReference_DisposeMain_AddRefFromMain()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      var again = main.AddReference();
      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      again.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void AddReference_DisposeOther_AddRefFromOther()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      var nested = other.AddReference();
      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    //

    [Fact]
    public void AddReference_Dispose_Other_MO()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void AddReference_Dispose_Other_OM()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    //

    [Fact]
    public void AddReference_Dispose_Nested_MON()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();
      var nested = other.AddReference();

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void AddReference_Dispose_Nested_MNO()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();
      var nested = other.AddReference();

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    //

    [Fact]
    public void AddReference_Dispose_Nested_ONM()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();
      var nested = other.AddReference();

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void AddReference_Dispose_Nested_OMN()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();
      var nested = other.AddReference();

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    //

    [Fact]
    public void AddReference_Dispose_Nested_NMO()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();
      var nested = other.AddReference();

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

    [Fact]
    public void AddReference_Dispose_Nested_NOM()
    {
      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());

      var main = new DisposableReferenceCounter(disposable.Object);
      var other = main.AddReference();
      var nested = other.AddReference();

      nested.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      other.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);

      main.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
    }

  }
}
