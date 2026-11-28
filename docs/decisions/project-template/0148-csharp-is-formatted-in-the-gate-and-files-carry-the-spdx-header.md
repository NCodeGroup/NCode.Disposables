# 0148. C# is formatted in the gate and files carry the SPDX header

- **Status:** Accepted
- **Date:** 2026-11-14
- **Amends:** 0117

## Context

ADR-0117 kept a formatter out of the gate. Two NCode repositories, identity and Buffers, then each wrote a
`build/gate.ps1` that wraps `build/dod.ps1` with a formatter check, and changed their Landing `gate`, CI, `AGENTS.md` and
`CONTRIBUTING.md` to run it. The template's instructions then contradicted what the projects did, and every project that
wanted a formatter kept a script of its own in step with `dod.ps1`.

## Decision

`build/dod.ps1` runs CSharpier as a `Format` stage, and a C# file without the SPDX header that `.editorconfig` templates
fails the build.

- A new visible Copier answer, `csharp_format`, is asked only for .NET projects and defaults to `true`. It decides
  whether the project gets `.config/dotnet-tools.json` and `.csharpierignore`, and whether `.editorconfig` raises IDE0073
  to `warning`. Warnings are errors ([ADR-0100](0100-warnings-are-errors-in-every-project.md)), so a file without the
  header then fails the build.
- An existing project is not opted in. `Update-Project.ps1` passes `csharp_format=false` when `.copier-answers.yml` has
  no such answer and `-Data` doesn't set it, and the template's `Adopt-Project.ps1` answers `false`. A project turns the formatter on
  with `.\.template\Update-Project.ps1 -Data @{ csharp_format = $true }` and runs `dotnet csharpier format .` once.
- The template writes the tool manifest itself, with CSharpier pinned and `"rollForward": false`, because
  `dotnet new tool-manifest` writes it at the root, where `dod.ps1`, the CI cache key and
  `copilot-setup-steps.yml` don't look. The manifest and `.csharpierignore` are project-owned, so Dependabot bumps and the
  project's own ignores persist. `.csharpierignore` lists `build/ProjectTemplate.props` and `nuget.config`, which updates
  overwrite, so the formatter never rewrites template-owned XML.
- The `Format` stage runs after `Tools` and before `Decisions`, in `-Fast` and in the full gate, whenever the manifest
  names the `csharpier` package, whatever the answer. It runs `dotnet csharpier check .`. A failure prints
  `Run: dotnet csharpier format .` and the gate ends `Definition of Done: FAILED at Format`.
- A CSharpier update from Dependabot that fails the stage is fixed by running `dotnet csharpier format .` and pushing the
  result to the same pull request. No Dependabot setting changes: its nuget ecosystem already reads the manifest.
- No script rewrites old file headers, because it runs once per project. The rewrite replaces each
  `#region Copyright Preamble` ... `#endregion` block with the two header lines and drops a UTF-8 BOM. A Rider
  `*.DotSettings` file may carry an old `FileHeaderText` that makes Rider generate the old header; change it too.
- `Update-Project.ps1`, when `-Data` turns `csharp_format` on, and the template's `Adopt-Project.ps1` count the C# files without the header `.editorconfig` sets and name the first five, so the first IDE0073 failures aren't a surprise; they rewrite none.
- The formatter's first pass changes layout only. Review that commit as such, and keep it apart from other changes.

## Options considered

- Wrap `dod.ps1` in a script per project. Rejected: every project maintains the wrapper and keeps it in step with the
  gate.
- A template-owned `gate.ps1` that adds the formatter. Rejected: a second gate to keep in step with the first.
- A stage in `dod.ps1` that reads the tool manifest (chosen). A project with no manifest, or one that doesn't name
  CSharpier, passes through unchanged.

## Consequences

- A new .NET project formats C# from its first commit, and no project needs a gate script of its own for it.
- An existing project is unchanged until it opts in, so a template update never fails its gate over formatting.
- A change that passes `-Fast` has passed the format check as well.
- A pinned CSharpier changes the layout of C# when it is bumped; the bump's pull request carries the reformatting.
- ADR-0117's "no formatter check" is superseded for a project whose manifest names CSharpier.

## References

- [ADR-0117](0117-one-definition-of-done-gates-every-change.md): one Definition of Done gates every change.
- [ADR-0100](0100-warnings-are-errors-in-every-project.md): warnings are errors in every project.
- `build/dod.ps1`, `.config/dotnet-tools.json`, `.csharpierignore`, `.editorconfig`
- [CSharpier](https://csharpier.com)
