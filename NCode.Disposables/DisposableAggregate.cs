using System;
using System.Threading;

namespace NCode.Disposables
{
	public interface IDisposableAggregate : IDisposable
	{
		IDisposable Disposable { get; set; }
	}

	public sealed class DisposableAggregate : IDisposableAggregate
	{
		private IDisposableAggregate _helper;

		public DisposableAggregate()
		{
			_helper = new DisposableAggregateBackingStore();
		}

		public void Dispose()
		{
			var helper = Interlocked.Exchange(ref _helper, DisposableAggregateDisposed.Instance);
			helper?.Dispose();
		}

		public IDisposable Disposable
		{
			get { return Volatile.Read(ref _helper).Disposable; }
			set { Volatile.Read(ref _helper).Disposable = value; }
		}
	}

	public sealed class DisposableAggregateDisposed : IDisposableAggregate
	{
		public static readonly IDisposableAggregate Instance = new DisposableAggregateDisposed();

		private DisposableAggregateDisposed()
		{
			// nothing
		}

		public void Dispose()
		{
			// nothing
		}

		public IDisposable Disposable
		{
			get { throw new ObjectDisposedException(typeof(DisposableAggregate).FullName); }
			set { throw new ObjectDisposedException(typeof(DisposableAggregate).FullName); }
		}
	}

	public sealed class DisposableAggregateBackingStore : IDisposableAggregate
	{
		private IDisposable _disposable;

		public void Dispose()
		{
			var disposable = Interlocked.Exchange(ref _disposable, null);
			disposable?.Dispose();
		}

		public IDisposable Disposable
		{
			get { return Volatile.Read(ref _disposable); }
			set { Interlocked.Exchange(ref _disposable, value); }
		}
	}

}