# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- Public API tracking: the public surface of v5.3.0, unchanged, is recorded in `PublicAPI.Shipped.txt`, and `PublicAPI.Unshipped.txt` is empty.

### Changed

- `AssemblyVersion` now moves only with the major version (`5.0.0.0` for every 5.x release), set by the project template's build settings. Releases up to v5.3.0 used the full version (for example `5.3.0.0`), so an application compiled against 5.3.0.0 needs a binding redirect, or an explicit `<AssemblyVersion>` in `Directory.Build.props`, to load a later 5.x assembly.
- `SharedReferenceOwner` throws through `ObjectDisposedException.ThrowIf`; the exception type and object name are unchanged.
- Updated the project from project-template v0.41.0 to v0.61.2: stricter analyzers, shared build settings, a Definition of Done script and CI updates.

[Unreleased]: https://github.com/NCodeGroup/NCode.Disposables/compare/v5.3.0...HEAD
