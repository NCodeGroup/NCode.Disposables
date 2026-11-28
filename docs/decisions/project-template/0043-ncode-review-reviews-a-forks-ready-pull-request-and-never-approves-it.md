# 0043. NCode Review reviews a fork's ready pull request and never approves it

- **Status:** Accepted
- **Date:** 2026-11-27
- **Amends:** 0019 and 0022

## Context

`code-review.yml` admitted only pull requests whose head is in the repository
(`head.repo.full_name == github.repository`) in both jobs, so an outside contribution got no review and recorded no
outcomes.
[ADR-0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md) decision 3 and
[ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) decision 4 leave a fork's pull request to a maintainer.

- NCodeGroup/code-review v1.7.0 adds the `review-forks` input (NCodeGroup/code-review#251, its ADR-1013). A fork's pull
  request is reviewed when it opens or reopens as ready for review, when it's marked ready and on a writer's `/review`,
  drafts included, and never as a draft when it opens.
- Its review is never an approval, whatever `approve` says: its author can't write to the repository and can try to
  steer the verdict through the diff. So the summary's `Approval:` line says a maintainer approves it, and the NCode
  Review Approver's token isn't minted.
- Nothing from the fork is built or run: the engine fetches the commit from `refs/pull/<number>/head` only to read it,
  and the author's description, commit subjects and one-click suggestions stay out.
- Without the input the engine skips a fork's pull request even when the `if:` admits it.
- `pull_request_target` runs the default branch's workflow, so a pull request can't change its own review.

## Decision

1. `code-review.yml` pins v1.7.0 or later and passes `review-forks: true` to both NCode Review steps.
2. The `review` job's `if:` admits a fork's pull request once it isn't a draft, with
   `(github.event.pull_request.head.repo.full_name == github.repository || !github.event.pull_request.draft) &&`. The
   `skip-review` label, the bot and push rules, Dependabot's among them
   ([ADR-0041](0041-ncode-review-reviews-and-approves-dependabots-pull-requests.md)), are unchanged.
3. The `outcomes` job's `if:` is `github.event_name == 'pull_request_target' && github.event.action == 'closed'`, so a
   fork's closed pull request records its outcomes.
4. A maintainer approves a fork's pull request. This amends ADR-0019 decision 3, where the workflow now skips a fork's
   draft until a writer comments `/review`, rather than every fork's pull request, and ADR-0022 decision 4, where NCode
   Review reviews a fork's pull request but leaves its approval to a maintainer.
5. A project leaves forks out by dropping `review-forks` from both steps, putting
   `github.event.pull_request.head.repo.full_name == github.repository &&` back in place of that `if:` line, and adding
   `&& github.event.pull_request.head.repo.full_name == github.repository` to the end of the `outcomes` job's `if:`.

## Options considered

- Keep forks out. Rejected: an outside contribution gets no review.
- Review a fork's draft too. Rejected: the engine never reviews a fork's draft when it opens, and each stranger's draft
  would cost a model call.
- Review each push to a fork's pull request. Rejected: a push starts no review for anyone but Dependabot, and a writer
  comments `/review`.
- Fork reviews once ready, never approved (chosen).

## Consequences

- Each ready fork pull request costs a review, and a burst from many accounts costs many: set GitHub's interaction
  limits or turn the input off.
- The `review` job holds the Bedrock key, the NCode Review app's key and the approver's key while it reads a fork's
  diff, as it does for any pull request. `pull_request_target` runs only the default branch's workflow, and nothing
  from the fork is built or run in the job: the engine reads its diff only as text, and the approver's token isn't
  minted for it (NCodeGroup/code-review's ADR-1013).
- Where the ruleset requires an approval, a fork's pull request still waits for a maintainer's.
- Whether it can merge under the baseline ruleset is unchanged (NCodeGroup/project-template#537).
- A project's `code-review.yml` is its own: a template update shows this change to it, and the project takes it in its
  template-update plan.

## References

- `.github/workflows/code-review.yml`
- [ADR-0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md),
  [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) and
  [ADR-0041](0041-ncode-review-reviews-and-approves-dependabots-pull-requests.md)
- [NCodeGroup/code-review ADR-1013](https://github.com/NCodeGroup/code-review/blob/main/docs/decisions/1013-a-forks-pull-request-is-reviewed-on-opt-in-once-ready-and-never-approved.md)
- NCodeGroup/project-template#540, NCodeGroup/code-review#251, #255 and #257
