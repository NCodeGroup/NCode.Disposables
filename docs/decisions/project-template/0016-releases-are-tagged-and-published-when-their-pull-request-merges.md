# 0016. Releases are tagged and published when their pull request merges

- **Status:** Accepted
- **Date:** 2026-11-08

## Context

A release needs a tag on the merged commit and a GitHub release made from the version's notes. When an agent makes
them with `/ship-plan` after the merge, a person has to start a session once the pull request merges.

A Claude Code cloud session cannot do it earlier or itself: its GitHub proxy refuses a tag push and GitHub's GraphQL
API. The version lives in `PLAN.md`, which is gitignored and never reaches the default branch. A pre-release changelog
(ADR-0015) has no version heading to read it from.

## Decision

On GitHub, `.github/workflows/tag-release.yml`, named Tag and publish release, runs on each push to `main` and by hand.
Its first job, with only `contents: read` and a checkout that keeps no token, reads `release` from `AGENTS.md`'s Landing
section with `.template/Publish-Release.ps1 -Policy`. Only for `tag` or `tag+release` does its second job run
`.template/Publish-Release.ps1` with `contents: write`, `actions: write` and `pull-requests: read`.

1. The version is the newest release heading `## [x.y.z]` of the changelog, whatever `pre-release` says. In a project
   with `pre-release: untracked` and no such heading, it is the `Release:` line in the description of the pull request
   that merged the commit; the notes are the `## [Unreleased]` section. With no such line, or with `pre-release:
   tracked` and no heading, nothing is published.
2. The merged pull request is the one GitHub lists for the commit whose `merge_commit_sha` is that commit, which holds
   for a merge commit and a rebase merge. Its description has a line `Release: <version>`. When the changelog has a
   release heading and the line names another version, the run fails and names both.
3. With `release: tag` or `tag+release`, the script creates an annotated tag on the merged commit, only when the version
   is higher than every version tag, with the prefix origin's tags use. With `tag+release`, it publishes the GitHub
   release through the REST API with the section as its notes. It never moves a tag or changes a release, so a run
   repeated on the same commit does nothing.
4. The workflow has no `paths` filter, because a pre-release plan can land a release without touching the changelog.
5. `/ship-plan` writes the `Release:` line into the pull request description. Run after the merge, it tags and publishes
   only what the workflow did not. On GitLab, which has no such workflow, `/ship-plan` tags and publishes after the
   merge.

## Options considered

- `/ship-plan` after the merge only. Rejected: it needs a person at a terminal for every release, and a cloud session
  cannot push the tag.
- A GitHub App token. Rejected: each project needs an app and a secret to keep.
- A version file in the repository. Rejected: it is a second source of truth beside the changelog and the plan.
- A `paths: [CHANGELOG.md]` filter. Rejected: it misses a pre-release plan that does not touch the changelog.
- Naming the workflow Release. Rejected: a project's own release workflow is often named Release too, and the Actions
  tab then lists two workflows of one name that both offer Run workflow.
- One job that reads the policy and holds the write permissions. Rejected: with `release: none` it would hold
  `contents: write` and `actions: write`, and keep checkout's token in the clone, on every push for nothing.
- A workflow that reads the version from the changelog or the pull request and tags and publishes with the workflow's
  token (chosen). No credential is stored, and nobody runs anything after the merge.

## Consequences

- A short run starts on every push to `main`. With `release: none`, only the read-only first job runs; otherwise the
  second job ends at once when the tag and release exist.
- "Protect release tags" refuses only updating and deleting a `v*` tag, so the token creates one and cannot move it.
- A release made with the workflow's token starts no other workflow, so after it publishes a release, the script starts
  the project's `.github/workflows/publish.yml`, when it has one, through `workflow_dispatch`, as ADR-0137 records.
- A failed run is retried with Re-run failed jobs on that run in the Actions tab, which keeps its commit; Run workflow uses the commit
  `main` points to at that time.
- Smell: a release whose tag a person pushed by hand, or a `Release:` line that differs from the changelog's newest
  release heading.

## References

- `.github/workflows/tag-release.yml`, `.template/Publish-Release.ps1`, `AGENTS.md` Landing section
- [ADR-0015](0015-a-pre-release-project-describes-the-product-as-it-is.md): a pre-release project describes the product
  as it is.
- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md): the default branch accepts changes
  only through pull requests that pass CI.
