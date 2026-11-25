// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/dispose-many-resources.mdx. The site's build includes each begin-snippet region in
// the page by its key (ADR-1010).
public class DisposeManyResourcesSamples
{
    #region Handle The Errors Samples

    [Fact]
    public void SiteDisposeManyResourcesErrorsSample_Runs()
    {
        // begin-snippet: SiteDisposeManyResourcesErrors
        var first = Disposable.Create(() => throw new InvalidOperationException("first"));
        var second = Disposable.Create(() =>
            throw new InvalidOperationException("second")
        );
        var resources = Disposable.Collection(first, second);

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
        var messages = failure
            .InnerExceptions.Select(exception => exception.Message)
            .ToList();
        Assert.Equal(["second", "first"], messages);
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
