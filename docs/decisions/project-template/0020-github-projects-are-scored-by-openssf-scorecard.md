# 0020. A GitHub project is scored by OpenSSF Scorecard

- **Status:** Accepted (amended by 0022, 0025, 0034, 0035, 0037, 0041 and 0044)
- **Date:** 2026-11-14

## Context

OpenSSF Scorecard grades a repository's supply-chain practice from outside, with a score from 0 to 10 over a fixed set
of checks. The Buffers library, built from this template, scored 7.8 on 2026-11-15 (Scorecard v5.6.0). The table follows
Scorecard's source. Some checks the template controls; others need a person, a second reviewer or a registration the
template cannot make.

On Scorecard v5.7.0, three projects from the template, each with one writer and NCode Review's approver set up
([ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)), scored 9.4 (NCodeGroup/Buffers,
2026-11-24), 9.5 (NCodeGroup/PropertyBag, 2026-11-24) and 8.2 (NCodeGroup/code-review, 2026-11-27): Branch-Protection
8, CII-Best-Practices 5 (passing) and Contributors 0 to 3 on all three, Signed-Releases 10 on the two libraries, and
Code-Review 8 to 10 as approved pull requests fill the last 30 commits
([#437](https://github.com/NCodeGroup/project-template/issues/437)).

## Decision

Every GitHub project runs `.github/workflows/scorecard.yml`. The workflow publishes the score for the README badge and
uploads its findings to code scanning. A private repository must list `.github/workflows/scorecard.yml` in its
`project_owned_files` answer and delete it, because publishing the score needs a public repository: the template cannot
see a repository's visibility, so it does not leave the workflow out by itself.

The table lists each check, what the template does for it and what needs a person.

| Check | The template | Needs a person |
| --- | --- | --- |
| Binary-Artifacts | Commits no binary files. | Nothing. |
| Dangerous-Workflow | No workflow runs untrusted pull request code with a privileged token. | Nothing. |
| Pinned-Dependencies | Workflow actions are pinned by commit SHA ([ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md)). | Nothing. |
| Token-Permissions | Every workflow's top-level token is read-only or empty. Job-level write is only where a job needs it: `issues: write` in `template-update.yml`; `contents: write`, `actions: write` and `id-token: write` in `tag-release.yml`; `contents: write` and `pull-requests: write` in `code-review-auto-merge.yml`; `security-events: write` in `codeql.yml`; `id-token: write` in `code-review.yml`; `security-events: write` and `id-token: write` in `scorecard.yml`; and, for a library, `id-token: write` in `publish.yml`; `attestations: write`, `contents: write` and `id-token: write` in `release-provenance.yml`; and `contents: write`, `pull-requests: write` and `actions: write` in `raise-baseline.yml`. The called workflows get what `publish.yml`'s provenance and baseline jobs grant them; job-level write warns and still scores 10. | Nothing. |
| Dependency-Update-Tool | Dependabot is configured. | Nothing. |
| Security-Policy | `SECURITY.md` ships. | Nothing. |
| License | `LICENSE.txt` ships, for the `Apache-2.0` license ([ADR-0024](0024-a-project-is-licensed-under-apache-2-0-unless-it-answers-otherwise.md)). | Nothing. |
| SAST | `codeql.yml` runs CodeQL on every pull request, a fork's too, and the ruleset's code-scanning rule requires its results ([ADR-0044](0044-a-fork-pull-request-merges-without-a-bypass.md)). | Nothing. |
| Vulnerabilities | Dependency and vulnerability scanning is configured. | Fix the findings it reports. |
| CI-Tests | Pull requests run the CI workflow. | Nothing. |
| Maintained | Scheduled automation keeps the repository active. | Keep committing and answering issues. |
| Signed-Releases | `publish.yml` calls `release-provenance.yml`, attaching packages, a signed `<tag>.intoto.jsonl` and an SBOM ([ADR-0139](0139-releases-carry-build-provenance.md), [ADR-0151](0151-release-follow-ups-are-jobs-publish-calls.md)), so each such release scores 10. The files arrive after a release is published, so the releases stay mutable ([ADR-0028](0028-github-releases-are-immutable-and-their-tags-signed.md)). | Of the five newest releases, those with assets are averaged: delete older unsigned assets (packages stay on nuget.org) or wait five releases. `release-provenance.yml`'s `workflow_dispatch` backfills a release only while the Publish run's `packages` artifact is kept (14 days); never attest packages downloaded from a registry, which claims a build the workflow never ran. |
| Packaging | `publish.yml` publishes the packages. | The first successful `publish.yml` run. |
| Branch-Protection | The ruleset blocks deletion and force pushes, requires a pull request, up-to-date checks and resolved conversations, and allows no bypass, so it applies to administrators. As this project's `code-review.yml` passes `approve: true`, it also dismisses stale approvals and, where NCode Review's approver is set up or a second person has write access, requires one approval, of the last push ([ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md), [ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md), [ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)), which passes tier 2, and tier 2 gates every later tier: the score is 8, with one writer too where the approver is set up. With one writer and no approver it requires neither, since GitHub counts no approval from the author or from NCode Review's own app, and the score stays at 4. It never requires approval of the last push without an approval. | Set up NCode Review's approver, or give a second person write access, for tier 2 ([ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)). 9 and 10 need tier 4's two approvals and code owners' review, which one writer can't get: at 8, Scorecard warns `required approving review count is 1` and `codeowners review is not required`. With one writer, dismiss the alert ([ADR-0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md)). |
| Code-Review | Any non-author approval counts, a bot's included: NCode Review approves a clean review, Dependabot's pull requests included ([ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md), [ADR-0041](0041-ncode-review-reviews-and-approves-dependabots-pull-requests.md)). | Approve each pull request another bot opens, which NCode Review reviews only after a `/review`; the score rises toward 10 over the last 30 commits. |
| Contributors | None. | Recent contributors from at least 3 companies, each with 5 of the last 30 commits. With one writer, dismiss the alert ([ADR-0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md)). |
| Fuzzing | Nothing: the template adds no property tests or package. | Write property tests with the stack's property-test library, for .NET `FsCheck.Xunit` with `[Property(Replay = Seed)]`, which Scorecard detects. |
| CII-Best-Practices | The six documents ([ADR-0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md)) and `docs/best-practices-criteria.md`, which maps every criterion to the answer to expect and its evidence ([ADR-0025](0025-every-best-practices-criterion-is-mapped-in-a-template-owned-page.md)). | Register at https://www.bestpractices.dev: in progress 2, passing 5, silver 7, gold 10; silver needs `access_continuity`, a second owner of the organization and the package registry; gold needs two-person review, two unassociated contributors, the organization's two-factor settings and a security review. With one writer, passing (5) is the most; dismiss the alert ([ADR-0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md)). |

## Options considered

- Requiring one approving review in the ruleset. Chosen where NCode Review approves (ADR-0022); elsewhere a sole
  maintainer could not merge.
- Running Scorecard without publishing the score. Rejected: the badge and code scanning findings are the point.
- Running the Scorecard workflow in every GitHub project (chosen).

## Consequences

- A solo project's score tops out below 10, for the reasons in the table's last column: Contributors,
  Branch-Protection's tier 4 and silver's second owner need other people. With NCode Review, Code-Review rises toward 10
  and Branch-Protection reaches 8 where its approver is set up, with one writer too, or a second person has write access
  ([ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md),
  [ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)); with one writer, the alerts ADR-0035
  lists are dismissed.
- A private repository lists the workflow in `project_owned_files` and deletes it; until it does, the workflow
  fails to publish its score.

## References

- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md): the default branch accepts changes
  only through pull requests.
- [ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md): external inputs are pinned and kept current.
- [ADR-0139](0139-releases-carry-build-provenance.md): releases carry build provenance.
- [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md): where NCode Review approves, the ruleset requires an
  approval.
- [ADR-0034](0034-with-one-writer-the-baseline-requires-no-approval.md): with one writer, the baseline requires no
  approval.
- [ADR-0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md): a sole maintainer
  dismisses the Scorecard alerts one person can't clear.
- [ADR-0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md): a required approval is met by NCode
  Review's approver app.
- [ADR-0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md): a project ships the
  documents the Best Practices silver level asks for.
- [ADR-0025](0025-every-best-practices-criterion-is-mapped-in-a-template-owned-page.md): every Best Practices criterion is
  mapped in a template-owned page.
- [ADR-0151](0151-release-follow-ups-are-jobs-publish-calls.md): release follow-ups are jobs `publish.yml` calls.
- `.github/workflows/scorecard.yml`
- Scorecard's `checks/evaluation/branch_protection.go`, `checks/evaluation/signed_releases.go` and
  `probes/codeApproved/impl.go`
