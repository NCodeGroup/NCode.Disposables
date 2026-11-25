# 1005. Disposables are concrete sealed types that report their disposal status

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

The factories return `IDisposable` or a per-type interface, which hides members such as `IsDisposed`. An interface
can't gain a member without breaking its implementers, so each addition to one costs a major version.

## Decision

**Every factory returns the concrete sealed type it creates, and every class that keeps disposal state implements
`IDisposalStatus`.**

1. `IDisposableAggregate`, `IAsyncDisposableAggregate`, `IDisposableCollection` and `IAsyncDisposableCollection` are
   `[Obsolete]` with `NCDISP002` and are deleted in 8.0.
2. New types are named `Disposable<Role>` or `AsyncDisposable<Role>`, which never collide with
   `System.Reactive.Disposables` (`SerialDisposable`, `CancellationDisposable`, `BooleanDisposable`, `ICancelable`).
3. Disposal hand-offs take no `CancellationToken`, because disposal must finish.
4. The `AsyncDisposable.Create` state overloads take only `Func<…, ValueTask>`, because an `Action<…>` overload would
   bind a `Task`-returning lambda and drop its `Task`.

## Options considered

- An interface per holder: rejected, because an interface can't grow without breaking its implementers.
- Return `IDisposable`: rejected, because it hides members such as `IsDisposed`.
- Concrete sealed types: chosen; System.Reactive does this.

## Consequences

- A caller reads `IsDisposed` and every other member of what a factory returns, and a later release adds members
  without a breaking change.
- The four obsolete interfaces keep compiling in 7.x with a warning, and 8.0 removes them.

## References

- [#65](https://github.com/NCodeGroup/NCode.Disposables/issues/65)
