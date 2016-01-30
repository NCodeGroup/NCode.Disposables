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
using System.Threading;

namespace NCode.Disposables
{
	/// <summary>
	/// Represents an <see cref="IDisposable"/> resource that uses reference
	/// counting and only disposes the underlying resource when all the
	/// references have been released (i.e. reference count is zero).
	/// </summary>
	/// <remarks>
	/// The very first instance of <see cref="IDisposableReference"/> will be
	/// initialized with a count of one (1) and additional references will
	/// increment that count until they are disposed.
	/// </remarks>
	public interface IDisposableReference : IDisposable
	{
		/// <summary>
		/// Increments the reference count and returns a disposable resource that
		/// can be used to decrement the newly incremented reference count.
		/// </summary>
		/// <exception cref="ObjectDisposedException">The reference count has reached zero (0) and the underlying resource has been disposed already.</exception>
		IDisposableReference AddReference();
	}

	/// <summary>
	/// Provides the implementation for <see cref="IDisposableReference"/> that
	/// represents additional references to the original <see cref="IDisposableReference"/>
	/// instance. This <see cref="IDisposable"/> implementation simply decrements
	/// the reference count which may dispose the underlying resource when the
	/// reference count reaches zero (0).
	/// </summary>
	public sealed class DisposableReference : IDisposableReference
	{
		private readonly IDisposableReference _parent;
		private IDisposable _release;

		/// <summary>
		/// Initializes a new instance of <see cref="IDisposableReference"/> that represents an increment of the reference count.
		/// </summary>
		/// <param name="parent">The original <see cref="IDisposableReference"/> instance.</param>
		/// <param name="release">The <see cref="IDisposable"/> instance that will decrement (i.e. release) the reference count.</param>
		/// <exception cref="ArgumentNullException"><paramref name="parent"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="release"/> is <c>null</c>.</exception>
		public DisposableReference(IDisposableReference parent, IDisposable release)
		{
			if (parent == null)
				throw new ArgumentNullException(nameof(parent));
			if (release == null)
				throw new ArgumentNullException(nameof(release));

			_parent = parent;
			_release = release;
		}

		/// <summary>
		/// Decrements the reference count of the original <see cref="IDisposableReference"/>
		/// instance. The underlying instance will be disposed if the reference
		/// count reaches zero (0). This method is idempotent safe and calling it
		/// multiple times has the same effect as calling it only once.
		/// </summary>
		public void Dispose()
		{
			var release = Interlocked.Exchange(ref _release, null);
			release?.Dispose();
		}

		/// <summary>
		/// Increments the reference count and returns a disposable resource that
		/// can be used to decrement the newly incremented reference count.
		/// </summary>
		/// <exception cref="ObjectDisposedException">The reference count has reached zero (0) and the underlying resource has been disposed already.</exception>
		public IDisposableReference AddReference()
		{
			return _parent.AddReference();
		}
	}

	/// <summary>
	/// Provides the implementation for <see cref="IDisposableReference"/> that represents
	/// original <see cref="IDisposable"/> resource with a reference counter.
	/// </summary>
	public sealed class DisposableReferenceCounter : IDisposableReference
	{
		private IDisposable _disposable;
		private IDisposable _release;
		private int _count;

		/// <summary>
		/// Initializes a new instance of <see cref="IDisposableReference"/> that
		/// contains the original <see cref="IDisposable"/> resource.
		/// </summary>
		/// <param name="disposable">The underlying <see cref="IDisposable"/> instance that this <see cref="IDisposableReference"/> will contain.</param>
		/// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <c>null</c>.</exception>
		public DisposableReferenceCounter(IDisposable disposable)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));

			_count = 1;
			_disposable = disposable;
			_release = Disposable.Create(Release);
		}

		/// <summary>
		/// Decrements the reference count of the original <see cref="IDisposableReference"/>
		/// instance. The underlying instance will be disposed if the reference
		/// count reaches zero (0). This method is idempotent safe and calling it
		/// multiple times has the same effect as calling it only once.
		/// </summary>
		public void Dispose()
		{
			var release = Interlocked.Exchange(ref _release, null);
			release?.Dispose();
		}

		/// <summary>
		/// Increments the reference count and returns a disposable resource that
		/// can be used to decrement the newly incremented reference count.
		/// </summary>
		/// <exception cref="ObjectDisposedException">The reference count has reached zero (0) and the underlying resource has been disposed already.</exception>
		public IDisposableReference AddReference()
		{
			var count = Interlocked.Increment(ref _count);
			if (count <= 1)
				throw new ObjectDisposedException(GetType().FullName);

			var release = Disposable.Create(Release);
			return new DisposableReference(this, release);
		}

		private void Release()
		{
			var count = Interlocked.Decrement(ref _count);
			if (count > 0) return;

			var disposable = Interlocked.Exchange(ref _disposable, null);
			disposable?.Dispose();
		}

	}
}