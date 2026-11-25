// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using NCode.Disposables;

namespace SiteSamples.Start;

// The samples of docs/site/start/which-disposable.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class WhichDisposableSamples
{
    #region Runs Code Samples

    [Fact]
    public void SiteAnswerActionSample_Runs()
    {
        // begin-snippet: SiteAnswerAction
        var calls = 0;
        var cleanup = Disposable.Create(() => calls++);
        cleanup.Dispose();
        cleanup.Dispose(); // calls == 1
        // end-snippet

        Assert.Equal(1, calls);
    }

    [Fact]
    public void SiteAnswerRepeatableActionSample_Runs()
    {
        // begin-snippet: SiteAnswerRepeatableAction
        var calls = 0;
        var cleanup = Disposable.CreateRepeatable(() => calls++);
        cleanup.Dispose();
        cleanup.Dispose(); // calls == 2
        // end-snippet

        Assert.Equal(2, calls);
    }

    [Fact]
    public void SiteAnswerValueActionSample_Runs()
    {
        // begin-snippet: SiteAnswerValueAction
        var counter = new StrongBox<int>();

        using (Disposable.CreateValue(static box => box.Value++, counter))
        {
            // a hot path: the struct allocates nothing
        }

        // counter.Value == 1
        // end-snippet

        Assert.Equal(1, counter.Value);
    }

    #endregion

    #region Holds Resources Samples

    [Fact]
    public void SiteAnswerCollectionSample_Runs()
    {
        // begin-snippet: SiteAnswerCollection
        List<string> log = [];

        using (var resources = Disposable.Collection())
        {
            resources.Add(Disposable.Create(() => log.Add("first")));
            resources.Add(Disposable.Create(() => log.Add("second")));
        }

        // log: "second", "first"
        // end-snippet

        Assert.Equal(["second", "first"], log);
    }

    [Fact]
    public void SiteAnswerAggregateSample_Runs()
    {
        // begin-snippet: SiteAnswerAggregate
        List<string> log = [];

        using (
            var current = Disposable.Aggregate(Disposable.Create(() => log.Add("old")))
        )
        {
            current.Disposable = Disposable.Create(() => log.Add("new")); // "old" is not disposed
        }

        // log: "new"
        // end-snippet

        Assert.Equal(["new"], log);
    }

    [Fact]
    public void SiteAnswerSerialSample_Runs()
    {
        // begin-snippet: SiteAnswerSerial
        List<string> log = [];

        using (var current = Disposable.Serial(Disposable.Create(() => log.Add("old"))))
        {
            current.Disposable = Disposable.Create(() => log.Add("new")); // disposes "old"
        }

        // log: "old", "new"
        // end-snippet

        Assert.Equal(["old", "new"], log);
    }

    [Fact]
    public void SiteAnswerSingleAssignmentSample_Runs()
    {
        // begin-snippet: SiteAnswerSingleAssignment
        List<string> log = [];

        using (var pending = Disposable.SingleAssignment())
        {
            pending.Disposable = Disposable.Create(() => log.Add("connection closed"));
        }

        // log: "connection closed"
        // end-snippet

        Assert.Equal(["connection closed"], log);
    }

    [Fact]
    public void SiteAnswerGuardSample_Runs()
    {
        // begin-snippet: SiteAnswerGuard
        List<string> log = [];
        DisposableAction connection;

        using (var guard = Disposable.Guard(Disposable.Create(() => log.Add("closed"))))
        {
            // a step that may throw
            connection = guard.Release(); // the caller owns it now
        }

        // log is empty
        // end-snippet

        Assert.Empty(log);
        Assert.False(connection.IsDisposed);
    }

    #endregion
}
