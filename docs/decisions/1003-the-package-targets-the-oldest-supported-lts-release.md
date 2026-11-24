# 1003. The package targets the oldest supported .NET LTS release

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

Microsoft supports each .NET LTS release for three years and each STS release for two, per its
[.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core). An assembly built for a .NET
release runs on every later one. Each extra target framework is another asset to build, test and validate, and removing
one breaks the consumers on it, so it waits for a major version
([ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)).

## Decision

**The package targets one framework, the oldest .NET LTS release Microsoft still supports, set by the single
`<TargetFramework>` in `src/NCode.Disposables/NCode.Disposables.csproj`.**

Apps on later releases, LTS or STS, use that asset. A framework that leaves Microsoft support is replaced by the next
one in the next major version, never in a minor or patch release. A second framework is added only when the library
needs an API the targeted one lacks.

## Options considered

- Every supported release, LTS and STS: rejected, because it adds assets to build and validate for no API the library
  needs.
- Keep a framework after it leaves support: rejected, because it serves apps on an unsupported runtime and delays
  nothing a major version wouldn't.
- The oldest supported LTS alone: chosen.

## Consequences

- The csproj and the changelog name the framework; this record doesn't change when a .NET release ships.
- Apps on a runtime that left support stay on the last major version that targeted it.
- Package validation reports a dropped framework as `PKV006` against the baseline package, suppressed in
  `src/NCode.Disposables/CompatibilitySuppressions.xml` until the baseline moves past it
  ([ADR-0147](project-template/0147-packages-are-validated-against-the-last-published-package.md)).

## References

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- `src/NCode.Disposables/NCode.Disposables.csproj`, `src/NCode.Disposables/CompatibilitySuppressions.xml`
- [ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)
- [ADR-0147](project-template/0147-packages-are-validated-against-the-last-published-package.md)
