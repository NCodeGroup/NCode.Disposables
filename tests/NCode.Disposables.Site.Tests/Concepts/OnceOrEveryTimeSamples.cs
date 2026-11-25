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
    public void SiteOnceOrEveryTimeThreadsSample_Runs()
    {
        // begin-snippet: SiteOnceOrEveryTimeThreads
        var calls = 0;
        var cleanup = Disposable.Create(() => Interlocked.Increment(ref calls));

        Parallel.For(0, 8, _ => cleanup.Dispose());
        // calls == 1: one thread takes the action, the others find it taken
        // end-snippet

        Assert.Equal(1, calls);
    }

    #endregion
}
