# 1006. Value disposables and lock scopes are non-idempotent structs

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

Hot paths such as taking a lock shouldn't allocate. The library's disposable classes are idempotent, but each one
allocates, so a lock scope built from them costs an allocation on every acquisition.

## Decision

**`ValueDisposable<TState>` and `AsyncValueDisposable<TState>` are `readonly struct`s that run their delegate on every
dispose of every copy; `default` does nothing; the lock-scope extensions return them with a cached static delegate.**
Disposing a scope twice releases the lock twice, as with `System.Threading.Lock.Scope` and `SemaphoreSlim.Release`.

## Options considered

- An idempotent class: rejected, because it allocates once per lock.
- A `ref struct`: rejected, because it can't be returned from an async method or held across `await`.
- A readonly struct: chosen; it doesn't allocate, and the cached static delegate keeps the lock-scope extensions
  allocation-free.

## Consequences

- Dispose a scope once, through `using`.
- A copy of a scope disposes independently of the original, so copying one is a mistake.
- A `default` value is a safe no-op.

## References

- [#65](https://github.com/NCodeGroup/NCode.Disposables/issues/65)
- <https://learn.microsoft.com/dotnet/api/system.threading.lock.enterscope>
