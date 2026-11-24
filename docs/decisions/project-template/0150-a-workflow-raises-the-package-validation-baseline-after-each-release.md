# 0150. A workflow raises the package validation baseline after each release

- **Status:** Accepted (amended by 0022, 0034, 0036 and 0151)
- **Date:** 2026-11-15
- **Amends:** 0147

## Context

ADR-0147 leaves the raise of `PackageValidationBaselineVersion` to a person after each release, which became a plan
follow-up and a step per release. nuget.org can take about ten minutes to list a version after `Publish packages`
succeeds, so the raise cannot run in the release itself.

## Decision

- `build/Update-Baseline.ps1 -Version <v>` waits for nuget.org's flat container index to list the version for every
  packable project (30-second requests, a failed request counts as not listed yet, back-off from 15 to 120 seconds with
  jitter, 30 minutes by default), then raises every `PackageValidationBaselineVersion` that `Directory.Build.props` or a
  project file sets and that is lower, never lowering one and adding none. It adds a `### Changed` entry under
  `## [Unreleased]` of a Keep a Changelog file with a release section, in place of any baseline entry an earlier run
  left there, so a raise that merges after the next release leaves one entry. When it raises a project's baseline to a
  new major version, it removes the baseline suppressions, the entries marked
  `<IsBaselineSuppression>true</IsBaselineSuppression>` and the `PKV006` and `PKV007` entries, from the
  `CompatibilitySuppressions.xml` beside the project, keeps the others, and deletes the file when none is left: package
  validation rejects a suppression that matches nothing. A prerelease is never a baseline.
- On GitHub, `.github/workflows/raise-baseline.yml` runs it when `publish.yml` calls it after publishing a stable
  version, or by hand with a tag, pushes `baseline-<version>`, opens it as a draft pull request, as workflows that open
  pull requests do, which a person marks ready and merges after CI, and starts `ci.yml` on the branch with
  `gh workflow run`, because a pull request opened with the workflow's token starts no workflow. Its checkout keeps no
  credential; the push takes the token through `http.extraheader`. Its description names each
  `CompatibilitySuppressions.xml` the script deleted or edited.
- The workflow needs "Allow GitHub Actions to create and approve pull requests", which
  `tools/Set-RepositoryBaseline.ps1` in the project template turns on only where this workflow is on the default branch;
  GitHub offers no setting that allows creating without approving.
- A project's plans never raise the baseline or list raising it as a follow-up.

## Options considered

- A person raises it (ADR-0147, the old way).
- A personal access token or app for the pull request. Rejected: a secret to manage.
- A pull request ready for review. Rejected: workflows open pull requests as drafts, so a person marks each one ready
  before it can merge.
- A template-owned script and workflow (chosen).

## Consequences

- The first set of the baseline stays manual.
- A GitLab library's `raise-baseline` job needs that token; without it, run the script by hand.
- A merge of the pull request needs CI to pass, as any change does.
- The same setting lets any workflow's token approve a pull request, which counts where NCode Review's approval is
  required ([ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md)); no template workflow approves, and this one
  can't approve its own pull request.

## References

- `build/Update-Baseline.ps1`
- `.github/workflows/raise-baseline.yml`
- `docs/api-compatibility.md`
- [ADR-0147](0147-packages-are-validated-against-the-last-published-package.md): packages are validated against the
  last published package.
