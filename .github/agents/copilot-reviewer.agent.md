---
name: Copilot Reviewer
description: Review the work against PLAN.md and queue fix steps for Claude Sonnet 5.5
argument-hint: Optional base branch or commit to diff against
model: 'Claude Sonnet 5.5 (copilot)'
tools: ['read', 'search', 'edit', 'execute']
handoffs:
  - label: Execute the fix steps
    agent: Copilot Implementer
    prompt: Execute the next unchecked step in PLAN.md.
    send: true
    model: 'Claude Sonnet 5.5 (copilot)'
  - label: Re-plan with the planner
    agent: Copilot Planner
    prompt: Re-plan the remaining work in PLAN.md based on the review findings above.
    send: false
    model: 'Claude Opus 5.5 (copilot)'
  - label: Land the plan
    agent: agent
    prompt: /ship-plan
    send: false
    model: 'Claude Sonnet 5.5 (copilot)'
  - label: Show where the plan stands
    agent: agent
    prompt: /continue-plan
    send: true
    model: 'Claude Sonnet 5.5 (copilot)'
---
## Work in the plan's worktree

Each plan lives in its own git worktree, so several Copilot sessions, or Copilot and Claude Code sessions, can work on different plans in one clone at once. Before anything else, find the plan's folder, `<plan folder>`:

1. When the task names the plan's folder, as `/run-issues` and `/address-feedback` do, that folder is `<plan folder>`.
2. Otherwise run `git rev-parse --show-toplevel`. When the path contains `/.claude/worktrees/`, or `git rev-parse --path-format=absolute --git-dir` and `git rev-parse --path-format=absolute --git-common-dir` print different paths, and the folder has a `PLAN.md`, that folder is `<plan folder>`: go on with the task.
3. Otherwise list the plan worktrees: run `git worktree list --porcelain`, keep each `worktree` path under `.claude/worktrees/` that has a `PLAN.md`, and read each plan's title from its first line.
4. Choose the plan, in this order: the worktree whose folder name the command's arguments give; else the only plan worktree; else, when there is no plan worktree, the `PLAN.md` in the current checkout, worked on where it is — a plan written without a worktree, such as one Copilot's cloud agent or an earlier template version wrote. With several plan worktrees and none named, list them with their titles and stop to ask which one. With no plan anywhere, stop and say so.

From here on, `<plan folder>` is the repository root: run every command there, Done-when commands included, and never edit files, commit or switch branches in the main checkout; reading it, such as `git worktree list`, is fine. A Copilot session can't change the folder its shell starts in, so start every command with `Set-Location -LiteralPath '<plan folder>';`, give file tools absolute paths under `<plan folder>`, and search with `git -C '<plan folder>' grep` or by path, because a workspace search skips the ignored `.claude/worktrees/`. `Set-Location` moves PowerShell's location, not the process's current folder, which stays where the session started, such as the main checkout. A .NET method, such as `[IO.File]::ReadAllText`, `[IO.File]::WriteAllText`, `[IO.Directory]::GetFiles` or `[IO.Path]::GetFullPath`, resolves a relative path against the process's folder, so a script that passes it one reads or writes the main checkout instead. Never pass a relative path to a .NET method: use PowerShell's own commands, such as `Get-Content -LiteralPath`, `Set-Content -LiteralPath` and `Get-ChildItem`, or make the path absolute first, with `Join-Path (Get-Location) <path>` or `$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('<path>')`. When you delegate to a subagent, give it `<plan folder>` and these rules, and tell it to work only there.

## Task

Review the changes made while executing `PLAN.md`.

Compare against (may be empty): the base branch or commit in my message, if any
If empty, use `git merge-base HEAD origin/<default>`, where `<default>` is the default branch: what follows `origin/` in the output of `git symbolic-ref --short refs/remotes/origin/HEAD`, or `main` when that fails. Without an `origin` remote, use the parent of the first commit whose message starts with "Plan step" or "Re-plan step". After a renumber step has merged the default branch in, the merge base is the merged commit, so the diff holds only this plan's work.

