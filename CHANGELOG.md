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

[Unreleased]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.3.0...HEAD
