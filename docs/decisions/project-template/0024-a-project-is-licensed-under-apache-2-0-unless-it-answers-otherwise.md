# 0024. A project is licensed under Apache-2.0 unless it answers otherwise

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

Without a license file, code is all rights reserved: no one else may use, copy or change it. The OpenSSF Best Practices
criteria ask for a published license ([ADR-0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md)),
and OpenSSF Scorecard's License check looks for one. The `license` answer was blank by default and only a NuGet package
had to answer it, so a project made with the defaults had no license, and no update said so: NCodeGroup/code-review
went through ten template versions without one. NCode Group's repositories use Apache-2.0.

## Decision

1. **`license` is asked of every project.** It is an SPDX license expression, `Apache-2.0` by default. Apache-2.0 writes
   `LICENSE.txt` and `COPYRIGHT.txt`, and MIT writes `LICENSE.txt`. Another expression is recorded in the README,
   CONTRIBUTING, the package metadata and the C# file header, and the project adds its license file. Blank means no
   license.
2. **One answer.** Blank is the "no", so no yes/no question stands beside it.
3. **An update or an adoption never chooses a license for code that already exists.** A project from before the
   question keeps none, and an adoption that finds none answers blank.
4. **A repository without a license file is warned about.** `.template/Update-Project.ps1`, also when the project is up
   to date, and the template's `Adopt-Project.ps1` warn when the repository has no `LICENSE` or `COPYING` file at its
   root. For a blank answer they print `.\.template\Update-Project.ps1 -Data @{ license = 'Apache-2.0' }`, in which
   another SPDX expression can take Apache-2.0's place; for any other answer, such as one the template writes no file
   for or one whose `LICENSE.txt` was deleted, they say to add that license's text as `LICENSE.txt`.

## Options considered

- Keep the blank default and only warn. Rejected: a new project still starts without a license.
- Default to Apache-2.0 only when `author_name` is NCode Group. Rejected: Copier asks `author_name` after `license`, and
  only for .NET or a licensed project.
- A yes/no question plus a list of licenses. Rejected: Copier's choices can't take any SPDX expression, such as
  `MIT OR Apache-2.0`, and two answers can disagree.
- Give existing and adopted projects the default too. Rejected: licensing existing code, maybe already published under
  other terms, is its owner's decision.
- One answer defaulting to Apache-2.0, with warnings (chosen).

## Consequences

- A new project gets `LICENSE.txt`, `COPYRIGHT.txt`, a License section in its README and, for .NET, the SPDX line in
  its C# file header, and is asked `author_name`.
- A repository that wants no license, such as a private one, answers blank and sees the warning on each update.
- An existing project without a license runs the printed command.
- A project whose answer names a license it has no file for, such as GPL-3.0-only, is warned too, and adds the
  license's text itself.
