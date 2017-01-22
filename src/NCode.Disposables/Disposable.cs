﻿#region Copyright Preamble
// 
//    Copyright © 2017 NCode Group
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
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<TResult>(Func<TResult> action)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action());
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T>(Action<T> action, T arg)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T, TResult>(Func<T, TResult> action, T arg)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, TResult>(Func<T1, T2, TResult> action, T1 arg1, T2 arg2)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> action, T1 arg1, T2 arg2, T3 arg3)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg6">The sixth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg6">The sixth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg6">The sixth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg7">The seventh parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, T6, T7>(Action<T1, T2, T3, T4, T5, T6, T7> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg6">The sixth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg7">The seventh parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Action"/> when <see cref="IDisposable.Dispose"/>
    /// is called.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg6">The sixth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg7">The seventh parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg8">The eighth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, T6, T7, T8>(Action<T1, T2, T3, T4, T5, T6, T7, T8> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
    }

    /// <summary>
    /// Creates and returns a new instance of <see cref="IDisposable"/> that
    /// will invoke an <see cref="Func{TResult}"/> when <see cref="IDisposable.Dispose"/>
    /// is called. The result of the <see cref="Func{TResult}"/> is disregarded.
    /// </summary>
    /// <param name="action">Specifies the <see cref="Action"/> to invoke when <see cref="IDisposable.Dispose"/> is called.</param>
    /// <param name="arg1">The first parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg2">The second parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg3">The third parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg4">The fourth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg5">The fifth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg6">The sixth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg7">The seventh parameter of the method that this dispose wrapper encapsulates.</param>
    /// <param name="arg8">The eighth parameter of the method that this dispose wrapper encapsulates.</param>
    /// <exception cref="ArgumentNullException"><paramref name="action"/> is <c>null</c>.</exception>
    public static IDisposable Create<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> action, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
    {
      if (action == null)
        throw new ArgumentNullException(nameof(action));

      return new DisposableAction(() => action(arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
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
