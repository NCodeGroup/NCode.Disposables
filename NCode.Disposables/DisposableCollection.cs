using System;
using System.Collections;
using System.Collections.Generic;

namespace NCode.Disposables
{
	public interface IDisposableCollection : IDisposable, ICollection<IDisposable>
	{
		// nothing
	}

	public class DisposableCollection : IDisposableCollection
	{
		private bool _disposed;
		private readonly object _lock = new object();
		private readonly List<IDisposable> _list;

		public DisposableCollection()
		{
			_list = new List<IDisposable>();
		}

		public DisposableCollection(IEnumerable<IDisposable> collection)
		{
			_list = new List<IDisposable>(collection);
		}

		public void Dispose()
		{
			if (_disposed) return;

			IEnumerable<IDisposable> list;
			lock (_lock)
			{
				if (_disposed) return;
				_disposed = true;

				list = _list.ToArray();
				_list.Clear();
				Count = 0;
			}

			foreach (var item in list)
			{
				item.Dispose();
			}
		}

		public bool IsReadOnly => false;

		public int Count { get; private set; }


		public void Add(IDisposable item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_list.Add(item);
				++Count;
			}
		}

		public bool Remove(IDisposable item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				if (_list.Remove(item))
				{
					--Count;
					return true;
				}
			}

			return false;
		}

		public void Clear()
		{
			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_list.Clear();
				Count = 0;
			}
		}

		public bool Contains(IDisposable item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				return _list.Contains(item);
			}
		}

		public void CopyTo(IDisposable[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_list.CopyTo(array, arrayIndex);
			}
		}

		public IEnumerator<IDisposable> GetEnumerator()
		{
			IEnumerable<IDisposable> list;
			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				list = _list.ToArray();
			}
			return list.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

	}
}