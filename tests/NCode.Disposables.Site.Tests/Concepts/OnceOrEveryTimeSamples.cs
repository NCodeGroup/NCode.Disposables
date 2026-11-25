// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Concepts;

// The samples of docs/site/concepts/once-or-every-time.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class OnceOrEveryTimeSamples
{
    #region Classes Run Their Cleanup Once Samples

    [Fact]
    public async Task SiteOnceOrEveryTimeThreadsSample_Runs()
    {
        // begin-snippet: SiteOnceOrEveryTimeThreads
        var calls = 0;
        var running = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously
        );
        using var finish = new ManualResetEventSlim();
        var cleanup = Disposable.Create(() =>
        {
            Interlocked.Increment(ref calls);
            running.SetResult(); // throws if the action ever runs twice
            finish.Wait();
        });

        var first = Task.Run(cleanup.Dispose);
        await running.Task; // one thread is inside the action

        // seven more threads dispose it while the action runs: they find it taken
        var others = Enumerable.Range(0, 7).Select(_ => Task.Run(cleanup.Dispose));
        await Task.WhenAll(others);

        finish.Set();
        await first;
        // calls == 1
        // end-snippet

        Assert.Equal(1, calls);
    }

    #endregion
}
