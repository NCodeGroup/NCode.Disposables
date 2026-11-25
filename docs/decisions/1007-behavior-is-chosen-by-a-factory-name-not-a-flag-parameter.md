# 1007. Behavior is chosen by a factory's name, not a flag parameter

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

These members take a `bool` whose only job is to pick a behavior: `idempotent` on `Disposable.Create(Action, bool)`,
`AsyncDisposable.Create(Func<ValueTask>, bool)`, `AsyncDisposable.Create(Action, bool)`,
`AsyncDisposable.Adapt(IDisposable, bool)` and the `DisposableAction`, `AsyncDisposableAction` and
`AsyncDisposableAdapter` constructors; `async` on `Disposable.Context` and the `DisposableContext` constructor;
`ignoreExceptions` on both `Disposable.Collection` and both `AsyncDisposable.Collection` overloads that take it, the two
`DisposableCollection` and two `AsyncDisposableCollection` constructors, `DisposeAll` and `DisposeAllAsync` (19
members). A call such as `Create(action, false)` doesn't say what `false` picks, and the C# conventions rule out
behavior-selecting flag parameters. Releases are tracked, so a shipped member is removed through `[Obsolete]` and
deleted in the next major version
([ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)).

## Decision

**Each behavior a flag picked has a factory or method named for it, and the flag members are obsolete until 8.0 deletes
them.**

1. `Disposable.CreateRepeatable`, `AsyncDisposable.CreateRepeatable` and `AsyncDisposable.AdaptRepeatable` run the
   action or dispose the resource on every call; `Create`, `Adapt` and the constructors do it once.
2. `Disposable.PostingContext` disposes through the context's `Post`; `Disposable.Context` and the `DisposableContext`
   constructor use `Send`.
3. `CollectionIgnoringExceptions` on both factories, `DisposeAllIgnoringExceptions()` and
   `DisposeAllIgnoringExceptionsAsync()` ignore the exceptions items throw; `Collection`, the constructors,
   `DisposeAll()` and `DisposeAllAsync()` throw them.
4. Each type keeps a constructor without a flag, which picks the default behavior; the other behavior comes only from
   its factory.
5. The 19 flag members report `NCDISP003` and keep working.

## Options considered

- Keep the flags: rejected, because the call site doesn't say what the `bool` picks.
- An `enum` mode parameter: rejected, because it is still a behavior-selecting parameter.
- A public type per behavior, such as a repeatable action class: rejected, because it doubles the types for one private
  difference.
- A factory named for each behavior: chosen; it reads at the call site and adds no type.

## Consequences

- 7.x callers that pass a flag see `NCDISP003` and move to the named member, or suppress it by its ID.
- 8.0 deletes the 19 members, tracked as a follow-up issue.
- Constructors can't create the repeatable, posting or exception-ignoring forms.

## References

- `src/NCode.Disposables/Disposable.cs`, `src/NCode.Disposables/AsyncDisposable.cs`,
  `src/NCode.Disposables/DisposableExtensions.cs`, `src/NCode.Disposables/AsyncDisposableExtensions.cs`
- [#68](https://github.com/NCodeGroup/NCode.Disposables/issues/68)
- [ADR-1005](1005-disposables-are-concrete-sealed-types-that-report-their-disposal-status.md)
