# NCode.Disposables

Instructions for every coding agent in this repository. This file is an index: read each file it lists, in order, then the sections below. Where the project's instructions differ from the template's, the project's win. Claude Code reads this file through `CLAUDE.md` and imports each `@` file. GitHub Copilot reads it directly and loads every `.github/instructions/*.instructions.md` file on its own.

This file belongs to the project: template updates never change it. Keep it under 200 lines; put anything longer in a listed file.

## Instructions

- @.github/instructions/project-template.instructions.md — the plan-then-execute workflow and base conventions, from the project template. Template updates replace it; never edit it. To change one of its rules, override it below or in a listed file.
- `.github/instructions/csharp.instructions.md` — C# conventions, from the project template; read it before editing `.cs` files.
- `.github/instructions/csharp-tests.instructions.md` — C# test conventions, from the project template; read it too before editing a test project's `.cs` files.
- `docs/decisions/` — this project's architecture decision records, and in `project-template/` the template's; read the ones a change touches before deciding otherwise.
- Project instruction files: add one line for each, `@<path> — what it covers`.

## Commands

Run from the repository root in PowerShell 7.

- Build: `dotnet build`
- Test: `dotnet test`
- Definition of Done: `./build/dod.ps1` (the full gate CI runs) or `./build/dod.ps1 -Fast` (Debug, no pack); it ends with `Definition of Done: PASSED`. Record a public API change with `./build/Update-PublicApi.ps1`

## Landing

How `/ship-plan` lands a reviewed plan. `integrate`: `pull-request` pushes the plan's branch and opens a pull request; `direct` pushes to the default branch. `release`: `none`; `tag` tags the plan's `Release:` version, or the next free one when another plan landed it first, and a plan has that line only when it changes something a release ships, such as the product's code, its package's readme or its build, so one that changes only site pages, CI, decision records, tests or repository docs lands with no tag and no release (ADR-0032 in `docs/decisions/project-template/`); when this project's releases ship something else, say what under `## Conventions`; `tag+release` also publishes a release from that version's section of `changelog`. On GitHub, `.github/workflows/tag-release.yml` tags and publishes it as the pull request merges, from the `Release:` line `/ship-plan` writes in its description or the changelog's newest section, and for a .NET library starts `.github/workflows/publish.yml`; `/ship-plan` run after the merge does only what that workflow didn't. `changelog`: the file every plan's last step writes, in the Keep a Changelog format, `CHANGELOG.md` by default. `pre-release`: `untracked` keeps the project pre-release until a version tag of 1.0.0 or later: nothing it released or stored has a reader that must keep working, so every text describes the product as it is, the changelog's `## [Unreleased]` is a catalog of what the product does that each plan edits in place, and a breaking change needs no deprecation or migration path, as ADR-0015 in `docs/decisions/project-template/` says; `tracked`, the default when the line is missing, tracks every change from the first release, for a project whose releases or stored data already have readers. `draft`: `no`, the default when the line is missing, which a template update never changes, so a project lands as it did; `yes` opens every pull request as a draft, as `/ship-plan draft` does, and a new project that runs NCode Review starts with it, since it reviews and approves a draft too, so marking it ready is the person's review; where NCode Review's approver is set up, which is where `.github/workflows/code-review.yml` passes `approve: true` and `approver-private-key` and the repository's `code-review` environment holds `NCODE_REVIEW_APPROVER_PRIVATE_KEY`, the NCode Review Approver submits that approval, and it meets the ruleset's required approval, of the last push, with one writer too; elsewhere NCode Review's own app approves, which counts for Scorecard but meets no required approval, which only someone with write access other than the author can give (ADR-0022, ADR-0034 and ADR-0036 in `docs/decisions/project-template/`); and `/run-issues` opens its pull requests as drafts either way. `follow-ups`: `issues` files a plan's follow-ups as issues in this repository; `<owner/repo>` files them in that repository; `none` files none, and `/ship-plan` lists them for you to record. `follow-up-label`: the label those issues get, `follow-up` by default; write a name with spaces as it is, such as `tech debt`. `gate`: `none`; a command, such as `./build/dod.ps1 -Fast`, that `/ship-plan`, `/run-issues`, `/address-feedback` and `/run-feedback` run from the repository root before each push, stopping when it exits non-zero or changes a file. `protect`: `none`; `hook` makes `.githooks/pre-push` refuse any push to the default branch, for a host that can't require pull requests, such as a private repository on GitHub Free. `pr-format`: `none`; `<file>#<heading>`, such as `AGENTS.md#Pull requests`, names the section that sets the title and description of the pull requests `/ship-plan` opens, which still end with their `Closes` lines. `review-bots`: the review bots, by login and separated by commas, whose findings `/run-issues`, `/address-feedback` and `/run-feedback` may fix, `ncode-review` by default, or `none`; `/run-issues` and `/run-feedback` judge every one of NCode Review's findings, whatever its severity, and fix each that holds, and a bot's finding alone never changes CI, the template, agent configuration, the build or the dependencies: `/address-feedback` and `/run-feedback` change them only when you approve it in their start question. `max-review-rounds`: `3`; after that many rounds of review feedback on a pull request, `/run-issues` and `/run-feedback` comment no further `/review`, so NCode Review isn't asked to review again, and still fix and answer every finding; they recommend the merge once every finding is fixed, declined or answered and the review of the last push they asked for is in. `prelude`: `none`; one PowerShell command that sets up the shell the workflow's commands run in, such as `./build/env.ps1`, a script in the repository, or `nvs use 26`, to put a tool installed through a version manager on `PATH`. `/ship-plan`, `/run-issues`, `/address-feedback` and `/run-feedback` run it before the gate, and executors and coordinators before each Done-when command, dot-sourced in the same PowerShell; a plan names it under Context for the executor, so no step repeats it. When it fails, they stop and show its error. Write it on one line, without a leading `. ` or `& ` operator, a `#` comment, or `;`, `|`, `&`, `&&` or `||`; `.template/Get-PlanState.ps1` prints any other value as `prelude=invalid`, and `/ship-plan` stops on it. `/run-issues` runs only when the host requires pull requests into the default branch, or `protect` is `hook` and the hooks are enabled.

