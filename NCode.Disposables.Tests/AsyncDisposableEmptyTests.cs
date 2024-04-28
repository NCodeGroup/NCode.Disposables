#region Copyright Preamble

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

namespace NCode.Disposables.Tests;

public class AsyncDisposableEmptyTests
{
    [Fact]
    public void Singleton()
    {
        var first = AsyncDisposable.Empty;
        Assert.NotNull(first);

        var second = AsyncDisposable.Empty;
        Assert.Same(first, second);
    }

    [Fact]
    public async Task Dispose()
    {
        var disposable = new AsyncDisposableEmpty();
        await disposable.DisposeAsync();
    }

    [Fact]
    public async Task Dispose_MultipleTimes()
    {
        var disposable = new AsyncDisposableEmpty();
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
    }
}