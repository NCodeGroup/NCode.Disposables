# 1003. The package targets net10.0 only

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

NCode.Disposables 5.x targets `net8.0` and `net10.0`. Per Microsoft's
[.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) (read 2026-11-24), .NET 8 left
support on 2026-11-10, and the supported releases are .NET 10 (LTS, to 2028-11-14) and .NET 11 (STS, to 2028-11-14). A
`net10.0` asset serves .NET 10 and later. Removing a target framework breaks consumers on it, so it waits for a major
version ([ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)).

## Decision

**From 6.0.0 the package targets `net10.0` alone, through one `<TargetFramework>` in
`src/NCode.Disposables/NCode.Disposables.csproj`.**

A target framework is added when a new LTS release needs an API the lowest one lacks, and dropped only in a major
version, after it leaves Microsoft support.

## Options considered

- Keep `net8.0;net10.0`: rejected, because it serves apps still on an unsupported runtime at the cost of building and
  validating a second asset no one should run in production.
- Add `net11.0`: rejected, because it is an STS release with nothing the library needs, so it would only multiply
  assets.
- `net10.0` alone: chosen.

## Consequences

- Apps on .NET 8 or 9 stay on NCode.Disposables 5.3.0.
- Package validation reports the dropped framework as `PKV006` against the 5.3.0 baseline, suppressed in
  `src/NCode.Disposables/CompatibilitySuppressions.xml` until the baseline moves to 6.0.0
  ([ADR-0147](project-template/0147-packages-are-validated-against-the-last-published-package.md)).
- The changelog lists the removal under 6.0.0's Removed.

## References

- [.NET support policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core)
- `src/NCode.Disposables/NCode.Disposables.csproj`, `src/NCode.Disposables/CompatibilitySuppressions.xml`
- [ADR-0131](project-template/0131-public-api-is-tracked-and-evolves-compatibly.md)
- [ADR-0147](project-template/0147-packages-are-validated-against-the-last-published-package.md)
