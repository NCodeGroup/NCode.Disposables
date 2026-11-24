# 0028. GitHub releases are immutable and their tags signed

- **Status:** Accepted (amends 0016)
- **Date:** 2026-11-16

## Context

The OpenSSF Best Practices silver MUST `signed_releases` asks for signed releases and a documented way to verify them.
Outside a .NET library on GitHub it was a Gap in `docs/best-practices-criteria.md`. The silver SUGGESTED
`version_tags_signed` was Unmet, because `tag-release.yml` made unsigned tags. NCodeGroup/code-review met
`signed_releases` with immutable releases and a documented `gh release verify`.

## Decision

A GitHub repository's releases are immutable, and a public repository's release tags are signed.

- The template's `tools/Set-RepositoryBaseline.ps1` turns on immutable releases, after which GitHub signs an attestation
  of each published release's tag and files. `SECURITY.md`, in Verify a Release, documents `gh release verify <tag>` and
  `gh release verify-asset <tag> <file>`.
- A repository keeps mutable releases where a workflow on its default branch, a `.yml` or `.yaml` file in
  `.github/workflows`, runs on a `release` event or uploads release assets (`gh release upload`,
  `actions/upload-release-asset`, `softprops/action-gh-release` or `uploadReleaseAsset`), or where the script can't read
  one: an immutable release refuses a file attached after it is published, as `release-provenance.yml` attaches a
  library's packages ([project-template#411](https://github.com/NCodeGroup/project-template/issues/411)).
  `-SkipImmutableReleases` leaves them mutable in any repository, the script never turns them off, and
  `.template/Update-Project.ps1` says to delete `SECURITY.md`'s paragraph that says releases are immutable in such a
  repository.
- Whether a repository's releases are immutable depends on the repository, not on the template. A project whose
  releases get files or attestations after they are published keeps mutable releases, as a .NET library does, whose
  `publish.yml` has `release-provenance.yml` attach its packages, their provenance and an SBOM; every other GitHub
  project gets immutable ones. In the template's release flow, `tag-release.yml` publishes each release, and
  `publish.yml` attaches its files afterwards
  ([project-template#393](https://github.com/NCodeGroup/project-template/issues/393)).
- In a public repository, `tag-release.yml` signs each tag with gitsign, Sigstore's keyless signing through the job's
  OIDC token (`id-token: write`), installed by `chainguard-dev/actions/setup-gitsign` pinned to a commit.
  `.template/Publish-Release.ps1 -Sign` tags with `git tag -s`.
- A tag is verified with
  `gitsign verify-tag --certificate-identity https://github.com/OWNER/REPO/.github/workflows/tag-release.yml@refs/heads/main --certificate-oidc-issuer https://token.actions.githubusercontent.com <tag>`.
- A private repository's tags stay unsigned: Sigstore's public transparency log would publish its name.

## Options considered

- A GPG or SSH key in a repository secret. Rejected: it is a long-lived key per repository, made by hand and kept on the
  site that distributes the software.
- Tags made through GitHub's API. Rejected: the API makes tag objects without a signature.
- gitsign (chosen): there is no key to keep, and the signature names the workflow.
- Drafting each release in `tag-release.yml` and publishing it from `publish.yml` once `release-provenance.yml` has
  attached its files, so a library's releases could be immutable too. Rejected: a project that attaches attestations
  after a release is published can't have immutable releases whatever the template does; `publish.yml` is the
  project's, so a template update would never move an existing library to that order; a rerun of Publish packages
  and a backfill through `release-provenance.yml` attach files to a release already published, which an immutable
  release refuses; and each release would stay a draft, its notes unseen, until a person approves the `nuget`
  environment's deployment.
- Immutable releases wherever no workflow attaches files to a published release, and mutable ones elsewhere (chosen).

## Consequences

- A new release can't be changed once it is published, so a fix is a new release.
- A workflow that runs on a `release` event only to publish elsewhere, such as to a package registry, also keeps
  releases mutable; its owner can turn them on with `gh api -X PUT repos/OWNER/REPO/immutable-releases`, which the
  script never undoes.
- Scorecard's Signed-Releases check counts only the releases that have assets among the five newest. Scorecard v5.7.0,
  which the template's `scorecard.yml` runs through `ossf/scorecard-action` v2.6.0, has a `releasesAreImmutable`
  probe and scores such a release 10 when it is immutable, as it scores one with a `*.intoto.jsonl` provenance file, so
  a library that keeps mutable releases and attaches its provenance scores as high as a project whose immutable
  releases have assets. A release without assets isn't counted, immutable or not, and the release attestation GitHub
  signs isn't an asset: where no recent release has assets, as in a project whose releases get no files, the check is
  inconclusive and left out of the score.
- GitHub shows a gitsign signature as unverified, since its certificate authority isn't GitHub's; `gitsign verify-tag`
  is the check that counts.

## References

- `tools/Set-RepositoryBaseline.ps1` in the template's repository, `.github/workflows/tag-release.yml`,
  `.template/Publish-Release.ps1`, `SECURITY.md`
- [ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md): releases are tagged and published
  when their pull request merges.
- Scorecard's `releasesAreImmutable` probe:
  [probes/releasesAreImmutable/def.yml](https://github.com/ossf/scorecard/blob/v5.7.0/probes/releasesAreImmutable/def.yml)
