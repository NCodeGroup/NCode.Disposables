// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/hold-a-changing-resource.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class HoldAChangingResourceSamples
{
    #region Replace And Dispose The Old One Samples

    [Fact]
    public void SiteHoldAChangingResourceSameInstanceSample_Runs()
    {
        // begin-snippet: SiteHoldAChangingResourceSameInstance
        List<string> log = [];
        var connection = Disposable.Create(() => log.Add("closed"));
        var current = Disposable.Serial(connection);

        current.Disposable = connection; // the same instance: nothing is disposed

        // log is empty
        // end-snippet

        Assert.Empty(log);
        Assert.Same(connection, current.Disposable);
    }

    #endregion

    #region Replace Asynchronously Samples

    [Fact]
    public async Task SiteHoldAChangingResourceAsyncSample_Runs()
    {
        // begin-snippet: SiteHoldAChangingResourceAsync
        List<string> log = [];
        await using (var current = AsyncDisposable.Serial())
        {
            await current.SetDisposableAsync(
                AsyncDisposable.Create(() => log.Add("first closed"))
            );
            await current.SetDisposableAsync(
                AsyncDisposable.Create(() => log.Add("second closed"))
            ); // disposes the first
        }

        // log: "first closed", "second closed"
        // end-snippet

        Assert.Equal(["first closed", "second closed"], log);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteHoldAChangingResourceSecondAssignmentSample_Runs()
    {
        // begin-snippet: SiteHoldAChangingResourceSecondAssignment
        var pending = Disposable.SingleAssignment();
        pending.Disposable = Disposable.Create(() => { });

        var rejected = false;
        try
        {
            pending.Disposable = Disposable.Create(() => { });
        }
        catch (InvalidOperationException)
        {
            rejected = true; // it takes one resource
        }
        // end-snippet

        Assert.True(rejected);
    }

    #endregion
}