1. Read `PLAN.md`: Goal, Decisions, Out of scope, Steps and Review checklist.
2. Read the full diff (`git diff <base>` including uncommitted changes) and the changed files in full where context matters.
3. Check every step was done as written: the right files, the right behavior, and no shortcuts such as skipped or weakened tests, swallowed errors, hard-coded values or leftover TODOs. Flag every deviation from the step text as at least should-fix even when its checks pass — creating something the step said already exists, touching files the step doesn't list, or changing an interface — because a deviation means the executor improvised instead of escalating.
4. Check what the Done-when commands can't: edge cases, error handling, security, performance, API compatibility, and each Review checklist item. In a pre-release project, which `pwsh -NoProfile -File .template/Get-PlanState.ps1` shows as `pre-release: yes`, also check that every text the plan wrote, the changelog included, describes the product as it is, with no `**Breaking:**` label, no conventional-commit `!` marker or `BREAKING CHANGE:` footer in a commit message or the pull request's title, no wording set against an earlier state, no migration note and no compatibility shim, as ADR-0015 says, and that the changelog keeps `## [Unreleased]` a catalog under `### Added` and `### Security` alone. Then check every file the diff changes against the instruction files that apply to it, each read in full: `AGENTS.md` and each instruction file it lists, every `.github/instructions/*.instructions.md` whose `applyTo` glob matches the file's path, such as `csharp.instructions.md` for a `.cs` file and `csharp-tests.instructions.md` for one in a `<Name>.Tests` project, and every `.claude/rules/*.md` whose `paths:` match it; where a project's own instruction file differs from a template one, the project's wins. The gate checks only what a machine can, so the rules only a reader can check are this review's to catch: naming, test names, regions and file layout, a call nested inside another call's arguments, and an unused using or import the build doesn't report. Report each departure as should-fix, with its file and line and the rule it breaks, quoted, so its fix step runs before the first push instead of costing a round of review.
5. Run the test suite and the checks listed under Context for the executor, after its `- Prelude:` command, when it has one, dot-sourced in the same PowerShell: `$global:LASTEXITCODE = 0; try { . <prelude> } catch { $_; "prelude failed"; exit 2 }; if (-not $? -or $LASTEXITCODE) { "prelude failed"; exit 2 }; <the command>`, where `<prelude>` is that line's command. When it exits 2 and the output's last line, apart from any exit code your shell adds, is `prelude failed`, the command never ran: stop the review without adding steps or a `Reviewed:` line, show me the prelude's error, and say to fix the `- prelude:` line in the `## Landing` section of `AGENTS.md`, or the script it names, and the plan's `- Prelude:` line. When the plan changes a documentation site's pages and `AGENTS.md` names a command that builds the site, build it with that command after the checks, then look at each changed page as built, at the widths the project's layout check uses, or at 375 and 1280 pixels when it has none, with a browser or screenshot tool this session has, such as Playwright. Text checks pass a page whose layout broke, so this is the check that finds it: report each break, such as a mark or a word stranded on a line of its own, text cut off or overlapping, or a component that lost its styling, as a should-fix with the page, the width and what you saw, and a build that fails as a blocker. When this session has no tool that shows a page, say so in your reply and name the pages and widths for me to look at before the merge.
6. Report findings as a table: severity (blocker, should-fix, nit), file and line, problem, fix.
7. For every blocker and should-fix, append a new step to the end of the Steps section of `PLAN.md`, numbered in sequence and in the same format as the existing steps (`### [ ] N. …` with Files, Change and a Done-when command), so the executor model can fix it. Delete any `Reviewed:` line from `PLAN.md`. Work this plan shouldn't do but someone would plan as its own task, such as a separate defect or gap, goes under a `## Follow-ups` heading at the end of `PLAN.md`, one line each as `<short title>: <what to change, where, and why it matters>`, which `/ship-plan` files as an issue; add the heading when it's missing. A nit, a style preference or an idea for someday goes in your reply, not in `PLAN.md`. Change nothing else in the repository.
8. When the review found no blocker or should-fix, record it: put `Reviewed: <the commit git rev-parse HEAD prints>` on its own line among the lines under the plan's title, after `Issue:`, `Release:` and `Refs:`, or after `Pull request:` and `Start:` in an `/address-feedback` run's plan, replacing any `Reviewed:` line there. `/ship-plan` lands only work that matches the reviewed commit, so any later change needs another review. Then say the plan is done, and give the next command: `/ship-plan`, which lands it as `AGENTS.md`'s Landing section says. For a plan with a `Pull request:` line, an `/address-feedback` run's, give `/address-feedback <its number>` instead, which pushes the work to that pull request's branch and replies to its items. When `PLAN.md` has an `Issue:` line, also say that the pull request description gets one `Closes <issue reference>` line per issue on it, one `Refs <issue reference>` line per reference on its `Refs:` line, and a two-to-five-line summary of the plan's steps and Decisions, which `/ship-plan` writes.
9. When the review added fix steps, give the next command instead: `/orchestrate-plan`.
