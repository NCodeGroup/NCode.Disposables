# 0018. Agent attribution is avoided, not enforced

- **Status:** Accepted
- **Date:** 2026-11-10

## Context

Coding agents add attribution to what they help write: trailers, footers, session links and bot authors. Some of it
can't be avoided. A Claude Code cloud session appends its footer to what it posts, and nothing edits a review-thread
reply without it. The Copilot cloud agent authors its commits as its own bot. Enforcing the rule with a hook that
refused commits, a required CI check that failed pull requests, and a read-back-and-edit loop on every post stopped
work over lines that change no code, and its fixes rewrote published history.

## Decision

Commits and pull requests record the people who wrote them, and every agent avoids adding attribution.

1. `attribution` in `.claude/settings.json` and `git.addAICoAuthor` in `.vscode/settings.json` stay off.
2. The rules in `.github/instructions/project-template.instructions.md` ask every agent to leave attribution out.
3. The workflow commands delete attribution lines from a pull request's title and description before they send them.
4. Nothing enforces it: no hook rewrites or refuses a commit, no CI job or required check fails a pull request, push
   or pipeline, and nothing reads a post back to remove attribution, warns about it or reports it. What gets through
   stays.

It supersedes [ADR-0002](0002-commits-are-attributed-to-people-not-agents.md), and amends
[ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md) and
[ADR-0013](0013-pull-requests-merge-with-a-merge-commit-or-by-rebasing.md): `Commit attribution` is not a required
check, and a project that squash-merges needs none.

## Options considered

- Enforce it (ADR-0002). Rejected: it blocks work over lines that can't always be avoided.
- Warn only. Rejected: a warning no one acts on is noise and costs an agent a round trip.
- Prevent it at the source only (chosen).

## Consequences

- A co-author trailer that gets through can list an agent as a contributor, also through a squash merge, and nothing
  asks to rewrite history for it.
- A repository whose ruleset requires `Commit attribution` drops it, or every pull request waits.
  `.template/Update-Project.ps1` says so when its update removes the check.
- `.githooks` holds only `pre-push`.

## References

- [ADR-0002](0002-commits-are-attributed-to-people-not-agents.md): commits are attributed to people, not agents.
- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md): the default branch accepts changes
  only through pull requests.
- [ADR-0013](0013-pull-requests-merge-with-a-merge-commit-or-by-rebasing.md): pull requests merge with a merge commit
  or by rebasing.
- `.claude/settings.json`, `.vscode/settings.json`
