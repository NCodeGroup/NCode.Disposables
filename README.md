[![ci](https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/main.yml/badge.svg)](https://github.com/NCodeGroup/NCode.Disposables/actions)
[![Nuget](https://img.shields.io/nuget/v/NCode.Disposables.svg)](https://www.nuget.org/packages/NCode.Disposables/)

# NCode.Disposables
This library provides a set of useful `IDisposable` and `IAsyncDisposable` implementations. For brevity,
the `IDisposable` interface is used in the examples below, but the same implementations are available for
`IAsyncDisposable` as well.

## DisposeAsync If Available
Provides an extension method that will call the `DisposeAsync` method if it is available on the `IDisposable` resource.
If the `DisposeAsync` method is not available, then the `Dispose` method is called instead.

```csharp
async ValueTask Example()
{
    IDisposable resource = CreateSomeResource();
    await resource.DisposeAsyncIfAvailable();
}
```

## Disposable Async Adapter
Provides an `IAsyncDisposable` adapter that wraps an `IDisposable` resource and forwards the `DisposeAsync` method to the `Dispose` method of the underlying resource.

```csharp
async ValueTask Example()
{
    IDisposable resource = CreateSomeResource();
    IAsyncDisposable asyncDisposable = AsyncDisposable.Adapt(resource);
    await asyncDisposable.DisposeAsync();
}
```

## Disposable Empty
Provides an implementation of `IDisposable` that is empty and performs nothing (i.e. nop) when `Dispose` is called.

```csharp
void Example()
{
    // using singleton instance:
    IDisposable disposable1 = Disposable.Empty;
    disposable1.Dispose();
    
    // using new instance:
    IDisposable disposable2 = new DisposableEmpty();
    disposable2.Dispose();
}
```

## Disposable Action
Provides an `IDisposable` implementation that will invoke an `Action` delegate when `Dispose` is called. If the `Dispose` method is called multiple times, the underlying action is invoked only once on the first call.

```csharp
void Example()
{
    IDisposable disposable = Disposable.Create(() =>
        Console.WriteLine("I am disposed."));
    // ...
    disposable.Dispose();
}
```

## Disposable Aggregate
Provides an `IDisposable` implementation that contains (i.e. aggregates) a property to another underlying `IDisposable` resource. The underlying resource may be assigned or retrieved multiple times as long as the aggregate hasn't been disposed yet.

```csharp
void Example()
{
    IDisposable resource = CreateSomeResource();
    // ...
    var aggregate = Disposable.Aggregate(resource);
    // ...
    if (SomeCondition) {
        var previous = aggregate.Disposable;
        // ...
        aggregate.Disposable = CreateSomeOtherResource();
    }
    aggregate.Dispose();
    // ...
}
```

## Disposable Collection
Provides an `IDisposable` collection that contains other `IDisposable` resources that will be disposed when the collection itself is disposed. The items in the collection are disposed in reverse order that they are added. The items in the collection may be added, removed, or cleared at any time before the collection is disposed itself.

```csharp
void Example()
{
    IDisposable resource1 = CreateResource1();
    IDisposable resource2 = CreateResource2();
    // ...
    var collection = Disposable.Collection(resource1, resource2);
    // ...
    var resource3 = CreateResource3();
    if (!collection.Contains(resource3))
        collection.Add(resource3);
    // ...
    collection.Dispose();
}
```

## Disposable Reference Count
Provides an `IDisposable` implementation that uses reference counting and only disposes the underlying resource when all the leases have been disposed (i.e. reference count is zero).
The leases are not idempotent safe and consumers must take care to not dispose the same lease multiple times otherwise the underlying resource will be disposed prematurely.
One solution for consumers is to assign the lease to `default` after disposing it.

```csharp
void Example()
{
    IDisposable resource = CreateResource();
    // ...
    var firstLease = SharedReference.Create(resource);
    var secondLease = first.AddReference();
    var thirdLease = second.AddReference();
    // ...
    firstLease.Value.DoSomething();
    firstLease.Dispose();
    firstLease = default;
    // ...
    secondLease.Value.DoSomethingElse();
    secondLease.Dispose();
    secondLease = default;
    // ...
    thirdLease.Value.DoSomethingElse();
    thirdLease.Dispose();
    thirdLease = default;
    // the resource will be disposed here after
    // all 3 leases have been disposed...
}
```

## Disposable Context
Provides an `IDisposable` implementation that will invoke the `Dispose` method of an underlying resource using an asynchronous or synchronous operation from a `SynchronizationContext`. If the `Dispose` method is called multiple times, the underlying resource is only disposed once on the first call.

```csharp
void Example()
{
    IDisposable resource = CreateResource();
    // ...
    const bool async = true;
    var context = SynchronizationContext.Current;
    var disposable = Disposable.Context(resource, context, async);
    // ...
    disposable.Dispose()
}
```

## Feedback
Please provide any feedback, comments, or issues to this GitHub project [here][issues].

[issues]: https://github.com/NCodeGroup/NCode.Disposables/issues

## Release Notes
 
* v1.0.0 - Initial release
* v1.0.1 - Unknown changes
* v2.0.1 - Unknown changes
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