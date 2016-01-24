using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (_disposed || !disposing) return;

			IEnumerable<IDisposable> list;
			lock (_lock)
			{
				if (_disposed) return;
				_disposed = true;

				list = _list.ToArray();
				_list.Clear();
			}

			// dispose in reverse order for any object dependencies
			foreach (var item in list.Reverse())
			{
				item.Dispose();
			}
		}

		public bool IsReadOnly => false;

		public int Count => _list.Count;

		public void Add(IDisposable item)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_list.Add(item);
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

				return _list.Remove(item);
			}
		}

		public void Clear()
		{
			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_list.Clear();
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