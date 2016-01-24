using System;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableActionTests
	{
		[Test]
		public void Dispose_ActionIsCalled()
		{
			var count = 0;
			Action action = () => ++count;
			var disposable = new DisposableAction(action);

			disposable.Dispose();
			Assert.AreEqual(1, count);
		}

		[Test]
		public void Dispose_ActionIsCalledOnlyOnce()
		{
			var count = 0;
			Action action = () => ++count;
			var disposable = new DisposableAction(action);

			disposable.Dispose();
			disposable.Dispose();
			disposable.Dispose();

			Assert.AreEqual(1, count);
		}

	}
}