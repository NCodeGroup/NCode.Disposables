# 0038. The template's scripts carry the template's copyright and license

- **Status:** Accepted
- **Date:** 2026-11-25
- **Amends:** 0027, where a project has it

## Context

The OpenSSF Best Practices gold MUSTs `copyright_per_file` and `license_per_file` ask for a copyright notice and a
license statement in each source file. A project's own files get both from `.editorconfig` for C# and from
`.template/Test-FileHeaders.ps1 -Fix` for the rest. The scripts the template owns are replaced by every template
update, so a header a project adds to one is discarded, and both criteria stay Unmet
([#503](https://github.com/NCodeGroup/project-template/issues/503)). These scripts are the template's code, published
under its MIT license, whose notice is kept with every copy.

## Decision

1. Every script the template owns carries the template's copyright line and license line as `#` comments: each
   `.ps1` and `.sh` file in `.template/`, the `build/*.ps1` scripts, `.claude/hooks/*.sh` and `.githooks/pre-push`.
   The lines are `# Copyright (c) 2026 Marcin Polewski`, the copyright line of the template's `LICENSE`, and
   `# SPDX-License-Identifier: MIT`. They come first in the file, or right after a `#!` line or a `# shellcheck`
   directive.
2. `.template/Test-FileHeaders.ps1` checks a project's own files for the project's `author_name` and `license`, and
   leaves the template's scripts alone, since they carry the template's lines.
3. The template's tests fail a script the template owns that lacks either line.

## Options considered

- Render the project's `author_name` and `license` into each script through Jinja. Rejected: each script would be a
  `.jinja` file that the template's tests can't run or analyze where it is, PowerShell text such as `${{ }}` would
  need escaping, and the template's code would be credited to each project.
- Keep the scripts without a header. Rejected: no project made from the template can meet either gold criterion.
- The template's own copyright and license lines (chosen): they are true of the code, the same in every project, and
  keep the MIT notice with each copy.

## Consequences

- A project's source files name two copyright holders: its own files name `author_name`, and the template's scripts
  name the template's author.
- A project under another license ships the template's scripts under MIT, which the MIT license allows; each script
  says so.
- `docs/best-practices-criteria.md` counts the template's scripts toward both criteria.
- Where a project has record 0027, this replaces its consequence that the template's own scripts carry no header.

## References

- `.template/Test-FileHeaders.ps1` and `docs/best-practices-criteria.md`; `tests/Test-FileHeaders.ps1` in the
  project template's repository
- NCodeGroup/project-template#503 and #449
