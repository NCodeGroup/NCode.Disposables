# 0041. NCode Review reviews and approves Dependabot's pull requests

- **Status:** Accepted
- **Date:** 2026-11-27
- **Amends:** 0006, 0020, 0022 and 0036

## Context

`code-review.yml` starts an automatic review only for an event a person sent, so NCode Review never reviews a pull
request Dependabot opens: [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) decision 4 and
[ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md) decision 5 leave its approval to a
maintainer. Where NCode Review's approver is set up, the ruleset requires one approval, so every Dependabot pull request
waits for a maintainer's `/review` or approval, the NCode Review pin bump that `code-review-auto-merge.yml` turns
auto-merge on for included.

- GitHub's Dependabot restrictions, a read-only token and only Dependabot secrets, apply to `push`, `pull_request`,
  `pull_request_review` and `pull_request_review_comment` events, not to `pull_request_target`, which runs the default
  branch's workflow with the repository's secrets.
- A `/review` on a Dependabot pull request already runs the review, and the approver approves a clean one
  (NCodeGroup/code-review#242, #244 and #248).
- NCode Review checks no author: the workflow's `if:` is the only gate. It leaves lockfiles out of the reviewed diff,
  and reads the code without building or running it.
- Dependabot pushes to its branch when it rebases or recreates it. A push dismisses the approval, and the ruleset
  requires approval of the last push.
- NCode Review pauses automatic reviews after three reviews with no comment from someone with write access in between,
  until such a person comments `/review`.

## Decision

1. `code-review.yml` reviews a pull request that Dependabot (`dependabot[bot]`) opened from the repository itself
   automatically, as it reviews a person's. Another bot's pull request is reviewed only when someone with write access
   comments `/review`.
2. The review job also runs on `synchronize`, only when Dependabot pushed to its own pull request, so the approval
   follows Dependabot's latest commit. A push by anyone else starts no review.
3. The review passes `approve: true` for Dependabot's pull requests too, the NCode Review pin bump included: where the
   ruleset requires an approval, the approval of a clean review meets it, and `code-review-auto-merge.yml` merges a
   minor or patch bump of the pin with no person's action. This amends ADR-0006's exception, which waits for a
   maintainer's approval where one is required, ADR-0022 decision 4, ADR-0036 decision 5 and ADR-0020's Code-Review
   row.
4. A Dependabot pull request adds no changelog entry: Dependabot can't write one, and its updates ship with the next
   release. The template's rules say so, so a review asks for none.

## Options considered

- Every bot's pull request. Rejected: no template workflow opens a pull request (ADR-0036 decision 4), and a person
  marking a Copilot coding agent's draft ready already starts its review.
- A review only when Dependabot opens the pull request. Rejected: its rebase dismisses the approval and starts nothing,
  so the pull request waits for a maintainer again.
- `approve: false` for the pin bump, or an auto-merge that waits for a person. Rejected: ADR-0006's exception rests on
  the release having been reviewed and merged by a person in NCodeGroup/code-review, not on an approval of the pin;
  where no approval is required, the bump already merges with no person's action; and the review runs the old pin
  from the default branch, so the new release gets no key before the bump merges, as with a maintainer's approval.
- Dependabot's pull requests and its own pushes (chosen).

## Consequences

- A clean review approves Dependabot's pull requests with no maintainer. A person still merges each one, except the
  pin bump.
- Where NCode Review's approver is set up, a minor or patch bump of the NCode Review pin merges with no person's action
  once the required checks pass, after Dependabot's three-day cooldown
  ([ADR-0009](0009-new-dependency-versions-wait-a-cooldown.md)). A finding left open on it keeps it from merging, and
  `gh workflow disable code-review-auto-merge.yml` stops it.
- Each Dependabot pull request costs a review, and each push Dependabot makes to it another. Its updates come weekly
  and grouped ([ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md)), and lockfiles are left out of the
  reviewed diff, so the cost is small.
- After three automatic reviews with no comment from someone with write access, such as an opening and two rebases,
  NCode Review pauses and says so. A `/review` from such a person resumes it, and any comment of theirs, such as
  `@dependabot rebase`, starts the count again.
- A review that leaves a blocker or should-fix finding open approves nothing, so that pull request waits for a person,
  as a person's does.
- A project's `code-review.yml` is its own: a template update shows this change to it, and the project takes it in its
  template-update plan.

## References

- `.github/workflows/code-review.yml` and `.github/workflows/code-review-auto-merge.yml`
- [ADR-0006](0006-a-person-merges-every-pull-request.md),
  [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md),
  [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) and
  [ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)
- NCodeGroup/project-template#519, NCodeGroup/code-review#253
