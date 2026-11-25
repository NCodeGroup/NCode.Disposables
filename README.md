<p align="center">
  <img src="NCodeGroup.png" alt="NCode" width="96" height="96">
</p>

<h1 align="center">NCode.Disposables</h1>

<p align="center">
  Building blocks for IDisposable and IAsyncDisposable in .NET: run an action on dispose, dispose many resources in reverse order, hold a swappable, serial or single-assignment resource, guard a resource until ownership moves on, cancel on dispose, share a resource through reference-counted leases, scope a lock, and dispose on a synchronization context.
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/NCode.Disposables/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/NCode.Disposables?logo=nuget"></a>
  <a href="https://www.nuget.org/packages/NCode.Disposables/"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/NCode.Disposables?logo=nuget"></a>
  <a href="https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/ci.yml?query=branch%3Amain"><img alt="CI" src="https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/ci.yml/badge.svg?branch=main"></a>
  <a href="https://www.nuget.org/packages/NCode.Disposables#supportedframeworks-body-tab"><img alt="Target framework" src="https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2FNCodeGroup%2FNCode.Disposables%2Fmain%2Fsrc%2FNCode.Disposables%2FNCode.Disposables.csproj&query=%2F%2FTargetFramework%20%7C%20%2F%2FTargetFrameworks&label=target&logo=dotnet&color=512BD4"></a>
  <a href="LICENSE.txt"><img alt="License" src="https://img.shields.io/github/license/NCodeGroup/NCode.Disposables"></a>
  <a href="https://scorecard.dev/viewer/?uri=github.com/NCodeGroup/NCode.Disposables"><img alt="OpenSSF Scorecard" src="https://api.scorecard.dev/projects/github.com/NCodeGroup/NCode.Disposables/badge"></a>
</p>

Cleanup in .NET often needs more than one `using` statement: undoing a registration, disposing several resources in the right order, handing one expensive resource to several owners, or disposing a UI object on its own thread. NCode.Disposables gives each of these a small type behind the `Disposable` and `AsyncDisposable` factories, each with an `IAsyncDisposable` counterpart except the flag and the lock scopes.

This page is the full guide; nuget.org shows the shorter [package readme](src/NCode.Disposables/README.md).

## Installation

```shell
dotnet add package NCode.Disposables
```

