# 0149. Stale NuGet logistics are named, not fixed

- **Status:** Accepted
- **Date:** 2026-11-14

## Context

This decision applies to projects published as NuGet packages.

Adopting the Buffers library (NCodeGroup/Buffers#33) showed that an older library carries NuGet logistics the template
now makes or checks itself, and that finding them took a hand investigation: a `.props` file that no project imports,
so its `Description` and `PackageTags` no longer reach the package; `GeneratePackageOnBuild`; settings that
`build/ProjectTemplate.props` already makes; a `.sln` where `.slnx` is current; files with a UTF-8 byte order mark; and
projects at the repository root. Adopting or updating one of many NuGet repositories should not need that search.

## Decision

The template's `Adopt-Project.ps1` and `.template/Update-Project.ps1` look for these six findings in a .NET library and name each one
with the change to make. They fix none, as [ADR-0147](0147-packages-are-validated-against-the-last-published-package.md)
says for `PackageValidationBaselineVersion`.

- Adoption prints each finding as a follow-up; the update prints them as one block with a `/plan-migration` request.
- The logistics check is offline and deterministic: it scans files with regular expressions and never loads a project as XML.
- The package metadata comparison, `Get-PackageMetadataDrift`, is the one part that isn't offline: for each packable
  project whose package is published, it reads the newest stable version's nuspec from nuget.org, evaluates the project
  with `dotnet msbuild -getProperty`, and names each of `Description` and `PackageTags` that differs, ignoring whitespace
  and the order and separators of tags. A published package without that field, a package never published, a project
  that doesn't evaluate and a failed lookup give nothing. Adoption lists each as a follow-up and the update prints them
  as a block of their own; neither edits a project file.
- It runs only when `nuget_package` is `true`.
- It does not detect a transitive dependency that an older target framework doesn't support (issue #360's
  `System.IO.Hashing` 11 on net8.0 through Nerdbank.Streams): that needs a restore, which an offline scan can't do.
  `build/ProjectTemplate.props` sets `TreatWarningsAsErrors`, so a project that follows the template already fails its
  build on a package's unsupported-framework warning. A project that keeps its own settings is caught by the
  `RepeatedSettings` finding only for the settings it lists, so its guidance names `TreatWarningsAsErrors` set to
  `false` and `NoWarn` entries. It is filed as a follow-up to make the build gate name the warning, and the changelog and
  pull request leave it open.
- `tools/adopt/Adoption.psm1` holds `Get-NuGetLogistics` and `Get-PackageMetadataDrift`, and
  `.template/Update-Project.ps1` holds a deliberate copy of each; `tests/Test-NuGetLogistics.ps1` and
  `tests/Test-LibraryRelease.ps1` keep the copies equal in behavior.

## Options considered

- Fix the findings automatically. Rejected: each fix needs a person's choice, such as which `.props` settings to keep,
  running `dotnet sln migrate`, or where to move a project.
- Report only (chosen). The scripts name each finding and the change to make; the project decides.

## Consequences

- An older library learns what to change on its first adoption or update, without a hand investigation.
- No project file is edited, so the change cannot break a project.
- Two copies of the function must change together; the test fails when they differ.

## References

- `tools/adopt/Adoption.psm1`
- [ADR-0147](0147-packages-are-validated-against-the-last-published-package.md): each pack is validated against the
  last published package.
