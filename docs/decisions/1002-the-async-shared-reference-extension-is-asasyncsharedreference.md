# 1002. The async shared reference extension is AsAsyncSharedReference

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

`DisposableExtensions.AsSharedReference<T>()` (`where T : IDisposable`) and
`AsyncDisposableExtensions.AsSharedReference<T>()` (`where T : IAsyncDisposable`) share a name in two static classes of
one namespace. For a type that implements both interfaces, such as `MemoryStream`, both apply, and C# reports CS0121.

`OverloadResolutionPriorityAttribute` orders overloads only within one declaring type: checked on 2026-11-24 with
.NET SDK 10.0, it leaves CS0121 for extensions in two classes. One class can't hold both members (CS0111), and moving a
shipped member to another class breaks compiled callers. The package is validated against 5.3.0 and releases are
tracked, so a member is removed through `[Obsolete]` and deleted in the next major version
([ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)).

## Decision

**`AsyncDisposableExtensions` names its lease extension `AsAsyncSharedReference()`, and its `AsSharedReference()` is
obsolete until the next major version deletes it.**

1. `AsAsyncSharedReference()` returns `AsyncSharedReferenceLease<T>` for any `IAsyncDisposable`, including a type that
   implements both interfaces.
2. The obsolete member reports `NCDISP001` and keeps working.
3. Once it is deleted, `AsSharedReference()` names only the synchronous lease.
4. An extension name used for one disposal interface is never reused by another extension class for the other
   interface.

## Options considered

- `OverloadResolutionPriority`: rejected, because it has no effect across classes.
- Demote the async member to a plain static method: rejected, because it is a source break in a minor version.
- Rename the synchronous member: rejected, because its name is right and the rename breaks more callers.
- A distinct async name with `[Obsolete]` on the old one: chosen.

## Consequences

- Until the next major version, `AsSharedReference()` on a dual-interface type stays ambiguous: call
  `AsAsyncSharedReference()` or `SharedReference.Create`.
- A consumer that treats warnings as errors suppresses `NCDISP001` by its ID.
- The deletion is tracked as a follow-up issue.

## References

- `src/NCode.Disposables/AsyncDisposableExtensions.cs`, `src/NCode.Disposables/DisposableExtensions.cs`
- [#55](https://github.com/NCodeGroup/NCode.Disposables/issues/55)
- [Overload resolution priority](https://learn.microsoft.com/dotnet/csharp/language-reference/proposals/csharp-13.0/overload-resolution-priority)
