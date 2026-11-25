# Assurance Case

This page states NCode.Disposables's security requirements, the threats it considers, and why the design and the way it
is built meet them. [architecture.md](architecture.md) describes the components;
[README.md](../README.md#thread-safety) is the user-facing summary.

## Security Requirements

[//]: # "Maintainers: list what the project is expected to do as R1, R2, ..., and what users must not rely on."

What the library is expected to do:

1. **R1.** Each disposable class runs its cleanup at most once, even when several threads dispose it at once; only an
   action or adapter from `CreateRepeatable` or `AdaptRepeatable` runs it on every call. The structs are covered below.
2. **R2.** A shared resource's release action runs once, on the thread that releases the last lease. Once the count
   reaches zero, `Value` and `AddReference` throw `ObjectDisposedException` and `TryAddReference` returns `false`, so a
   released resource is never handed out again.
3. **R3.** A resource handed to a holder or collection after it is disposed is disposed instead of kept
   ([ADR-1004](decisions/1004-a-resource-handed-to-a-disposed-holder-is-disposed.md)); `AsyncDisposableAggregate`'s
   setter and `AsyncDisposableCollection.Add` throw `ObjectDisposedException` instead, and `SetDisposableAsync` and
   `AddAsync` dispose it.
4. **R4.** Disposing a collection, or calling `DisposeAll`, disposes every item in reverse order even when one throws,
   then rethrows the one exception or throws an `AggregateException` holding all of them. The
   members named for ignoring exceptions, such as `CollectionIgnoringExceptions` and
   `DisposeAllIgnoringExceptions`, also dispose every item, and ignore what the items throw.
5. **R5.** A factory, constructor or extension rejects a `null` delegate, resource to wrap, collection or item with
   `ArgumentNullException` ([ADR-1008](decisions/1008-a-disposable-that-wraps-a-resource-rejects-a-null-resource.md));
   only these take `null`: the aggregate and serial holders, meaning they hold nothing; the `SharedReferenceOwner<T>`
   and `AsyncSharedReferenceOwner<T>` constructors, a `null` value, which they hand to their release callback; the
   `SharedReferenceLease<T>` and `AsyncSharedReferenceLease<T>` constructors, a `null` owner, which makes an inactive
   lease, as `default` does; the `Create` overloads that take arguments, the `CreateValue` factories and the
   `ValueDisposable<TState>` and `AsyncValueDisposable<TState>` constructors, a `null` argument or state, which they
   pass to the action; `DisposeAsyncIfAvailable()`, which does nothing for a `null` instance; and `DisposeAll()`,
   `DisposeAllIgnoringExceptions()`, `DisposeAllAsync()` and `DisposeAllIgnoringExceptionsAsync()`, which skip a `null`
   item. A collection's constructor, and so its `Collection` and `CollectionIgnoringExceptions` factories, rejects an
   initial collection that holds a `null` item with `ArgumentException`. A second assignment of a single-assignment
   holder throws `InvalidOperationException`, and so does `Release()` on a guard already released.
6. **R6.** `DisposableContext` calls `OperationCompleted` for each `OperationStarted`, also when the context's `Post`
   throws, and rethrows.
7. **R7.** The package consumers install is built from this repository's reviewed code, and they can check that.

What it does not do, and users must not rely on: dispose anything the caller never disposes, since it has no
finalizers; make a second dispose of a struct safe, since a lease, a value disposable or a lock scope releases again on
every dispose
([ADR-1006](decisions/1006-value-disposables-and-lock-scopes-are-non-idempotent-structs.md)); make the caller's own
resource thread-safe; or stop a cleanup delegate the caller supplies from throwing, blocking or deadlocking.

## Threat Model

[//]: # "Maintainers: add a table (Threat, In scope, Countered by) and state the trust boundary."

| Threat | In scope | Countered by |
| --- | --- | --- |
| Cleanup runs twice when threads race to dispose | Yes | R1, R2: `Interlocked.Exchange` and `Interlocked.CompareExchange` take the action or state once |
| A released shared resource is used or leased again | Yes | R2: a compare-and-swap loop on the count refuses a lease at zero |
| A resource handed to a disposed holder leaks | Yes | R3 |
| One failing item leaves the rest undisposed | Yes | R4 |
| A `null` argument or a second assignment corrupts a holder's state | Yes | R5 |
| A context's count of pending operations is left unbalanced | Yes | R6 |
| A tampered or substituted package | Yes | R7, below |
| A struct disposed twice | No | Documented misuse (ADR-1006, the lease's remarks) |
| A cleanup delegate that throws, blocks or misbehaves | No | It runs with the caller's privileges; the library rethrows what it throws, except from a member named for ignoring exceptions |
| A caller that never disposes | No | No finalizer to rely on |

**Trust boundary.** The library runs inside the caller's process with the caller's privileges, and every input is an
object the caller passes: delegates, disposables, cancellation tokens, locks and synchronization contexts. It parses no
data, does no I/O, starts no threads, has no `unsafe` code and depends on no other package, so its attack surface is its
public API.

## Secure Design Principles Applied

[//]: # "Maintainers: say how the design applies economy of mechanism, fail-safe defaults, complete mediation, least
privilege and open design."

- **Economy of mechanism:** one assembly with no package dependency, no I/O, no threads and no `unsafe` or native
  code; each disposable is a small sealed type that does one job
  ([ADR-1005](decisions/1005-disposables-are-concrete-sealed-types-that-report-their-disposal-status.md)).
- **Fail-safe defaults:** a disposable runs its cleanup once unless a factory named for the other behavior, such as
  `CreateRepeatable`, creates it
  ([ADR-1007](decisions/1007-behavior-is-chosen-by-a-factory-name-not-a-flag-parameter.md)); a resource handed to a
  disposed holder is disposed, not dropped (R3); a `null` resource to wrap is rejected, not ignored (R5).
- **Complete mediation:** every member of a collection takes one lock and enumeration walks a snapshot; the holders
  swap their resource with atomic operations; every lease goes through its owner's atomic count, which refuses a new
  lease at zero (R1, R2).
- **Least common mechanism:** the library keeps no mutable static state, only immutable singletons, sentinels and
  cached delegates, so no two instances share state; it awaits with `ConfigureAwait(false)`, so it never resumes on
  the caller's synchronization context.
- **Open design:** the code, the tests, the decision records and this case are public.

## Common Weaknesses Countered

[//]: # "Maintainers: list the CWE ids the design counters, each with the requirement that counters it."

- **CWE-675, multiple operations on a resource in a single-operation context:** R1, R2.
- **CWE-416, use after free, as use of a released shared resource:** R2.
- **CWE-362 / CWE-367, race condition and time-of-check to time-of-use:** R1, R2, R3.
- **CWE-772 / CWE-404, missing release of a resource and improper shutdown:** R3, R4.
- **CWE-460, improper cleanup on a thrown exception:** R4, R6.
- **CWE-476, null pointer dereference:** R5.
- **CWE-494 / CWE-829, untrusted code or inclusion:** R7.

## Evidence

[//]: # "Maintainers: add the tests and the coverage the project keeps."

- **Tests (R1–R6):** xUnit tests of every type, 723 in all, cover a second dispose (such as
  `Dispose_Twice_DisposesOnce`), concurrent disposal and assignment from several threads (such as
  `Dispose_ConcurrentCalls_DisposesOnlyOnce` and `Dispose_ConcurrentWithAdd_DisposesEveryItemOnce`), `null` arguments,
  a resource handed to a disposed holder, and a context whose `Post` throws
  (`Dispose_Posting_WhenPostThrows_CompletesOperationAndPropagatesException`). FsCheck property tests, replayed from a
  fixed seed, check R1, R2 and R4 for any number of items, dispose calls and leases
  (`tests/NCode.Disposables.Tests/DisposablePropertyTests.cs`). Measured on 2026-11-25, they cover 97.9% of the
  statements and 96.0% of the branches; the gate doesn't enforce a minimum. `publish.yml` runs them on the tagged
  commit before each release.
- **Gate:** `./build/gate.ps1` checks that the README samples match their compiled tests
  ([ADR-1000](decisions/1000-readme-samples-are-compiled-tests-that-markdownsnippets-copies-into-the-readmes.md)), then
  runs `./build/dod.ps1`, which builds in Release with warnings as errors
  ([ADR-0100](decisions/project-template/0100-warnings-are-errors-in-every-project.md)), runs the analyzers and the
  banned-API list in `build/BannedSymbols.txt`
  ([ADR-0102](decisions/project-template/0102-disallowed-apis-are-banned-at-build-time.md)), and runs the tests; CI runs
  `./build/gate.ps1` on every pull request.
- **Static analysis:** with CodeQL's default setup configured and analyzing at least one language, the repository
  ruleset blocks a pull request that adds a CodeQL alert at warning level or above, or a security alert of any severity
  ([ADR-0005](decisions/project-template/0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
  [ADR-0030](decisions/project-template/0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md)).
- **Review:** every change reaches the default branch through a pull request with passing CI and every conversation
  resolved, and the ruleset refuses force pushes and deletion
  ([ADR-0005](decisions/project-template/0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
  [ADR-0014](decisions/project-template/0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md)).
  NCode Review reviews each pull request and approves a clean one.
- **Dependencies:** pinned, and moved by reviewed updates
  ([ADR-0008](decisions/project-template/0008-external-inputs-are-pinned-and-kept-current.md)) that wait three days for
  a new version ([ADR-0009](decisions/project-template/0009-new-dependency-versions-wait-a-cooldown.md)). Dependabot proposes the updates. Central package management keeps one version of each package
  ([ADR-0101](decisions/project-template/0101-central-package-management.md)).
- **Release integrity (R7):** packages are deterministic and source-linked
  ([ADR-0130](decisions/project-template/0130-source-link-and-symbol-packages.md)). `publish.yml` builds them from a tagged commit the default branch holds and pushes them through
  nuget.org trusted publishing with no stored key, after a maintainer approves
  ([ADR-0135](decisions/project-template/0135-packages-are-published-from-a-github-release-through-trusted-publishing.md),
  [ADR-0137](decisions/project-template/0137-the-release-workflow-starts-package-publishing.md)), and each release
  carries signed provenance and an SBOM ([ADR-0139](decisions/project-template/0139-releases-carry-build-provenance.md),
  [ADR-0151](decisions/project-template/0151-release-follow-ups-are-jobs-publish-calls.md)). [SECURITY.md](../SECURITY.md#verify-a-package) says how to verify a package.
- **Response:** [SECURITY.md](../SECURITY.md) defines private reporting, coordinated disclosure and credit.

## Security Reviews

[//]: # "Maintainers: record each security review, newest first: its date, who did it, what it covered (the security requirements and the trust boundary above) and what it found. The OpenSSF Best Practices gold level asks for one in the last five years."

None recorded yet.
