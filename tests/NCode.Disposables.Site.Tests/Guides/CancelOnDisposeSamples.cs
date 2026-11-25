// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/cancel-on-dispose.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class CancelOnDisposeSamples
{
    #region Cancel A Source You Own Samples

    [Fact]
    public void SiteCancelOnDisposeOwnSourceSample_Runs()
    {
        // begin-snippet: SiteCancelOnDisposeOwnSource
        using var source = new CancellationTokenSource();
        var cancellation = Disposable.Cancellation(source);

        cancellation.Dispose(); // cancels the source and leaves it to you to dispose

        var cancelled = source.Token.IsCancellationRequested; // true: Token still works, so the source is not disposed
        // end-snippet

        Assert.True(cancelled);
    }

    #endregion

    #region Cancel Asynchronously Samples

    [Fact]
    public async Task SiteCancelOnDisposeAsyncSample_Runs()
    {
        // begin-snippet: SiteCancelOnDisposeAsync
        var cancellation = AsyncDisposable.Cancellation();
        var work = Task.Delay(Timeout.Infinite, cancellation.Token);

        await cancellation.DisposeAsync(); // CancelAsync, then disposes the source it created

        // work.IsCanceled == true
        // end-snippet

        Assert.True(work.IsCanceled);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteCancelOnDisposeThrowingCallbackSample_Runs()
    {
        // begin-snippet: SiteCancelOnDisposeThrowingCallback
        var cancellation = Disposable.Cancellation();
        cancellation.Token.Register(() =>
            throw new InvalidOperationException("Callback failed.")
        );

        var failed = false;
        try
        {
            cancellation.Dispose();
        }
        catch (AggregateException)
        {
            failed = true; // the callback's exception, after the source was disposed
        }
        // end-snippet

        Assert.True(failed);
    }

    #endregion
}
