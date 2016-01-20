using System;
using System.Threading;

namespace NCode.Disposables
{
	public static class Disposable
	{
		public static IDisposable Empty
		{
			get { return DisposableEmpty.Instance; }
		}

		public static IDisposable Create(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			return new DisposableAction(action);
		}

		public static IDisposable Synchronize(IDisposable disposable, SynchronizationContext context, bool async)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			return new DisposableSynchronize(disposable, context, async);
		}

		public static IDisposableReference CreateReferenceCounter(IDisposable disposable)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));

			return new DisposableReferenceCounter(disposable);
		}

	}
}