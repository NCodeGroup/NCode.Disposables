[![ci](https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/main.yml/badge.svg)](https://github.com/NCodeGroup/NCode.Disposables/actions)
[![Nuget](https://img.shields.io/nuget/v/NCode.Disposables.svg)](https://www.nuget.org/packages/NCode.Disposables/)

# NCode.Disposables

A comprehensive .NET library providing useful `IDisposable` and `IAsyncDisposable` implementations for common resource management patterns.

## Features

### Core Implementations
- **Empty Disposable** - No-op singleton for null object pattern
- **Action Disposable** - Execute a delegate on dispose (idempotent)
- **Aggregate Disposable** - Wrapper with replaceable underlying resource
- **Collection Disposable** - Manage multiple disposables with LIFO disposal order
- **Context Disposable** - Dispose on a specific `SynchronizationContext` (e.g., UI thread)
- **Shared Reference** - Reference-counted resource sharing with lease pattern

### Async Support
All implementations have async counterparts (`IAsyncDisposable`):
- `AsyncDisposable.Empty`, `Create()`, `Aggregate()`, `Collection()`, `Shared()`
- `AsyncDisposableAdapter` - Wraps `IDisposable` as `IAsyncDisposable`

### Extension Methods
- `DisposeAsyncIfAvailable()` - Calls `DisposeAsync` if available, otherwise `Dispose`
- `DisposeAll()` / `DisposeAllAsync()` - Dispose all items in a collection (LIFO order)
- `AsSharedReference()` - Convert a disposable to a shared reference with leasing

## Quick Reference

| Feature | Sync API | Async API |
|---------|----------|-----------|
| Empty (no-op) | `Disposable.Empty` | `AsyncDisposable.Empty` |
| Action callback | `Disposable.Create(action)` | `AsyncDisposable.Create(func)` |
| Aggregate wrapper | `Disposable.Aggregate(disposable)` | `AsyncDisposable.Aggregate(disposable)` |
| Collection | `Disposable.Collection(items)` | `AsyncDisposable.Collection(items)` |
| Shared reference | `SharedReference.Create(value)` | `AsyncSharedReference.Create(value)` |
| Context disposal | `Disposable.Context(disposable, ctx)` | — |
| Adapt sync→async | — | `AsyncDisposable.Adapt(disposable)` |

## Usage Examples

### Empty Disposable
```csharp
// Singleton instance - useful as default/placeholder
IDisposable disposable = Disposable.Empty;
disposable.Dispose(); // no-op
```

### Action Disposable
```csharp
// Execute cleanup logic on dispose (idempotent - runs only once)
IDisposable disposable = Disposable.Create(() => Console.WriteLine("Disposed!"));
disposable.Dispose(); // prints "Disposed!"
disposable.Dispose(); // no-op
```

### Aggregate Disposable
```csharp
// Wrapper allowing the underlying resource to be swapped
var aggregate = Disposable.Aggregate(initialResource);
aggregate.Disposable = newResource; // swap resource
aggregate.Dispose(); // disposes current resource
```

### Collection Disposable
```csharp
// Manage multiple disposables - disposed in reverse (LIFO) order
var collection = Disposable.Collection(resource1, resource2, resource3);
collection.Add(resource4);
collection.Remove(resource2); // removed items are NOT disposed
collection.Dispose(); // disposes: resource4, resource3, resource1
```

### Shared Reference (Reference Counting)
```csharp
// Share a resource with reference counting
IDisposable resource = CreateExpensiveResource();
var lease1 = SharedReference.Create(resource);
var lease2 = lease1.AddReference();
var lease3 = lease2.AddReference();

lease1.Value.DoWork();
lease1.Dispose();

lease2.Value.DoWork();
lease2.Dispose();

lease3.Value.DoWork();
lease3.Dispose(); // resource disposed here (ref count = 0)
```

### Context Disposable
```csharp
// Dispose on a specific SynchronizationContext (e.g., UI thread)
var context = SynchronizationContext.Current;
var disposable = Disposable.Context(uiResource, context, async: false);
disposable.Dispose(); // runs on the context's thread
```

### Async Adapter
```csharp
// Wrap IDisposable as IAsyncDisposable
IDisposable syncResource = CreateResource();
IAsyncDisposable asyncResource = AsyncDisposable.Adapt(syncResource);
await asyncResource.DisposeAsync();
```

### Extension Methods
```csharp
// Dispose async if available, otherwise sync
await disposable.DisposeAsyncIfAvailable();

// Dispose all items in a collection (LIFO order)
var items = new object[] { resource1, resource2, nonDisposable, resource3 };
items.DisposeAll(); // disposes only IDisposable items, in reverse order

// Convert to shared reference
using var lease = myDisposable.AsSharedReference();
```

## Feedback

Please provide any feedback, comments, or issues to this GitHub project [here][issues].

[issues]: https://github.com/NCodeGroup/NCode.Disposables/issues

## Release Notes

* v1.0.0 - Initial release
* v2.0.2 - Port to .NET Core/Standard
* v3.0.0 - Port to .NET 8.0 and refactor shared reference implementation
* v3.0.1 - Updated xml documentation
* v3.1.0 - Split ISharedReference into ISharedReferenceScope and ISharedReferenceProvider
* v4.0.0 - Revert the split
* v4.1.0 - Added async support
* v4.2.0 - Added async adapter
* v4.3.0 - Added DisposeAsyncIfAvailable extension. Added `idempotent` option to certain methods.
* v4.4.0 - Refactored the `idempotent` option to use function overloads.
* v5.0.0 - Refactored shared references/leases to use structs
* v5.0.1 - Removing dead code
* v5.0.2 - Removing more dead code
* v5.1.0 - Allow creating of async shared references without requiring async callsite
* v5.1.1 - Minor resharper cleanup
* v5.2.1 - Collections can be any object instead of just IDisposable or IAsyncDisposable
* v5.3.0 - .NET 10 upgrade
