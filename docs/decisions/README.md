# Architecture Decision Records

How NCode.Disposables records its decisions: one short Markdown file per decision, kept in this folder and changed in
the same pull request as the code it explains.

## When to Write One

Write a record for a non-obvious choice that someone would otherwise question again: a design with real alternatives,
a trade-off that looks wrong without its reasons, or a departure from the project template. The conventions themselves
go in `AGENTS.md` or the instruction files it lists; a record explains why a convention is what it is.
`/analyze-decision` compares the options and writes one, in a worktree of its own, where it lands with the plan that
builds it. Work bigger than one plan starts as a design document in `docs/designs/`, from
[its template](../designs/_template.md), and each of its decisions becomes a record here when its phase lands.

## How to Write One

- Copy [0000-template.md](0000-template.md) to `NNNN-kebab-case-title.md`, starting from `1000`. Numbers below 1000
  are the project template's, in [project-template/](project-template/README.md), so an ADR number names one record
  across both folders. `.template/Test-AdrIndex.ps1` fails a new record numbered below 1000; records the project
  already had keep their numbers.
- Take a number no one else holds. Fetch first, then take the next number above both the highest record numbered 1000
  or more on `origin/<default branch>` and any record an open branch, pull request or worktree is writing. Check again
  after rebasing or merging the default branch, and when the number was taken meanwhile, renumber the file, its title,
  its index row and every link to it. Two records never share a number; `.template/Test-AdrIndex.ps1` fails when they
  do.
- Add the record to the index below, in the same change.

## Statuses

- `Proposed`: the decision is open.
- `Accepted`: the decision holds.
- `Deprecated`: the decision no longer applies, and nothing replaces it.
- `Superseded by NNNN`: a later record replaces it whole.

Never rewrite the decision of an accepted record; fixing a typo or a broken link is fine. A changed decision is a new
record, and the old record's status and index row say so. When the new record changes only part of the old one, the
old one stays `Accepted` with a note, "Accepted (amended by NNNN)", and the new one's index row says "(amends NNNN)".
The old record's text is not edited beyond its status line.

## What a Record Is

A record is a decision the project made, not an authority. Where an external standard or current security practice
governs, it wins: a record that conflicts with one is superseded to align with it, never cited to justify the
deviation.

## Writing Rules

- Write every section in the present tense, so it reads the same a year later: what is, and why. Avoid words that
  date it, such as "now", "new", "currently", "previously", "originally", "we decided to change" or "the first
  version"; the history belongs in the commit log and in the records a record supersedes.
- Give a volatile fact (a price, a quota, a limit, a supported version) its source and the date it was read, and mark
  an estimate as an estimate.
- Keep a record short, and link to the code, issues and records it depends on rather than restating them.

## The Template's Decisions

The project template's own decisions are in [project-template/](project-template/README.md). Template updates replace
that folder. To decide otherwise, write a record here that says "Supersedes: project-template ADR-NNNN" and list the
override in `AGENTS.md`.

## Index

| ADR | Decision | Status |
| --- | --- | --- |
| [1000](1000-readme-samples-are-compiled-tests-that-markdownsnippets-copies-into-the-readmes.md) | README samples are compiled tests that MarkdownSnippets copies into the READMEs | Accepted |
| [1001](1001-synchronous-disposal-disposes-an-item-that-implements-both-interfaces-through-idisposable.md) | Synchronous disposal disposes an item that implements both interfaces through IDisposable | Accepted |
| [1002](1002-the-async-shared-reference-extension-is-asasyncsharedreference.md) | The async shared reference extension is AsAsyncSharedReference | Accepted |
| [1003](1003-the-package-targets-the-oldest-supported-lts-release.md) | The package targets the oldest supported .NET LTS release | Accepted |
| [1004](1004-a-resource-handed-to-a-disposed-holder-is-disposed.md) | A resource handed to a disposed holder is disposed | Accepted |
| [1005](1005-disposables-are-concrete-sealed-types-that-report-their-disposal-status.md) | Disposables are concrete sealed types that report their disposal status | Accepted |
| [1006](1006-value-disposables-and-lock-scopes-are-non-idempotent-structs.md) | Value disposables and lock scopes are non-idempotent structs | Accepted |
| [1007](1007-behavior-is-chosen-by-a-factory-name-not-a-flag-parameter.md) | Behavior is chosen by a factory's name, not a flag parameter | Accepted |
| [1008](1008-a-disposable-that-wraps-a-resource-rejects-a-null-resource.md) | A disposable that wraps a resource rejects a null resource | Accepted |
| [1009](1009-the-documentation-site-is-narrative-guides-built-by-a-shared-toolchain-outside-this-repository.md) | The documentation site is narrative guides, built by a shared toolchain outside this repository | Accepted |
| [1010](1010-site-samples-are-compiled-tests-in-a-project-of-their-own-included-by-key-when-the-site-is-built.md) | Site samples are compiled tests in a project of their own, included by key when the site is built | Accepted |
| [1011](1011-ncodegroup-docs-releases-are-taken-without-a-cooldown-during-the-build-out.md) | NCodeGroup/docs releases are taken without a cooldown during the build-out | Accepted |
| [1012](1012-the-documentation-site-is-published-to-github-pages-from-the-default-branch.md) | The documentation site is published to GitHub Pages from the default branch | Accepted |
| [1013](1013-the-documentation-site-is-served-at-disposables-ncode-io.md) | The documentation site is served at disposables.ncode.io | Accepted |
