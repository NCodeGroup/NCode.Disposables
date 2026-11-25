// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/bridge-sync-and-async.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class BridgeSyncAndAsyncSamples
{
    private sealed class Connection(List<string> log) : IDisposable, IAsyncDisposable
    {
        public void Dispose() => log.Add("Dispose");

        public ValueTask DisposeAsync()
        {
            log.Add("DisposeAsync");
            return ValueTask.CompletedTask;
        }
    }

    #region Adapt For Every Call Samples

    [Fact]
    public async Task SiteBridgeSyncAndAsyncRepeatableSample_Runs()
    {
        // begin-snippet: SiteBridgeSyncAndAsyncRepeatable
        var calls = 0;
        var legacy = Disposable.CreateRepeatable(() => calls++);
        var adapter = AsyncDisposable.AdaptRepeatable(legacy);

        await adapter.DisposeAsync();
        await adapter.DisposeAsync(); // calls == 2
        // end-snippet

        Assert.Equal(2, calls);
    }

    #endregion

    #region Items That Implement Both Samples

    [Fact]
    public async Task SiteBridgeSyncAndAsyncBothFormsSample_Runs()
    {
        // begin-snippet: SiteBridgeSyncAndAsyncBothForms
        List<string> log = [];

        // Connection implements both IDisposable and IAsyncDisposable.
        using (Disposable.Collection(new Connection(log))) { }
        await using (AsyncDisposable.Collection(new Connection(log))) { }

        // log: "Dispose", "DisposeAsync": each collection disposes through its own interface
        // end-snippet

        Assert.Equal(["Dispose", "DisposeAsync"], log);
    }

    #endregion
}
