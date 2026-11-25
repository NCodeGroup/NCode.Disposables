// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/dispose-many-resources.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class DisposeManyResourcesSamples
{
    #region Handle The Errors Samples

    [Fact]
    public void SiteDisposeManyResourcesErrorsSample_Runs()
    {
        // begin-snippet: SiteDisposeManyResourcesErrors
        var resources = Disposable.Collection(
            Disposable.Create(() => throw new InvalidOperationException("first")),
            Disposable.Create(() => throw new InvalidOperationException("second"))
        );

        AggregateException? failure = null;
        try
        {
            resources.Dispose();
        }
        catch (AggregateException exception)
        {
            failure = exception;
            // InnerExceptions: "second", then "first", the order they were disposed in
        }
        // end-snippet

        Assert.NotNull(failure);
        Assert.Equal(
            ["second", "first"],
            failure.InnerExceptions.Select(exception => exception.Message)
        );
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteDisposeManyResourcesAsyncOnlySample_Runs()
    {
        // begin-snippet: SiteDisposeManyResourcesAsyncOnly
        List<string> log = [];
        object[] items =
        [
            Disposable.Create(() => log.Add("disposed")),
            AsyncDisposable.Create(() => log.Add("never disposed")),
        ];

        var failed = false;
        try
        {
            items.DisposeAll();
        }
        catch (InvalidOperationException)
        {
            failed = true;
            // the other items first: log: "disposed"
        }
        // end-snippet

        Assert.True(failed);
        Assert.Equal(["disposed"], log);
    }

    #endregion
}
