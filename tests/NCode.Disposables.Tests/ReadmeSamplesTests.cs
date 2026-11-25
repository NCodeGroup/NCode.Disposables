// Copyright © NCode Group
// SPDX-License-Identifier: Apache-2.0

using System.Buffers;
using System.Diagnostics;

namespace NCode.Disposables.Tests;

// `dotnet mdsnippets` copies each begin-snippet block into README.md and src/NCode.Disposables/README.md (ADR-1000).
public class ReadmeSamplesTests
{
    #region Quick Start Tests

    [Fact]
    public void QuickStartSample_Runs()
    {
        // begin-snippet: QuickStart
        var log = new List<string>();
        var connection = Disposable.Create(() => log.Add("connection closed"));
        var fileLock = Disposable.Create(() => log.Add("lock released"));

        using (var resources = Disposable.Collection())
        {
            resources.Add(connection);
            resources.Add(fileLock);
        }

        // log: "lock released", "connection closed": the last resource added is disposed first
        // end-snippet

        Assert.Equal(["lock released", "connection closed"], log);
    }

    #endregion

    #region Action Tests

    [Fact]
    public void ActionDisposableSample_Runs()
    {
        // begin-snippet: ActionDisposable
        var count = 0;

        var once = Disposable.Create(() => count++);
        once.Dispose();
        once.Dispose(); // count == 1: the action runs on the first call only

        var everyTime = Disposable.Create(() => count++, idempotent: false);
        everyTime.Dispose();
        everyTime.Dispose(); // count == 3: the action runs on every call
        // end-snippet

        Assert.Equal(3, count);
    }

    [Fact]
    public void ActionWithStateSample_Runs()
    {
        // begin-snippet: ActionWithState
        var stopwatch = Stopwatch.StartNew();

        using (Disposable.Create(watch => watch.Stop(), stopwatch))
        {
            // timed work
        }

        // stopwatch.IsRunning == false
        // end-snippet

        Assert.False(stopwatch.IsRunning);
    }

    [Fact]
    public async Task AsyncActionDisposableSample_Runs()
    {
        // begin-snippet: AsyncActionDisposable
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);

        await using (AsyncDisposable.Create(async () => await writer.FlushAsync()))
        {
            await writer.WriteAsync("hello");
        }

        // stream.Length == 5: the writer flushed when the scope ended
        // end-snippet

