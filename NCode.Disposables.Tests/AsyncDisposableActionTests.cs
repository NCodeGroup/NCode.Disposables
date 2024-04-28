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

public class AsyncDisposableActionTests
{
    [Fact]
    public async Task DisposeAsync_ActionIsCalled()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action);

        await disposable.DisposeAsync();
        Assert.Equal(1, count);
    }

    [Fact]
    public async ValueTask DisposeAsync_ActionIsCalledOnlyOnce()
    {
        var count = 0;
        var action = () =>
        {
            ++count;
            return ValueTask.CompletedTask;
        };
        var disposable = new AsyncDisposableAction(action);

        await disposable.DisposeAsync();
        await disposable.DisposeAsync();
        await disposable.DisposeAsync();

        Assert.Equal(1, count);
    }
}