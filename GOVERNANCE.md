# Governance

NCode.Disposables belongs to NCode Group, under `Apache-2.0`. This page says who does
what and how decisions are made.

## Roles

| Role | Who | Responsibilities |
| --- | --- | --- |
| Maintainer | [@polewskm](https://github.com/polewskm) | Sets direction, triages issues, reviews and merges pull requests, approves each publication in the `nuget` environment, answers vulnerability reports, owns the repository's settings and the nuget.org packages |
| Contributor | Anyone | Opens issues and pull requests, under [CONTRIBUTING.md](CONTRIBUTING.md) and the [Code of Conduct](CODE_OF_CONDUCT.md) |
| Automation | `ncode-review[bot]`, `ncode-review-approver[bot]`, Dependabot, GitHub Actions | Reviews and approves a clean pull request, as `ncode-review-approver[bot]` where NCode Review's approver is set up, whose approval meets the ruleset's required approval ([ADR-0036](docs/decisions/project-template/0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md)), proposes dependency updates, runs CI, tags and publishes releases where `AGENTS.md`'s Landing section sets `release: tag+release`; never merges a pull request except the pin bump `code-review-auto-merge.yml` merges once the required checks pass and, where the ruleset requires an approval, it is approved: by `ncode-review-approver[bot]` after a clean review where NCode Review's approver is set up ([ADR-0041](docs/decisions/project-template/0041-ncode-review-reviews-and-approves-dependabots-pull-requests.md)), else by a maintainer |

Only a maintainer has write access. A contributor who keeps contributing reviewed, merged work can be invited to become
a maintainer; the existing maintainers decide, and the change is recorded in this file.

## Decisions

- Day-to-day decisions are made in issues and pull requests. Anyone can comment; a maintainer decides.
- A decision someone would otherwise re-argue is recorded as an architecture decision record in
  [docs/decisions/](docs/decisions/README.md), before or with the pull request that builds it. A change consumers can
  observe, such as to the public API, behavior or security posture, starts as an issue.
- Where maintainers disagree, they discuss it on the issue; with no agreement, the change doesn't happen.

## Releases

Versions follow Semantic Versioning. Releases are tagged when `AGENTS.md`'s Landing section sets `release: tag`, and published too with
`tag+release`, as
[ADR-0016](docs/decisions/project-template/0016-releases-are-tagged-and-published-when-their-pull-request-merges.md)
records. `.github/workflows/publish.yml` publishes the packages to nuget.org through trusted publishing, only after a maintainer approves the `nuget` environment. [SECURITY.md](SECURITY.md) says how to verify a package.

## Continuity

The repository, its issues, decision records, build, release automation and documentation are all in the
GitHub organization that owns it, so anyone given access can carry on with no knowledge held elsewhere.
The organization's owners can administer every repository it holds, so they can step in when the repository's
maintainers can't. nuget.org publishing uses trusted publishing, with no stored API key to hand over.

Owners today: [@polewskm](https://github.com/polewskm) is the one owner of the NCodeGroup GitHub organization, and
the nuget.org account [NCode](https://www.nuget.org/profiles/NCode) owns the package. Continuity needs at least two
people who can administer both; until there are two, it is incomplete, and the OpenSSF Best Practices silver level
can't be claimed ([docs/best-practices-badge.md](docs/best-practices-badge.md)). This list changes in the same pull
request as the ownership it describes.

## Changing This Document

Governance changes through a pull request to this file, which a maintainer merges.
