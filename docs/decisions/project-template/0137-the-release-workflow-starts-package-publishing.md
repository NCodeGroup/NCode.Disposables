# 0137. The Tag and publish release workflow starts package publishing through workflow_dispatch

- **Status:** Accepted
- **Date:** 2026-11-08
- **Amends:** 0135

## Context

ADR-0135 publishes packages when a GitHub release is published, through `release: published`. The Tag and publish
release workflow (`.github/workflows/tag-release.yml`) publishes the release with `GITHUB_TOKEN`, and an event created
with that token starts no workflow, except `workflow_dispatch` and `repository_dispatch`. A release the workflow
publishes therefore never starts `publish.yml` by the event alone.

## Decision

`publish.yml` also runs on `workflow_dispatch` with a required `tag` input, and the Tag and publish release workflow
starts it.

- `tag-release.yml` starts `publish.yml` on the tag's ref, with the tag as input, through the REST API. Its release job
  holds `actions: write` for that call, and it starts the run only when it published the release.
- `publish.yml` reads the version from the release tag or the `tag` input, and checks out `refs/tags/<tag>`, so a run
  started from another branch still packs the tagged commit. `pack` still refuses a tag that is not a version or whose
  commit the default branch does not hold.
- `publish` keeps the `nuget` environment and trusted publishing. The nuget.org policy names the repository and the
  workflow file, not the event.
- A person can run Publish packages by hand from the Actions tab with a tag.

## Options considered

- A GitHub App token or a personal access token in a secret, so the release publication starts the workflow by event.
  Rejected: it adds a credential to every project, and a personal access token is long-lived.
- Publishing on a push to the default branch. Rejected by ADR-0135.
- Starting `publish.yml` through `workflow_dispatch` from the Tag and publish release workflow (chosen).

## Consequences

- A project-owned `publish.yml` without the `workflow_dispatch` trigger ships nothing from a release the workflow
  publishes. The Tag and publish release workflow fails after publishing the release and names the trigger, and
  `Update-Project.ps1` warns about the missing trigger.
- A rerun of the Tag and publish release workflow finds the release published and starts nothing, so a person runs
  Publish packages by hand with the tag.

## References

- [ADR-0135](0135-packages-are-published-from-a-github-release-through-trusted-publishing.md): packages are published
  from a GitHub release through trusted publishing.
- [ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md): releases are tagged and
  published when their pull request merges.
- `.github/workflows/publish.yml`, `.github/workflows/tag-release.yml`
