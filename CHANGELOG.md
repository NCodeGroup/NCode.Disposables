# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Public API tracking: the public surface of v5.3.0, unchanged, is recorded in `PublicAPI.Shipped.txt`, and `PublicAPI.Unshipped.txt` is empty.
- Each pack is validated against NCode.Disposables 5.3.0, the last version on nuget.org, through `PackageValidationBaselineVersion`, so a change that would break 5.3.0's public API fails the pack. ([#37](https://github.com/NCodeGroup/NCode.Disposables/issues/37), [ADR-0147](docs/decisions/project-template/0147-packages-are-validated-against-the-last-published-package.md))
- The changelog has a section for every release from 1.0.0 to 5.3.0, from their release notes. ([#27](https://github.com/NCodeGroup/NCode.Disposables/issues/27))
- `src/NCode.Disposables/README.md` is the package readme nuget.org shows: installation, the main samples, and absolute links to the full guide, the API overview, the changelog and the security policy.
- Every C# sample in the READMEs is a compiled test in `tests/NCode.Disposables.Tests/ReadmeSamplesTests.cs` that MarkdownSnippets copies into the READMEs, and `./build/gate.ps1`, which CI and the Landing section's `gate` run, fails when a README differs from its test, then runs `./build/dod.ps1`. ([ADR-1000](docs/decisions/1000-readme-samples-are-compiled-tests-that-markdownsnippets-copies-into-the-readmes.md))
- The `AsAsyncSharedReference()` extension puts an `IAsyncDisposable` behind a reference count and returns its first `AsyncSharedReferenceLease<T>`, also for a type that implements `IDisposable` too, such as a `Stream`, where `AsSharedReference()` is ambiguous. ([#55](https://github.com/NCodeGroup/NCode.Disposables/issues/55), [ADR-1002](docs/decisions/1002-the-async-shared-reference-extension-is-asasyncsharedreference.md))

### Changed

- `AssemblyVersion` stays `5.3.0.0` for every 5.x release and moves only with the major version from the next major on, set by `assemblyVersion` in `version.json`. An application compiled against 5.3.0.0 loads later 5.x assemblies without a binding redirect.
- `SharedReferenceOwner` throws through `ObjectDisposedException.ThrowIf`; the exception type and object name are unchanged.
- Updated the project from project-template v0.41.0 to v0.61.2: stricter analyzers, shared build settings, a Definition of Done script and CI updates.
- Updated the project from project-template v0.61.2 to v0.112.2: package versions now come from `version.json` through Nerdbank.GitVersioning, the CI workflow checks out the full history and builds a release with `-p:PublicRelease=true`, the Commit attribution check and `commit-msg` hook are gone, and the project gains a code of conduct, governance, roadmap, architecture and assurance documents, a coverage and reproducible-build toolkit, and the Scorecard workflow. ([#28](https://github.com/NCodeGroup/NCode.Disposables/issues/28))
- The solution file is `NCode.Disposables.slnx`, the XML solution format current .NET SDKs create, in place of `NCode.Disposables.sln`, and the ReSharper and Rider settings layer moves with it to `NCode.Disposables.slnx.DotSettings`. `dotnet build` and `dotnet test` at the repository root work as before.
- The package carries its description and tags again: `Directory.Build.props` imports `src/NCode.Disposables/project.props`, and the project files no longer repeat settings the project template makes or reference `Microsoft.SourceLink.GitHub`, which the .NET SDK includes. ([#36](https://github.com/NCodeGroup/NCode.Disposables/issues/36))
- Every C# file starts with the two-line SPDX header, `// Copyright © NCode Group` and `// SPDX-License-Identifier: Apache-2.0`, in place of the `Copyright Preamble` region, Rider's file header matches it, and no file starts with a UTF-8 byte order mark. ([#39](https://github.com/NCodeGroup/NCode.Disposables/issues/39), [#36](https://github.com/NCodeGroup/NCode.Disposables/issues/36), [ADR-0148](docs/decisions/project-template/0148-csharp-is-formatted-in-the-gate-and-files-carry-the-spdx-header.md))
- NCode Review v1.5.0 reviews each pull request and approves a clean one; once the NCode Review Approver's key is stored, that approval meets a required approval. Pull requests the workflow commands open start as drafts (`draft: yes`). ([#38](https://github.com/NCodeGroup/NCode.Disposables/issues/38), [ADR-0036](docs/decisions/project-template/0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md))
- `./build/dod.ps1` checks the C# formatting with CSharpier, the code and project files are formatted with it, and a C# file without the SPDX header fails the build: the project turned on the template's `csharp_format` answer. ([#39](https://github.com/NCodeGroup/NCode.Disposables/issues/39), [ADR-0148](docs/decisions/project-template/0148-csharp-is-formatted-in-the-gate-and-files-carry-the-spdx-header.md))
- Packages reach nuget.org through `.github/workflows/publish.yml`, the project template's publishing workflow, from a published GitHub release: it tests and packs the tagged commit and pushes the packages through nuget.org trusted publishing once a maintainer approves the protected `nuget` environment, so no long-lived NuGet API key is stored. Each release also gets its packages, a signed build provenance file and an SBOM, attached by `release-provenance.yml`, and `raise-baseline.yml` opens the change that raises the package validation baseline. ([#44](https://github.com/NCodeGroup/NCode.Disposables/issues/44), [ADR-0135](docs/decisions/project-template/0135-packages-are-published-from-a-github-release-through-trusted-publishing.md))
- `README.md` is a guide to every type, in the layout of the other NCode Group projects: a tested sample for each disposable, the shared references, the async adapter and the extensions, their errors and edge cases, an API overview, thread safety, and NuGet, CI, target framework, license and OpenSSF Scorecard badges. Its hand-kept release notes are gone; [CHANGELOG.md](CHANGELOG.md) covers every release.
- The package description reads "Building blocks for IDisposable and IAsyncDisposable in .NET", followed by what the library does, without the Markdown backticks nuget.org showed as text.
- Updated the project from project-template v0.112.2 to v0.113.1: Claude Code's project settings no longer ask before a command that mentions `.env` or `secrets/`, the release workflows and the template's scripts accept a version only in ASCII digits after a lowercase `v`, and the template's Best Practices record is ADR-0037, renumbered from ADR-0153.
- `docs/architecture.md` describes the components, the factories, the disposables, the shared references and the extensions, as a diagram and a table. ([#56](https://github.com/NCodeGroup/NCode.Disposables/issues/56))

### Deprecated

- `AsyncDisposableExtensions.AsSharedReference()` is obsolete and reports warning `NCDISP001`: call `AsAsyncSharedReference()` instead. It keeps working until the next major version deletes it. ([#55](https://github.com/NCodeGroup/NCode.Disposables/issues/55), [ADR-1002](docs/decisions/1002-the-async-shared-reference-extension-is-asasyncsharedreference.md))

### Removed

- `.github/workflows/main.yml`: its build and test jobs repeated `ci.yml`'s `build-and-test` on every push and pull request, and `publish.yml` replaces its publish job. ([#43](https://github.com/NCodeGroup/NCode.Disposables/issues/43), [#44](https://github.com/NCodeGroup/NCode.Disposables/issues/44))

### Fixed

- `DisposeAll(ignoreExceptions: true)` ignores the failures of the items disposed after an item that implements only `IAsyncDisposable`; the `InvalidOperationException` for that item is still thrown. ([#53](https://github.com/NCodeGroup/NCode.Disposables/issues/53))
- `Disposable.Collection(...).Dispose()` and `DisposeAll()` dispose an item that implements both `IDisposable` and `IAsyncDisposable`, such as a `Stream` or a `DbConnection`, through `IDisposable.Dispose` instead of throwing `InvalidOperationException`, which `DisposeAll()` throws only for an item that implements `IAsyncDisposable` alone. ([#54](https://github.com/NCodeGroup/NCode.Disposables/issues/54), [ADR-1001](docs/decisions/1001-synchronous-disposal-disposes-an-item-that-implements-both-interfaces-through-idisposable.md))
- The XML docs of `DisposableCollection` and `AsyncDisposableCollection` say that `Add`, `Remove`, `Clear` and enumeration take no lock, and `Disposable.Collection`'s docs say that several failures are thrown together as an `AggregateException`.

## [5.3.0] - 2026-01-17

### Changed

- Upgraded to .NET 10: the package ships `net10.0` and `net8.0` builds.

## [5.2.1] - 2024-11-29

### Changed

- Collections can hold any object instead of only `IDisposable` or `IAsyncDisposable` instances; items that aren't disposable are skipped.

## [5.1.1] - 2024-11-27

### Changed

- Minor ReSharper code cleanup, with no change in behavior.

## [5.1.0] - 2024-05-16

### Added

- Async shared references can be created without an async call site.

## [5.0.2] - 2024-05-16

### Removed

- More dead code.

## [5.0.1] - 2024-05-15

### Removed

- Dead code.

## [5.0.0] - 2024-05-15

### Changed

- **Breaking:** Shared references and their leases are structs instead of classes; recompile code that uses them.

## [4.4.0] - 2024-05-15

### Changed

- The `idempotent` option is a set of method overloads instead of a parameter.

## [4.3.0] - 2024-05-15

### Added

- The `DisposeAsyncIfAvailable` extension, which disposes an instance asynchronously when it implements `IAsyncDisposable` and synchronously otherwise.
- An `idempotent` option on some methods.

## [4.2.0] - 2024-04-28

### Added

- `AsyncDisposableAdapter`, which adds asynchronous disposal to an existing `IDisposable`, so it works with `await using`.

## [4.1.0] - 2024-04-28

### Added

- Async support: `IAsyncDisposable` counterparts of the disposables and shared references, such as `AsyncDisposable` and `AsyncSharedReference`.

## [4.0.0] - 2024-04-21

### Changed

- **Breaking:** Reverted the 3.1.0 split: `ISharedReference<T>` is one interface again, in place of `ISharedReferenceScope<T>` and `ISharedReferenceProvider<T>`.

## [3.1.0] - 2024-04-20

### Changed

- Split `ISharedReference<T>` into `ISharedReferenceScope<T>` and `ISharedReferenceProvider<T>`.

## [3.0.1] - 2024-04-20

### Changed

- Updated the XML documentation.

## [3.0.0] - 2024-04-20

### Changed

- **Breaking:** Ported to .NET 8.0 (`net8.0`) and refactored the shared reference implementation.

## [2.0.3] - 2017-01-21

### Changed

- Republished 2.0.2 from the same commit, with no code changes.

## [2.0.2] - 2017-01-21

### Changed

- **Breaking:** Ported to .NET Core and .NET Standard: the package targets `netstandard1.1` and `net45`, links the project's URL, and has fewer dependencies.

## [1.0.1] - 2016-02-10

### Added

- `Action` and `Func` overloads of the `Disposable` factory methods.

## [1.0.0] - 2016-01-30

### Added

- Initial release for .NET Framework 4.5.1: `IDisposable` implementations for common resource management patterns, such as actions, aggregates, collections and shared references.

[Unreleased]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.3.0...HEAD
[5.3.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.2.1...v5.3.0
[5.2.1]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.1.1...v5.2.1
[5.1.1]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.1.0...v5.1.1
[5.1.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.0.2...v5.1.0
[5.0.2]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.0.1...v5.0.2
[5.0.1]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.0.0...v5.0.1
[5.0.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v4.4.0...v5.0.0
[4.4.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v4.3.0...v4.4.0
[4.3.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v4.2.0...v4.3.0
[4.2.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v4.1.0...v4.2.0
[4.1.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v4.0.0...v4.1.0
[4.0.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v3.1.0...v4.0.0
[3.1.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v3.0.1...v3.1.0
[3.0.1]: https://github.com/NCodeGroup/NCode.Disposables/compare/v3.0.0...v3.0.1
[3.0.0]: https://github.com/NCodeGroup/NCode.Disposables/compare/v2.0.3...v3.0.0
[2.0.3]: https://github.com/NCodeGroup/NCode.Disposables/compare/v2.0.2...v2.0.3
[2.0.2]: https://github.com/NCodeGroup/NCode.Disposables/compare/v1.0.1...v2.0.2
[1.0.1]: https://github.com/NCodeGroup/NCode.Disposables/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/NCodeGroup/NCode.Disposables/releases/tag/v1.0.0