- integrate: pull-request
- release: none
- changelog: CHANGELOG.md
- pre-release: tracked
- draft: no
- follow-ups: issues
- follow-up-label: follow-up
- gate: ./build/dod.ps1 -Fast
- protect: none
- pr-format: none
- review-bots: ncode-review
- max-review-rounds: 3
- prelude: none

## Conventions

- A public API change is recorded with `./build/Update-PublicApi.ps1`; the release that ships it runs it with `-Release`.
- Packages are published from a GitHub release by the `publish` job of `.github/workflows/main.yml`; the template's `publish.yml` is deleted because the project publishes from its own workflow.
- Package versions come from `version.json` through Nerdbank.GitVersioning (ADR-0138 in `docs/decisions/project-template/`): `version` is the version the next release ships, and the change that releases a version sets it to that version, so the release tag `v<version>` matches. `assemblyVersion` stays `5.3` with precision `minor`, the AssemblyVersion the 5.x releases carry, until the change that starts the next major version sets it to `{ "precision": "major" }`. `.github/workflows/main.yml` checks out the full history and builds a release with `-p:PublicRelease=true`.
- Package validation compares each pack with `PackageValidationBaselineVersion` in `Directory.Build.props`, the last version on nuget.org (ADR-0147 in `docs/decisions/project-template/`). No workflow raises it here, because `.github/workflows/main.yml` publishes instead of the template's `publish.yml`: once nuget.org lists a new release, raise it with `./build/Update-Baseline.ps1 -Version <version>` in a change of its own.
- This repository keeps `.github/workflows/code-review-auto-merge.yml` and accepts the template's ADR-0006 exception: it auto-merges only a Dependabot pull request whose one change is the NCode Review pin.
