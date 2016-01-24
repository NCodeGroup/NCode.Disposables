using System;
using Moq;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableReferenceCounterTests
	{
		[Test]
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

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void AddReference_Fail_Dispose_M()
		{
			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());

			var main = new DisposableReferenceCounter(disposable.Object);

			main.Dispose();

			main.AddReference();
		}

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void AddReference_Fail_Dispose_MO()
		{
			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());

			var main = new DisposableReferenceCounter(disposable.Object);
			var other = main.AddReference();

			main.Dispose();
			other.Dispose();

			main.AddReference();
		}

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void AddReference_Fail_Dispose_OM()
		{
			var disposable = new Mock<IDisposable>(MockBehavior.Strict);
			disposable.Setup(_ => _.Dispose());

			var main = new DisposableReferenceCounter(disposable.Object);
			var other = main.AddReference();

			other.Dispose();
			main.Dispose();

			main.AddReference();
		}

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		[Test]
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

		//

	}
}