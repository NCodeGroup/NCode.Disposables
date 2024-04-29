[![ci](https://github.com/NCodeGroup/NCode.Disposables/actions/workflows/main.yml/badge.svg)](https://github.com/NCodeGroup/NCode.Disposables/actions)
[![Nuget](https://img.shields.io/nuget/v/NCode.Disposables.svg)](https://www.nuget.org/packages/NCode.Disposables/)

# NCode.Disposables
This library provides a set of useful `IDisposable` and `IAsyncDisposable` implementations. For brevity,
the `IDisposable` interface is used in the examples below, but the same implementations are available for
`IAsyncDisposable` as well.

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
Provides an `IDisposable` implementation that uses reference counting and only disposes the underlying resource when all the references have been released (i.e. reference count is zero). Calling the `Dispose` method is idempotent safe and calling it multiple times has the same effect as calling it only once.

```csharp
void Example()
{
    IDisposable resource = CreateResource();
    // ...
    var first = Disposable.Shared(resource);
    var second = first.AddReference();
    var third = second.AddReference();
    // ...
    first.Dispose();
    second.Dispose();
    third.Dispose();
    // the resource will be disposed here after
    // all 3 references have been disposed...
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
