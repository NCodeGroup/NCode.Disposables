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

		public static IDisposable Context(IDisposable disposable, SynchronizationContext context, bool async = false)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			return new DisposableContext(disposable, context, async);
		}

		public static IDisposableReference Counter(IDisposable disposable)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));

			return new DisposableReferenceCounter(disposable);
		}

	}
}