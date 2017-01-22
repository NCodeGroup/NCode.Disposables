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
  public class DisposableCollectionTests
  {
    [Fact]
    public void Dispose_Add_Fail()
    {
      var collection = new DisposableCollection();
      collection.Dispose();

      Assert.Throws<ObjectDisposedException>(() => collection.Add(Disposable.Empty));
    }

    [Fact]
    public void Dispose_Remove_Fail()
    {
      var collection = new DisposableCollection();
      collection.Dispose();

      Assert.Throws<ObjectDisposedException>(() => collection.Remove(Disposable.Empty));
    }

    [Fact]
    public void Dispose_Clear_Fail()
    {
      var collection = new DisposableCollection();
      collection.Dispose();

      Assert.Throws<ObjectDisposedException>(() => collection.Clear());
    }

    [Fact]
    public void Dispose_Count_IsEmpty()
    {
      var collection = new DisposableCollection();
      collection.Dispose();

      var count = collection.Count;
      Assert.Equal(0, count);
    }

    [Fact]
    public void Add_Dispose()
    {
      var collection = new DisposableCollection();

      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());
      collection.Add(disposable.Object);

      collection.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Once);
      Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Add_Remove_Dispose()
    {
      var collection = new DisposableCollection();

      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());
      collection.Add(disposable.Object);

      var contains = collection.Remove(disposable.Object);
      Assert.True(contains);

      collection.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);
      Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Add_Clear_Dispose()
    {
      var collection = new DisposableCollection();

      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());
      collection.Add(disposable.Object);
      collection.Clear();

      collection.Dispose();
      disposable.Verify(_ => _.Dispose(), Times.Never);
      Assert.Equal(0, collection.Count);
    }

    [Fact]
    public void Add_Contains()
    {
      var collection = new DisposableCollection();

      var disposable = new Mock<IDisposable>(MockBehavior.Strict);
      disposable.Setup(_ => _.Dispose());
      collection.Add(disposable.Object);

      var contains = collection.Contains(disposable.Object);
      Assert.True(contains);
      Assert.Equal(1, collection.Count);
    }

    [Fact]
    public void Dispose_Reverse()
    {
      var order = String.Empty;
      var collection = new DisposableCollection();

      const int count = 6;
      for (var i = 1; i <= count; ++i)
      {
        var local = i;
        var disposable = new Mock<IDisposable>(MockBehavior.Strict);
        disposable.Setup(_ => _.Dispose()).Callback(() => order += local);
        collection.Add(disposable.Object);
      }

      Assert.Equal(count, collection.Count);
      collection.Dispose();

      Assert.Equal(0, collection.Count);
      Assert.Equal("654321", order);
    }

    [Fact]
    public void GetEnumerator_ThenAdd_IsSnapshot()
    {
      var collection = new DisposableCollection();

      collection.Add(Disposable.Empty);
      Assert.Equal(1, collection.Count);

      var enumerator = collection.GetEnumerator();

      collection.Add(Disposable.Empty);
      Assert.Equal(2, collection.Count);

      var count = 0;
      while (enumerator.MoveNext())
      {
        ++count;
      }

      Assert.Equal(1, count);
    }

  }
}
