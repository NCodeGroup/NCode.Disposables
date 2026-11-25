// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/share-a-resource.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class ShareAResourceSamples
{
    #region Add An Owner Samples

    [Fact]
    public void SiteShareAResourceExtensionSample_Runs()
    {
        // begin-snippet: SiteShareAResourceExtension
        List<string> log = [];
        var connection = Disposable.Create(() => log.Add("connection closed"));

        using (var lease = connection.AsSharedReference())
        {
            // a second owner, such as a background task
            using var worker = lease.AddReference();
        }

        // log: "connection closed": both leases were released
        // end-snippet

        Assert.Equal(["connection closed"], log);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteShareAResourceDoubleDisposeSample_Runs()
    {
        // begin-snippet: SiteShareAResourceDoubleDispose
        List<string> log = [];
        var first = SharedReference.Create(Disposable.Create(() => log.Add("closed")));
        var second = first.AddReference();

        first.Dispose();
        first.Dispose(); // releases a second reference: log: "closed"

        // second.Value now throws ObjectDisposedException
        // end-snippet

        Assert.Equal(["closed"], log);
        Assert.Throws<ObjectDisposedException>(() =>
        {
            _ = second.Value;
        });
    }

    #endregion
}
