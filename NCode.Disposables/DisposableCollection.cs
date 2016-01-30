#region Copyright Preamble
// 
//    Copyright © 2015 NCode Group
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// 
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NCode.Disposables
{
	/// <summary>
	/// Represents an <see cref="IDisposable"/> collection that contains other
	/// <see cref="IDisposable"/> items that will be disposed when the collection
	/// itself is disposed. The items in the collection are disposed in reverse
	/// order that they were added.
	/// </summary>
	public interface IDisposableCollection : IDisposable, ICollection<IDisposable>
	{
		// nothing
	}

	/// <summary>
	/// Provides the implementation for <see cref="IDisposableCollection"/>.
	/// </summary>
	public class DisposableCollection : IDisposableCollection
	{
		private bool _disposed;
		private readonly object _lock = new object();
		private readonly List<IDisposable> _list;

		/// <summary>
		/// Initializes a new instance of <see cref="IDisposableCollection"/> with an empty collection.
		/// </summary>
		public DisposableCollection()
		{
			_list = new List<IDisposable>();
		}

		/// <summary>
		/// Initializes a new instance of <see cref="IDisposableCollection"/> that contains elements copied from the specified collection.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new <see cref="IDisposableCollection"/>.</param>
		public DisposableCollection(IEnumerable<IDisposable> collection)
		{
			_list = new List<IDisposable>(collection);
		}

		/// <summary>
		/// Removes and disposes all the items contained in this collection.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Removes and disposes all the items contained in this collection.
		/// </summary>
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

		/// <summary>
		/// Always returns <c>false</c>.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Gets the number of items contained in the collection.
		/// </summary>
		public int Count => _list.Count;

		/// <summary>
		/// Adds an item to the collection that will be disposed when the collection itself is disposed.
		/// </summary>
		/// <param name="item">The item to add to the collection.</param>
		/// <exception cref="ObjectDisposedException">The <see cref="IDisposableCollection"/> is disposed.</exception>
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

		/// <summary>
		/// Removes the first occurrence of the specific item from the collection but does not dispose it.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="item"/> was successfully removed from the collection; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="item">The item to remove from the collection.</param>
		/// <exception cref="ObjectDisposedException">The <see cref="IDisposableCollection"/> is disposed.</exception>
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

		/// <summary>
		/// Removes all the items from the collection but does not dispose them.
		/// </summary>
		/// <exception cref="ObjectDisposedException">The <see cref="IDisposableCollection"/> is disposed.</exception>
		public void Clear()
		{
			lock (_lock)
			{
				if (_disposed)
					throw new ObjectDisposedException(GetType().FullName);

				_list.Clear();
			}
		}

		/// <summary>
		/// Determines whether the collection contains a specific item.
		/// </summary>
		/// <returns>
		/// <c>true</c> if <paramref name="item"/> is found in the collection; otherwise, <c>false</c>.
		/// </returns>
		/// <param name="item">The item to locate in the collection.</param>
		/// <exception cref="ObjectDisposedException">The <see cref="IDisposableCollection"/> is disposed.</exception>
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

		/// <summary>
		/// Copies the elements of the collection to an <see cref="Array"/>, starting at a particular <see cref="Array"/> index.
		/// </summary>
		/// <param name="array">The one-dimensional <see cref="Array"/> that is the destination of the elements copied from collection. The <see cref="Array"/> must have zero-based indexing.</param>
		/// <param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param>
		/// <exception cref="ArgumentNullException"><paramref name="array"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="ArgumentException">The number of elements in the source collection is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
		/// <exception cref="ObjectDisposedException">The <see cref="IDisposableCollection"/> is disposed.</exception>
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

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// An <see cref="IEnumerator{IDisposable}"/> that can be used to iterate through the collection.
		/// </returns>
		/// <exception cref="ObjectDisposedException">The <see cref="IDisposableCollection"/> is disposed.</exception>
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