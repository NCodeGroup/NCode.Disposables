# 0151. Release follow-ups are jobs that publish.yml calls

- **Status:** Accepted
- **Date:** 2026-11-15
- **Amends:** 0139, 0150

## Context

Two workflows act on what a `Publish packages` run produced: `release-provenance.yml` attests the packages and attaches
them, a signed provenance file and an SBOM to the GitHub release ([ADR-0139](0139-releases-carry-build-provenance.md)),
and `raise-baseline.yml` opens the pull request that raises the package validation baseline
([ADR-0150](0150-a-workflow-raises-the-package-validation-baseline-after-each-release.md)). Both started on a
`workflow_run` event of `Publish packages`.

`tag-release.yml` publishes each release with the workflow's `GITHUB_TOKEN` and starts `publish.yml` through
`workflow_dispatch` ([ADR-0137](0137-the-release-workflow-starts-package-publishing.md)). GitHub creates no workflow
run for an event caused by `GITHUB_TOKEN`, apart from `workflow_dispatch` and `repository_dispatch`, and that includes
the `workflow_run` event of a run the token started. So neither follow-up runs for a release that `tag-release.yml`
makes. In NCodeGroup/Buffers, `Publish packages` run 43737880017, for v5.0.2, ran as `github-actions[bot]` and started
neither.

## Decision

**`publish.yml` calls `release-provenance.yml` and `raise-baseline.yml` as reusable workflows, in jobs that need the
`publish` job, and neither listens for `workflow_run`.**

1. Job `provenance` calls `release-provenance.yml` with the tag. The called job downloads the run's own `packages`
   artifact, checks every package carries the tag's version, attests, attaches and verifies.
2. Job `baseline` calls `raise-baseline.yml` with the tag, for a stable version only.
3. Each caller job grants exactly the permissions its called job declares; `pack` and `publish` keep theirs.
4. `release-provenance.yml` also runs by hand with a tag and the id of a successful `Publish packages` run, to backfill
   a release while that run's artifact is kept. It refuses an id that isn't a successful run of `publish.yml` in the
   repository.
5. `release-provenance.yml` splits its two paths on `inputs.run-id`, never on `github.event_name`, which holds the
   caller's event in a called workflow.
6. Each called workflow's concurrency group is set per job from `inputs.tag`, so two releases never share a group.
7. `publish.yml` is project-owned, so a project that already has one keeps it: the update and the adoption warn when it
   doesn't call the two workflows.
8. [ADR-0139](0139-releases-carry-build-provenance.md)'s rejected option, steps inside `publish.yml`, stands.

## Options considered

- `workflow_run`, as before. Rejected: it never fires for the runs `tag-release.yml` starts.
- A token for `tag-release.yml` that isn't `GITHUB_TOKEN`, so its events start workflows. Rejected: a secret to manage
  and rotate.
- A final job in `publish.yml` that starts each follow-up with `workflow_dispatch`. Rejected: it needs `actions: write`
  for a hop the call doesn't need, and a run a person started would still need `workflow_run`, so there are two paths.
- Steps inside the `publish` job. Rejected: that job runs in the `nuget` environment with only `id-token: write`, and
  would then also hold release and pull request write access.
- Reusable workflows that `publish.yml` calls (chosen).

## Consequences

- The follow-ups run whoever started `Publish packages`, and only after its push succeeded.
- The attestation of a release published through `publish.yml` names the tag's ref and commit, because the call runs in
  the `Publish packages` run. A backfilled one names the commit the backfill was started on.
- A rerun of a failed follow-up job reruns only that job.
- The `Publish packages` run lasts as long as `raise-baseline.yml`'s wait for nuget.org, 30 minutes by default
  ([ADR-0150](0150-a-workflow-raises-the-package-validation-baseline-after-each-release.md)), and at most the baseline
  job's 45-minute timeout.
- Scorecard's Signed-Releases check averages the last five releases with assets, and an old build can't honestly get
  provenance afterwards. A project deletes the older unsigned release assets (the packages stay on nuget.org) or waits
  for five releases.
- A project whose `publish.yml` predates this record gets no provenance and no baseline pull request until it adds the
  two jobs.

## References

- `.github/workflows/publish.yml`, `.github/workflows/release-provenance.yml`, `.github/workflows/raise-baseline.yml`
- [ADR-0137](0137-the-release-workflow-starts-package-publishing.md): the release workflow starts package publishing.
- [ADR-0139](0139-releases-carry-build-provenance.md): releases carry build provenance.
- [ADR-0150](0150-a-workflow-raises-the-package-validation-baseline-after-each-release.md): a workflow raises the
  package validation baseline after each release.
- [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md): GitHub projects are scored by OpenSSF Scorecard.
- [NCodeGroup/project-template#381](https://github.com/NCodeGroup/project-template/issues/381)
