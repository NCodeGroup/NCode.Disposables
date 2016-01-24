using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableEmptyTests
	{
		[Test]
		public void Singleton()
		{
			var first = Disposable.Empty;
			Assert.IsNotNull(first);

			var second = Disposable.Empty;
			Assert.AreSame(first, second);
		}

		[Test]
		public void Dispose()
		{
			var disposable = new DisposableEmpty();
			disposable.Dispose();
		}

		[Test]
		public void Dispose_MultipleTimes()
		{
			var disposable = new DisposableEmpty();
			disposable.Dispose();
			disposable.Dispose();
			disposable.Dispose();
		}

	}
}