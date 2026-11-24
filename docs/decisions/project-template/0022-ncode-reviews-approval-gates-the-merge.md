# 0022. Where NCode Review approves, the ruleset requires an approval and pull requests open as drafts

- **Status:** Accepted (amended by 0031, 0034 and 0036)
- **Date:** 2026-11-15
- **Amends:** 0005, 0006, 0019, 0020, 0150

## Context

OpenSSF Scorecard v5.6.0's Code-Review check (`probes/codeApproved/impl.go`) counts a review in the `APPROVED` state
from anyone but the pull request's author, a bot included. Its Branch-Protection check
(`checks/evaluation/branch_protection.go`) scores no tier after tier 2 until tier 2, one required approval, is met.
A sole maintainer's pull requests have no approving reviewer, so every review is `COMMENTED`: NCodeGroup/Buffers scored
0 on Code-Review and 4 on Branch-Protection. NCode Review v1.4.0 adds the `approve` input, which submits an approving
review when the review is clean. Last-push approval at zero approvals blocked NCodeGroup/Buffers#62.

## Decision

1. A project's `code-review.yml` passes `approve: true` to NCode Review.
2. Where the default branch's `code-review.yml` has `approve: true`, `tools/Set-RepositoryBaseline.ps1` in the project
   template gives the "Protect main" ruleset one required approval, dismissal of stale approvals on push, and approval
   of the last push together, with no bypass. Where it doesn't, the ruleset requires zero approvals and never
   last-push approval. The script never loosens a ruleset: it prints the fix only for approval of the last push with no
   required approval.
3. A new project with NCode Review sets `draft: yes`, so its workflows open pull requests as drafts. Marking one
   ready is the maintainer's review.
4. A maintainer approves the pull requests of bots, which NCode Review skips, and those from forks, which it skips too.
   No template workflow approves a pull request, though a token's approval would count.
5. A push dismisses the approval. The maintainer comments `/review` to get a new one, as `/run-feedback` and
   `/run-issues` do.
6. A project without NCode Review has no approvals.

## Options considered

- Zero approvals. Rejected: it scores 0 on Code-Review and 4 on Branch-Protection.
- A second account that approves ([ADR-0006](0006-a-person-merges-every-pull-request.md)). Rejected: a second account
  is a second identity to keep, and a person merges.
- The auto-merge workflow approving. Rejected: any workflow's token could approve, so an approval would mean nothing.
- Requiring approval without `approve`. Rejected: a sole maintainer can't meet it.
- NCode Review's `approve: true` with one required approval (chosen).

## Consequences

- Code-Review rises toward 10 as approved pull requests fill the last 30 commits.
- Branch-Protection reaches about 8; tier 4 needs two approvals and code owners.
- Scorecard's documentation describes human review, so a bot's approval may not count the way a person's does.
- An NCode Review outage blocks merges until it recovers or the maintainer changes the ruleset.
- Dependabot pull requests wait for a maintainer's approval.
- An existing project adopts this by pinning NCode Review v1.4.0, adding `approve: true`, setting `draft: yes` and
  rerunning the baseline.

## References

- `.github/workflows/code-review.yml`, `.github/workflows/code-review-auto-merge.yml`
- `tools/Set-RepositoryBaseline.ps1`
- The Landing section of `AGENTS.md`
- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md): the default branch accepts changes
  only through pull requests.
- [ADR-0006](0006-a-person-merges-every-pull-request.md): a person merges every pull request.
- [ADR-0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md): nothing requires the
  code review check.
- [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md): GitHub projects are scored by OpenSSF Scorecard.
- [NCodeGroup/code-review ADR-1009](
  https://github.com/NCodeGroup/code-review/blob/main/docs/decisions/1009-a-clean-review-approves-the-pull-request.md):
  a clean review approves the pull request.
- [NCodeGroup/project-template#382](https://github.com/NCodeGroup/project-template/issues/382)
- [NCodeGroup/project-template#385](https://github.com/NCodeGroup/project-template/issues/385)
