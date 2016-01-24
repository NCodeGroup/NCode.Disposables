using System;
using Moq;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableReferenceTests
	{
		[Test]
		public void Dispose_ActionIsCalled()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);

			var count = 0;
			Action action = () => ++count;

			var reference = new DisposableReference(parent.Object, action);
			reference.Dispose();

			Assert.AreEqual(1, count);
		}

		[Test]
		public void Dispose_ActionIsCalledOnlyOnce()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);

			var count = 0;
			Action action = () => ++count;

			var reference = new DisposableReference(parent.Object, action);
			reference.Dispose();
			reference.Dispose();
			reference.Dispose();

			Assert.AreEqual(1, count);
		}

		[Test]
		public void AddReference_CallParent()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);
			parent.Setup(_ => _.AddReference()).Returns(parent.Object);
			
			var count = 0;
			Action action = () => ++count;

			var reference = new DisposableReference(parent.Object, action);
			var other = reference.AddReference();

			Assert.IsNotNull(other);
			parent.Verify(_ => _.AddReference(), Times.Once);
		}

		[Test]
		public void AddReference_CallParent_EvenAfterDispose()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);
			parent.Setup(_ => _.AddReference()).Returns(parent.Object);

			var count = 0;
			Action action = () => ++count;

			var reference = new DisposableReference(parent.Object, action);

			reference.Dispose();
			Assert.AreEqual(1, count);

			var other = reference.AddReference();
			Assert.IsNotNull(other);
			parent.Verify(_ => _.AddReference(), Times.Once);
		}

	}
}