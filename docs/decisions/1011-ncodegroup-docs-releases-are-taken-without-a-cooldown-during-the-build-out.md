# 1011. NCodeGroup/docs releases are taken without a cooldown during the build-out

- **Status:** Accepted
- **Date:** 2026-11-25
- **Deciders:** NCode.Disposables maintainers
- **Supersedes:** project-template ADR-0009, for releases of NCodeGroup/docs only, until the build-out this record
  describes ends.

## Context

The documentation site in `docs/site/` is built by the shared toolchain in NCodeGroup/docs
([ADR-1009](1009-the-documentation-site-is-narrative-guides-built-by-a-shared-toolchain-outside-this-repository.md)),
which `.github/workflows/docs.yml` calls at a pinned commit. The toolchain, the NCode brand it vendors
(NCodeGroup/brand) and the first sites built with them, this one among them, are built out together by the same
maintainers: a site asks the toolchain for a feature, the toolchain releases it, and the site adopts it and finds the
next gap. NCodeGroup/docs published six releases, from v0.15.0 to v0.18.0, between 2026-11-19 and 2026-11-24 UTC (its
releases, read 2026-11-24); its versions are below 1.0.0, and its changelog describes it as pre-release.

Project-template ADR-0009 adopts a dependency version only once it is three days old, so that a compromised or
withdrawn release is found before it arrives. For a toolchain released several times a day, that wait keeps each site
several releases behind the contract its pages are written against, and each step of the build-out takes days instead
of hours. The risk the wait guards against is smaller for this dependency: NCode Group writes and releases it, tags a
release only once its CI passes, signs the tag and makes the release immutable, and takes its own npm packages only
once they are three days old.

## Decision

A release of NCodeGroup/docs is adopted as soon as it is published, while the NCode brand, the toolchain and the first
sites built with them are built out. The pin is v0.18.0.

1. The pin in `.github/workflows/docs.yml` stays a full commit SHA with its tag in a comment, and a comment there names
   this record.
2. `.github/dependabot.yml` excludes NCodeGroup/docs from the github-actions cooldown, so Dependabot offers a release in
   its next weekly update.
3. Every other dependency keeps project-template ADR-0009's three days. NCode Review (NCodeGroup/code-review) is among
   them: it receives the organization's keys, and `.github/workflows/code-review-auto-merge.yml` merges its pin without
   a person (project-template ADR-0006's exception), so a release adopted the day it appears would reach those keys
   unreviewed.
   NCodeGroup/brand reaches this repository only through the toolchain's vendored copy, which the toolchain verifies
   against the release's attestation.
4. The exception ends with the first stable release of NCodeGroup/docs, 1.0.0, or earlier when the maintainers declare
   the build-out done. The change that ends it removes the cooldown exclusion and the comment in `docs.yml`, marks this
   record Deprecated, and project-template ADR-0009 applies to NCodeGroup/docs again.

## Options considered

- Wait three days for every release, as project-template ADR-0009 says. Rejected: it holds the site behind the toolchain
  through the build-out, when the two change together.
- A commented exception per release, as project-template ADR-0009's third rule allows. Rejected: with several releases a
  day, an exception written and removed for each one is churn that says nothing a single record does not.
- Exclude every NCodeGroup dependency. Rejected: NCode Review handles the organization's keys and merges without a
  person, and is not part of the build-out.
- Exclude NCodeGroup/docs alone, until its first stable release (chosen).

## Consequences

- The site follows the toolchain's contract the day a release ships, and a gap the site finds reaches the toolchain
  sooner.
- A compromised or broken toolchain release can reach this repository's documentation build up to three days sooner than
  project-template ADR-0009 allows. That build runs the toolchain's workflow with the permissions `docs.yml` grants it,
  `pages: write` and `id-token: write` included. The pin is a full SHA, so the toolchain moves only through a pull
  request a person reviews and merges (project-template ADR-0006).
- A Dependabot pull request can move the pin to a release that asks something of the site, such as a renamed token; the
  documentation site workflow fails it when a page or component breaks the release's checks, and its reviewer reads the
  release's notes before merging it.
- When the exception ends, `.github/dependabot.yml`, the comment in `docs.yml` and the `AGENTS.md` line that lists this
  override change with it.

## References

- [Project-template ADR-0009](project-template/0009-new-dependency-versions-wait-a-cooldown.md)
- [Project-template ADR-0006](project-template/0006-a-person-merges-every-pull-request.md)
- [ADR-1009](1009-the-documentation-site-is-narrative-guides-built-by-a-shared-toolchain-outside-this-repository.md)
- `.github/workflows/docs.yml`, `.github/dependabot.yml`
- [NCodeGroup/docs releases](https://github.com/NCodeGroup/docs/releases)
