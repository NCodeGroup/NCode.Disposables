// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Concepts;

// The samples of docs/site/concepts/disposal-status.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class DisposalStatusSamples
{
    #region A Resource That Arrives After Disposal Samples

    [Fact]
    public void SiteDisposalStatusLateAggregateSample_Runs()
    {
        // begin-snippet: SiteDisposalStatusLateAggregate
        List<string> log = [];
        var current = Disposable.Aggregate();
        current.Dispose();
        // disposed at once
        current.Disposable = Disposable.Create(() => log.Add("late resource closed"));

        // log: "late resource closed"; current.Disposable == null
        // end-snippet

        Assert.Equal(["late resource closed"], log);
        Assert.Null(current.Disposable);
    }

    #endregion
}
