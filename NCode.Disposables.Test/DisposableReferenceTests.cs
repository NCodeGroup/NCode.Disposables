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
using Moq;
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableReferenceTests
	{
		[Test]
		public void Dispose_ReleaseIsCalled()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);

			var release = new Mock<IDisposable>(MockBehavior.Strict);
			release.Setup(_ => _.Dispose());

			var reference = new DisposableReference(parent.Object, release.Object);
			reference.Dispose();

			release.Verify(_ => _.Dispose(), Times.Once);
		}

		[Test]
		public void Dispose_ActionIsCalledOnlyOnce()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);

			var release = new Mock<IDisposable>(MockBehavior.Strict);
			release.Setup(_ => _.Dispose());

			var reference = new DisposableReference(parent.Object, release.Object);
			reference.Dispose();
			reference.Dispose();
			reference.Dispose();

			release.Verify(_ => _.Dispose(), Times.Once);
		}

		[Test]
		public void AddReference_CallParent()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);
			parent.Setup(_ => _.AddReference()).Returns(parent.Object);

			var release = new Mock<IDisposable>(MockBehavior.Strict);
			release.Setup(_ => _.Dispose());

			var reference = new DisposableReference(parent.Object, release.Object);
			var other = reference.AddReference();
			Assert.IsNotNull(other);

			release.Verify(_ => _.Dispose(), Times.Never);
			parent.Verify(_ => _.AddReference(), Times.Once);
		}

		[Test]
		public void AddReference_CallParent_EvenAfterDispose()
		{
			var parent = new Mock<IDisposableReference>(MockBehavior.Strict);
			parent.Setup(_ => _.AddReference()).Returns(parent.Object);

			var release = new Mock<IDisposable>(MockBehavior.Strict);
			release.Setup(_ => _.Dispose());

			var reference = new DisposableReference(parent.Object, release.Object);

			reference.Dispose();
			release.Verify(_ => _.Dispose(), Times.Once);

			var other = reference.AddReference();
			Assert.IsNotNull(other);

			parent.Verify(_ => _.AddReference(), Times.Once);
		}

	}
}