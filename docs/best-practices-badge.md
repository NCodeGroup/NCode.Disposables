# OpenSSF Best Practices Badge

A maintainer registers NCode.Disposables at [bestpractices.dev](https://www.bestpractices.dev) and answers the criteria
there, which is the source of truth; this page doesn't copy the answers. It maps the criteria that need a document to
the evidence this repository holds, so registering is answering, not researching.
[best-practices-criteria.md](best-practices-criteria.md), which the template keeps current, maps every criterion of the
three levels to the answer to expect and its evidence, and holds the prompt that updates the answers.

NCode.Disposables is [project 17114](https://www.bestpractices.dev/projects/17114).

## The Badge

[README.md](../README.md) shows it among the badges at its top:

```html
<a href="https://www.bestpractices.dev/projects/17114"><img alt="OpenSSF Best Practices"
  src="https://www.bestpractices.dev/projects/17114/badge"></a>
```

OpenSSF Scorecard's CII-Best-Practices check scores the badge level: in progress 2, passing 5, silver 7, gold 10
([ADR-0020](decisions/project-template/0020-github-projects-are-scored-by-openssf-scorecard.md)).

## Toward Silver

The silver criteria that need a document point to these:

| Criteria | Where |
| --- | --- |
| `governance`, `roles_responsibilities` | [GOVERNANCE.md](../GOVERNANCE.md) |
| `code_of_conduct` | [CODE_OF_CONDUCT.md](../CODE_OF_CONDUCT.md) |
| `documentation_roadmap` | [ROADMAP.md](../ROADMAP.md) |
| `documentation_architecture` | [architecture.md](architecture.md) |
| `documentation_security`, `assurance_case`, `implement_secure_design` | [assurance-case.md](assurance-case.md) |
| `signed_releases` | [SECURITY.md](../SECURITY.md#verify-a-package) and each GitHub release's `<tag>.intoto.jsonl` |
| Vulnerability response and credit | [SECURITY.md](../SECURITY.md) |
| Coding standards and their enforcement | [CONTRIBUTING.md](../CONTRIBUTING.md), `.editorconfig` and the analyzers in `./build/dod.ps1` |
| Test policy (`test_policy_mandated`) | [CONTRIBUTING.md](../CONTRIBUTING.md), which says that a change that adds or changes behavior adds or updates the automated tests that cover it |

## Where It Stands

The site shows the passing level, earned on 2026-11-25.

- **Silver** is kept from being earned by `access_continuity`, a MUST that only a second owner of the GitHub
  organization and the nuget.org package meets.
- **Gold** is kept from being earned by silver and by these MUST criteria: `bus_factor`, `two_person_review`,
  `contributors_unassociated`, `require_2FA` (the organization doesn't require two-factor authentication) and
  `security_review`, which need people; `copyright_per_file` and `license_per_file`, because the scripts the template
  owns, under `.template/`, `.claude/hooks/` and five under `build/`, carry no copyright or license header;
  `small_tasks`, while no open issue carries the `good first issue` label; and `hardened_site`, because the
  documentation site, served by GitHub Pages, sends no `strict-transport-security`, `content-security-policy` or
  `x-content-type-options` header.

## Needs a Person

- `access_continuity` (silver MUST): a second owner of the GitHub organization and the nuget.org packages, as
  [GOVERNANCE.md](../GOVERNANCE.md#continuity) says. Silver can't be claimed until there is one.
- `bus_factor` (silver SHOULD, gold MUST): a bus factor of 2 or more, which the same second owner gives.
- `two_person_review` (gold MUST): a second maintainer.
- `contributors_unassociated` (gold MUST): two significant contributors from different organizations.
- `require_2FA` (gold MUST) and `secure_2FA` (gold SHOULD): the GitHub organization requires two-factor authentication, with
  secure methods only, which only an owner can set and confirm. The organization doesn't require it today.
- `security_review` (gold MUST): a security review in the last five years, recorded in
  [assurance-case.md](assurance-case.md#security-reviews).
- `dco` (silver SHOULD): unmet. The project doesn't ask for `Signed-off-by`
  ([ADR-0023](decisions/project-template/0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md)).

## Also Supported

OpenSSF Scorecard runs on every push to `main` and once a week (`.github/workflows/scorecard.yml`), and
each release carries provenance attestations and an SBOM (`.github/workflows/release-provenance.yml`, which
`publish.yml` calls,
[ADR-0151](decisions/project-template/0151-release-follow-ups-are-jobs-publish-calls.md)).
