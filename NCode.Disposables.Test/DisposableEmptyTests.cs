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

using NUnit.Framework;

namespace NCode.Disposables.Test
{
	[TestFixture]
	public class DisposableEmptyTests
	{
		[Test]
		public void Singleton()
		{
			var first = Disposable.Empty;
			Assert.IsNotNull(first);

			var second = Disposable.Empty;
			Assert.AreSame(first, second);
		}

		[Test]
		public void Dispose()
		{
			var disposable = new DisposableEmpty();
			disposable.Dispose();
		}

		[Test]
		public void Dispose_MultipleTimes()
		{
			var disposable = new DisposableEmpty();
			disposable.Dispose();
			disposable.Dispose();
			disposable.Dispose();
		}

	}
}