        Assert.Equal(5, stream.Length);
    }

    #endregion

    #region Empty Tests

    [Fact]
    public void EmptyDisposableSample_Runs()
    {
        // begin-snippet: EmptyDisposable
        static IDisposable Subscribe(bool enabled) =>
            enabled ? Disposable.Create(() => Console.WriteLine("Unsubscribed")) : Disposable.Empty;

        using var subscription = Subscribe(enabled: false); // Disposable.Empty: disposing it does nothing
        // end-snippet

        Assert.Same(Disposable.Empty, subscription);
    }

    #endregion

    #region Aggregate Tests

    [Fact]
    public void AggregateDisposableSample_Runs()
    {
        // begin-snippet: AggregateDisposable
        var log = new List<string>();
        var primary = Disposable.Create(() => log.Add("primary closed"));
        var replica = Disposable.Create(() => log.Add("replica closed"));

        using (var current = Disposable.Aggregate(primary))
        {
            current.Disposable = replica; // swapping doesn't dispose primary
            primary.Dispose();
        }

        // log: "primary closed", "replica closed"
        // end-snippet

        Assert.Equal(["primary closed", "replica closed"], log);
    }

    #endregion

    #region Serial Tests

    [Fact]
    public void SerialDisposableSample_Runs()
    {
        // begin-snippet: SerialDisposable
        var log = new List<string>();
        using (var subscription = Disposable.Serial())
        {
            subscription.Disposable = Disposable.Create(() => log.Add("first closed"));
            subscription.Disposable = Disposable.Create(() => log.Add("second closed")); // disposes the first
        }

        // log: "first closed", "second closed"
        // end-snippet

        Assert.Equal(["first closed", "second closed"], log);
    }

    #endregion

    #region Single Assignment Tests

    [Fact]
    public void SingleAssignmentDisposableSample_Runs()
    {
        // begin-snippet: SingleAssignmentDisposable
        var log = new List<string>();
        var pending = Disposable.SingleAssignment();
        pending.Dispose(); // disposed before the resource arrived
        pending.Disposable = Disposable.Create(() => log.Add("late resource closed")); // disposed at once

        // log: "late resource closed"
        // end-snippet

        Assert.Equal(["late resource closed"], log);
    }

    #endregion

    #region Collection Tests

    [Fact]
    public void CollectionDisposableSample_Runs()
    {
        // begin-snippet: CollectionDisposable
        var log = new List<string>();
        var first = Disposable.Create(() => log.Add("first"));
        var second = Disposable.Create(() => log.Add("second"));
        var third = Disposable.Create(() => log.Add("third"));

        using (var resources = Disposable.Collection(first, second, third))
        {
            resources.Remove(second); // removing an item doesn't dispose it
        }

        // log: "third", "first"
        // end-snippet

        Assert.Equal(["third", "first"], log);
    }

    [Fact]
    public void CollectionLateAddSample_Runs()
    {
        // begin-snippet: CollectionLateAdd
        var log = new List<string>();
        var resources = Disposable.Collection();
        resources.Dispose();
        resources.Add(Disposable.Create(() => log.Add("late item closed"))); // disposed at once, not kept

        // log: "late item closed"; resources.Count == 0
        // end-snippet

        Assert.Equal(["late item closed"], log);
        Assert.Empty(resources);
    }

    [Fact]
    public void CollectionErrorsSample_Runs()
    {
        // begin-snippet: CollectionErrors
        var log = new List<string>();
        var first = Disposable.Create(() => log.Add("first"));
        var failing = Disposable.Create(() =>
            throw new InvalidOperationException("Failed to close.")
        );
        var third = Disposable.Create(() => log.Add("third"));
        var resources = Disposable.Collection(ignoreExceptions: true, first, failing, third);

        resources.Dispose(); // log: "third", "first": the failure is ignored
        // end-snippet

        Assert.Equal(["third", "first"], log);
    }

    [Fact]
    public void CollectionStreamSample_Runs()
    {
        var stream = new RecordingStream();

        // begin-snippet: CollectionStream
        // stream is an open Stream, such as a FileStream.
        using (var resources = Disposable.Collection(stream))
        {
            stream.WriteByte(42);
        }

        // stream.Dispose ran: the collection disposes every item through IDisposable
        // end-snippet

        Assert.False(stream.CanWrite);
        Assert.False(stream.DisposeAsyncCalled);
    }

    #endregion

    #region Shared Reference Tests

    [Fact]
    public void SharedReferenceSample_Runs()
    {
        // begin-snippet: SharedReference
        var log = new List<string>();
        var connection = Disposable.Create(() => log.Add("connection closed"));

        var first = SharedReference.Create(connection);
        var second = first.AddReference();

        first.Dispose(); // log is empty: second still holds a reference
        second.Dispose(); // log: "connection closed"
        // end-snippet

        Assert.Equal(["connection closed"], log);
    }

    [Fact]
    public void SharedReferenceTryAddSample_Runs()
    {
        // begin-snippet: SharedReferenceTryAdd
        var resource = Disposable.Create(() => { });
        var lease = SharedReference.Create(resource);
        lease.Dispose(); // the last lease is released, so the resource is disposed

        var added = lease.TryAddReference(out var extra); // false: no lease can be added
        // end-snippet

        Assert.False(added);
        Assert.False(extra.IsActive);
        Assert.Throws<ObjectDisposedException>(() => lease.AddReference());
    }

    [Fact]
    public void SharedReferenceReleaseSample_Runs()
    {
        // begin-snippet: SharedReferenceRelease
        var pool = ArrayPool<byte>.Create();
        var buffer = pool.Rent(1024);

        using (var lease = SharedReference.Create(buffer, array => pool.Return(array)))
        {
            lease.Value[0] = 42;
        }

        // the buffer is back in the pool
        // end-snippet

        var rented = pool.Rent(1024);

        Assert.Same(buffer, rented);
    }

    [Fact]
    public async Task AsyncSharedReferenceSample_Runs()
    {
        var stream = new RecordingStream();

        // begin-snippet: AsyncSharedReference
        // stream is an open Stream, such as a FileStream.
        await using (var lease = AsyncSharedReference.Create(stream))
        {
            lease.Value.WriteByte(42);
        }

        // stream.DisposeAsync ran when the last lease was released
        // end-snippet

        Assert.True(stream.DisposeAsyncCalled);
    }

    [Fact]
    public async Task AsAsyncSharedReferenceSample_Runs()
    {
        var stream = new RecordingStream();

        // begin-snippet: AsAsyncSharedReference
        // stream is an open Stream, which implements both IDisposable and IAsyncDisposable.
        await using (var lease = stream.AsAsyncSharedReference())
        {
            lease.Value.WriteByte(42);
        }

        // stream.DisposeAsync ran when the last lease was released
        // end-snippet

        Assert.True(stream.DisposeAsyncCalled);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void ContextDisposableSample_Runs()
    {
        var uiContext = new RecordingSynchronizationContext();

        // begin-snippet: ContextDisposable
        var log = new List<string>();
        var control = Disposable.Create(() => log.Add("control disposed"));

        // uiContext is the SynchronizationContext captured on the UI thread.
        var disposable = Disposable.Context(control, uiContext);
        disposable.Dispose(); // Send: returns once the UI thread has disposed the control
        // end-snippet

        Assert.Equal(["control disposed"], log);
        Assert.Equal(1, uiContext.SendCount);
    }

    #endregion

    #region Async Bridge Tests

    [Fact]
    public async Task AdaptSample_Runs()
    {
        // begin-snippet: Adapt
        var log = new List<string>();
        IDisposable legacy = Disposable.Create(() => log.Add("legacy disposed"));

        await using (AsyncDisposable.Adapt(legacy))
        {
            // code that takes an IAsyncDisposable
        }

        // log: "legacy disposed"
        // end-snippet

        Assert.Equal(["legacy disposed"], log);
    }

    [Fact]
    public async Task DisposeAsyncIfAvailableSample_Runs()
    {
        var stream = new RecordingStream();

        // begin-snippet: DisposeAsyncIfAvailable
        // stream is an open Stream, such as a FileStream.
        IDisposable? resource = stream;
        await resource.DisposeAsyncIfAvailable(); // a Stream is IAsyncDisposable: DisposeAsync runs

        resource = null;
        await resource.DisposeAsyncIfAvailable(); // null: nothing happens
        // end-snippet

        Assert.True(stream.DisposeAsyncCalled);
    }

    #endregion

    #region Dispose All Tests

    [Fact]
    public void DisposeAllSample_Runs()
    {
        // begin-snippet: DisposeAll
        var log = new List<string>();
        object?[] items =
        [
            Disposable.Create(() => log.Add("first")),
            "not disposable",
            null,
            Disposable.Create(() => log.Add("second")),
        ];

        items.DisposeAll(); // log: "second", "first": items that aren't IDisposable are skipped
        // end-snippet

        Assert.Equal(["second", "first"], log);
    }

    [Fact]
    public async Task DisposeAllAsyncSample_Runs()
    {
        var stream = new RecordingStream();

        // begin-snippet: DisposeAllAsync
        var log = new List<string>();
        // stream is an open Stream, such as a FileStream.
        object[] items = [Disposable.Create(() => log.Add("disposed")), stream];

        await items.DisposeAllAsync(); // DisposeAsync for the stream, then Dispose for the rest
        // end-snippet

        Assert.Equal(["disposed"], log);
        Assert.True(stream.DisposeAsyncCalled);
    }

    #endregion

    private sealed class RecordingSynchronizationContext : SynchronizationContext
    {
        public int SendCount { get; private set; }

        public override void Send(SendOrPostCallback d, object? state)
        {
            SendCount++;
            d(state);
        }
    }

    private sealed class RecordingStream : MemoryStream
    {
        public bool DisposeAsyncCalled { get; private set; }

        public override ValueTask DisposeAsync()
        {
            DisposeAsyncCalled = true;
            return base.DisposeAsync();
        }
    }
}
