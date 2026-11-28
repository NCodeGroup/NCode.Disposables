# 0040. The template serves NCodeGroup's repositories on GitHub only

- **Status:** Accepted
- **Date:** 2026-11-27
- **Supersedes:** 0029
- **Amends:** 0005, 0007, 0008, 0009, 0013, 0014, 0016, 0023, 0026 and, where a project has them, 0027, 0135, 0150,
  0154, 0200 and 0201

## Context

The project template serves NCodeGroup's repositories, and NCodeGroup is on GitHub only. The template also supports
projects on GitLab: the `git_host` answer, `.gitlab-ci.yml`, `tools/Set-GitLabBaseline.ps1`, `glab` in the workflow
commands and `.template/Get-PlanState.ps1`, and the GitLab branches of the docs, the decision records and the tests,
about 1,500 mentions in 226 files outside `CHANGELOG.md`, counted on 2026-11-27
([#527](https://github.com/NCodeGroup/project-template/issues/527)). Every change, test and review keeps both hosts
working.

Every project made from the template answers `git_host: github`: the 17 NCodeGroup repositories that have a
`.copier-answers.yml`, read on 2026-11-27. No project made from the template is on GitLab.

## Decision

1. The template is for NCodeGroup's repositories on GitHub. It supports no other host: GitLab support is removed from
   what a project gets, from the plan workflow, and from the template's own tools, tests and docs
   ([#527](https://github.com/NCodeGroup/project-template/issues/527)). A GitLab template, if one is needed, is a
   separate template on GitLab.
2. GitLab support is removed without a deprecation phase, because no project made from the template is on GitLab to
   migrate. A project that answers `git_host: github` takes the release that removes the question and gets the same
   files, less the GitLab text that was never rendered for it.
3. It supersedes record 0029, "GitLab projects audit their dependencies in CI", which only a GitLab project gets.
4. It amends each record whose text describes GitLab for a project on GitHub too:
   [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
   [ADR-0007](0007-pending-work-is-tracked-as-issues.md),
   [ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md),
   [ADR-0009](0009-new-dependency-versions-wait-a-cooldown.md),
   [ADR-0013](0013-pull-requests-merge-with-a-merge-commit-or-by-rebasing.md),
   [ADR-0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md),
   [ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md),
   [ADR-0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md) and
   [ADR-0026](0026-tests-gate-coverage-at-the-gold-thresholds-and-a-release-waits-for-them.md), and, where a project
   has them, records 0027, 0135, 0150, 0154, 0200 and 0201. Their GitLab passages don't apply, and what each decides
   for GitHub holds. A passage that a record renders only for a GitLab project needs no amendment.

## Options considered

- Keep GitLab support for possible future use. Rejected: every change, test and review pays for its upkeep, and no
  project uses it.
- Deprecate GitLab support first and remove it in a later release. Rejected: there is no GitLab project to migrate.
- Support GitHub only and remove GitLab support outright (chosen).

## Consequences

- Changes, tests and reviews cover one host.
- The template takes no change that adds support for another host. A team that needs GitLab makes a separate
  template there.
- Where GitLab code or text remains in the template, this record holds over it, and the stories of
  [#527](https://github.com/NCodeGroup/project-template/issues/527) remove it.
- The `gh` REST fallback for sessions whose network refuses GitHub's GraphQL API is GitHub's own, and stays.

## References

- [NCodeGroup/project-template#527](https://github.com/NCodeGroup/project-template/issues/527) and
  [#528](https://github.com/NCodeGroup/project-template/issues/528)
- Record 0029, "GitLab projects audit their dependencies in CI", which only a GitLab project has
