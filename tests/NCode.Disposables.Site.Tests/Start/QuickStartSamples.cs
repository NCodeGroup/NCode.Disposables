// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Start;

// The samples of docs/site/start/quick-start.mdx. The site's build includes each begin-snippet region in the page by
// its key (ADR-1010).
public class QuickStartSamples
{
    #region Run Code When A Scope Ends Samples

    [Fact]
    public void SiteQuickStartActionSample_Runs()
    {
        // begin-snippet: SiteQuickStartAction
        List<string> log = [];

        using (Disposable.Create(() => log.Add("unsubscribed")))
        {
            log.Add("subscribed");
        }

        // log: "subscribed", "unsubscribed"
        // end-snippet

        Assert.Equal(["subscribed", "unsubscribed"], log);
    }

    #endregion

    #region Dispose Several Resources Samples

    [Fact]
    public void SiteQuickStartManySample_Runs()
    {
        // begin-snippet: SiteQuickStartMany
        List<string> log = [];

        var connection = Disposable.Create(() => log.Add("connection closed"));
        var cache = Disposable.Create(() => log.Add("cache flushed"));
        var fileLock = Disposable.Create(() => log.Add("lock released"));

        using (var resources = Disposable.Collection(connection, cache, fileLock))
        {
            // work with the three resources
        }

        // log: "lock released", "cache flushed", "connection closed"
        // end-snippet

        Assert.Equal(["lock released", "cache flushed", "connection closed"], log);
    }

    #endregion

    #region Dispose Asynchronously Samples

    [Fact]
    public async Task SiteQuickStartAsyncSample_Runs()
    {
        // begin-snippet: SiteQuickStartAsync
        List<string> log = [];

        await using (
            AsyncDisposable.Create(async () =>
            {
                await Task.Yield();
                log.Add("flushed");
            })
        )
        {
            log.Add("writing");
        }

        // log: "writing", "flushed"
        // end-snippet

        Assert.Equal(["writing", "flushed"], log);
    }

    #endregion
}
