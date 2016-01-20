using System;
using System.Threading;

namespace NCode.Disposables
{
	public sealed class DisposableAction : IDisposable
	{
		private Action _action;

		public DisposableAction(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_action = action;
		}

		public void Dispose()
		{
			var action = Interlocked.Exchange(ref _action, null);
			action?.Invoke();
		}

	}
}