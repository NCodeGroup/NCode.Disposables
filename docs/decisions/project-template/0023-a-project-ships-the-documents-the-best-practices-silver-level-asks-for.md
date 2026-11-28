# 0023. A project ships the documents the OpenSSF Best Practices silver level asks for

- **Status:** Accepted (amended by 0025 and 0040)
- **Date:** 2026-11-15

## Context

The OpenSSF Best Practices badge has a passing, a silver and a gold level. The silver level asks for governance, roles
and responsibilities, a code of conduct, a roadmap, an architecture description and an assurance case. The template
already meets most of its other criteria: a security policy, a license, tests in CI, reviewed and pinned dependencies,
and releases tagged and published as ADR-0016 records once the Landing section sets `release: tag` or `tag+release`. On GitHub, the Scorecard's CII-Best-Practices check scores a passing badge 5 and a silver
badge 7 ([ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md)).

Each project that wants the badge would otherwise research the same criteria again and write the same documents from
nothing. NCodeGroup/Buffers wrote them (Best Practices project 16727), and they carry over once what the template
controls is separated from what only the maintainers know.

## Decision

**Every project ships six project-owned documents: `GOVERNANCE.md`, `CODE_OF_CONDUCT.md`, `ROADMAP.md`,
`docs/architecture.md`, `docs/assurance-case.md` and `docs/best-practices-badge.md`.**

1. A template update adds each one that is missing and never replaces one that exists, so a project edits them freely.
2. Where the template controls a fact, the document states it, generated from the project's answers: the host, the
   stack, whether packages are published, whether NCode Review runs. Where only the maintainers know, the document holds
   a prompt, a comment beginning "Maintainers:", that says what to write.
3. `CODE_OF_CONDUCT.md` is the Contributor Covenant 2.1 verbatim, apart from its contact. The `conduct_contact` answer
   names it, such as an email address or a URL. Left blank, a report goes to GitHub's private vulnerability report form
   with a title that starts "Conduct:", or to a confidential issue on GitLab.
4. The documents say what needs people: the criterion `access_continuity` needs a second owner of the repository's
   organization or group, and gold's two-person review needs a second maintainer. Gold also needs two unassociated
   contributors, the organization's two-factor settings and a recorded security review. The template can't supply
   these, so a project is not claimed to meet the silver level before it has them.
5. The template asks for no `dco`, the criterion that wants a `Signed-off-by` line on each commit.

## Options considered

- Ship nothing, so each project researches the criteria again. Rejected: the work repeats, and the answers drift.
- Generate the security claims, the threat model and the assurance case from the answers. Rejected: the template can't
  check them, and an unchecked security claim is worse than a prompt.
- A public email address as the conduct contact. Rejected: a project may have none, and it is the answer
  `conduct_contact` asks for.
- Require `Signed-off-by` on commits to meet `dco`. Rejected: it adds friction for every contributor, and
  [ADR-0018](0018-agent-attribution-is-avoided-not-enforced.md) keeps authorship in the commit's author.
- Six project-owned documents, generated where the template controls the facts and prompted elsewhere (chosen).

## Consequences

- The prompts stay in a document until its maintainers fill them, and a reader sees which parts are unwritten.
- A project that keeps one of these files elsewhere, such as `.github/CODE_OF_CONDUCT.md`, gets a copy at the root on
  its next update, to merge into its own or delete.
- The silver level waits for a second owner, and the gold level for a second maintainer.
- The documents map the silver criteria that need one to their evidence, and `docs/best-practices-criteria.md` maps
  every criterion ([ADR-0025](0025-every-best-practices-criterion-is-mapped-in-a-template-owned-page.md)), so
  registering at bestpractices.dev is answering, not researching.

## References

- `GOVERNANCE.md`, `CODE_OF_CONDUCT.md`, `ROADMAP.md`, `docs/architecture.md`, `docs/assurance-case.md`,
  `docs/best-practices-badge.md`
- [Contributor Covenant 2.1](https://www.contributor-covenant.org/version/2/1/code_of_conduct/)
- [OpenSSF Best Practices](https://www.bestpractices.dev)
- [ADR-0018](0018-agent-attribution-is-avoided-not-enforced.md): agent attribution is avoided, not enforced.
- [ADR-0020](0020-github-projects-are-scored-by-openssf-scorecard.md): GitHub projects are scored by OpenSSF Scorecard.
- [ADR-0025](0025-every-best-practices-criterion-is-mapped-in-a-template-owned-page.md): every Best Practices criterion
  is mapped in a template-owned page.
- [NCodeGroup/project-template#383](https://github.com/NCodeGroup/project-template/issues/383)
