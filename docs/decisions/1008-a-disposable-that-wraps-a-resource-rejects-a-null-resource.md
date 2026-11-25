# 1008. A disposable that wraps a resource rejects a null resource

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

A factory or constructor that wraps one resource and disposes it, such as `Disposable.Context`,
`AsyncDisposable.Context`, `Disposable.Guard` and `AsyncDisposable.Guard`, throws `ArgumentNullException` for a `null`
resource. An adapter over `null` disposes nothing, so the mistake that passed `null` shows up only as a leak.
`Disposable.Aggregate`, `AsyncDisposable.Aggregate`, `Disposable.Serial` and `AsyncDisposable.Serial` hold an
optional resource, where `null` means an empty slot. A change to validation a consumer can observe ships only in a
major version ([ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)).

## Decision

**A factory or constructor that wraps one resource it must dispose throws `ArgumentNullException` for a `null`
resource; a holder whose slot may be empty accepts `null`.**

1. `AsyncDisposable.Adapt`, `AsyncDisposable.AdaptRepeatable` and the `AsyncDisposableAdapter` constructor throw
   `ArgumentNullException` for a `null` disposable.
2. The aggregate and serial holders accept `null` as an empty slot.

## Options considered

- Accept `null` and dispose nothing: rejected, because it hides the caller's mistake until the resource leaks.
- Throw `ArgumentNullException`: chosen; it matches the other wrappers and fails where the mistake is made.

## Consequences

- A caller that passes `null` to the adapter learns it at the call, not from a leaked resource.
- The check changes validation a consumer can observe, so it ships in 8.0.0, a major version.
- A wrapper added later validates its resource the same way.

## References

- `src/NCode.Disposables/AsyncDisposableAdapter.cs`, `src/NCode.Disposables/AsyncDisposable.cs`
- [#76](https://github.com/NCodeGroup/NCode.Disposables/issues/76)
