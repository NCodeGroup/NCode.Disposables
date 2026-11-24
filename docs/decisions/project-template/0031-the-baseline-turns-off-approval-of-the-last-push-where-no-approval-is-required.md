# 0031. The baseline turns off approval of the last push where no approval is required

- **Status:** Accepted (amended by 0034)
- **Date:** 2026-11-16
- **Amends:** 0022

## Context

[ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md) has `tools/Set-RepositoryBaseline.ps1` in the project
template give "Protect main" no required approval and no approval of the last push, except where `code-review.yml`
passes NCode Review's `approve: true`. It also says the script never loosens a ruleset, so for a ruleset that has the
last-push approval on with no required approval it only prints the command that turns it off.

GitHub's rule "Require approval of the most recent reviewable push" says "whether the most recent reviewable push must
be approved by someone other than the person who pushed it", and the documentation adds that "at least one other
authorized reviewer has approved any changes" (GitHub REST API, "Rules for rulesets"; GitHub Docs, "About protected
branches", both read 2026-11-16). It does not say that the rule is idle while the ruleset requires zero approvals: the
one page that says "This setting has no effect if the ruleset requires zero approvals" says it of another setting,
the extra approval for a pull request Copilot opened without a human collaborator (GitHub Docs, "Available rules for
rulesets", read 2026-11-16). "Pull request authors cannot approve their own pull requests" (GitHub Docs, "Reviewing
proposed changes in a pull request", read 2026-11-16). Every pull request in these repositories is authored by their
owner, since the agents act as the owner, so no approval the rule would accept can exist.

Two repositories showed it. On 2026-11-15 NCodeGroup/Buffers#62 was `BLOCKED` with "Approval required" while the rule was
on and no approval was required, and `CLEAN` once it was off (#385). On 2026-11-16 NCodeGroup/docs, whose ruleset
required zero approvals and had the rule on, kept a pull request `mergeable_state: "blocked"` with every check green,
NCode Review approving, every conversation resolved and auto-merge enabled, until its owner unticked "Require approval
of the most recent reviewable push"; NCodeGroup/Buffers, whose ruleset requires zero approvals and has the rule off,
merges. ADR-0022's decision never creates that combination in a ruleset the script makes, but an update keeps whatever
an older ruleset has, and so does a ruleset made by hand under the same name.

## Decision

With zero required approvals, the baseline never has approval of the last push on in its own "Protect main" ruleset.

1. A new "Protect main" ruleset has approval of the last push on only together with at least one required approval,
   where `code-review.yml` passes `approve: true`, as ADR-0022 says.
2. A run that finds "Protect main" with approval of the last push on and no required approval, whatever wrote it,
   turns the setting off, changing nothing else in the ruleset, and says so: a line naming the ruleset and the change
   (`turn off approval of the last push, which blocks every merge while no approval is required`) that ends in `set`,
   `would set` with `-WhatIf` or, on a plain run where GitHub refuses it, `not available` with the reason. A run that
   writes the whole ruleset, as with `-RequireChecks`, turns the setting off in that write and says so in its summary;
   where GitHub refuses that write, the run reports the repository as failed ("Couldn't update the ruleset"), as it
   does for any refused write of the whole ruleset, and prints no command. On a plain run it does so in the same write
   as the other settings the script turns on, such as conversation resolution, whether the ruleset is alone or beside
   one of the owner's.
3. A "Protect main" ruleset that requires at least one approval keeps approval of the last push as it is, on or off,
   and its count; the baseline turns it on only where `code-review.yml` passes `approve: true`. It never turns it on
   with zero required approvals.
4. A ruleset of the owner's keeps its shape: the baseline prints the notice and the command that turns the setting
   off, as ADR-0022 says, and writes nothing in it but conversation resolution ([ADR-0014](
   0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md)).
5. Dismissing stale reviews on push stays on in "Protect main": with zero approvals it dismisses nothing a merge
   waits for, and with one it makes NCode Review's approval cover the latest push.
6. This amends ADR-0022's decision 2, whose last sentence reads "The script never loosens a ruleset: it prints the fix
   only for approval of the last push with no required approval." ADR-0022's text stays as it was written, and this
   record changes that sentence: the script never loosens a ruleset of the owner's, and it loosens its own "Protect
   main" in this one way alone, turning off approval of the last push where no approval is required. For a ruleset of
   the owner's the sentence stands as written.

## Options considered

- Print the command and write nothing, as ADR-0022 says. Rejected: the notice left NCodeGroup/docs blocked until its
  owner found the setting and unticked it by hand, and the ruleset is the script's own, whose shape ADR-0022 sets as
  zero approvals and no approval of the last push. Turning the setting off restores that shape; every other
  requirement of the ruleset, the checks, the conversations and the merge methods, stays.
- Turn it off in every ruleset that requires pull requests. Rejected: a ruleset of the owner's is shaped by its owner,
  and the baseline touches it only for conversation resolution.
- Turn it off only on a run with `-RequireChecks`. Rejected: a plain run and a `-RequireChecks` run would leave
  different rulesets, and the plain run is the one `-All` makes.
- Require one approval wherever the setting is on. Rejected: ADR-0022 rejects a required approval without NCode
  Review's `approve`, which a sole maintainer can't give.
- Turn it off with zero approvals, and keep it with one or more (chosen).

## Consequences

- A repository whose "Protect main" has the setting on with zero approvals, as NCodeGroup/docs does, gets it turned off
  on the next run of the baseline; `-All -WhatIf` lists each such repository first.
- A maintainer who wants approval of the last push raises the required approvals to one or more, which the baseline
  keeps, or passes `approve: true`, as ADR-0022 says. With zero approvals it can't be kept.
- The baseline loosens one setting in its own ruleset, the only exception to "the baseline never loosens a ruleset"
  in ADR-0022 and [ADR-0030](0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md). It makes no
  other ruleset looser, and the setting it turns off could not be met, so nothing a merge could have waited for goes.
- On a plain run, where GitHub refuses the change, the run says it was not available, with the reason, prints the notice
  with the command that turns the setting off, as it does for a ruleset of the owner's, and goes on; the owner then
  runs the command or unticks the setting in the ruleset. On a run with `-RequireChecks`, which writes the whole
  ruleset, a refused write fails the repository like any other refused ruleset write, and the owner runs the baseline
  again, without `-RequireChecks` if the refusal is about this setting.

## References

- `tools/Set-RepositoryBaseline.ps1` and `tests/Test-RepositoryBaseline.ps1` in the project template's repository
- [Rules for rulesets, `require_last_push_approval`](https://docs.github.com/en/rest/repos/rules), GitHub's REST API,
  read 2026-11-16
- [About protected branches](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches),
  GitHub Docs, read 2026-11-16
- [Available rules for rulesets](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-rulesets/available-rules-for-rulesets),
  GitHub Docs, read 2026-11-16
- [Reviewing proposed changes in a pull request](https://docs.github.com/en/pull-requests/collaborating-with-pull-requests/reviewing-changes-in-pull-requests/reviewing-proposed-changes-in-a-pull-request),
  GitHub Docs, read 2026-11-16
- [ADR-0022](0022-ncode-reviews-approval-gates-the-merge.md): where NCode Review approves, the ruleset requires an
  approval and pull requests open as drafts.
- [NCodeGroup/project-template#385](https://github.com/NCodeGroup/project-template/issues/385)
