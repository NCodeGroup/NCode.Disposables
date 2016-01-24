using System;
using System.Threading;
using Moq;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableContextTests
	{
		[Test]
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

		[Test]
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