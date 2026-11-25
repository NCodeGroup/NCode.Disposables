// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/hand-on-ownership.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class HandOnOwnershipSamples
{
    #region Guard An Asynchronous Resource Samples

    [Fact]
    public async Task SiteHandOnOwnershipAsyncSample_Runs()
    {
        // begin-snippet: SiteHandOnOwnershipAsync
        List<string> log = [];

        async Task<AsyncDisposableAction> OpenAsync(bool handshakeFails)
        {
            var connection = AsyncDisposable.Create(() => log.Add("connection closed"));
            await using var guard = AsyncDisposable.Guard(connection);

            await Task.Yield();

            if (handshakeFails)
                throw new InvalidOperationException("Handshake failed."); // the guard closes the connection

            return guard.Release(); // the caller owns the connection now
        }

        await using (await OpenAsync(handshakeFails: false)) { }

        try
        {
            await OpenAsync(handshakeFails: true);
        }
        catch (InvalidOperationException)
        {
            log.Add("handshake failed");
        }

        // log: "connection closed", "connection closed", "handshake failed"
        // end-snippet

        Assert.Equal(["connection closed", "connection closed", "handshake failed"], log);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteHandOnOwnershipReleaseTwiceSample_Runs()
    {
        // begin-snippet: SiteHandOnOwnershipReleaseTwice
        using var guard = Disposable.Guard(Disposable.Create(() => { }));
        var connection = guard.Release(); // the caller owns it now

        var rejected = false;
        try
        {
            guard.Release();
        }
        catch (InvalidOperationException)
        {
            rejected = true; // ownership was released already
        }
        // end-snippet

        Assert.True(rejected);
        Assert.False(connection.IsDisposed);
    }

    #endregion
}
