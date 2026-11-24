# 0035. A sole maintainer dismisses the Scorecard alerts one person can't clear

- **Status:** Accepted
- **Date:** 2026-11-20
- **Amends:** 0020, 0037

## Context

`scorecard.yml` uploads Scorecard's findings to code scanning, which keeps an alert open for each check that scores
under 10 ([ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md)). Where one person has write access,
three checks can't reach 10. These are the scores on NCodeGroup/PropertyBag on 2026-11-20:

- Branch-Protection scores 4, because the ruleset requires no approval where one person can write
  ([ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md)).
- CII-Best-Practices scores 2 for in progress, 5 for passing, 7 for silver and 10 for gold. Silver needs a second
  owner (`access_continuity`), and gold needs two-person review, a bus factor of 2 and the organization's two-factor
  settings.
- Contributors scores 0, because 10 needs recent contributors from at least 3 companies, each with 5 of the last 30
  commits (Scorecard's `docs/checks.md`, read 2026-11-20).

Code-Review (10, from NCode Review's approvals and the maintainer's approvals of bots' pull requests) and Maintained
(10) reach 10 with one person. PropertyBag's maintainer dismissed alerts #8 and #14 by hand, in words of his own.

## Decision

With one writer, the alerts of the three checks are dismissed with `dismissed_reason` `won't fix` and a standard
comment, each under GitHub's 280-character limit:

| Check | Score one writer reaches | Dismissal comment |
| --- | --- | --- |
| Branch-Protection | 4 | `One person has write access. GitHub counts no approval from the author or from NCode Review, so a required approval or last-push approval would block every merge, and the ruleset requires neither (project-template ADR-0034).` |
| CII-Best-Practices | 5 (passing) | `One person maintains the project. Silver needs a second owner, and gold needs two-person review and a bus factor of 2, so one person reaches passing at most and the check stays below 10 (project-template ADR-0035).` |
| Contributors | 0 | `One person maintains the project. The check scores 10 only with recent contributors from at least 3 companies, each with 5 of the last 30 commits, which one person can't meet (project-template ADR-0035).` |

1. `tools/Dismiss-ScorecardAlerts.ps1 -Repo <owner>/<name>` in the project template does it, run from its clone with
   `-WhatIf` first. It reads who can write and dismisses nothing with two writers or more. It lists every open Scorecard
   alert first, dismisses those of the three checks and leaves every other alert open.
2. `tools/Set-RepositoryBaseline.ps1` ends the run for a public repository with one writer with a notice that names the
   helper.
3. `/best-practices` counts the writers. It reports a check with a dismissed alert and no open one as accepted, with
   its reason, never recommends a required approval or approval of the last push with one writer, and names the helper
   for the three checks.

## Options considered

- Leave the alerts open. Rejected: they never close, and each run of Scorecard reports them again.
- Dismiss them by hand. Rejected: each project words the reason differently, and the reason is the record.
- Dismiss Code-Review and Maintained too. Rejected: one person clears both, so a dismissal would hide a real finding.
- A Node.js helper, [ADR-0021](0021-tooling-is-written-in-nodejs-then-go-then-powershell.md)'s first choice. Rejected:
  the helper is glue around `gh api` calls, beside the PowerShell baseline it mirrors.
- A PowerShell 7 helper (chosen).

## Consequences

- The score doesn't change, only the alerts.
- GitHub keeps a dismissed alert dismissed when Scorecard finds it again. A new alert for one of the checks needs the
  helper again.
- Once a second person has write access, the Branch-Protection alert is reopened, since the baseline then requires the
  approval again.

## References

- `tools/Dismiss-ScorecardAlerts.ps1` and `tests/Test-ScorecardAlerts.ps1` in the project template's repository
- [Scorecard checks](https://github.com/ossf/scorecard/blob/main/docs/checks.md), read 2026-11-20
- [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md),
  [ADR-0021](0021-tooling-is-written-in-nodejs-then-go-then-powershell.md),
  [ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md) and
  [ADR-0037](0037-best-practices-reports-scorecard-and-answers-for-the-default-branch.md)
- NCodeGroup/project-template#456 and #386
