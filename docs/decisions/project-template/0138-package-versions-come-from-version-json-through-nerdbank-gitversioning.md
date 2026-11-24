# 0138. Package versions come from version.json through Nerdbank.GitVersioning

- **Status:** Accepted
- **Date:** 2026-11-14
- **Amends:** 0136

## Context

ADR-0136 takes a package's version from the release tag, which publishing passes as `-p:Version`. Every other build
packs `1.0.0`, so a package from a branch can't say which version or which commit it is.

NCode repositories that went further version from a `version.json` file, each with its own ADR. A shared file and tool
keep them alike.

## Decision

`version` in the root `version.json` is the version the next release ships, and Nerdbank.GitVersioning stamps it into
every assembly and package.

- The version has three parts and an optional prerelease suffix, and no `{height}`. The tag is `v<version>`.
- A tag build or a build with `-p:PublicRelease=true` packs exactly `<version>`. Any other commit packs
  `<version>-g<commit>`.
- `nuGetPackageVersion.semVer` is 2, so a prerelease such as `1.2.0-rc.1` stays as written.
- `assemblyVersion` is `{ "precision": "major" }`, which is ADR-0136's rule. A library whose release shipped a higher
  assembly version sets it, such as `{ "version": "5.3", "precision": "minor" }`, until the change that starts the next
  major sets it back. FileVersion is `major.minor.patch` plus the git height.
- `cloudBuild.setVersionVariables` is false.
- Every checkout that builds fetches the full history.
- A plan with a `Release:` line sets `version` to that version. The state script reports a mismatch, `/ship-plan` stops
  on one, and the Tag and publish release workflow refuses to tag a commit whose `version.json` disagrees. On GitHub,
  `publish.yml` packs with `-p:PublicRelease=true` and refuses a package whose version isn't the tag's. It also passes
  the tag's version as `-p:Version`, which Nerdbank.GitVersioning ignores, so a library that deletes `version.json`
  still packs the tag's version.
- `build/ProjectTemplate.props` references the package only while `version.json` exists. A library that deletes it
  keeps ADR-0136's tag-based version.

## Options considered

- The tag only. Rejected: every other build is `1.0.0`.
- `{height}` in the version. Rejected: a version becomes a counter.
- MinVer. Rejected: the NCode repositories share one tool and one file.
- `version.json` with Nerdbank.GitVersioning (chosen).

## Consequences

- A release changes `version.json`.
- A non-release package is visibly not a release.
- Builds need git history.
- A project-owned `publish.yml` without `-p:PublicRelease=true` publishes nothing, so `Update-Project.ps1` warns.
- A project that references Nerdbank.GitVersioning itself removes its own reference.

## References

- [ADR-0136](0136-packages-follow-semantic-versioning-and-the-assembly-version-moves-only-with-the-major.md): packages
  follow Semantic Versioning, and the assembly version moves only with the major.
- [ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md): releases are tagged and
  published when their pull request merges.
- `version.json`, `build/ProjectTemplate.props`
- [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
