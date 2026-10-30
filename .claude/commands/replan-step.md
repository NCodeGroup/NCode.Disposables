---
description: "Re-plan a PLAN.md step the executor escalated ([?]) or failed ([!]), then hand it back"
argument-hint: "[Step number, plus the error or escalation note if you have it; the plan's worktree name when several are open]"
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

A step of `PLAN.md` came back from the executor: it escalated the step (marked `[?]` with an `- Escalation:` note) or failed it (marked `[!]`).

Step and details (may be empty): $ARGUMENTS
If that is empty, take the first step marked `[?]` or `[!]`.

You are the planner. Do the deep reasoning the executor must not do, then hand the step back for execution.

1. Read `PLAN.md`, the step and its Escalation note, and the current state of the files it touches — `git status` and `git diff` show any partial work.
2. If the step failed, run its Done-when command and read the failure.
3. Decide why it came back, with evidence: the step was ambiguous, too big, wrong about the code, missing context, needs a redesign, or is beyond the executor model.
4. Fix the plan rather than the code:
   - Ambiguous or wrong about the code: rewrite the step with exact files, symbols and behavior.
   - Too big: split it into smaller steps and renumber the steps after it.
   - Missing context: add it to Context for the executor or to the step.
   - Needs a redesign: make the design decision, record it under Decisions, and update every later step it affects.
   - Beyond the executor model: implement only this step yourself, run its Done-when command, and mark it `[x]`.
5. Keep or revert the partial work deliberately — say which, and run `git checkout -- <files>` when reverting.
6. Remove the step's `- Escalation:` line (its outcome now lives in the step or under Decisions) and change the heading marker back from `[?]` or `[!]` to `[ ]`, unless you completed the step.
7. When `PLAN.md` has an `Issue:` line under its title and this re-plan changed the Decisions or added or removed steps, post a short comment on each issue on that line yourself, one comment per issue: `Re-plan of step N` and two to five lines on what changed and why, without the full plan. On GitHub run `gh issue comment <number> --body-file <file>`, with the text in a temporary file outside the repository; on GitLab (`git remote get-url origin` contains `gitlab`) run `glab issue note <number> --message <text>`. When posting fails, say so in your report. A re-plan that only rewords or clarifies a step posts nothing.
8. Report what changed and why, then hand the step back for execution. If `git log --oneline` already shows two "Re-plan step N" commits for this step, stop and ask me instead of re-planning a third time.
