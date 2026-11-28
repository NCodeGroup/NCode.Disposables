# 0026. Tests gate coverage at the gold thresholds, and a release waits for them

- **Status:** Accepted (amends 0016; amended by 0040)
- **Date:** 2026-11-16

## Context

The OpenSSF Best Practices gold MUSTs `test_statement_coverage90` and `test_branch_coverage80`, the silver MUST
`test_statement_coverage80`, and `dynamic_analysis` (SUGGESTED at the passing level, a MUST at gold) were Check or Gap
rows of `docs/best-practices-criteria.md`: nothing in a new project measured coverage or failed below a threshold. The
badge counts "an automated test suite with at least 80% branch coverage" as dynamic analysis, if it runs before each
release, and nothing made the tests run again before a release was tagged.

## Decision

A `coverage_gate` answer turns on a coverage check at the gold thresholds, and a release is tagged only after CI runs
again on its commit.

- A new visible Copier answer, `coverage_gate`, is asked for .NET, Python, Node.js and PowerShell and defaults to `true`.
  An update gives an existing project `false`, an adoption answers `false`, and
  `.\.template\Update-Project.ps1 -Data @{ coverage_gate = $true }` turns it on.
- `.template/Test-Coverage.ps1` reads Cobertura and LCOV reports, merges them line by line, leaves out test code and the
  template's files, counts a line as a statement, and fails below 90% of the statements or 80% of the branches. A report
  with no branch data counts as fully covered, since Pester measures commands, not branches.
- .NET's `build/dod.ps1` collects `Code Coverage;Format=cobertura` with Microsoft.CodeCoverage, which
  Microsoft.NET.Test.Sdk brings, in a Coverage stage, skipped with `-Filter` and while no `*.Tests` project exists.
  Python's default test command adds `--cov --cov-branch --cov-report=xml`, and CI installs pytest-cov. PowerShell's runs
  Pester with Cobertura code coverage. A Node.js project's `test` script writes `lcov.info` or `coverage/lcov.info`, such
  as Node.js's own test runner with `--experimental-test-coverage` and its `lcov` reporter.
- GitHub CI checks the coverage in a step after the tests; GitLab CI in a `coverage` job in the .NET SDK image, which has
  PowerShell, from `build-and-test`'s artifacts.
- `ci.yml` also starts on `workflow_call`, and `tag-release.yml` calls it as its `test` job, which its `release` job
  needs: a release is tagged only after its commit's build and tests, and with `coverage_gate` the coverage, pass again.
  With the gate, that is the dynamic analysis the badge asks for before a release.
- `tag-release.yml`'s `test` job grants `ci.yml` only `contents: read`. GitHub checks every job of a called workflow
  against what its caller grants when the run starts, whatever the job's `if:` says, and fails the whole run when one
  asks for more. So no job of `ci.yml`, and not its top-level `permissions:`, asks for more than `contents: read`: a job
  that needs more, such as a coverage upload through OIDC with `id-token: write`, goes in a workflow of its own.
- `.template/Update-Project.ps1` and the template's `Adopt-Project.ps1` warn when `tag-release.yml` calls `ci.yml`
  and `ci.yml` has no `workflow_call` trigger, or when its top-level `permissions:` or a job's asks for more than
  `contents: read`.
- CI's concurrency group holds the commit SHA on the default branch, so the call waits for the push's own CI run and is
  never cancelled.

## Options considered

- Each runner's own threshold, such as Node.js's `--test-coverage-lines` or pytest-cov's `--cov-fail-under`. Rejected:
  it is one setting per stack, and pytest-cov's is one combined figure, not a statement and a branch threshold.
- A fuzzing step. Rejected as a default: it needs a harness per project. Property tests, such as FsCheck or CsCheck,
  fast-check and Hypothesis, stay the project's choice.
- Polling CI's check run from `tag-release.yml`. Rejected: it is a wait loop and a job name to keep in step.
- Calling `ci.yml` from `tag-release.yml` (chosen).

## Consequences

- A new project's CI fails below the thresholds from its first tested code.
- An existing project is unchanged until it opts in.
- A release takes as long as CI.
- A project whose own `ci.yml` lacks `workflow_call`, or asks for more than `contents: read`, tags no release until it
  fixes that, which the update and the adoption say.
- GitLab has no `tag-release.yml`, so a GitLab release relies on its merge request's pipeline.

## References

- `.template/Test-Coverage.ps1`, `build/dod.ps1`, `.github/workflows/ci.yml`, `.github/workflows/tag-release.yml`
- [ADR-0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md): releases are tagged and published
  when their pull request merges.
