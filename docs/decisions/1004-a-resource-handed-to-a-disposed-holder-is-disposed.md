# 1004. A resource handed to a disposed holder is disposed

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

A holder (an aggregate, a serial, a single-assignment holder or a collection) can receive a resource after the holder is
disposed, typically from a producer that races disposal. Throwing forces every producer to catch the exception, and
keeping the resource leaks it.

## Decision

**A resource handed to a holder that is already disposed is disposed at once, and reading a disposed holder returns
`null`.**

1. The synchronous hand-offs (the `DisposableAggregate.Disposable` setter, the `DisposableSerial.Disposable` setter,
   the `DisposableSingleAssignment.Disposable` setter and `DisposableCollection.Add`) call `Dispose`.
2. The asynchronous hand-offs (`AsyncDisposableAggregate.SetDisposableAsync`, `AsyncDisposableSerial.SetDisposableAsync`,
   `AsyncDisposableSingleAssignment.SetDisposableAsync` and `AsyncDisposableCollection.AddAsync`) await `DisposeAsync`.
3. A synchronous hand-off to an asynchronous holder (the `AsyncDisposableAggregate.Disposable` setter and
   `AsyncDisposableCollection.Add`) throws `ObjectDisposedException` and doesn't take the resource.
4. A single-assignment holder's second assignment throws `InvalidOperationException`, before or after disposal.

## Options considered

- Throw `ObjectDisposedException`: rejected, because a producer racing disposal must catch it.
- Keep the resource: rejected, because it leaks.
- Fire-and-forget `DisposeAsync`: rejected, because its exceptions go unobserved.
- Dispose the resource: chosen; Rx's `SerialDisposable` and `CompositeDisposable` do this.

## Consequences

- The change is breaking and ships in 7.0.0; the README documents it.
- A producer that races disposal needs no `try`/`catch`, and the resource never outlives the holder.

## References

- [#65](https://github.com/NCodeGroup/NCode.Disposables/issues/65)
- `src/NCode.Disposables/DisposableAggregate.cs`, `src/NCode.Disposables/DisposableSerial.cs`,
  `src/NCode.Disposables/DisposableSingleAssignment.cs`, `src/NCode.Disposables/DisposableCollection.cs`
- `src/NCode.Disposables/AsyncDisposableAggregate.cs`, `src/NCode.Disposables/AsyncDisposableSerial.cs`,
  `src/NCode.Disposables/AsyncDisposableSingleAssignment.cs`, `src/NCode.Disposables/AsyncDisposableCollection.cs`
