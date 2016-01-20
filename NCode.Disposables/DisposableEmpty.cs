using System;

namespace NCode.Disposables
{
	public sealed class DisposableEmpty : IDisposable
	{
		public static readonly DisposableEmpty Instance = new DisposableEmpty();

		public void Dispose()
		{
			// nothing
		}

	}
}