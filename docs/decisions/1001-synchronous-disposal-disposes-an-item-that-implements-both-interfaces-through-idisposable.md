# 1001. Synchronous disposal disposes an item that implements both interfaces through IDisposable

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

`DisposeAll` and `DisposableCollection.Dispose` run synchronously. Many .NET types implement both `IDisposable` and
`IAsyncDisposable`, such as `Stream`, `DbConnection` and `ServiceProvider`. A type that implements `IDisposable`
promises that `Dispose` releases its resources. `Disposable.Collection` takes `IDisposable` items, so a `Stream`
compiles in, and a synchronous disposal that rejects it fails on a type the API accepts.

## Decision

**In synchronous disposal, an item that implements `IDisposable` is disposed through `Dispose`, whether or not it also
implements `IAsyncDisposable`.**

1. An item that implements only `IAsyncDisposable` is not disposed. `DisposeAll` disposes the other items, then throws
   `InvalidOperationException` for it. `ignoreExceptions` never ignores that exception, because ignoring it leaks the
   item silently.
2. `ignoreExceptions` applies to the exceptions `Dispose` throws, for every item.
3. Asynchronous disposal (`DisposeAllAsync`, `AsyncDisposableCollection`, `DisposeAsyncIfAvailable`) prefers
   `DisposeAsync`.

## Options considered

- Throw for every `IAsyncDisposable` item: rejected, because it rejects most real resources.
- Block on `DisposeAsync`: rejected, because sync-over-async can deadlock on a synchronization context, and
  `build/BannedSymbols.txt` bans blocking on tasks.
- Skip async-only items silently: rejected, because it leaks them.
- `Dispose` first: chosen. Microsoft.Extensions.DependencyInjection's synchronous scope `Dispose` does the same and
  throws `InvalidOperationException` only for an async-only service.

## Consequences

- A resource whose `DisposeAsync` does more than `Dispose`, such as an asynchronous flush, belongs in
  `AsyncDisposable.Collection`.
- The exception message for an async-only item names `DisposeAllAsync`.

## References

- `src/NCode.Disposables/DisposableExtensions.cs`, `src/NCode.Disposables/DisposableCollection.cs`
- [#53](https://github.com/NCodeGroup/NCode.Disposables/issues/53),
  [#54](https://github.com/NCodeGroup/NCode.Disposables/issues/54)
- [Implement a DisposeAsync method](https://learn.microsoft.com/dotnet/standard/garbage-collection/implementing-disposeasync)
