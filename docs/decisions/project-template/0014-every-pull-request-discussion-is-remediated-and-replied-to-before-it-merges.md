# 0014. Every pull request discussion is remediated and replied to before it merges

- **Status:** Accepted (amended by 0033 and 0040)
- **Date:** 2026-11-01

## Context

A pull request collects discussions from people, from NCode Review, from CodeQL and from other bots. The ruleset's
conversation rule blocks a merge until every thread is resolved, but a thread that is resolved without a reply leaves no
record of what was done, or why nothing was. GitHub can require resolution; it cannot require a reply.

An agent that replies only to the threads it acts on leaves nits, findings on protected paths and threads that others
started without a reply, and the person who resolves them reads the code again to learn what became of each one.

An agent that sets a finding aside by its severity, such as a nit, leaves the person to fix it by hand, though fixing
every finding that holds is what lets a review bot's next review come back with nothing left to act on.

## Decision

Every discussion on a pull request is remediated and replied to before the pull request merges, and the person resolves
it.

1. A discussion is a review thread (a resolvable discussion on GitLab), or a top-level comment or review summary that
   asks for something.
2. It is remediated when it is fixed in a commit, or answered with why nothing changed: declined with its reason,
   answered, put to the maintainer, left as a follow-up, or left for the maintainer because it touches a protected path.
   A reply that explains why nothing changed remediates a discussion as fully as a fix does. Every discussion is judged
   against the code, whatever its severity: a nit gets the same judgment as a blocker, and severity orders the work, not
   whether it is done. An agent judges before it replies, and never posts a placeholder reply, which would spend one of
   its two replies.
3. It is replied to when a reply says which of these it is. An agent's reply starts with `Autopilot:`, and an agent
   posts at most two replies on a thread that a bot or someone else started.
4. The default branch's ruleset requires conversation resolution. `tools/Set-RepositoryBaseline.ps1` turns it on in every
   default-branch ruleset that requires pull requests, and on GitLab the project's "All threads must be resolved" merge
   check plays that part, which `tools/Set-GitLabBaseline.ps1` turns on.
5. `/address-feedback`, `/run-feedback`, `/run-issues` and `/continue-plan` reply to, or list, every unresolved thread,
   and recommend a merge only when every thread is answered and the review bot's review of the last push they asked for
   has come back with nothing left to act on; `max-review-rounds` limits how often they ask for that review, not what
   they fix. A thread that a run's item limit leaves out, such as `/address-feedback`'s 40 items, gets no reply in that
   run, so its last comment still awaits one and the next run takes it; the run lists it.
6. An agent never resolves a thread: the person resolves it
   ([ADR-0006](0006-a-person-merges-every-pull-request.md)).

## Options considered

- A required CI check that fails while a thread has no reply. Rejected: no workflow runs when a thread is resolved, so
  the check goes stale, and it costs a run per comment.
- Agents resolve the threads they fixed. Rejected: resolving is the person's sign-off
  ([ADR-0006](0006-a-person-merges-every-pull-request.md)).
- Reply only to the threads an agent acts on. Rejected: the person is left with unanswered threads to judge from
  scratch.
- The ruleset plus replies from every agent. Chosen: the ruleset keeps an open thread from merging, and the replies
  tell the person what became of each one.

## Consequences

- Every nit is judged and, when it holds, fixed like any finding; every thread gets a reply, and a thread that a bot
  started gets at most two.
- A bot's finding on a protected path is fixed only when the maintainer approves its change in the run's start question;
  otherwise it is left for the maintainer with the change it proposes.
- `/run-issues-all-in` lifts the two-reply limit and fixes a bot's finding on a protected path when it holds, on the
  maintainer's word ([ADR-0033](0033-an-all-in-run-lifts-the-autopilots-limits-on-the-maintainers-word.md)).
- `tools/Set-RepositoryBaseline.ps1` turns on conversation resolution in a ruleset that an owner shaped, and changes
  nothing else in it.
- A project that wants otherwise writes a record that supersedes this one.

## References

- [ADR-0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md): the default branch accepts changes
  only through pull requests that pass CI.
- [ADR-0006](0006-a-person-merges-every-pull-request.md): a person merges every pull request.
- `AGENTS.md` Landing section: the pull request flow and the ruleset.
- `tools/Set-RepositoryBaseline.ps1` in the project template's repository: the baseline's ruleset.
- `tools/Set-GitLabBaseline.ps1` in the project template's repository: the GitLab project's merge checks.
