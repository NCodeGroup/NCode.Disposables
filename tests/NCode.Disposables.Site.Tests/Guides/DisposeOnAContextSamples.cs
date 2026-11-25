// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/dispose-on-a-context.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class DisposeOnAContextSamples
{
    private sealed class QueueContext : SynchronizationContext
    {
        private readonly Queue<(SendOrPostCallback Callback, object? State)> _queue =
            new();

        public override void Post(SendOrPostCallback d, object? state) =>
            _queue.Enqueue((d, state));

        public void RunPosted()
        {
            while (_queue.TryDequeue(out var item))
            {
                item.Callback(item.State);
            }
        }
    }

    private sealed class ClosedContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) =>
            throw new InvalidOperationException("The dispatcher has shut down.");
    }

    #region Dispose Without Waiting Samples

    [Fact]
    public void SiteDisposeOnAContextPostingSample_Runs()
    {
        var uiContext = new QueueContext();

        // begin-snippet: SiteDisposeOnAContextPosting
        List<string> log = [];
        var control = Disposable.Create(() => log.Add("control disposed"));

        // uiContext is the SynchronizationContext captured on the UI thread.
        var disposable = Disposable.PostingContext(control, uiContext);

        // Post: returns at once; the UI thread disposes the control later
        disposable.Dispose();
        // end-snippet

        Assert.Empty(log);
        uiContext.RunPosted();
        Assert.Equal(["control disposed"], log);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteDisposeOnAContextPostFailsSample_Runs()
    {
        var uiContext = new ClosedContext();

        // begin-snippet: SiteDisposeOnAContextPostFails
        List<string> log = [];
        var control = Disposable.Create(() => log.Add("control disposed"));

        // uiContext's dispatcher has shut down: its Post throws.
        var disposable = Disposable.PostingContext(control, uiContext);

        var failed = false;
        try
        {
            disposable.Dispose();
        }
        catch (InvalidOperationException)
        {
            failed = true; // log is empty: the control is not disposed on this thread
        }
        // end-snippet

        Assert.True(failed);
        Assert.Empty(log);
    }

    #endregion
}
