# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Public API tracking: the public surface of v5.3.0, unchanged, is recorded in `PublicAPI.Shipped.txt`, and `PublicAPI.Unshipped.txt` is empty.

### Changed

- `AssemblyVersion` stays `5.3.0.0` for every 5.x release and moves only with the major version from the next major on, set by `assemblyVersion` in `version.json`. An application compiled against 5.3.0.0 loads later 5.x assemblies without a binding redirect.
- `SharedReferenceOwner` throws through `ObjectDisposedException.ThrowIf`; the exception type and object name are unchanged.
- Updated the project from project-template v0.41.0 to v0.61.2: stricter analyzers, shared build settings, a Definition of Done script and CI updates.
- Updated the project from project-template v0.61.2 to v0.112.2: package versions now come from `version.json` through Nerdbank.GitVersioning, the CI workflow checks out the full history and builds a release with `-p:PublicRelease=true`, the Commit attribution check and `commit-msg` hook are gone, and the project gains a code of conduct, governance, roadmap, architecture and assurance documents, a coverage and reproducible-build toolkit, and the Scorecard workflow.
- The solution file is `NCode.Disposables.slnx`, the XML solution format current .NET SDKs create, in place of `NCode.Disposables.sln`, and the ReSharper and Rider settings layer moves with it to `NCode.Disposables.slnx.DotSettings`. `dotnet build` and `dotnet test` at the repository root work as before.

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
