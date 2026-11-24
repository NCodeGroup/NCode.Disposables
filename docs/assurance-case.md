# Assurance Case

This page states NCode.Disposables's security requirements, the threats it considers, and why the design and the way it
is built meet them. [architecture.md](architecture.md) describes the components; [README.md](../README.md) is the
user-facing summary.

## Security Requirements

[//]: # "Maintainers: list what the project is expected to do as R1, R2, ..., and what users must not rely on."

Not written yet.

## Threat Model

[//]: # "Maintainers: add a table (Threat, In scope, Countered by) and state the trust boundary."

Not written yet.

## Secure Design Principles Applied

[//]: # "Maintainers: say how the design applies economy of mechanism, fail-safe defaults, complete mediation, least
privilege and open design."

Not written yet.

## Common Weaknesses Countered

[//]: # "Maintainers: list the CWE ids the design counters, each with the requirement that counters it."

Not written yet.

## Evidence

[//]: # "Maintainers: add the tests and the coverage the project keeps."

- **Gate:** `./build/dod.ps1` builds in Release with warnings as errors
  ([ADR-0100](decisions/project-template/0100-warnings-are-errors-in-every-project.md)), runs the analyzers and the
  banned-API list in `build/BannedSymbols.txt`
  ([ADR-0102](decisions/project-template/0102-disallowed-apis-are-banned-at-build-time.md)), and runs the tests; CI runs
  the commands on every pull request.
- **Static analysis:** with CodeQL's default setup configured and analyzing at least one language, the repository
  ruleset blocks a pull request that adds a CodeQL alert at warning level or above, or a security alert of any severity
  ([ADR-0005](decisions/project-template/0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
  [ADR-0030](decisions/project-template/0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md)).
- **Review:** every change reaches the default branch through a pull request with passing CI and every conversation
  resolved, and the ruleset refuses force pushes and deletion
  ([ADR-0005](decisions/project-template/0005-the-default-branch-accepts-changes-only-through-pull-requests.md),
  [ADR-0014](decisions/project-template/0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md)).
  NCode Review reviews each pull request and approves a clean one.
- **Dependencies:** pinned, and moved by reviewed updates
  ([ADR-0008](decisions/project-template/0008-external-inputs-are-pinned-and-kept-current.md)) that wait three days for
  a new version ([ADR-0009](decisions/project-template/0009-new-dependency-versions-wait-a-cooldown.md)). Dependabot proposes the updates. Central package management keeps one version of each package
  ([ADR-0101](decisions/project-template/0101-central-package-management.md)).
- **Release integrity:** packages are deterministic and source-linked
  ([ADR-0130](decisions/project-template/0130-source-link-and-symbol-packages.md)). `publish.yml` builds them from a tagged commit the default branch holds and pushes them through
  nuget.org trusted publishing with no stored key, after a maintainer approves
  ([ADR-0135](decisions/project-template/0135-packages-are-published-from-a-github-release-through-trusted-publishing.md),
  [ADR-0137](decisions/project-template/0137-the-release-workflow-starts-package-publishing.md)), and each release
  carries signed provenance and an SBOM ([ADR-0139](decisions/project-template/0139-releases-carry-build-provenance.md),
  [ADR-0151](decisions/project-template/0151-release-follow-ups-are-jobs-publish-calls.md)). [SECURITY.md](../SECURITY.md#verify-a-package) says how to verify a package.
- **Response:** [SECURITY.md](../SECURITY.md) defines private reporting, coordinated disclosure and credit.

## Security Reviews

[//]: # "Maintainers: record each security review, newest first: its date, who did it, what it covered (the security requirements and the trust boundary above) and what it found. The OpenSSF Best Practices gold level asks for one in the last five years."

None recorded yet.
