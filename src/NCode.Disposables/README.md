# NCode.Disposables

[![CI](https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/ci.yml?query=branch%3Amain) [![Target framework](https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2FNCodeGroup%2FNCode.Disposables%2Fmain%2Fsrc%2FNCode.Disposables%2FNCode.Disposables.csproj&query=%2F%2FTargetFramework%20%7C%20%2F%2FTargetFrameworks&label=target&logo=dotnet&color=512BD4)](https://www.nuget.org/packages/NCode.Disposables#supportedframeworks-body-tab) [![License](https://img.shields.io/github/license/NCodeGroup/NCode.Disposables)](https://github.com/NCodeGroup/NCode.Disposables/blob/main/LICENSE.txt)

Building blocks for `IDisposable` and `IAsyncDisposable` in .NET: run an action on dispose, dispose many resources in reverse order, hold a swappable, serial or single-assignment resource, guard a resource until ownership moves on, cancel on dispose, share a resource through reference-counted leases, scope a lock, and dispose on a synchronization context.

## Installation

```shell
dotnet add package NCode.Disposables
```

The package depends on no other package. Its target frameworks are the ones its `.csproj` declares; they are listed on [nuget.org's Frameworks tab](https://www.nuget.org/packages/NCode.Disposables#supportedframeworks-body-tab) and shown by the badge above. [CHANGELOG.md](https://github.com/NCodeGroup/NCode.Disposables/blob/main/CHANGELOG.md) records when a framework is added or dropped.

## Example

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

By default the action runs on the first `Dispose` only; `Disposable.CreateRepeatable` runs it on every call:

<!-- snippet: ActionDisposable -->
```cs
var count = 0;

var once = Disposable.Create(() => count++);
once.Dispose();
once.Dispose(); // count == 1: the action runs on the first call only

var everyTime = Disposable.CreateRepeatable(() => count++);
everyTime.Dispose();
everyTime.Dispose(); // count == 3: the action runs on every call
```
<!-- endSnippet -->

`SharedReference.Create` puts a resource behind a reference count, and the resource is disposed when the last lease is disposed:

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

## Learn more

- [README and samples](https://github.com/NCodeGroup/NCode.Disposables#readme): every type, with a tested sample for each
- [API overview](https://github.com/NCodeGroup/NCode.Disposables#api-overview)
- [Thread safety](https://github.com/NCodeGroup/NCode.Disposables#thread-safety)
- [Changelog](https://github.com/NCodeGroup/NCode.Disposables/blob/main/CHANGELOG.md)
- [Report a vulnerability or verify a package](https://github.com/NCodeGroup/NCode.Disposables/blob/main/SECURITY.md)
