# 0153. /best-practices reports the Scorecard gaps and answers for the default branch

- **Status:** Accepted (amended by 0035)
- **Date:** 2026-11-20

## Context

A by-hand OpenSSF Scorecard session on NCodeGroup/PropertyBag found four gaps in the `/best-practices` command
([#451](https://github.com/NCodeGroup/project-template/issues/451)). It reported the Best Practices badge criteria but
not the Scorecard checks the badge is one of. It wrote a registration prompt for a project that bestpractices.dev
already listed, because it knew only the number the project had recorded. It answered for `HEAD`, which can hold
evidence that Scorecard and bestpractices.dev, which read the default branch, can't see yet. And it left
`no_leaked_credentials` open whenever `gitleaks` wasn't installed.

## Decision

- For a GitHub project the command reads Scorecard's API and gives each check scoring under 10 its action and who
  takes it: a person, or a command the person runs.
- It looks a registration up at `projects.json?url=` by the repository's address before it writes a registration
  prompt, and uses the number it finds.
- It answers for the tip of `origin/<default branch>`, in a temporary worktree when the checkout isn't at it, and
  reports evidence that exists only in unmerged commits apart, never as a Met answer.
- It fetches `gitleaks` only with checksum verification, from the newest release older than the cooldown of ADR-0009,
  into a temporary folder it deletes afterwards.
- It changes no tracked file, ruleset, release or registration; it names the command or setting a person runs.

## Options considered

- A separate `/scorecard` command. Rejected: the badge is one Scorecard check, and both answer what raises the score.
- Answer for `HEAD`. Rejected: Scorecard and bestpractices.dev see the default branch, so an answer for `HEAD` can
  promise what they don't yet show.
- Skip `gitleaks` when it is missing. Rejected: `no_leaked_credentials` would stay open every time.

## Consequences

- A project that is already registered isn't registered twice.
- The report separates what is true on the default branch from what a pending merge would change, and asks for a
  second run after the merge.
- The Branch-Protection claim in ADR-0020 stays as it is until
  [#437](https://github.com/NCodeGroup/project-template/issues/437).

## References

- [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md): the Scorecard checks and their actions.
- [ADR-0025](0025-every-best-practices-criterion-is-mapped-in-a-template-owned-page.md): the criteria page.
- [ADR-0009](0009-new-dependency-versions-wait-a-cooldown.md): the cooldown for new versions.
- [#451](https://github.com/NCodeGroup/project-template/issues/451)
