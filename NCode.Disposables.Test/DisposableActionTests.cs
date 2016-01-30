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
using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableActionTests
	{
		[Test]
		public void Dispose_ActionIsCalled()
		{
			var count = 0;
			Action action = () => ++count;
			var disposable = new DisposableAction(action);

			disposable.Dispose();
			Assert.AreEqual(1, count);
		}

		[Test]
		public void Dispose_ActionIsCalledOnlyOnce()
		{
			var count = 0;
			Action action = () => ++count;
			var disposable = new DisposableAction(action);

			disposable.Dispose();
			disposable.Dispose();
			disposable.Dispose();

			Assert.AreEqual(1, count);
		}

	}
}