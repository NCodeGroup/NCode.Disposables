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
using Moq;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableAggregateTests
	{
		[Test]
		public void Dispose_Nothing_Once()
		{
			var aggregate = new DisposableAggregate();
			aggregate.Dispose();
		}

		[Test]
		public void Dispose_Nothing_Multiple()
		{
			var aggregate = new DisposableAggregate();
			aggregate.Dispose();
			aggregate.Dispose();
			aggregate.Dispose();
		}

		[Test]
		public void Set_Get()
		{
			var aggregate = new DisposableAggregate();

			var action = new Mock<IDisposable>(MockBehavior.Strict);
			action.Setup(_ => _.Dispose());
			aggregate.Disposable = action.Object;

			Thread.MemoryBarrier();
			Thread.Yield(/* do something, anything, etc... */);

			var get = aggregate.Disposable;
			Assert.AreSame(action.Object, get);
			action.Verify(_ => _.Dispose(), Times.Never);
		}

		[Test]
		public void Set_SetNull_GetNull()
		{
			var aggregate = new DisposableAggregate();

			var action = new Mock<IDisposable>(MockBehavior.Strict);
			action.Setup(_ => _.Dispose());
			aggregate.Disposable = action.Object;

			Thread.MemoryBarrier();
			Thread.Yield(/* do something, anything, etc... */);

			aggregate.Disposable = null;

			Thread.MemoryBarrier();
			Thread.Yield(/* do something, anything, etc... */);

			var get = aggregate.Disposable;
			Assert.IsNull(get);
			action.Verify(_ => _.Dispose(), Times.Never);
		}

		[Test]
		public void Set_Dispose()
		{
			var aggregate = new DisposableAggregate();

			var action = new Mock<IDisposable>(MockBehavior.Strict);
			action.Setup(_ => _.Dispose());
			aggregate.Disposable = action.Object;

			aggregate.Dispose();
			action.Verify(_ => _.Dispose(), Times.Once);
		}

		[Test]
		public void Set_Dispose_Multiple()
		{
			var aggregate = new DisposableAggregate();

			var action = new Mock<IDisposable>(MockBehavior.Strict);
			action.Setup(_ => _.Dispose());
			aggregate.Disposable = action.Object;

			aggregate.Dispose();
			aggregate.Dispose();
			aggregate.Dispose();
			action.Verify(_ => _.Dispose(), Times.Once);
		}

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Set_Dispose_Get()
		{
			var aggregate = new DisposableAggregate();

			var action = new Mock<IDisposable>(MockBehavior.Strict);
			action.Setup(_ => _.Dispose());
			aggregate.Disposable = action.Object;

			aggregate.Dispose();
			action.Verify(_ => _.Dispose(), Times.Once);

			var get = aggregate.Disposable;
			Assert.Fail("Should not get here {0}", get);
		}

		[Test]
		[ExpectedException(typeof(ObjectDisposedException))]
		public void Set_Dispose_Set()
		{
			var aggregate = new DisposableAggregate();

			var action = new Mock<IDisposable>(MockBehavior.Strict);
			action.Setup(_ => _.Dispose());
			aggregate.Disposable = action.Object;

			aggregate.Dispose();
			action.Verify(_ => _.Dispose(), Times.Once);

			aggregate.Disposable = new DisposableEmpty();
			Assert.Fail("Should not get here");
		}

	}
}