The package depends on no other package. Its target frameworks are the ones its `.csproj` declares; they are listed on [nuget.org's Frameworks tab](https://www.nuget.org/packages/NCode.Disposables#supportedframeworks-body-tab) and shown by the badge above. [CHANGELOG.md](CHANGELOG.md) records when a framework is added or dropped.

## Quick start

Every type is in the `NCode.Disposables` namespace. `Disposable.Create` turns a delegate into an `IDisposable`, and `Disposable.Collection` disposes many of them together, the last one added first:

<!-- snippet: QuickStart -->
```cs
var log = new List<string>();
var connection = Disposable.Create(() => log.Add("connection closed"));
var fileLock = Disposable.Create(() => log.Add("lock released"));

using (var resources = Disposable.Collection())
{
    resources.Add(connection);
    resources.Add(fileLock);
}

// log: "lock released", "connection closed": the last resource added is disposed first
```
<!-- endSnippet -->

## Usage

### Running an action on dispose

`Disposable.Create` wraps an `Action`. By default the action runs on the first `Dispose` only, even when several threads dispose at once; pass `idempotent: false` to run it on every call:

<!-- snippet: ActionDisposable -->
```cs
var count = 0;

var once = Disposable.Create(() => count++);
once.Dispose();
once.Dispose(); // count == 1: the action runs on the first call only

var everyTime = Disposable.Create(() => count++, idempotent: false);
everyTime.Dispose();
everyTime.Dispose(); // count == 3: the action runs on every call
```
<!-- endSnippet -->

Overloads take up to eight state arguments, which the action receives when it runs, and the `Func` overloads discard the delegate's result:

<!-- snippet: ActionWithState -->
```cs
var stopwatch = Stopwatch.StartNew();

using (Disposable.Create(watch => watch.Stop(), stopwatch))
{
    // timed work
}

// stopwatch.IsRunning == false
```
<!-- endSnippet -->

`AsyncDisposable.Create` takes a `Func<ValueTask>`, or an `Action` that it runs synchronously, and works with `await using`:

<!-- snippet: AsyncActionDisposable -->
```cs
var stream = new MemoryStream();
var writer = new StreamWriter(stream);

await using (AsyncDisposable.Create(async () => await writer.FlushAsync()))
{
    await writer.WriteAsync("hello");
}

// stream.Length == 5: the writer flushed when the scope ended
```
<!-- endSnippet -->

`AsyncDisposable.Create` also passes up to eight arguments to a `Func<…, ValueTask>`, so a static lambda needs no closure:

<!-- snippet: AsyncActionWithState -->
```cs
var stream = new MemoryStream();
var writer = new StreamWriter(stream);

await using (AsyncDisposable.Create(static w => new ValueTask(w.FlushAsync()), writer))
{
    await writer.WriteAsync("hello");
}

// stream.Length == 5
```
<!-- endSnippet -->

### Running an action without allocating

`Disposable.CreateValue` and `AsyncDisposable.CreateValue` return a struct that runs a static delegate with a state value. It allocates nothing, but every dispose of every copy runs the action again, so dispose it once, through `using`:

<!-- snippet: ValueDisposable -->
```cs
var stopwatch = Stopwatch.StartNew();

using (Disposable.CreateValue(static watch => watch.Stop(), stopwatch))
{
    // timed work; the struct allocates nothing
}

// stopwatch.IsRunning == false
```
<!-- endSnippet -->

### The empty disposable

`Disposable.Empty` and `AsyncDisposable.Empty` are singletons whose dispose does nothing, for a method that must return a disposable when there is nothing to clean up:

<!-- snippet: EmptyDisposable -->
```cs
static IDisposable Subscribe(bool enabled) =>
    enabled ? Disposable.Create(() => Console.WriteLine("Unsubscribed")) : Disposable.Empty;

using var subscription = Subscribe(enabled: false); // Disposable.Empty: disposing it does nothing
```
<!-- endSnippet -->

### Swapping the resource an aggregate holds

`Disposable.Aggregate` holds one resource, or `null`, in its `Disposable` property, and disposes the resource it holds when it is disposed. Setting the property replaces the resource without disposing the old one. Once the aggregate is disposed, the property reads `null` and setting it disposes the resource it is given; `AsyncDisposable.Aggregate`'s setter throws `ObjectDisposedException` then, and its `SetDisposableAsync` disposes the resource. `AsyncDisposable.Aggregate` does the same for an `IAsyncDisposable`:

<!-- snippet: AggregateDisposable -->
```cs
var log = new List<string>();
var primary = Disposable.Create(() => log.Add("primary closed"));
var replica = Disposable.Create(() => log.Add("replica closed"));

using (var current = Disposable.Aggregate(primary))
{
    current.Disposable = replica; // swapping doesn't dispose primary
    primary.Dispose();
}

// log: "primary closed", "replica closed"
```
<!-- endSnippet -->

### Replacing a resource and disposing the old one

`Disposable.Serial` holds one resource and disposes it when another replaces it. Once the serial disposable is disposed, it disposes any resource it is given, and `Disposable` reads `null`. `AsyncDisposable.Serial` does the same through `SetDisposableAsync`:

<!-- snippet: SerialDisposable -->
```cs
var log = new List<string>();
using (var subscription = Disposable.Serial())
{
    subscription.Disposable = Disposable.Create(() => log.Add("first closed"));
    subscription.Disposable = Disposable.Create(() => log.Add("second closed")); // disposes the first
}

// log: "first closed", "second closed"
```
<!-- endSnippet -->

### Assigning a resource once

`Disposable.SingleAssignment` takes one resource, once; a second assignment throws `InvalidOperationException`. A resource that arrives after it is disposed is disposed at once. `AsyncDisposable.SingleAssignment` takes it through `SetDisposableAsync`:

<!-- snippet: SingleAssignmentDisposable -->
```cs
var log = new List<string>();
var pending = Disposable.SingleAssignment();
pending.Dispose(); // disposed before the resource arrived
pending.Disposable = Disposable.Create(() => log.Add("late resource closed")); // disposed at once

// log: "late resource closed"
```
<!-- endSnippet -->

### Disposing many resources together

`Disposable.Collection` is an `ICollection<IDisposable>` that disposes its items in reverse order, the last one added first, on its first `Dispose`; later calls do nothing, and an item added after that is disposed at once. `Remove` and `Clear` take items out without disposing them:

<!-- snippet: CollectionDisposable -->
```cs
var log = new List<string>();
var first = Disposable.Create(() => log.Add("first"));
var second = Disposable.Create(() => log.Add("second"));
var third = Disposable.Create(() => log.Add("third"));

using (var resources = Disposable.Collection(first, second, third))
{
    resources.Remove(second); // removing an item doesn't dispose it
}

// log: "third", "first"
```
<!-- endSnippet -->

`AsyncDisposable.Collection`'s `Add` throws `ObjectDisposedException` once it is disposed; `AddAsync` disposes the item instead:

<!-- snippet: CollectionLateAdd -->
```cs
var log = new List<string>();
var resources = Disposable.Collection();
resources.Dispose();
resources.Add(Disposable.Create(() => log.Add("late item closed"))); // disposed at once, not kept

// log: "late item closed"; resources.Count == 0
```
<!-- endSnippet -->

Every item is disposed even when one throws. A single failure is rethrown as it is, several are thrown together as one `AggregateException`, and `ignoreExceptions: true` ignores them:

<!-- snippet: CollectionErrors -->
```cs
var log = new List<string>();
var first = Disposable.Create(() => log.Add("first"));
var failing = Disposable.Create(() =>
    throw new InvalidOperationException("Failed to close.")
);
var third = Disposable.Create(() => log.Add("third"));
var resources = Disposable.Collection(ignoreExceptions: true, first, failing, third);

resources.Dispose(); // log: "third", "first": the failure is ignored
```
<!-- endSnippet -->

An item that also implements `IAsyncDisposable`, such as a `Stream`, is disposed through `IDisposable.Dispose`, like every other item:

<!-- snippet: CollectionStream -->
```cs
// stream is an open Stream, such as a FileStream.
using (var resources = Disposable.Collection(stream))
{
    stream.WriteByte(42);
}

// stream.Dispose ran: the collection disposes every item through IDisposable
```
<!-- endSnippet -->

To dispose such items with `DisposeAsync`, put them in `AsyncDisposable.Collection`.

### Sharing a resource through leases

`SharedReference.Create` puts a resource behind a reference count and returns the first lease. Each `AddReference` returns another lease on the same resource, and the resource is disposed when the last lease is disposed:

<!-- snippet: SharedReference -->
```cs
var log = new List<string>();
var connection = Disposable.Create(() => log.Add("connection closed"));

var first = SharedReference.Create(connection);
var second = first.AddReference();

first.Dispose(); // log is empty: second still holds a reference
second.Dispose(); // log: "connection closed"
```
<!-- endSnippet -->

`Disposable.Shared(value)` and the `AsSharedReference()` extension do the same. Once the last lease is released, `AddReference` and `Value` throw `ObjectDisposedException`, and `TryAddReference` returns `false`:

<!-- snippet: SharedReferenceTryAdd -->
```cs
var resource = Disposable.Create(() => { });
var lease = SharedReference.Create(resource);
lease.Dispose(); // the last lease is released, so the resource is disposed

var added = lease.TryAddReference(out var extra); // false: no lease can be added
```
<!-- endSnippet -->

A lease is a `readonly struct`, so a copy of a lease is the same lease: dispose each lease exactly once, because disposing it twice releases two references. `IsActive` is `false` only for a `default` lease, whose `Value` and `AddReference` throw `InvalidOperationException`.

For a value that isn't disposable, or that needs other cleanup, pass the release action yourself:

<!-- snippet: SharedReferenceRelease -->
```cs
var pool = ArrayPool<byte>.Create();
var buffer = pool.Rent(1024);

using (var lease = SharedReference.Create(buffer, array => pool.Return(array)))
{
    lease.Value[0] = 42;
}

// the buffer is back in the pool
```
<!-- endSnippet -->

`AsyncSharedReference.Create` does the same for an `IAsyncDisposable`, or with a `Func<T, ValueTask>` release action, and returns its first lease synchronously:

<!-- snippet: AsyncSharedReference -->
```cs
// stream is an open Stream, such as a FileStream.
await using (var lease = AsyncSharedReference.Create(stream))
{
    lease.Value.WriteByte(42);
}

// stream.DisposeAsync ran when the last lease was released
```
<!-- endSnippet -->

The `AsAsyncSharedReference()` extension does the same, also for a type that implements both `IDisposable` and `IAsyncDisposable`, such as a `Stream`:

<!-- snippet: AsAsyncSharedReference -->
```cs
// stream is an open Stream, which implements both IDisposable and IAsyncDisposable.
await using (var lease = stream.AsAsyncSharedReference())
{
    lease.Value.WriteByte(42);
}

// stream.DisposeAsync ran when the last lease was released
```
<!-- endSnippet -->

On a type that implements both `IDisposable` and `IAsyncDisposable`, such as a `MemoryStream`, `AsSharedReference()` returns a `SharedReferenceLease<T>`, which disposes the resource with `Dispose`, and `AsAsyncSharedReference()` returns an `AsyncSharedReferenceLease<T>`, which disposes it with `DisposeAsync`.

### Handing on ownership with a guard

`Disposable.Guard` disposes a resource when the scope ends, unless `Release()` hands it on first, so a factory can't leak what it built when a later step throws. `AsyncDisposable.Guard` does the same for an `IAsyncDisposable`:

<!-- snippet: GuardDisposable -->
```cs
var log = new List<string>();

DisposableAction Connect(bool handshakeFails)
{
    using var guard = Disposable.Guard(
        Disposable.Create(() => log.Add("connection closed"))
    );

    if (handshakeFails)
        throw new InvalidOperationException("Handshake failed."); // the guard closes the connection

    return guard.Release(); // the caller owns the connection now
}

using (Connect(handshakeFails: false)) { }

try
{
    Connect(handshakeFails: true);
}
catch (InvalidOperationException)
{
    log.Add("handshake failed");
}

// log: "connection closed", "connection closed", "handshake failed"
```
<!-- endSnippet -->

### Cancelling on dispose

`Disposable.Cancellation()` cancels its token when disposed, then disposes the `CancellationTokenSource` it created; `Disposable.Cancellation(source)` only cancels a source you own. `AsyncDisposable.Cancellation` cancels with `CancelAsync`:

<!-- snippet: CancellationDisposable -->
```cs
using var cancellation = Disposable.Cancellation();
var work = Task.Delay(Timeout.Infinite, cancellation.Token);
cancellation.Dispose(); // cancels the token, then disposes the source it created
// work.IsCanceled == true
```
<!-- endSnippet -->

### Checking whether a disposable is disposed

Every disposable that keeps state implements `IDisposalStatus`, whose `IsDisposed` is `true` once disposal has started. `Disposable.Flag()` does nothing else:

<!-- snippet: DisposalStatus -->
```cs
var flag = Disposable.Flag();
var cleanup = Disposable.Create(() => { });
flag.Dispose();
IDisposalStatus status = cleanup; // every disposable that keeps state reports it
// flag.IsDisposed == true, status.IsDisposed == false
```
<!-- endSnippet -->

### Disposing on a synchronization context

`Disposable.Context` and `AsyncDisposable.Context` dispose a resource on a `SynchronizationContext`, such as a UI thread's. By default `Dispose` calls the context's `Send` and returns once the resource is disposed; with `async: true` it calls `Post` and returns at once, telling the context through `OperationStarted` and `OperationCompleted`. The resource is disposed once, however often `Dispose` is called:

<!-- snippet: ContextDisposable -->
```cs
var log = new List<string>();
var control = Disposable.Create(() => log.Add("control disposed"));

// uiContext is the SynchronizationContext captured on the UI thread.
var disposable = Disposable.Context(control, uiContext);
disposable.Dispose(); // Send: returns once the UI thread has disposed the control
```
<!-- endSnippet -->

`AsyncDisposable.Context` posts the disposal to the context and completes once it has run there:

<!-- snippet: AsyncContextDisposable -->
```cs
var log = new List<string>();
var viewModel = AsyncDisposable.Create(() => log.Add("view model disposed"));

// uiContext is the SynchronizationContext captured on the UI thread.
await AsyncDisposable.Context(viewModel, uiContext).DisposeAsync(); // Post: completes once the UI thread has disposed it
```
<!-- endSnippet -->

### Scoping a lock

`semaphore.Lock()` and `LockAsync()` wait for a `SemaphoreSlim` and return a scope that releases it; `ReadLock()`, `UpgradeableReadLock()` and `WriteLock()` do the same for a `ReaderWriterLockSlim`. A scope is a struct that allocates nothing: dispose it once, through `using`:

<!-- snippet: SemaphoreLock -->
```cs
using var semaphore = new SemaphoreSlim(1, 1);
var countInScope = -1;
using (await semaphore.LockAsync())
{
    countInScope = semaphore.CurrentCount; // 0 while the scope holds it
}
// semaphore.CurrentCount == 1
```
<!-- endSnippet -->

<!-- snippet: ReaderWriterLock -->
```cs
using var cacheLock = new ReaderWriterLockSlim();
var readLockHeld = false;
using (cacheLock.ReadLock())
{
    readLockHeld = cacheLock.IsReadLockHeld; // true while the scope holds it
}
using (cacheLock.WriteLock())
{
    // update the cache
}
// cacheLock.IsWriteLockHeld == false
```
<!-- endSnippet -->

### Bridging sync and async disposal

`AsyncDisposable.Adapt` wraps an `IDisposable` as an `IAsyncDisposable`, for code that only takes the async form. It disposes the resource once; `Adapt(disposable, idempotent: false)` disposes it on every call:

<!-- snippet: Adapt -->
```cs
var log = new List<string>();
IDisposable legacy = Disposable.Create(() => log.Add("legacy disposed"));

await using (AsyncDisposable.Adapt(legacy))
{
    // code that takes an IAsyncDisposable
}

// log: "legacy disposed"
```
<!-- endSnippet -->

`DisposeAsyncIfAvailable()` calls `DisposeAsync` when the instance implements `IAsyncDisposable`, `Dispose` otherwise, and does nothing for `null`:

<!-- snippet: DisposeAsyncIfAvailable -->
```cs
// stream is an open Stream, such as a FileStream.
IDisposable? resource = stream;
await resource.DisposeAsyncIfAvailable(); // a Stream is IAsyncDisposable: DisposeAsync runs

resource = null;
await resource.DisposeAsyncIfAvailable(); // null: nothing happens
```
<!-- endSnippet -->

### Disposing every item in a list

`DisposeAll()` disposes each `IDisposable` in an `IEnumerable<object?>` in reverse order, also one that implements `IAsyncDisposable`, such as a `Stream`, and skips `null` and every other item. Its errors and `ignoreExceptions` work as a collection's do. An item that implements only `IAsyncDisposable` isn't disposed: once the other items are disposed, `DisposeAll()` throws `InvalidOperationException` for it, which `ignoreExceptions: true` doesn't ignore:

<!-- snippet: DisposeAll -->
```cs
var log = new List<string>();
object?[] items =
[
    Disposable.Create(() => log.Add("first")),
    "not disposable",
    null,
    Disposable.Create(() => log.Add("second")),
];

items.DisposeAll(); // log: "second", "first": items that aren't IDisposable are skipped
```
<!-- endSnippet -->

`DisposeAllAsync()` calls `DisposeAsync` on an item that implements `IAsyncDisposable` and `Dispose` on any other `IDisposable`, also in reverse order:

<!-- snippet: DisposeAllAsync -->
```cs
var log = new List<string>();
// stream is an open Stream, such as a FileStream.
object[] items = [Disposable.Create(() => log.Add("disposed")), stream];

await items.DisposeAllAsync(); // DisposeAsync for the stream, then Dispose for the rest
```
<!-- endSnippet -->

## API overview

### Factories

| Type | Members |
| --- | --- |
| `Disposable` | `Empty`, `Create`, `CreateValue`, `Aggregate`, `Serial`, `SingleAssignment`, `Guard`, `Cancellation`, `Flag`, `Collection`, `Context` and `Shared` |
| `AsyncDisposable` | `Empty`, `Create`, `CreateValue`, `Adapt`, `Aggregate`, `Serial`, `SingleAssignment`, `Guard`, `Cancellation`, `Collection`, `Context` and `Shared` |
| `SharedReference` | `Create(value)` for an `IDisposable`, and `Create(value, onRelease)` for any value |
| `AsyncSharedReference` | `Create(value)` for an `IAsyncDisposable`, and `Create(value, onRelease)` for any value |

### Types

| Type | Role |
| --- | --- |
| `DisposableEmpty`, `AsyncDisposableEmpty` | Do nothing; `Singleton` is the instance `Empty` returns |
| `DisposableAction`, `AsyncDisposableAction` | Run a delegate on dispose, once unless `idempotent` is `false` |
| `DisposableAggregate`, `AsyncDisposableAggregate` | Hold one replaceable resource; the obsolete `IDisposableAggregate` and `IAsyncDisposableAggregate` |
| `DisposableSerial`, `AsyncDisposableSerial` | Hold one resource and dispose the previous one when it is replaced |
| `DisposableSingleAssignment`, `AsyncDisposableSingleAssignment` | Take one resource, once |
| `DisposableGuard<T>`, `AsyncDisposableGuard<T>` | Dispose a resource unless `Release()` hands ownership on |
| `DisposableCancellation`, `AsyncDisposableCancellation` | Cancel a token on dispose |
| `DisposableFlag` | Only records that it was disposed |
| `DisposableCollection`, `AsyncDisposableCollection` | Dispose their items in reverse order; the obsolete `IDisposableCollection` and `IAsyncDisposableCollection` |
| `DisposableContext` | Disposes a resource on a `SynchronizationContext` |
| `AsyncDisposableContext` | Posts the async disposal of a resource to a `SynchronizationContext` |
| `ValueDisposable<TState>`, `AsyncValueDisposable<TState>` | An allocation-free struct that runs a static delegate with its state on every dispose |
| `IDisposalStatus` | Exposes `IsDisposed` |
| `AsyncDisposableAdapter` | An `IAsyncDisposable` over an `IDisposable` |
| `SharedReferenceLease<T>`, `AsyncSharedReferenceLease<T>` | A lease on a shared resource: `Value`, `IsActive`, `AddReference` and `TryAddReference` |
| `SharedReferenceOwner<T>`, `AsyncSharedReferenceOwner<T>` | The reference count behind the leases; `ISharedReferenceOwner<T>` and `IAsyncSharedReferenceOwner<T>` |

### Extensions

| Type | Members |
| --- | --- |
| `DisposableExtensions` | `DisposeAsyncIfAvailable()`, `AsSharedReference()` and `DisposeAll()` |
| `AsyncDisposableExtensions` | `AsAsyncSharedReference()` and `DisposeAllAsync()` |
| `SemaphoreSlimExtensions` | `Lock()` and `LockAsync()` |
| `ReaderWriterLockSlimExtensions` | `ReadLock()`, `UpgradeableReadLock()` and `WriteLock()` |

## Thread safety

Every class runs its cleanup at most once, even when several threads dispose it at once, unless an action or adapter is created with `idempotent: false`. The aggregate, serial and single-assignment holders swap their resource atomically, and a collection's members all take one lock, its enumeration walking a snapshot. A resource handed to a holder after it is disposed is disposed at once. A lease's `AddReference`, `TryAddReference` and `Dispose` are safe from any thread, and the release action runs once, on the thread that releases the last lease. The structs (`ValueDisposable<TState>`, `AsyncValueDisposable<TState>` and the lock scopes) run their action on every dispose: dispose them once.

## Project

- [CONTRIBUTING.md](CONTRIBUTING.md): how to set up, build, test and propose a change.
- [Issues](https://github.com/NCodeGroup/NCode.Disposables/issues): report a bug or ask for a feature.
- [SECURITY.md](SECURITY.md): report a vulnerability privately, never in a public issue, and verify a package's provenance.
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md), [GOVERNANCE.md](GOVERNANCE.md) and [ROADMAP.md](ROADMAP.md): how we work together, who decides, and where the project is going.
- [docs/architecture.md](docs/architecture.md) and [docs/assurance-case.md](docs/assurance-case.md): how the project is built, and why it meets its security requirements.
- [docs/decisions/](docs/decisions/README.md): the decisions behind the design, the project's and the template's.
- [CHANGELOG.md](CHANGELOG.md): what changed in each release, from 1.0.0 on.
- [LICENSE.txt](LICENSE.txt): Apache License 2.0, copyright NCode Group.
