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

namespace NCode.Disposables
{
	/// <summary>
	/// Provides an implementation of <see cref="IDisposable"/> that is empty and performs nothing (i.e. nop) when <see cref="Dispose"/> is called.
	/// </summary>
	public sealed class DisposableEmpty : IDisposable
	{
		/// <summary>
		/// Contains a singleton instance of <see cref="IDisposable"/> that performs nothing when <see cref="Dispose"/> is called.
		/// </summary>
		public static readonly DisposableEmpty Instance = new DisposableEmpty();

		/// <summary>
		/// This specific implementation of <see cref="IDisposable"/> is empty and performs nothing.
		/// </summary>
		public void Dispose()
		{
			// nothing
		}

	}
}