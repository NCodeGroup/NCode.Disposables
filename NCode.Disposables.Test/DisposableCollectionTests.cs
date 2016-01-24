using System;
using Moq;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableCollectionTests
	{
		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Dispose_Add_Fail()
		{
			var collection = new DisposableCollection();
			collection.Dispose();
			collection.Add(Disposable.Empty);
		}

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Dispose_Remove_Fail()
		{
			var collection = new DisposableCollection();
			collection.Dispose();
			collection.Remove(Disposable.Empty);
		}

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Dispose_Clear_Fail()
		{
			var collection = new DisposableCollection();
			collection.Dispose();
			collection.Clear();
		}

		[Test]
		public void Dispose_Count_IsEmpty()
		{
			var collection = new DisposableCollection();
			collection.Dispose();
			var count = collection.Count;
			Assert.AreEqual(0, count);
		}

		[Test]
		public void Add_Dispose()
		{
			var collection = new DisposableCollection();

			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());
			collection.Add(disposable.Object);

			collection.Dispose();
			disposable.Verify(_ => _.Dispose(), Times.Once);
			Assert.AreEqual(0, collection.Count);
		}

		[Test]
		public void Add_Remove_Dispose()
		{
			var collection = new DisposableCollection();

			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());
			collection.Add(disposable.Object);

			var contains = collection.Remove(disposable.Object);
			Assert.IsTrue(contains);

			collection.Dispose();
			disposable.Verify(_ => _.Dispose(), Times.Never);
			Assert.AreEqual(0, collection.Count);
		}

		[Test]
		public void Add_Clear_Dispose()
		{
			var collection = new DisposableCollection();

			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());
			collection.Add(disposable.Object);
			collection.Clear();

			collection.Dispose();
			disposable.Verify(_ => _.Dispose(), Times.Never);
			Assert.AreEqual(0, collection.Count);
		}

		[Test]
		public void Add_Contains()
		{
			var collection = new DisposableCollection();

			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());
			collection.Add(disposable.Object);

			var contains = collection.Contains(disposable.Object);
			Assert.IsTrue(contains);
			Assert.AreEqual(1, collection.Count);
		}

		[Test]
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

			Assert.AreEqual(count, collection.Count);
			collection.Dispose();

			Assert.AreEqual(0, collection.Count);
			Assert.AreEqual("654321", order);
		}

		[Test]
		public void GetEnumerator_ThenAdd_IsSnapshot()
		{
			var collection = new DisposableCollection();

			collection.Add(Disposable.Empty);
			Assert.AreEqual(1, collection.Count);

			var enumerator = collection.GetEnumerator();

			collection.Add(Disposable.Empty);
			Assert.AreEqual(2, collection.Count);

			var count = 0;
			while (enumerator.MoveNext())
			{
				++count;
			}

			Assert.AreEqual(1, count);
		}

	}
}