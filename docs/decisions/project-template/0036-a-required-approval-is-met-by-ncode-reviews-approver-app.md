# 0036. A required approval is met by NCode Review's approver app

- **Status:** Accepted (amended by 0041)
- **Date:** 2026-11-21
- **Amends:** 0022, 0034 and, where a project has it, 0150

## Context

[ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md) holds that NCode Review's approval can't count
toward a required approval, because its app has no write access and every one of its reviews has `author_association`
`NONE`.

#460 measured what GitHub counts:

- An approval counts toward a ruleset's required approval when its reviewer can write to the repository
  (`authorCanPushToRepository`). It counts also when the token that submitted it can only write pull requests.
  GitHub checks the reviewer's write access when it reads the review.
- `author_association` is `NONE` for an app's review either way, so it says nothing about whether the approval counts.
- Any workflow with `pull-requests: write` approves its own pull request while the setting "Allow GitHub Actions to
  create and approve pull requests" is on, so a required approval is a gate only while no writer's workflow can
  approve.

NCodeGroup/code-review#232 adds the NCode Review Approver, an app with Contents: write whose token, minted after the
review, can only write pull requests.

## Decision

1. NCode Review approves as the NCode Review Approver (NCodeGroup/code-review#232, its ADR-1011). The app is installed
   on the repositories that approve, with Contents: write and Pull requests: write. Its token is minted after the review
   with Pull requests: write only. Its key is a secret of the `code-review` environment, which only the default branch
   can deploy to.
2. Where the approver is set up, the baseline requires one approval, of the last push, with one writer too. The approver
   is set up when the default branch's `.github/workflows/code-review.yml` passes `approve: true` and a non-empty
   `approver-private-key:`, and the repository's `code-review` environment has the secret
   `NCODE_REVIEW_APPROVER_PRIVATE_KEY`. A run checks both, since requiring an approval that nobody can give blocks every
   merge, and names the fix when the secret is missing. Elsewhere ADR-0034 holds: no approval with one writer, one
   approval with two or more. This amends ADR-0034's decisions 2, 6 and 8 and ADR-0022's decision 2.
3. Every repository and the organization's defaults keep GitHub Actions from approving pull requests and give the
   workflow token read-only access (#461). `tools/Set-RepositoryBaseline.ps1` sets the repository's, and its
   `-OrganizationPolicy` sets the organization's, whether or not the repository has `raise-baseline.yml`.
4. `raise-baseline.yml` pushes `baseline-<version>` and opens an issue whose link opens the pull request, with its
   title, description and `Closes #<issue>` filled in. It opens no pull request itself, so it needs neither the setting
   nor a key, and a pull request a person opens starts CI and NCode Review's review. Where a project has record 0150,
   this amends its decision on the setting and its consequence that a workflow token's approval counts.
5. A maintainer approves a bot's pull request, which NCode Review skips.

## Options considered

- Give NCode Review's own app write access. Rejected: that app is installed wherever NCode Review runs, and write
  access lets it push to every one of those repositories.
- Approve with `GITHUB_TOKEN`. Rejected: the setting that allows it lets any writer's workflow approve its own pull
  request.
- Keep requiring no approval with one writer. Rejected: the approval can count, so the ruleset can ask for it.
- A dedicated approver app, installed only where approval is wanted (chosen).

## Consequences

- An NCode Review outage or budget stop blocks merges where the approval is required, until it is fixed or the ruleset
  changes. This amends the consequences of ADR-0019 and ADR-0022.
- A leaked approver key can mint a Contents: write token for the repositories the approver is installed on.
- The baseline pull request needs a person to open it from the issue's link.
- A project's pin reaches NCode Review v1.5.0 before the action knows `approver-private-key`; until then it warns that
  it doesn't know the input and goes on.

## References

- `tools/Set-RepositoryBaseline.ps1`, `template/.github/workflows/code-review.yml` and
  `template/.github/workflows/raise-baseline.yml` in the project template's repository
- [ADR-0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md),
  [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md),
  and [ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md)
- NCodeGroup/project-template#460 and #461, NCodeGroup/code-review#232
