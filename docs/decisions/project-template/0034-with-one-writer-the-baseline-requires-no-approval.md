# 0034. With one writer, the baseline requires no approval

- **Status:** Accepted (amended by 0036)
- **Date:** 2026-11-20
- **Amends:** 0005, 0006, 0019, 0020, 0022, 0031 and, where a project has it, 0150

## Context

[ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) has `tools/Set-RepositoryBaseline.ps1` in the project
template give "Protect main" one required approval, of the last push, where `code-review.yml` passes NCode Review's
`approve: true`. It assumes that NCode Review's approval meets the requirement.

GitHub says "collaborators can only push changes to a protected branch via a pull request that is approved by the
required number of reviewers with write permissions" (GitHub Docs, "About protected branches", read 2026-11-20), and an
author can't approve their own pull request ([ADR-0031](
0031-the-baseline-turns-off-approval-of-the-last-push-where-no-approval-is-required.md)). Approval of the last push
needs someone other than the pusher as well (#385, ADR-0031).

NCodeGroup/PropertyBag#53 showed what that means where one person has write access. The ruleset required one approval,
approval of the last push and dismissal of stale reviews. `ncode-review[bot]` approved the head commit (`f306975`) and
every check passed, yet GitHub showed "Approvals, 1 still required" and the pull request's `mergeable_state` was
`blocked`. Each of the bot's reviews has `author_association` `NONE`: its app has no write access. Scorecard's
Code-Review check counts any approval that is not the author's, so the same approval still scores: on 2026-11-20
NCodeGroup/PropertyBag scored 10 for Code-Review and 4 for Branch-Protection.

## Decision

1. The baseline counts the users that the collaborators list gives with push access or more
   (`repos/<owner>/<name>/collaborators?affiliation=all&permission=push&per_page=100`; one page is enough).
2. With one such user, or none, "Protect main" requires no approval and no approval of the last push, even where
   `code-review.yml` passes `approve: true`, and keeps the dismissal of stale reviews. A run says why. A run that finds
   an earlier "Protect main" that requires an approval turns both settings off and says so (`require no approval and no
   approval of the last push, which block every merge while one person has write access`): a plain run changes only
   those settings and conversation resolution, `-RequireChecks` writes them off in the whole ruleset, and `-WhatIf`
   says it would. Where GitHub refuses the change on a plain run, the run reports it, prints the command that turns
   both settings off in it, as decision 5 does for a ruleset of the owner's, and goes on.
3. With two such users or more, ADR-0022 holds as written: one approval, of the last push, where `approve: true`.
   The approval that meets it is another writer's: NCode Review's approval never counts toward it, and counts for
   Scorecard's Code-Review.
4. A list the script can't read is named, with the reason. A new ruleset then gets no approval, `approve: true` raises
   none, and an earlier ruleset keeps the approvals it has: requiring an approval that nobody may be able to give is
   worse than missing one.
5. A ruleset of the owner's keeps its shape, as ADR-0031 says: the baseline writes nothing in it for this, and with one
   writer prints a notice with the command that requires no approval and no approval of the last push in it, as
   ADR-0031's notice does for approval of the last push.
6. NCode Review keeps `approve: true` and gets no write access. An app installed across the organization's
   repositories with write access could push to each of them, far more than a review needs, and
   [ADR-0006](0006-a-person-merges-every-pull-request.md) keeps the merge with a person. Its approval still
   counts for Scorecard's Code-Review.
7. This amends ADR-0022's decision 2 and its "about 8" for Branch-Protection, which needs a second writer. It adds a
   second way the script loosens its own ruleset, beside ADR-0031's, because either setting blocks every merge where
   one person can write. So ADR-0031's decision 6, which says the script loosens its own "Protect main" "in this one
   way alone", reads "in these two ways".
8. GitHub counts no approval from NCode Review toward a required approval, whatever the number of writers, while its app
   has no write access. A required approval is met only by someone with write access other than the author; NCode
   Review's approval is a signal the person who merges reads, and counts for Scorecard's Code-Review. So the records
   that say its approval meets a required approval read that way, and their text stays as written, as the decision
   records' README asks: [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md)'s Context
   and its rejected option of one approving review; [ADR-0006](0006-a-person-merges-every-pull-request.md)'s Context
   ("its approval is the formal one") and its rejected option of one approving review;
   [ADR-0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md)'s consequence that an
   outage blocks a merge where the ruleset requires NCode Review's approval, since no ruleset waits for its approval and
   an outage blocks no merge; ADR-0022's title, decision 2 and its consequence that an outage blocks merges, for the
   same reason; ADR-0031's decision 5, where dismissing stale reviews makes a writer's approval, not NCode Review's,
   cover the latest push; and, where a project has record 0150, its consequence that a workflow token's approval counts
   "where NCode Review's approval is required", which reads: where the ruleset requires an approval.

## Options considered

- Give the NCode Review app write access. Rejected: the app is installed across the organization's repositories, and
  write access lets it push to every one of them.
- Have NCode Review stop approving. Rejected: Code-Review falls from 10, and the approval is a signal the person who
  merges reads.
- Keep requiring the approval. Rejected: no pull request merges.
- Have the maintainer untick the settings by hand. Rejected: each run of the baseline turned them back on.
- Count the writers and require no approval with one (chosen).

## Consequences

- A project with one writer stays at Branch-Protection tier 1, score 4. A second writer brings ADR-0022's approval back
  on the next run of the baseline.
- A sole maintainer should expect the Branch-Protection alert (High; `Warn:` "does not require approvers", "codeowners
  review is not required", "'last push approval' is disabled"), and the CII-Best-Practices and Contributors alerts, and
  dismisses them as [ADR-0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md) says.
- The script loosens its own ruleset in a second way, and makes no other ruleset looser. The two settings it turns off
  could not be met, so nothing a merge could have waited for goes.

## References

- `tools/Set-RepositoryBaseline.ps1` and `tests/Test-RepositoryBaseline.ps1` in the project template's repository
- [About protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches),
  GitHub Docs, read 2026-11-20
- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
  [ADR-0006](0006-a-person-merges-every-pull-request.md),
  [ADR-0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md), [ADR-0020](
  0020-github-projects-are-scored-by-openssf-scorecard.md), [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md),
  [ADR-0031](0031-the-baseline-turns-off-approval-of-the-last-push-where-no-approval-is-required.md) and
  [ADR-0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md)
- NCodeGroup/project-template#455, #385 and #382
