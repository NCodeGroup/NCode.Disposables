# 0042. Work is planned as epics, stories and plan steps, on a Kanban flow

- **Status:** Accepted
- **Date:** 2026-11-27
- **Amends:** 0007

## Context

[ADR-0007](0007-pending-work-is-tracked-as-issues.md) makes the repository host's issues the single source of truth
for pending work, and a plan implements up to three of them. Work larger than one plan has no shape of its own: it is
an issue with a hand-kept checklist, and the order of its parts lives in prose.

GitHub gives issues a hierarchy and an order: an organization's issue types, sub-issues, which nest up to eight
levels and count their parent's progress, and "blocked by" links. Their `gh issue` shortcuts need GitHub's GraphQL
API, which a Claude Code cloud session's proxy refuses, while the REST API covers each of them and takes an issue's
`id`, not its number, for a sub-issue or a blocker (read on 2026-11-27). Every project made from the template is one
of NCodeGroup's repositories on GitHub
([ADR-0040](0040-the-template-serves-ncodegroups-repositories-on-github-only.md)). NCodeGroup has the issue types
Epic, Feature, Bug and Task, and its public `.github` repository supplies the default issue forms of every repository
without a `.github/ISSUE_TEMPLATE/` folder; a repository with that folder ignores the organization's forms entirely
(GitHub Docs, "Creating a default community health file", read on 2026-11-27).

NCodeGroup/coordination#4's PostgreSQL phase, broken down by hand on 2026-11-27, shows what goes wrong without rules:
stories filed before their decisions were reviewed, a wait on a pull request that no link can show, "Part of" lines
never linked as sub-issues, one list of stories kept in four documents, and an issue body flattened by a round trip
through a shell variable ([#521](https://github.com/NCodeGroup/project-template/issues/521)).

## Decision

1. **Three levels.** An epic is work larger than one plan, often a design in `docs/designs/`: an issue of type Epic.
   A story is one plan, one worktree, one pull request and at most one release: an issue of type Feature, Bug or
   Task, a sub-issue of its epic when it has one. A subtask is a step of the story's `PLAN.md`, never an issue: the
   plan snapshot on the story shows the steps.
2. **Types choose the plan.** A Bug is planned as a bugfix, or as a security plan when its labels say security. A
   Feature or a Task, or an issue without a type, takes the kind of plan its labels give. An Epic is never planned:
   `/decompose-issue` splits it into stories, and so a story too large for one plan.
3. **Links are the record.** A story's place is its parent link and its order is its "blocked by" links. A command
   that files issues makes those links as it files them, and `/triage-issues` reports a "Part of" or "Depends on"
   line without its link. A design, `ROADMAP.md` or any other document links the epic and never copies its stories.
4. **Decisions come first.** A story that needs a decision no record makes is blocked by a story of its own that
   records it, because a pull request can be neither a sub-issue nor a blocker: the wait is a link, and the epic
   counts it.
5. **The Definition of Ready.** A story is ready to plan when its outcome is stated, what is not in it is stated, its
   acceptance can be checked, every issue it is blocked by is closed or will be by the time it is planned, every
   decision it needs is recorded or is a story that blocks it, and it fits one plan. `/triage-issues` judges it.
6. **One story body.** A story's body has a "Part of #N" line when it has an epic, then What, Not in this issue and
   Depends on; an epic's has Goal, Design and Stories. A story filed by hand and one filed by a command read alike.
7. **The forms are the organization's.** NCodeGroup's `.github` repository holds a form per type, each setting its
   type: an epic asks for its goal, its design and its stories; a feature for its outcome, what is not in it, its
   acceptance and what it depends on; a bug for what happens, what should, how to reproduce it and its acceptance;
   a task for its outcome and its acceptance. The template ships no `.github/ISSUE_TEMPLATE/` folder and no answer
   that adds one, and no project falls back to labels for types.
8. **Kanban, without sprints.** Work flows through Backlog (filed), Ready (judged ready), In progress (its plan
   snapshot is posted), In review (its pull request is open) and Done (closed by the merge), with at most two stories
   in progress and in review together, the limit `/run-issues` keeps by landing one pull request while it prepares
   the next. A GitHub Project can show the columns: the Landing section's `board` line names it, `none` by default,
   and the commands move a story only when it is set. The issues stay the record; the board is a view of them.
9. **Two rituals.** Run `/triage-issues` when Ready runs low. When an epic's last story closes, hold a retrospective
   whose lessons go into an instruction file or a decision record, as
   [ADR-0001](0001-conventions-and-lessons-live-in-the-repository.md) asks.
10. **Through REST.** The commands read and write types, sub-issues and "blocked by" links through `gh api` with REST
    paths in every session, pass an issue's `id` where the API asks for it, and round-trip an issue's body through
    JSON and a file, never through a shell variable's lines.

## Options considered

- Large work as an issue with a checklist. Rejected: the checklist drifts from the issues, and the order is prose.
- Each plan step as an issue. Rejected: it duplicates the plan, whose snapshot shows the steps.
- Sprints, with an iteration field, estimates and velocity. Rejected for projects with one or a few maintainers:
  their commitment and ceremonies cost more than they return. A team that wants sprints adds an iteration field to
  its board; the hierarchy and the commands stay as they are.
- Issue forms in every project, optional through a Copier answer. Rejected: every project is in NCodeGroup, whose
  forms apply, and a project's own forms would hide them and drift.
- Labels as a fallback for repositories without issue types. Rejected: every project's organization has the types.
- Epics, stories and plan steps on issue types and links, with an optional Kanban board (chosen).

## Consequences

- An epic's progress counts every story, the one that records its decisions included.
- `/decompose-issue` files an epic's stories with their types, parent and "blocked by" links in one pass;
  `/triage-issues` takes `epic:#N`, judges readiness by the Definition of Ready and reports links that drift;
  `/plan-issue` refuses an epic; `/run-issues` holds a story until its blocker lands; `/ship-plan` files follow-ups
  under the story's epic and says when the epic's last story closes.
- A project with a board sets it up once with the project template's `tools/kanban-board.mjs`. Each move needs the
  `gh` token's `project` scope; a move that fails is reported and stops nothing. GitHub's API sets none of a
  project's built-in workflows, so the ones that add an item and set Backlog are turned on in its settings.

## References

- [#521](https://github.com/NCodeGroup/project-template/issues/521), the epic, and its stories
  [#522](https://github.com/NCodeGroup/project-template/issues/522) to
  [#526](https://github.com/NCodeGroup/project-template/issues/526); NCodeGroup/.github#3, the organization's forms
- [ADR-0003](0003-agent-work-is-planned-on-a-frontier-model-and-executed-on-a-cheaper-one.md): agent work is planned
  on a frontier model and executed on a cheaper one.
