# 0155. .NET code targets only LTS releases, and prose names no .NET version

- **Status:** Accepted
- **Date:** 2026-11-26

## Context

Microsoft ships a new .NET major version every November. Even-numbered majors are Long Term Support (LTS) releases,
supported for three years; odd-numbered majors are Standard Term Support (STS) releases, supported for two years. Two
LTS releases are therefore in support together for about a year, and a project that follows every major migrates every
year ([.NET and .NET Core Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core), read
2026-11-26).

The template already pins the SDK in `global.json`, rolls it forward only within its major (`latestFeature`), keeps
Dependabot from offering a new SDK major, and makes a major move a planned migration (ADR-0008 item 7). Nothing says
which majors a project builds for, so an STS target framework or SDK is as acceptable as an LTS one.

Prose that names a .NET version goes stale on Microsoft's schedule, not the project's. Records, instruction files,
README tables and script comments name target frameworks, runtime channels and SDK image tags as examples, and each new
release leaves them describing a version that is no longer current or no longer supported, with no build or test to
notice. Each such mention is an edit that every new release asks for, in files whose decision did not change.

## Decision

.NET code is built and maintained only for LTS releases that Microsoft supports, and only the files that must pin a
.NET version name one.

1. A project's target frameworks are LTS releases that Microsoft supports. Every project targets the newest LTS it has
   migrated to; a project, usually a library, may also target an older LTS until that LTS reaches its end of support.
   STS releases are never targeted.
2. `global.json` pins the SDK of the newest LTS the project targets, never an STS or preview SDK. Its `rollForward`
   stays within that major, and Dependabot keeps ignoring SDK majors (ADR-0008). CI images that name a .NET major name
   the same LTS.
3. When Microsoft ships a new LTS, the project plans its move with `/plan-migration`, and finishes it before the older
   LTS it targets reaches its end of support. The move adds the new LTS as a target framework, moves `global.json` and
   the CI images to its SDK, and drops each target framework whose LTS has reached, or reaches within the migration,
   its end of support. An STS release in between is skipped.
4. A .NET version (a target framework moniker such as `netN.0`, a runtime channel such as `N.0.x`, an SDK version or
   image tag, or ".NET N") appears only where it is pinned or recorded:
   - files that pin it: `global.json`, the target frameworks in project and props files, CI image tags and installer
     inputs, and package versions in `Directory.Packages.props`;
   - test fixtures and generated baselines, which are data;
   - `CHANGELOG.md`, which records what a past release did.

   Everything else — decision records, `README.md` and other docs, instruction files, skills and commands, and comments
   in scripts, workflows and project files — names the release by its role: "the current LTS", "the LTS the project
   targets", "the SDK `global.json` pins", "each older LTS a test project targets". Prose that explains behavior a
   release introduced describes the behavior, not the release. A sample that needs a target framework uses
   `$(TargetFramework)` or a placeholder such as `netN.0`.
5. Checks hold the rule:
   - The template's shared build settings (`build/ProjectTemplate.props`) fail the build with `PT0155` when a
     `netN.0` target framework, with or without a platform suffix, has an odd major of the yearly even/odd cadence.
     The check reads only the project's own properties, so it needs no network and no clock, and a commit builds the
     same on any day.
   - `build/Test-DotnetSupport.ps1`, which CI runs once it has installed .NET, reads Microsoft's release metadata
     (`releases-index.json`, whose `release-type` and `support-phase` name each channel's) for the SDK `global.json`
     pins and every target framework of the projects in the root solutions. It fails a Standard Term Support
     release, one past its end of support and one the metadata doesn't list, warns for an LTS in its maintenance
     phase, and names `/plan-migration`.
   - The template's own tests fail a Markdown file of the template's repository, or a full-line comment in one of its
     scripts, workflows or project files, that names a .NET version outside the exceptions of item 4. A project's own
     prose follows the rule through review.
6. A project that can't migrate yet opts out until it has: `ProjectTemplateLtsCheck` set to `false` in
   `Directory.Build.props` turns the build check off, and `-WarnOnly` on CI's `build/Test-DotnetSupport.ps1` step
   prints its failures as warnings. Both are in files the project owns or merges, so a template update keeps them,
   and the migration removes them.

.NET Standard and .NET Framework target frameworks are not .NET runtime releases, and this record does not govern them.

## Options considered

- Do nothing: the SDK pin and Dependabot's ignored majors keep a project on the major it starts on. Rejected: nothing
  stops an STS target framework, nothing says when to move, and prose mentions drift unseen; the template's own records
  name a runtime channel whose support has already ended.
- Follow every release, LTS and STS. Rejected: a migration every year, and an STS adds no support time beyond the LTS
  before it, so it buys only earlier features at the price of an extra migration.
- Only the newest LTS, dropped the day the next ships. Rejected: a library that drops the older LTS at once strands
  consumers whose runtime Microsoft still supports, and an application gains nothing from moving inside the overlap
  rather than at its end.
- LTS releases Microsoft supports, written down without numbers and held by checks (chosen). The policy reads the same
  after every release, the only files a new LTS changes are pins, and a build or test failure, not a reviewer, catches
  an STS target or a stale mention.
- The same rule without checks. Rejected: the stale mentions it removes passed review once already.

## Consequences

- A new .NET release changes no record, document or instruction file. Moving to a new LTS is a migration whose diff
  touches only pins: `global.json`, target frameworks, CI image tags, and the framework packages in
  `Directory.Packages.props`.
- A feature that ships first in an STS release waits for the next LTS, up to a year.
- Each LTS gives a project about a year, from the new LTS's release to the older one's end of support, to migrate; CI
  warns during the last months of support and fails after it, so a missed migration blocks the pipeline rather than
  shipping on an unsupported runtime.
- Dependabot still moves the SDK within its LTS major; nothing about the weekly updates changes.
- A project's own prose that names a .NET version is rewritten by role the next time it is edited, or in the migration
  to the next LTS, whichever comes first.
- A project on a release the checks refuse fails its build or CI after the template update that brings them, until
  it migrates or opts out (item 6).
- Smells: a target framework or `global.json` SDK with an odd major; a document, record or instruction file edited
  only because Microsoft shipped a release; a target framework past its end of support; an opt-out still set after
  the migration it waited for.

## References

- [.NET and .NET Core Support Policy](https://dotnet.microsoft.com/platform/support/policy/dotnet-core) and its release
  metadata, `https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json`.
- `global.json`, `build/ProjectTemplate.props`, `build/Get-TestRuntimes.ps1`, `build/Install-Dotnet.ps1`,
  `build/Test-DotnetSupport.ps1`, `.github/actions/install-dotnet`, `.github/dependabot.yml`, `/plan-migration`.
- [ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md): external inputs are pinned and kept current; a major
  move is a planned migration. This record extends it with which majors a project moves to.
