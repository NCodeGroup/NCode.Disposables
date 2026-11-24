# 0017. Cloud sessions bootstrap their tools through ensure scripts

- **Status:** Accepted
- **Date:** 2026-11-09

## Context

A Claude Code cloud session runs in a fresh container that has the image's tools and not necessarily the project's:
no PowerShell, which every project's `.template` scripts need, and no nvs, which selects a project's Node.js. A cloud
environment's setup script can install tools, but it is stored on the environment, outside the repository, and one
environment serves many repositories. A `SessionStart` hook in `.claude/settings.json` is versioned with the project,
runs at every session start, and knows from `CLAUDE_CODE_REMOTE` whether it runs in a cloud session.

## Decision

The repository decides which tools its cloud sessions bootstrap, through scripts in `.claude/hooks/`.

1. `.claude/settings.json` has one `SessionStart` hook, `.claude/hooks/session-start.sh`. In a cloud session it runs
   every `.claude/hooks/ensure-*.sh` in name order, each with its own `bash` and at most `ENSURE_SCRIPT_TIMEOUT` seconds
   (240 by default), goes on past one that fails or times out, and always exits 0; elsewhere it does nothing.
2. An `ensure-<tool>.sh` installs one tool. It does nothing outside a cloud session and when the tool is already
   there, pins what it downloads, prints at most a line or two, and exits 0, so it never blocks a session.
3. The template ships `ensure-pwsh.sh` for every project and `ensure-nvs.sh` for a project that answers `uses_node`,
   which also does nothing without a `.node-version`.
4. A project bootstraps a tool of its own by adding `.claude/hooks/ensure-<tool>.sh`. Template updates replace the
   template's scripts and leave the project's alone.

## Options considered

- The cloud environment's setup script. Not chosen as the mechanism: it lives outside the repository and serves every
  repository the environment holds. It stays useful for heavy installs, which make an ensure script a no-op.
- One `SessionStart` entry per script. Rejected: adding a tool would mean editing a merged settings file as well.
- A dispatcher that runs drop-in `ensure-*.sh` scripts (chosen).

## Consequences

- A new or updated project's cloud sessions have `pwsh`, and a Node.js project's have nvs and its `.node-version`
  Node.js, without stopping to ask.
- The scripts run one after another, each for at most 240 seconds, within the hook's timeout of 600 seconds, so a
  stalled download in one can't keep the next from running.
- A project that edited its `SessionStart` hook resolves the merge when it updates to the template version that
  brought `session-start.sh`.

## References

- `.claude/settings.json`, `.claude/hooks/`
- [ADR-0008](0008-external-inputs-are-pinned-and-kept-current.md): external inputs are pinned exactly and kept
  current.
