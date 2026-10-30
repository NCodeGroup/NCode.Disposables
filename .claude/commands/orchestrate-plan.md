---
description: "Run PLAN.md in one session — you verify and re-plan, a cheaper subagent executes each step"
argument-hint: "[Optional extra instructions, such as a step to start from; the plan's worktree name when several are open]"
model: opus
effort: high
disable-model-invocation: true
---
## Work in the plan's worktree

Each plan lives in its own git worktree, so several Claude Code sessions can work on different plans on this machine at once. Before anything else, move this session to the plan:

1. Run `git rev-parse --show-toplevel`. If the path contains `/.claude/worktrees/` and has a `PLAN.md`, this session is in the plan's worktree: go on with the task.
2. Otherwise list the plan worktrees: run `git worktree list --porcelain`, keep each `worktree` path under `.claude/worktrees/` that has a `PLAN.md`, and read each plan's title from its first line.
3. Choose the plan, in this order: the worktree whose folder name the command's arguments give; else the only plan worktree; else, when there is no plan worktree, the `PLAN.md` in the current checkout, worked on where it is — a plan written without a worktree, such as in Copilot. Switch into a chosen worktree with the `EnterWorktree` tool and its `path`. With several plan worktrees and none named, list them with their titles and stop to ask which one. With no plan anywhere, stop and say so.

From here on, the plan's folder is the repository root: run every command there, Done-when commands included. In a worktree, never edit files or run git in the main checkout. When you delegate to a subagent, give it the folder's absolute path and tell it to work only there.

## Task

Execute `PLAN.md` as its coordinator. You are the strong reasoning model: you verify every result and do all re-planning. A cheaper, faster subagent makes every edit.

Extra instructions (may be empty): $ARGUMENTS

Delegate execution to:

- Claude Code: the `plan-executor` subagent.
- Copilot CLI: the `plan-executor` custom agent (or the general-purpose subagent, with its model set through `/subagents`).
- VS Code: the Implementer agent for your setup.

For each step whose heading starts with `### [ ]`, in order:

1. Delegate the step with a self-contained task. The subagent does not see this conversation, so include the step number, the full step text copied from `PLAN.md`, the Context for the executor section, and the instruction to escalate with `[?]` instead of improvising.
2. When it returns, run the step's Done-when command yourself from the repository root, also when the subagent reports that it could not run it: a check the subagent made instead does not count.
3. Passed: mark the step `[x]` if the subagent did not. Run `git status`: a changed file the step does not list is a deviation to resolve before you commit. Then stage only the files the step lists, with `git add <those files>` — never `git add -A` or `git add .`, and never `PLAN.md`, which stays untracked and is never committed or pushed — run `git commit -m "Plan step N: <title>"`, and continue with the next step.
4. Escalated (`[?]`) or failed: re-plan the step yourself — diagnose it, rewrite or split it, record any decision under Decisions, remove the Escalation line, reset it to `[ ]` — then run `git commit --allow-empty -m "Re-plan step N: <title>"` (the plan is untracked, so this commit only records the re-plan), and delegate the step again. When `PLAN.md` has an `Issue:` line and the re-plan changed the Decisions or added or removed steps, post a two-to-five-line comment on each issue on that line saying what changed and why — `gh issue comment <number> --body-file <file>`, or `glab issue note <number> --message <text>` on GitLab — yourself, never through the subagent.
5. Stop and ask me when a step has been re-planned twice and still does not pass, or when re-planning would change the Goal or Out of scope.

Make code changes yourself only as a last resort, after a step has been re-planned twice, and say so in the summary. Finish with a table of step, result, delegations, re-plans and notes, then give the next command: `/review-plan-diff`, or `/review-plan-diff <worktree name>` from another session.
