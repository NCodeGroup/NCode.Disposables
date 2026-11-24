# 0015. A pre-release project describes the product as it is and keeps no compatibility

- **Status:** Accepted
- **Date:** 2026-11-07

## Context

A project that has shipped nothing has no reader that must keep working. No consumer holds a version, no deployment
stores data that a release must read, and every earlier state is an unreleased commit.

Text that describes the product as a delta addresses a reader who does not exist. A `**Breaking:**` entry, "renamed `X`
to `Y`" and a migration note name members that are absent from the code, go stale on the next change, and make a reader
replay history to learn what the product does.

Compatibility machinery protects readers who do not exist and costs every change. A deprecation step, a compatibility
shim, an upcaster for stored documents, an expand-then-contract migration and a major version for a break each add work
and code for nobody.

Keep a Changelog's `### Changed`, `### Deprecated`, `### Removed` and `### Fixed` groups and the conventional-commit `!`
marker compare a version with a released predecessor. A project with no release has no predecessor to compare with.

Semantic Versioning reserves `0.y.z` for initial development, but a `0.y.z` project can already have consumers or
stored data, so the version alone does not say whether anything must keep working.

## Decision

A project with `pre-release: untracked` describes the product as it is, and keeps no compatibility, until its first
stable release.

1. The key is `pre-release` in `AGENTS.md`'s `## Landing` section, with the value `untracked` or `tracked`, which is
   the default when the line is missing. A new project starts with `untracked`; the template's `Adopt-Project.ps1`
   writes `tracked` into an `AGENTS.md` it writes, since an existing repository has a history. Choose `tracked` when
   anything the project released or stored has a reader that must keep working, such as a published `0.y.z` package
   with consumers or a deployment whose data must survive. The posture is the project's choice, not the version's: with
   `untracked`, a consumer of a published `0.y.z` release gets no compatibility promise, so a library whose `0.y.z`
   packages have consumers it keeps working sets `tracked`, and its changelog marks each breaking change with
   `**Breaking:**`. With `untracked`, the project is pre-release until a version tag of 1.0.0 or later, shaped like
   `v1.2.3` or `1.2.3`, is merged into its branch; `.template/Get-PlanState.ps1` prints `pre-release: yes` while it is.
2. Every text describes the product as it is: a changelog entry, a document, a code comment, an XML doc, a decision
   record, a test name, a commit message and a pull request's title and description carry no `**Breaking:**` label; no
   "now", "new", "no longer", "previously", "used to", "replaces", "renamed", "moved" or "migrated" set against an
   earlier state; no name of a removed member; and no upgrade, migration, rollback or compatibility note. Words that
   name the product's own mechanisms, such as an EF Core migration or a key that rotation replaces, stay. A fix to
   behavior no release shipped is not recorded as a fix.
3. The changelog is a catalog: a Keep a Changelog file whose `## [Unreleased]` holds only `### Added`, the
   capabilities, and `### Security`, the security posture and protections. Each plan edits it in place, adding,
   rewriting or deleting the entry its change touches. It has no release section and no link reference definitions, its
   preamble says so, and a release's notes are that section. Version tags below 1.0.0 need no section.
4. No compatibility is kept: a breaking change needs no deprecation step, compatibility shim, migration path or major
   version, in the public API, settings, wire formats, stored documents or the database schema. Commit subjects and pull
   request titles carry no conventional-commit `!` marker and no `BREAKING CHANGE:` footer. The .NET records
   ADR-0108, ADR-0131, ADR-0136 and ADR-0143 say how their rules apply.
5. Decision records keep their `Status` and `Supersedes` lines: relating decisions is not change tracking.
6. The first stable release ends it: the plan whose `Release:` line names the first stable version moves the catalog
   into that version's section, the first release's notes, and from then on Keep a Changelog records every change and
   every compatibility rule binds. The key keeps `untracked`, which records that the tags below 1.0.0 have no section.
   With `release: none`, the first plan after the first stable tag writes that section.

## Options considered

- Track every change from the first commit. Rejected: it writes for upgraders who don't exist; it stays the `tracked`
  default for a project whose releases already have readers.
- No changelog until the first stable release. Rejected: the catalog tells reviewers and the first release's readers
  what the product does, and the workflow's tooling expects the file.
- Infer the posture from a `0.y.z` version. Rejected: such a project can have consumers or stored data.
- A switch for the changelog alone, such as `changelog-mode: catalog`, beside separate compatibility rules. Rejected:
  both follow from one fact, and two switches can disagree.
- One Landing key for the whole posture (chosen). The catalog and the absence of compatibility rules follow from the
  same fact, that nothing has a reader that must keep working.

## Consequences

- A pre-release project's texts read correctly against the code beside them.
- Each plan's changelog step edits a catalog, and a review rejects delta wording, a `**Breaking:**` label, another
  heading and the `!` marker.
- The first stable release's section describes everything it ships.
- A project that sets `untracked` over a tracked changelog converts it in a plan of its own.
- Smell: a pre-release changelog with a `### Changed` heading, an `[Obsolete]` member, an upcaster or a second
  migration in a context.

## References

- `AGENTS.md` Landing section, `CHANGELOG.md`, `.template/Get-PlanState.ps1`
- [Keep a Changelog 1.1.0](https://keepachangelog.com/en/1.1.0/)
- [Semantic Versioning 2.0.0, item 4](https://semver.org/spec/v2.0.0.html#spec-item-4)
- [Conventional Commits 1.0.0](https://www.conventionalcommits.org/en/v1.0.0/)
- [ADR-0001](0001-conventions-and-lessons-live-in-the-repository.md): conventions and lessons live in the repository.
- [ADR-0010](0010-a-change-consumers-can-observe-is-decided-before-it-is-built.md): a change consumers can observe is
  decided before it is built.
