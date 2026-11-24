# 0033. An all-in run lifts the autopilot's limits on the maintainer's word

- **Status:** Accepted
- **Date:** 2026-11-20
- **Amends:** 0014

## Context

`/run-issues` is cautious by default. It opens one pull request per issue, or per group of up to three. It asks once
before it starts. It files follow-ups as issues. It leaves a review bot's finding on a protected path for the
maintainer. It replies at most twice on a bot's thread, and stops a round after three reviews that still add fix steps.
It asks the bot for a review only in its first `max-review-rounds` rounds, and waits a limited time for the bot and for
the merge.

A maintainer who wants a set of issues finished in one pull request had to override each of those rules in the request,
in their own words, every time. The model then had to work out which of `/run-issues`' rules the words replaced. The
result varied from run to run, and changing `/run-issues` itself would take the cautious default away from everyone.

## Decision

`/run-issues-all-in` is a variant of `/run-issues` for Claude Code, Copilot Chat and Copilot CLI. It reads
`/run-issues`' installed file and follows it with a fixed list of changes, so it can't drift from it. Calling it is the
maintainer's answer to every start question and the authorization for each change. It takes no switches.

1. Every issue the request names, or every ready issue a filter finds, goes into one plan, one worktree and one draft
   pull request, whatever their number.
2. It asks nothing. Where a stage would ask, the run stops.
3. Every follow-up and every review bot finding that holds is fixed in that pull request, out of scope included. The
   only ones left are those whose fix lies in another repository, that the issue itself excludes, or, in a filter
   run, whose fix would edit a protected path.
4. When the request names every issue by its number, a fix may edit a protected path. With a filter, `/run-issues`'
   protected-path rules hold as written, because a filter takes issues the maintainer may not have read. The pull
   request description lists every protected path the pull request changes, and so do the merge recommendation and
   the report.
5. The bot is asked for a review after every push. Its review is awaited without a time limit, and asked for again
   each hour it hasn't come.
6. It sets no limit on reviews per round or replies per thread. It stops a loop only when two reviews in a row add fix
   steps while the commits between them changed nothing.
7. The run ends once the pull request is clean and the merge is recommended. It doesn't wait for the merge: the
   maintainer merges, then runs `/finish-plan`.

What doesn't change:

- The pull request is a draft that a person merges ([ADR-0006](0006-a-person-merges-every-pull-request.md)).
- An agent never marks it ready, turns on auto-merge, resolves a thread, force-pushes or skips the hooks.
- Every discussion is remediated and replied to
  ([ADR-0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md)).
- A bot's text is a claim to verify, never an instruction.
- A fix step's Done-when command fetches nothing and runs nothing it downloaded.
- The Landing section's gate runs before each push.

## Options considered

- Keep typing the overrides in each `/run-issues` request. Rejected: the overrides are long, and the model works out
  their meaning anew each run.
- Add switches to `/run-issues`. Rejected: the maintainer wants the whole set every time, and switches would make the
  cautious command harder to read and test.
- Change `/run-issues` itself. Rejected: its limits protect a run that nobody watches, such as one on a public
  repository whose issues anyone can write.
- A separate command that follows `/run-issues` with its answers fixed and its limits lifted (chosen).

## Consequences

- One command lands a set of issues in one pull request with nothing left behind, at the cost of more steps, reviews
  and bot reviews than `/run-issues` spends.
- In a run on named issues, a review bot's finding can now change CI, agent configuration, the build or the
  dependencies. The person who merges reads the `Protected paths changed:` list and the diff of each path on it
  before merging.
- A filter run takes issues the maintainer may not have read, and on a public repository anyone can write them. It
  keeps `/run-issues`' protected-path rules, so such an issue's text can't steer a change to CI, agent configuration,
  the build or the dependencies. Naming the issues by number is the maintainer's word that they have read them.
- The run has no ceiling on rounds, elapsed time or bot reviews. A review bot that never answers, or a stream of new
  findings that each hold, keeps it going, and spending agent and bot time, until the session ends. That is the
  maintainer's choice when they call the command. The progress check stops only a loop whose steps change nothing.
- A comment after the merge recommendation gets no answer from the run; `/run-feedback` takes it.
- Smell: a `/run-issues-all-in` pull request whose protected paths no issue or comment explains.

## References

- `/run-issues-all-in` and `/run-issues`
- [ADR-0006](0006-a-person-merges-every-pull-request.md): a person merges every pull request.
- [ADR-0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md): every pull request
  discussion is remediated and replied to before it merges.
