# 0019. Nothing requires the Code review check; the person who merges waits for it

- **Status:** Accepted (amended by 0022, 0034 and 0043)
- **Date:** 2026-11-14

## Context

In a project that runs NCode Review, `.github/workflows/code-review.yml` reviews a pull request when it opens, is
reopened or is marked ready for review, and when someone with write access comments `/review`; a push in between starts
no review. The review posts a "Code review" check run on the commit it reviewed, a summary comment and its findings as
review threads. The default branch's ruleset requires the CI jobs and every conversation resolved
([ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
[ADR-0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md)), but not the review:
`tools/Set-RepositoryBaseline.ps1 -RequireChecks auto` leaves out "Code review" and the `review` job, which runs on
`pull_request_target`. While the review runs, GitHub shows "Able to merge, some non-required checks are still in
progress", and conversation resolution covers only the threads that exist, so a merge before the review posts its
findings skips them.

## Decision

The review is advice: no ruleset requires the "Code review" check or the `review` job, and the person who merges waits
for the review.

1. `-RequireChecks auto` leaves out "Code review" and the `review` job, and a person who names the required checks
   leaves them out too.
2. When the review runs on a pull request, the person who merges waits for its "Code review" check to complete and for
   its threads to be remediated and replied to, as
   [ADR-0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md) says, before merging
   or turning on auto-merge.
3. When no review comes, because the workflow skips the pull request (a `skip-review` label, a fork, a pull request a
   bot opened) or the run fails, the person decides: re-run the job, comment `/review`, or merge without it. That holds
   only where no approval is required. Where the ruleset requires one
   ([ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md)), a failed run or a `skip-review` label on the
   maintainer's own pull request needs a re-run or `/review`, because the author can't approve it, and a maintainer
   approves only someone else's skipped pull request, a bot's or a fork's.
4. `/run-issues` and `/run-feedback` recommend the merge once NCode Review's findings are answered and its review of the
   last push they asked for is in; after `max-review-rounds` rounds they ask for no further review, so a push after that
   has no review before their recommendation, which doesn't replace the person's wait.

## Options considered

- Require the "Code review" check or the `review` job in the ruleset. Rejected: the check run is on the commit the
  review read and a push starts no review, so each pushed commit would wait for a `/review` comment, a model call,
  before it could merge; an outage, an expired key or a failed run would block every merge, and the ruleset has no
  bypass actor to get past it ([ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md)),
  a cost [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) accepts for a project whose review approves; and
  a skipped required job counts as passing, so a pull request the workflow skips would pass without a review all the
  same.
- Keep it advice that the person who merges waits for (chosen): once the review posts, its threads block the merge like
  any other, and an outage costs only the review, except where ADR-0022 requires the approval: there an outage blocks
  merges, a cost that record accepts.

## Consequences

- A merge before the review posts its findings skips them. Waiting is the person's step, and nothing enforces it.
- Auto-merge waits only for the required checks, so turned on before the review posts, it can merge without it: the
  person turns it on once the review is in ([ADR-0006](0006-a-person-merges-every-pull-request.md)); where
  [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) requires the approval, auto-merge waits for it too.
- An outage or a failed review never blocks a merge, except where the ruleset requires NCode Review's approval
  ([ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md)): there a merge waits for an approval, or for the
  maintainer to change the ruleset.
- A project that wants the review to gate its merges writes a record that supersedes this one, adds the check to its
  ruleset and says how a merge gets past an outage.

## References

- `.github/workflows/code-review.yml`: when the review runs, and the `skip-review` label.
- `tools/Set-RepositoryBaseline.ps1` in the project template's repository: `$excludedChecks`, the checks
  `-RequireChecks auto` never requires.
- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md): the default branch accepts changes
  only through pull requests that pass CI.
- [ADR-0006](0006-a-person-merges-every-pull-request.md): a person merges every pull request.
- [ADR-0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md): every pull request
  discussion is remediated and replied to before it merges.
- [NCodeGroup/project-template#361](https://github.com/NCodeGroup/project-template/issues/361): the decision's issue.
