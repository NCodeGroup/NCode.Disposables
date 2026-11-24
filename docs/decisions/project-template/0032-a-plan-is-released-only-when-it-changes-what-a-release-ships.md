# 0032. A plan is released only when it changes what a release ships

- **Status:** Accepted
- **Date:** 2026-11-19
- **Amends:** 0016

## Context

With `release: tag` or `tag+release`, the plan format gave every plan a `Release:` line, and
[ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md) has each merge tag and publish
that version. NCodeGroup/Buffers#114 and NCodeGroup/Buffers#118 changed only the documentation site's pages and the
repository's docs, yet they got `Release: v5.3.1` and `Release: v5.3.2`. Each merge tagged a version, published a
GitHub release with provenance and an SBOM, pushed a NuGet package whose code matched the one before it, and opened a
draft pull request to raise the package validation baseline (NCodeGroup/Buffers#119). None of that reached a reader:
the site deploys from every push to the default branch, with or without a release.

## Decision

1. A planning command adds a plan's `Release:` line only when the plan changes something a release ships: the
   product's code, a file its package or download carries, such as the `README.md` a package takes as its readme,
   which nuget.org shows, or what its build puts out, such as a build setting, a dependency or the SDK version.
2. A plan that changes only what reaches its readers without a release, such as documentation-site pages, CI
   workflows, decision records, tests or repository docs, gets no `Release:` line. Its last step still writes its
   changelog entries, under `## [Unreleased]`, where the next release's section takes them in.
3. When `AGENTS.md` says what the project's releases ship, planners follow it. A plan for several issues gets the line
   when any of them changes something a release ships.
4. `/ship-plan` lands a plan without the line with no tag and no release, and writes no `Release:` line into its pull
   request. `.template/Get-PlanState.ps1` prints its `tag` as `-` and sends it to `/finish-plan` once it has landed.
5. `.github/workflows/tag-release.yml` needs no change: a merge without a `Release:` line finds the changelog's newest
   section already tagged and published, so `.template/Publish-Release.ps1` tags nothing, publishes nothing and starts
   no `publish.yml`. When that earlier release's tag or GitHub release is still missing, because its own run failed,
   the merge's run finishes that release, as any run of the script does, and never makes a version of its own.

## Options considered

- Release every plan, as before. Rejected: a release that changes nothing a user installs costs a tag, a release, an
  identical package and a baseline pull request, and tells users there is something new when there isn't.
- A Landing key that lists the paths a release ships. Rejected: one more setting each project must keep current,
  while the planner already reads the change; a project whose releases ship something unusual says so in `AGENTS.md`.
- Have the release workflow skip a merge that changed no shipped file. Rejected: the workflow can't tell from the diff
  what a release ships, and the plan's `Release:` line already carries that decision.
- Give a plan the line only when it changes what a release ships (chosen).

## Consequences

- A documentation, CI or decision-record change reaches its readers as soon as its pull request merges, with no
  version.
- Its changelog entries appear in the notes of the next release, beside that release's own changes.
- A planner that misjudges a change is corrected in review: adding or removing the `Release:` line and the changelog
  section is one fix step.
- Smell: a release whose package is identical to the one before it, or a plan with a `Release:` line whose diff
  touches nothing a release ships.

## References

- The plan format in each planning command, `/ship-plan`, `.template/Get-PlanState.ps1`,
  `.template/Publish-Release.ps1`
- [ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md): releases are tagged and
  published when their pull request merges.
- [NCodeGroup/project-template#439](https://github.com/NCodeGroup/project-template/issues/439)
