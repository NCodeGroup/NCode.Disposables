// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.CompilerServices;
using NCode.Disposables;

namespace SiteSamples.Guides;

// The samples of docs/site/guides/run-code-on-dispose.mdx. The site's build includes each begin-snippet region in the
// page by its key (ADR-1010).
public class RunCodeOnDisposeSamples
{
    #region Pass State To The Action Samples

    [Fact]
    public void SiteRunCodeOnDisposeStateSample_Runs()
    {
        // begin-snippet: SiteRunCodeOnDisposeState
        HashSet<string> active = ["user:42"];

        using (Disposable.Create(static (set, key) => set.Remove(key), active, "user:42"))
        {
            // work that reads the entry
        }

        // active.Count == 0: a static lambda takes its state as arguments
        // end-snippet

        Assert.Empty(active);
    }

    #endregion

    #region Discard A Result Samples

    [Fact]
    public void SiteRunCodeOnDisposeFuncSample_Runs()
    {
        // begin-snippet: SiteRunCodeOnDisposeFunc
        var stack = new Stack<string>();
        stack.Push("request");

        using (Disposable.Create(() => stack.Pop()))
        {
            // stack.Count == 1
        }

        // stack.Count == 0: the result of Pop is discarded
        // end-snippet

        Assert.Empty(stack);
    }

    #endregion

    #region Pitfalls Samples

    [Fact]
    public void SiteRunCodeOnDisposeValueCopySample_Runs()
    {
        // begin-snippet: SiteRunCodeOnDisposeValueCopy
        var count = new StrongBox<int>();

        var scope = Disposable.CreateValue(static box => box.Value++, count);
        var copy = scope;

        scope.Dispose();
        copy.Dispose(); // count.Value == 2: every copy runs the action
        // end-snippet

        Assert.Equal(2, count.Value);
    }

    #endregion
}
