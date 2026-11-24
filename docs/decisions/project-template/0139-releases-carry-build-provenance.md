# 0139. Releases carry build provenance

- **Status:** Accepted (amended by 0151)
- **Date:** 2026-11-14
- **Amends:** 0135

## Context

ADR-0135 publishes packages to nuget.org from a GitHub release. A consumer has no way to check that a package came from
the project's workflow and commit, and the release carries no packages or provenance to compare against.

## Decision

`release-provenance.yml` runs as a job of a successful `Publish packages` run
([ADR-0151](0151-release-follow-ups-are-jobs-publish-calls.md)), attests the packages with
`actions/attest-build-provenance`, and attaches the packages and `<tag>.intoto.jsonl` to the GitHub release.

- Consumers verify a package with `gh attestation verify <package>.nupkg --repo <owner>/<repo>`.

## Options considered

- cosign keyless signing. Rejected: a second tool for the same Sigstore trust.
- The SLSA generic generator. Rejected: it is a reusable workflow referenced by tag, not by SHA, against ADR-0008.
- Adding the steps to `publish.yml`. Rejected: it is project-owned, so updates would never reach projects, and its
  publish job must hold only `id-token: write`.
- A template-owned workflow that runs after `Publish packages` (chosen).

## Consequences

- The assets stay on the release.
- A rerun of Publish packages re-attests and replaces the assets (`--clobber`).
- The release gets its files after it is published, so the repository keeps mutable releases
  ([ADR-0028](0028-github-releases-are-immutable-and-their-tags-signed.md)).
- A project with its own publish workflow gets a workflow that runs only once that workflow calls it.
- A release that `publish.yml` publishes is covered once its `provenance` job calls the workflow, and an earlier one
  can be backfilled while its `Publish packages` run's artifact is kept
  ([ADR-0151](0151-release-follow-ups-are-jobs-publish-calls.md)).
- The release also gets an SPDX SBOM, `<tag>.spdx.json`, exported from GitHub's dependency graph when the workflow runs; it describes the default branch at that time and is not attested.

## References

- [ADR-0135](0135-packages-are-published-from-a-github-release-through-trusted-publishing.md): packages are published
  from a GitHub release through trusted publishing.
- [ADR-0137](0137-the-release-workflow-starts-package-publishing.md): the Tag and publish release workflow starts
  package publishing.
- [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md): GitHub projects are scored by OpenSSF Scorecard.
- `.github/workflows/release-provenance.yml`
