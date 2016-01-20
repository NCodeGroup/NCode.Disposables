using System;
using System.Diagnostics;
using System.Threading;

namespace NCode.Disposables
{
	public interface IDisposableReference : IDisposable
	{
		IDisposableReference AddReference();
	}

	public sealed class DisposableReference : IDisposableReference
	{
		private readonly IDisposableReference _parent;
		private Action _action;

		public DisposableReference(IDisposableReference parent, Action action)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			_parent = parent;
			_action = action;
		}

		public void Dispose()
		{
			var action = Interlocked.Exchange(ref _action, null);
			action?.Invoke();
		}

		public IDisposableReference AddReference()
		{
			return _parent.AddReference();
		}
	}

	public sealed class DisposableReferenceCounter : IDisposableReference
	{
		private readonly object _lock = new object();
		private IDisposable _disposable;
		private bool _disposed;
		private int _count;

		public DisposableReferenceCounter(IDisposable disposable)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));

			_disposable = disposable;
		}

		public void Dispose()
		{
			IDisposable disposable = null;
			lock (_lock)
			{
				if (_disposable != null && !_disposed)
				{
					_disposed = true;

					if (_count == 0)
					{
						disposable = _disposable;
						_disposable = null;
					}
				}
			}

			disposable?.Dispose();
		}

		public IDisposableReference AddReference()
		{
			lock (_lock)
			{
				if (_disposable == null)
					throw new ObjectDisposedException(GetType().FullName);

				++_count;
				return new DisposableReference(this, Release);
			}
		}

		private void Release()
		{
			IDisposable disposable = null;
			lock (_lock)
			{
				if (_disposable != null)
				{
					var count = --_count;
					Debug.Assert(count >= 0);

					if (_disposed && count == 0)
					{
						disposable = _disposable;
						_disposable = null;
					}
				}
			}

			disposable?.Dispose();
		}

	}
}