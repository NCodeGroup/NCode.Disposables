# 1014. CodeQL runs through a workflow of the repository's own, so it analyzes pull requests from forks

- **Status:** Accepted
- **Date:** 2026-11-27
- **Deciders:** NCode.Disposables maintainers
- **Supersedes:** project-template ADR-0020, in the SAST row of its table only, and the assumption of
  project-template ADR-0005 and ADR-0030 that CodeQL runs through its default setup.

## Context

The project template runs CodeQL through GitHub's default setup, which the repository's code scanning settings turn on
([ADR-0020](project-template/0020-github-projects-are-scored-by-openssf-scorecard.md)'s SAST row, "CodeQL runs
through the ruleset"), and the "Protect main" ruleset's code-scanning rule asks for CodeQL's results
([ADR-0005](project-template/0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
[ADR-0030](project-template/0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md)).

Default setup analyzes a pull request "based against the repository's default branch, or any protected branch,
excluding pull requests from forks" (GitHub Docs, "About setup types for code scanning", read 2026-11-27). Pull request
#95, from a fork, merged with no CodeQL analysis: its head commit has no check run from the `github-advanced-security`
app, and no CodeQL run started for it.

OpenSSF Scorecard's SAST check (v5.7.0) takes the last 30 commits of the default branch and, for each one a merged pull
request brought in, looks for a completed check run from `github-advanced-security`, `github-code-scanning`,
`sonarcloud` or `sonarqubecloud` on that pull request's head commit (`checks/raw/sast.go`, read 2026-11-27). Each of
#95's three commits and its merge commit counted as unanalyzed, which scored 8 and opened code scanning alert 25. Every
later pull request from a fork would do the same, and the library takes outside contributions.

CodeQL's advanced setup is a workflow in the repository that runs `github/codeql-action`. Its `pull_request` trigger
runs for a pull request from a public fork, and GitHub's `pull_request` event needs a repository setting only for a
pull request from a private fork (GitHub Docs, "Workflow configuration options for code scanning", read 2026-11-27).
Code scanning refuses an advanced setup's results while default setup is on, so the two never run together.

## Decision

**`.github/workflows/codeql.yml` runs CodeQL on every push to `main`, every pull request, a fork's included, every
merge queue entry and once a week, and the repository's code scanning settings keep default setup off.**

1. The workflow analyzes the languages default setup analyzed, `actions`, `csharp` and `javascript-typescript`, each
   with `build-mode: none` and the `security-extended` query suite, the one default setup names `extended`.
2. Its actions are pinned by commit, as every workflow's are, and Dependabot moves them
   ([ADR-0008](project-template/0008-external-inputs-are-pinned-and-kept-current.md)).
3. The ruleset's code-scanning rule stays as it is: it asks for the results of the tool named `CodeQL`, which either
   setup uploads.
4. A language the repository adds that CodeQL analyzes is added to the workflow's matrix in the same change.

## Options considered

- **Keep default setup and dismiss alert 25.** Rejected: the next pull request from a fork opens it again, and that
  pull request merges with no CodeQL analysis.
- **Keep default setup and wait for #95's commits to leave Scorecard's window of 30.** Rejected for the same reason.
- **Run CodeQL from `pull_request_target`, which a fork's pull request starts with the base repository's token.**
  Rejected: the job would check out and analyze the fork's code with a token that can write security events, the
  pattern Scorecard's Dangerous-Workflow check flags.
- **An advanced setup that runs on `pull_request`** (chosen): it analyzes every pull request with the token GitHub
  grants that event, read-only for a fork.

## Consequences

- Every pull request, a fork's included, has CodeQL's results before it merges, and Scorecard's SAST check scores 10
  once the last commit with no analysis leaves its window of 30.
- The repository owns the analysis' configuration, so a language CodeQL starts to support, or a change to GitHub's
  default suite, reaches it only through a change to the workflow. Default setup would follow both by itself.
- A project template update that changes how CodeQL runs is merged with this record in mind.
  [NCodeGroup/project-template#541](https://github.com/NCodeGroup/project-template/issues/541) proposes the same setup
  for every project built from the template; once a template version ships it, this record is superseded by the
  template's.
- Turning default setup on again makes code scanning refuse the workflow's results, and every pull request's CodeQL
  check fails until it is turned off.

## References

- `.github/workflows/codeql.yml`
- [Code scanning alert 25](https://github.com/NCodeGroup/NCode.Disposables/security/code-scanning/25)
- [About setup types for code scanning](https://docs.github.com/en/code-security/concepts/code-scanning/setup-types),
  GitHub Docs, read 2026-11-27
- [Workflow configuration options for code scanning](https://docs.github.com/en/code-security/reference/code-scanning/workflow-configuration-options),
  GitHub Docs, read 2026-11-27
- [Scorecard's `checks/raw/sast.go`](https://github.com/ossf/scorecard/blob/main/checks/raw/sast.go), read 2026-11-27
- [NCodeGroup/project-template#541](https://github.com/NCodeGroup/project-template/issues/541)
- [ADR-0005](project-template/0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
  [ADR-0020](project-template/0020-github-projects-are-scored-by-openssf-scorecard.md),
  [ADR-0030](project-template/0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md)
