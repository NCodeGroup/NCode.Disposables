---
name: execute-step
description: "Execute one step of PLAN.md on the execution model; escalate instead of improvising"
---
## Work in the plan's worktree

Each plan lives in its own git worktree, so several Copilot sessions, or Copilot and Claude Code sessions, can work on different plans in one clone at once. Before anything else, find the plan's folder, `<plan folder>`:

1. When the task names the plan's folder, as `/run-issues` and `/address-feedback` do, that folder is `<plan folder>`.
2. Otherwise run `git rev-parse --show-toplevel`. When the path contains `/.claude/worktrees/`, or `git rev-parse --path-format=absolute --git-dir` and `git rev-parse --path-format=absolute --git-common-dir` print different paths, and the folder has a `PLAN.md`, that folder is `<plan folder>`: go on with the task.
3. Otherwise list the plan worktrees: run `git worktree list --porcelain`, keep each `worktree` path under `.claude/worktrees/` that has a `PLAN.md`, and read each plan's title from its first line.
4. Choose the plan, in this order: the worktree whose folder name the command's arguments give; else the only plan worktree; else, when there is no plan worktree, the `PLAN.md` in the current checkout, worked on where it is — a plan written without a worktree, such as one Copilot's cloud agent or an earlier template version wrote. With several plan worktrees and none named, list them with their titles and stop to ask which one. With no plan anywhere, stop and say so.

From here on, `<plan folder>` is the repository root: run every command there, Done-when commands included, and never edit files, commit or switch branches in the main checkout; reading it, such as `git worktree list`, is fine. A Copilot session can't change the folder its shell starts in, so start every command with `Set-Location -LiteralPath '<plan folder>';`, give file tools absolute paths under `<plan folder>`, and search with `git -C '<plan folder>' grep` or by path, because a workspace search skips the ignored `.claude/worktrees/`. `Set-Location` moves PowerShell's location, not the process's current folder, which stays where the session started, such as the main checkout. A .NET method, such as `[IO.File]::ReadAllText`, `[IO.File]::WriteAllText`, `[IO.Directory]::GetFiles` or `[IO.Path]::GetFullPath`, resolves a relative path against the process's folder, so a script that passes it one reads or writes the main checkout instead. Never pass a relative path to a .NET method: use PowerShell's own commands, such as `Get-Content -LiteralPath`, `Set-Content -LiteralPath` and `Get-ChildItem`, or make the path absolute first, with `Join-Path (Get-Location) <path>` or `$ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath('<path>')`. When you delegate to a subagent, give it `<plan folder>` and these rules, and tell it to work only there.

## Task

You are executing one step of an approved plan in `PLAN.md`. A stronger model wrote the plan and owns every design decision; your job is to carry out the step exactly as written.

Step to execute: the request given after the command name
If that is blank, take the first step whose heading starts with `### [ ]`.

1. Read the Goal, Context for the executor, Decisions and Out of scope sections of `PLAN.md`, then your step.
2. Check the step against the code before editing anything. Every file, function, class and behavior the step names must exist and work as the step describes — except what the step explicitly says to create. If anything does not match, escalate (see below). Creating something the step says already exists is improvising, not executing.
3. Do only this step. Do not start later steps, even small ones.
4. Edit only the files the step lists, following the step's Pattern and the existing style of each file. Do not reformat unrelated code.
5. Run the step's Done-when command in a child PowerShell that prints its exit code, because a shell tool can show a command's output without its exit code. With PowerShell, run `pwsh -NoProfile -NonInteractive -Command '<command>'; "exit=$LASTEXITCODE"`; with Bash, run `pwsh -NoProfile -NonInteractive -Command '<command>'; echo "exit=$?"`. `<command>` is the Done-when command exactly as the step writes it. When the plan's Context for the executor has a `- Prelude:` line, `<command>` starts with that line's command, `<prelude>`, dot-sourced so it sets up the shell the check runs in: `$global:LASTEXITCODE = 0; try { . <prelude> } catch { $_; "prelude failed"; exit 2 }; if (-not $? -or $LASTEXITCODE) { "prelude failed"; exit 2 }; <the Done-when command>`. Write each `'` in `<command>` as `''` for PowerShell or as `'\''` for Bash. Judge the result by that `exit=` line alone: `exit=0` passes, and any other value fails. No output, output that looks right, or a check you made another way never counts as a pass. If it fails because of a mistake in your edit, fix it and run it again — at most three attempts. If the command can't run at all — the shell or a permission rule refuses it, or its output prints `prelude failed` on the line before `exit=2`, because the prelude failed and the check never ran — never substitute a check of your own: leave the step marked `[ ]`, say in your report that the Done-when command did not run and why, quoting the prelude's error when it failed, and stop. Whoever coordinates the plan runs the check and marks the step.
6. When it passes, change this step's heading marker from `[ ]` to `[x]` in `PLAN.md`. Change nothing else in `PLAN.md`.
7. Finish with a short report: files changed, the Done-when result, quoting its `exit=` line, and anything the reviewer should check.

## Escalate instead of improvising

You are the fast executor, not the designer. Hand the step back to the planner — do not work around it — whenever finishing it would need thinking the plan has not done for you:

- a design decision, or a choice between approaches, that the step does not settle;
- a refactor, rename or restructuring the step does not list, or redoing a step already marked `[x]`;
- changing a file the step does not list, or an interface that other code depends on;
- the plan contradicts the code: a named file, function or behavior does not exist or works differently;
- the Done-when command still fails after three attempts, or fails for a reason outside your change.

To escalate:

1. Undo partial edits that depend on the open question (`git checkout -- <file>`), so the planner starts from a clean state.
2. In `PLAN.md`, change this step's marker to `[?]` and add one line at the end of the step: `- Escalation: <what you found, with file and symbol names, and the decision or redesign that is needed>`.
3. Stop and report the escalation. The planner re-plans the step with deep reasoning and hands it back to you; the loop repeats until the step is precise enough to execute.
