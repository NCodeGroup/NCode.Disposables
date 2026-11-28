# 0044. A fork's pull request merges without a bypass

- **Status:** Accepted
- **Date:** 2026-11-28
- **Amends:** 0005, 0020, 0022 and 0030

## Context

[NCodeGroup/NCode.Disposables#95](https://github.com/NCodeGroup/NCode.Disposables/pull/95), a pull request from a fork,
sat `BLOCKED` with passing CI, a maintainer's approval and every thread resolved. It merged once through a temporary
`pull_request`-mode bypass for repository admins, removed right after. Two causes kept it from merging under the
baseline ruleset.

1. The code-scanning rule waits for CodeQL, and CodeQL never analyzes a fork's pull request.
   - CodeQL's default setup runs on pull requests "excluding pull requests from forks" (GitHub Docs, "Code scanning
     setup types", read 2026-11-27).
   - The rule blocks a pull request while "a required tool's analysis is still in progress" or "is not configured",
     whatever the thresholds ("Available rules for rulesets", read 2026-11-27), so the rule waits forever.
   - Code scanning "always allows the uploading of results when the pull_request event triggers the action run"
     ("Resource not accessible", read 2026-11-27), so a workflow's results meet the rule for a fork's pull request.
   - GitHub refuses a CodeQL workflow's results while default setup is on, so the two can't run side by side.
2. `strict_required_status_checks_policy` makes a pull request behind `main` update its branch before it merges.
   GitHub counts the merge that "Update branch" makes as the maintainer's push. `require_last_push_approval` then needs
   another writer's approval, which a repository with one writer doesn't have, and NCode Review never approves a fork's
   pull request, so the approver
   ([ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)) skips forks
   ([ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) decision 4,
   [ADR-0043](0043-ncode-review-reviews-a-forks-ready-pull-request-and-never-approves-it.md) decision 4).

## Decision

1. Every GitHub project gets a project-owned `.github/workflows/codeql.yml`.
   - It runs on every pull request, on a push to `main`, in a merge queue, weekly and on request, with a read-only token
     except `security-events: write` in the job that uploads the results.
   - A `CodeQL languages` job picks the languages at run time: `actions` always, and `c-cpp`, `csharp`, `java-kotlin`,
     `javascript-typescript`, `python`, `ruby` and `rust` where `git ls-files` lists a file in that language. A new
     project has no source file yet, and CodeQL fails a language with no code. These are the languages that analyze
     without a build (GitHub Docs, "CodeQL code scanning for compiled languages", read 2026-11-28). `java-kotlin` runs
     with `build-mode: none`, which analyzes only its Java and skips Kotlin. Go, Swift and Kotlin need a build, so a
     project that has them adds an Analyze job for each.
   - It runs the `security-extended` suite, the one the baseline gave default setup
     ([ADR-0030](0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md)).
2. `tools/Set-RepositoryBaseline.ps1` counts a workflow as CodeQL's setup when its top-level `on:` names `pull_request`
   and a line that isn't a comment uses `github/codeql-action/analyze@`. A CodeQL workflow that runs only on push
   doesn't count, so default setup keeps deciding there. The baseline reads the workflow's text alone: it doesn't check
   that the workflow is enabled or has uploaded an analysis, because a new project's first baseline run comes
   before any analysis exists. Where a workflow counts, which amends ADR-0030 decision 1 for such repositories:
   - a ruleset the baseline creates or rewrites gets the code-scanning rule whatever the default setup says, with no
     suite switch, and the baseline prints neither the suite notice nor ADR-0030's no-language warning;
   - the baseline turns the default setup off before it writes the ruleset, so GitHub accepts the workflow's results; a
     refusal fails the repository with nothing changed, as a refused suite switch does;
   - on a run that leaves the ruleset alone, the turn-off is reported as set, already set, would set or not available,
     and never fails the run;
   - the line names the languages default setup analyzed, so the owner can add one the workflow lacks;
   - `-RequireChecks auto` leaves out `CodeQL languages`, as it leaves out `Analyze (*)`: the code-scanning rule covers
     it.
3. A fork's author brings its branch up to date, and the maintainer approves it, as ADR-0022 decision 4 says, and never
   updates it. The contributor's update is their push, so the maintainer's approval, which counts because the
   maintainer isn't the author and can write, meets approval of the last push. `/address-feedback` stops on a fork's
   pull request and says its author pushes the fixes and brings the branch up to date.
4. `strict_required_status_checks_policy` and `require_last_push_approval` stay as they are.

## Options considered

- Drop the code-scanning rule, or set its thresholds to `none`. Rejected: the rule also blocks while a required tool's
  analysis is in progress or not configured, whatever the thresholds, and dropping it loses the SAST gate on every pull
  request; ADR-0030 never removes a kept rule.
- Turn strict checks off in a repository with one writer. Rejected: `/run-issues` lands plans side by side, and two pull
  requests that each pass could break `main` together.
- Turn approval of the last push off. Rejected: a maintainer could push to a bot's or a fork's pull request and approve
  it themselves, past NCode Review's gate.
- A `pull_request`-mode bypass for repository admins. Rejected: the issue rules it out; a bypass is the workaround this
  decision replaces.
- Have the approver app approve a fork's pull request after `/review`. Rejected:
  [ADR-0043](0043-ncode-review-reviews-a-forks-ready-pull-request-and-never-approves-it.md) decides that NCode Review
  never approves a fork's pull request, whatever `approve` says, because its author can't write to the repository and
  can try to steer the verdict through the diff.
- A project-owned CodeQL workflow, and a fork's author who updates its own branch (chosen).

## Consequences

- Each pull request costs Actions minutes for CodeQL.
- A new project's baseline run turns default setup off and adds the rule right after the first push, so ADR-0030's
  no-language case doesn't arise with the workflow.
- An existing project's Analyze jobs fail between its template update and the baseline run, because GitHub refuses the
  workflow's results while default setup is on. The update's note says to run the baseline once the update merges.
- A language default setup analyzed that needs a build (Go, Swift, Kotlin) goes unanalyzed until the project adds an
  Analyze job for it, which the baseline's line names.
- An organization security configuration that enforces default setup makes the baseline fail or report until an owner
  sets code scanning to "Enabled with advanced setup allowed" or leaves it out of the configuration.
  `-OrganizationPolicy` names each such configuration.
- A contributor new to GitHub waits for a maintainer's approval before CodeQL runs too
  ([ADR-0039](0039-fork-pull-request-workflows-run-for-established-contributors.md)).
- A fork's pull request whose author doesn't update it can't merge while it is behind. The maintainer closes it or opens
  the change again from a branch of their own after reading every file.
- A workflow that is disabled, or fails before it uploads, leaves the code-scanning rule waiting for a result, so every
  pull request waits until it is fixed; the baseline doesn't detect that.
- A run that can't read a repository's workflows prints a line saying so and decides the rule and the suite from
  the default setup, as before this record; the next run that reads them turns the default setup off.

## References

- `.github/workflows/codeql.yml`, `tools/Set-RepositoryBaseline.ps1` and `tests/Test-RepositoryBaseline.ps1`
- GitHub Docs, [Code scanning setup types](https://docs.github.com/en/code-security/concepts/code-scanning/setup-types),
  [Available rules for rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets),
  [Resource not accessible](https://docs.github.com/en/code-security/reference/code-scanning/troubleshoot-analysis-errors/resource-not-accessible)
  and
  [CodeQL code scanning for compiled languages](https://docs.github.com/en/code-security/code-scanning/creating-an-advanced-setup-for-code-scanning/codeql-code-scanning-for-compiled-languages)
- [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md),
  [ADR-0030](0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md),
  [ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md),
  [ADR-0039](0039-fork-pull-request-workflows-run-for-established-contributors.md) and
  [ADR-0043](0043-ncode-review-reviews-a-forks-ready-pull-request-and-never-approves-it.md)
- [NCodeGroup/project-template#537](https://github.com/NCodeGroup/project-template/issues/537),
  [NCodeGroup/NCode.Disposables#95](https://github.com/NCodeGroup/NCode.Disposables/pull/95)
