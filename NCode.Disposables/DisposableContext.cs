using System;
using System.Threading;

namespace NCode.Disposables
{
	public sealed class DisposableContext : IDisposable
	{
		private IDisposable _disposable;
		private readonly SynchronizationContext _context;
		private readonly bool _async;

		public DisposableContext(IDisposable disposable, SynchronizationContext context, bool async = false)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			_disposable = disposable;
			_context = context;
			_async = async;
		}

		public void Dispose()
		{
			var disposable = Interlocked.Exchange(ref _disposable, null);
			if (disposable == null) return;

			if (_async)
			{
				_context.OperationStarted();
				_context.Post(AsynchronousCallback, disposable);
			}
			else
			{
				_context.Send(SynchronousCallback, disposable);
			}
		}

		private static void SynchronousCallback(object state)
		{
			var disposable = (IDisposable)state;
			disposable.Dispose();
		}

		private void AsynchronousCallback(object state)
		{
			try
			{
				SynchronousCallback(state);
			}
			finally
			{
				_context.OperationCompleted();
			}
		}

	}
}