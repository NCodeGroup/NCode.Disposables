# 0147. Each pack is validated against the last published package

- **Status:** Accepted (amended by 0150)
- **Date:** 2026-11-14

## Context

This decision applies to projects published as NuGet packages.

[ADR-0131](0131-public-api-is-tracked-and-evolves-compatibly.md) catches a public change that the `PublicAPI.*.txt`
files do not record, but not a deliberate one: deleting a member together with its `PublicAPI.Shipped.txt` line passes
the build. The files also show nothing when a package drops a target framework or a member that exists in one target
framework but not in another. A binary break then reaches nuget.org and every consumer.

## Decision

Every `dotnet pack` validates the package, and once the project sets a baseline it validates against the last package
published to nuget.org.

- `build/ProjectTemplate.props` sets `EnablePackageValidation` to `true` in every project that packs. Package
  validation compares the package's target frameworks with each other and, with a baseline, with the baseline package.
  Without a baseline it still checks the compatibility of the target frameworks.
- The project sets `PackageValidationBaselineVersion` itself, in its own `Directory.Build.props` or in a packable
  project's `.csproj`, never in the template. The right value is the last version on nuget.org, which a release's own
  change cannot download, and a template-owned file cannot be edited per project. A baseline that is one release behind
  still compares against a real release and only misses the breaks since then, so a late raise is safe: raise it in the
  first change after a release reaches nuget.org.
- Validation runs on `dotnet pack`, so `build/dod.ps1 -Fast`, which does not pack, skips it; the full gate, CI and
  `publish.yml` run it.
- An intended break in a new major version is recorded with `-p:ApiCompatGenerateSuppressionFile=true`, which writes
  `CompatibilitySuppressions.xml`. It is reviewed like the `PublicAPI.*.txt` diff and deleted when the baseline moves to
  that major's first release.
- The project's update and adoption scripts never edit `Directory.Build.props` for this. They look up the newest
  stable version of each packable project's package on nuget.org and say which version to set. A package that was never
  published, or a lookup that fails, says nothing.

## Options considered

- A baseline in the template. Rejected: the value differs per project, and the package does not exist yet when the
  release is made.
- A baseline derived from the newest tag. Rejected: the tag exists before its package does.
- A baseline the project sets by hand (chosen). The template and its scripts name the version to set; the project
  decides when.

## Consequences

- A pack that removes or changes a public member the baseline package has fails with `CP0002` or a related error
  where it is made, before the package ships.
- A library that already ships sees the new failures on its first update; this is a breaking change of the template.
- A project that never sets a baseline still gets the target-framework check.
- Following a release, the first change raises the baseline, and a major version records its intended breaks in
  `CompatibilitySuppressions.xml`.

## References

- `build/ProjectTemplate.props`, `docs/api-compatibility.md`
- [Package validation](https://learn.microsoft.com/dotnet/fundamentals/apicompat/package-validation/overview)
- [ADR-0131](0131-public-api-is-tracked-and-evolves-compatibly.md): the public API surface is tracked and evolves
  backward-compatibly.
