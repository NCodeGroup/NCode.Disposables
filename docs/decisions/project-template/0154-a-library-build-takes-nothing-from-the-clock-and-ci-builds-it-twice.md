# 0154. A library build takes nothing from the clock, and CI builds it twice

- **Status:** Accepted
- **Date:** 2026-11-20

## Context

The year in the build's `Copyright` came from the build machine's clock. A rebuild of a tag in a later year changed
`AssemblyCopyright`, and with it the deterministic MVID, the PE timestamp, the PDB id and the `.nuspec`. So the OpenSSF
Best Practices criteria `build_repeatable` and `build_reproducible` could not be shown
([NCodeGroup/project-template#454](https://github.com/NCodeGroup/project-template/issues/454)). NCodeGroup/PropertyBag
rebuilt v2.0.1 byte for byte except for zip timestamps, and the year was the only input left that the commit didn't fix.

## Decision

**A library's `Copyright` names no year, and CI packs the commit twice and compares the packages entry by entry.**

1. `Copyright` is `Copyright © <author>` with no year, the same text as the SPDX header every C# file and
   `COPYRIGHT.txt` carry.
2. `.template/Test-ReproducibleBuild.ps1` packs the commit twice from clean `bin` and `obj` folders and compares the
   entry names and the SHA-256 of each entry's contents, never zip timestamps.
3. A library's `ci.yml` runs it as the job `reproducible-build`, and `tag-release.yml` runs `ci.yml` before it tags, so
   a release waits for it.
4. A GitLab library runs the script by hand.

## Options considered

- The commit date as the year. Rejected: it needs Git in the build, and a tag and a rebuild of its tree would still
  differ when a checkout has no history.
- A separate workflow. Rejected: it would need wiring in `publish.yml`, in the adoption and in the update.
- Keep the clock. Rejected: a rebuild in a later year differs from the release.
- No year, and a check in CI (chosen).

## Consequences

- A tag rebuilds to the same package contents in any year.
- A GitHub library's CI builds its packages twice, which adds one job of up to 30 minutes.
- The copyright notice in a package names the author without a year.
- A GitLab library gets no automatic check.

## References

- `.template/Test-ReproducibleBuild.ps1`, `.github/workflows/ci.yml`, `build/ProjectTemplate.props`
- [ADR-0147](0147-packages-are-validated-against-the-last-published-package.md): packages are validated against the
  last published package.
- [ADR-0148](0148-csharp-is-formatted-in-the-gate-and-files-carry-the-spdx-header.md): files carry the SPDX header.
- [NCodeGroup/project-template#454](https://github.com/NCodeGroup/project-template/issues/454)
