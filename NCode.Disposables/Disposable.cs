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
using System.Collections.Generic;
using System.Threading;

namespace NCode.Disposables
{
	/// <summary>
	/// Contains factory methods for various <see cref="IDisposable"/> implementations.
	/// </summary>
	public static class Disposable
	{
		/// <summary>
		/// Returns a singleton instance of <see cref="IDisposable"/> that performs
		/// nothing when <see cref="IDisposable.Dispose"/> is called.
		/// </summary>
		public static IDisposable Empty => DisposableEmpty.Instance;

		/// <summary>
		/// Creates and returns a new instance of <see cref="IDisposable"/> that
		/// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
		/// is called.
		/// </summary>
		/// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
		/// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
		public static IDisposable Create(Action action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			return new DisposableAction(action);
		}

		/// <summary>
		/// Creates and returns a new instance of <see cref="IDisposableAggregate"/>
		/// that contains (i.e. aggregates) a property to another <see cref="IDisposable"/>
		/// instance.
		/// </summary>
		/// <param name="disposable">The underlying <see cref="IDisposable"/> instance that the <see cref="IDisposableAggregate"/> will contain (i.e. aggregate).</param>
		public static IDisposableAggregate Aggregate(IDisposable disposable)
		{
			return new DisposableAggregate(disposable);
		}

		/// <summary>
		/// Creates and returns a new instance of an <see cref="IDisposable"/>
		/// instance which will invoke the <see cref="IDisposable.Dispose"/>
		/// method of an underlying resource using an asynchronous or synchronous
		/// operation from a <see cref="SynchronizationContext"/>.
		/// </summary>
		/// <param name="disposable">The underlying <see cref="IDisposable"/> instance to synchronize.</param>
		/// <param name="context">The <see cref="SynchronizationContext"/> to invoke the operation.</param>
		/// <param name="async"><c>true</c> to asynchronously invoke the operation; otherwise, <c>false</c> to synchronously invoke the operation.</param>
		/// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <c>null</c>.</exception>
		/// <exception cref="ArgumentNullException"><paramref name="context"/> is <c>null</c>.</exception>
		public static IDisposable Context(IDisposable disposable, SynchronizationContext context, bool async = false)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			return new DisposableContext(disposable, context, async);
		}

		/// <summary>
		/// Creates and returns a new instance of <see cref="IDisposableReference"/>
		/// which contains an <see cref="IDisposable"/> resource that uses reference
		/// counting and only disposes the underlying resource when all the
		/// references have been released (i.e. reference count is zero). 
		/// </summary>
		/// <param name="disposable">The underlying <see cref="IDisposable"/> instance that this <see cref="IDisposableReference"/> will contain.</param>
		/// <exception cref="ArgumentNullException"><paramref name="disposable"/> is <c>null</c>.</exception>
		public static IDisposableReference Counter(IDisposable disposable)
		{
			if (disposable == null)
				throw new ArgumentNullException(nameof(disposable));

			return new DisposableReferenceCounter(disposable);
		}

		/// <summary>
		/// Creates and returns a new instance of <see cref="IDisposableCollection"/>
		/// which contains other <see cref="IDisposable"/> items that will be
		/// disposed when the collection itself is disposed. The collection is
		/// initialized from a variable argument array of <see cref="IDisposable"/>
		/// items.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new <see cref="IDisposableCollection"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
		public static IDisposableCollection Collection(params IDisposable[] collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			return new DisposableCollection(collection);
		}

		/// <summary>
		/// Creates and returns a new instance of <see cref="IDisposableCollection"/>
		/// which contains other <see cref="IDisposable"/> items that will be
		/// disposed when the collection itself is disposed. The collection is
		/// initialized from an enumeration of <see cref="IDisposable"/> items.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new <see cref="IDisposableCollection"/>.</param>
		/// <exception cref="ArgumentNullException"><paramref name="collection"/> is <c>null</c>.</exception>
		public static IDisposableCollection Collection(IEnumerable<IDisposable> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			return new DisposableCollection(collection);
		}

	}
}