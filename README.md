<p align="center">
  <img src="NCodeGroup.png" alt="NCode" width="96" height="96">
</p>

<h1 align="center">NCode.Disposables</h1>

<p align="center">
  Building blocks for IDisposable and IAsyncDisposable in .NET: run an action on dispose, dispose many resources in reverse order, swap the resource an aggregate holds, share a resource through reference-counted leases, and dispose on a synchronization context.
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/NCode.Disposables/"><img alt="NuGet version" src="https://img.shields.io/nuget/v/NCode.Disposables?logo=nuget"></a>
  <a href="https://www.nuget.org/packages/NCode.Disposables/"><img alt="NuGet downloads" src="https://img.shields.io/nuget/dt/NCode.Disposables?logo=nuget"></a>
  <a href="https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/ci.yml?query=branch%3Amain"><img alt="CI" src="https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/ci.yml/badge.svg?branch=main"></a>
  <a href="https://www.nuget.org/packages/NCode.Disposables#supportedframeworks-body-tab"><img alt="Target framework" src="https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2FNCodeGroup%2FNCode.Disposables%2Fmain%2Fsrc%2FNCode.Disposables%2FNCode.Disposables.csproj&query=%2F%2FTargetFramework%20%7C%20%2F%2FTargetFrameworks&label=target&logo=dotnet&color=512BD4"></a>
  <a href="LICENSE.txt"><img alt="License" src="https://img.shields.io/github/license/NCodeGroup/NCode.Disposables"></a>
  <a href="https://scorecard.dev/viewer/?uri=github.com/NCodeGroup/NCode.Disposables"><img alt="OpenSSF Scorecard" src="https://api.scorecard.dev/projects/github.com/NCodeGroup/NCode.Disposables/badge"></a>
</p>

Cleanup in .NET often needs more than one `using` statement: undoing a registration, disposing several resources in the right order, handing one expensive resource to several owners, or disposing a UI object on its own thread. NCode.Disposables gives each of these a small type behind the `Disposable` and `AsyncDisposable` factories, with an `IAsyncDisposable` counterpart for everything except disposal on a synchronization context.

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

using (var resources = Disposable.Collection())
{
    resources.Add(Disposable.Create(() => log.Add("connection closed")));
    resources.Add(Disposable.Create(() => log.Add("lock released")));
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

`Disposable.Aggregate` holds one resource, or `null`, in its `Disposable` property, and disposes the resource it holds when it is disposed. Setting the property replaces the resource without disposing the old one. Once the aggregate is disposed, reading or setting the property throws `ObjectDisposedException`. `AsyncDisposable.Aggregate` does the same for an `IAsyncDisposable`:

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

### Disposing many resources together

`Disposable.Collection` is an `ICollection<IDisposable>` that disposes its items in reverse order, the last one added first, on its first `Dispose`; later calls do nothing, and an item added after that is never disposed. `Remove` and `Clear` take items out without disposing them:

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

Every item is disposed even when one throws. A single failure is rethrown as it is, several are thrown together as one `AggregateException`, and `ignoreExceptions: true` ignores them:

<!-- snippet: CollectionErrors -->
```cs
var log = new List<string>();
var resources = Disposable.Collection(
    ignoreExceptions: true,
    Disposable.Create(() => log.Add("first")),
    Disposable.Create(() => throw new InvalidOperationException("Failed to close.")),
    Disposable.Create(() => log.Add("third"))
);

resources.Dispose(); // log: "third", "first": the failure is ignored
```
<!-- endSnippet -->

An item that implements `IAsyncDisposable`, even one that also implements `IDisposable`, such as a `Stream`, makes the collection's `Dispose` throw `InvalidOperationException`, whatever `ignoreExceptions` says. Put such items in `AsyncDisposable.Collection`, which disposes its items with `DisposeAsync` when they support it.

## Project

- [CONTRIBUTING.md](CONTRIBUTING.md): how to set up, build, test and propose a change.
- [Issues](https://github.com/NCodeGroup/NCode.Disposables/issues): report a bug or ask for a feature.
- [SECURITY.md](SECURITY.md): report a vulnerability privately, never in a public issue, and verify a package's provenance.
- [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md), [GOVERNANCE.md](GOVERNANCE.md) and [ROADMAP.md](ROADMAP.md): how we work together, who decides, and where the project is going.
- [docs/architecture.md](docs/architecture.md) and [docs/assurance-case.md](docs/assurance-case.md): how the project is built, and why it meets its security requirements.
- [docs/decisions/](docs/decisions/README.md): the decisions behind the design, the project's and the template's.
- [CHANGELOG.md](CHANGELOG.md): what changed in each release, from 1.0.0 on.
- [LICENSE.txt](LICENSE.txt): Apache License 2.0, copyright NCode Group.
