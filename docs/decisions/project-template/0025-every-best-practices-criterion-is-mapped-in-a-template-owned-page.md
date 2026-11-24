# 0025. Every Best Practices criterion is mapped in a template-owned page

- **Status:** Accepted (amends 0023; amended by 0037)
- **Date:** 2026-11-16

## Context

[ADR-0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md) gave every project the
documents the OpenSSF Best Practices silver level asks for, and `docs/best-practices-badge.md` mapped only the silver
criteria that need one. The other questions of the passing, silver and gold levels, 145 criteria in all, were
researched again for each project, and so were the checks the form can't make itself: the host's security settings,
coverage, the secret history, cryptography and regression tests. NCodeGroup/code-review registered as project 16752
on 2026-11-15: it earned passing, and silver stopped at 93% on four MUST criteria, three of them gaps in the template.

`docs/best-practices-badge.md` is project-owned, so no template update reaches it, and the criteria change upstream,
in `criteria/criteria.yml` of ossf/best-practices-badge.

## Decision

1. **Every project gets `docs/best-practices-criteria.md`, template-owned.** It lists every criterion of the three
   levels, one table per section of each level's form, with its requirement at that level, whether the form offers
   N/A, whether a Met answer needs a URL, and the answer a project made from the template should expect, with its
   evidence for the project's stack and host. Each answer has a kind: Met, Check, N/A, Gap, Person or Unmet.
2. **The page holds defaults and evidence, never the saved answers or the criteria's text.** bestpractices.dev is the
   source of truth for both, and the page links each level's criteria.
3. **A test in the template repository compares the page with `criteria/criteria.yml`**: each level's IDs,
   requirements, N/A and URL flags. A criterion added, renamed or retired upstream fails CI until the page follows.
4. **The `best_practices_project` answer records the project's number.** It is never asked: a maintainer sets it with
   `.\.template\Update-Project.ps1 -Data @{ best_practices_project = '<number>' }` once the project is registered.
   The page then shows the badge for `README.md` and the address of the saved answers, and a project created or
   adopted with the answer gets the badge in `README.md` and `docs/best-practices-badge.md`.
5. **The template meets the documentation criteria it can.** A new project's `SECURITY.md` acknowledges a report
   within 14 days, its `CONTRIBUTING.md` states the review requirements and names the `good first issue` label, and
   its `docs/assurance-case.md` has a section that records security reviews.
6. **The page says what stops a level.** With one maintainer, silver stops at `access_continuity`; gold also needs a
   second maintainer, two unassociated contributors, two-factor settings and a security review, which only people
   can provide.

## Options considered

- Extend `docs/best-practices-badge.md`. Rejected: it is project-owned, so no update would bring a project the
  criteria that change upstream or the template's new answers.
- Copy each criterion's question into the page. Rejected: the site shows it, the text changes upstream, and the page
  would double in length.
- Check the page against a copy of `criteria/criteria.yml` kept in the template. Rejected: the copy would go stale
  with the page, and the test would never see an upstream change.
- Generate the page from a data file. Rejected: one more tool to keep; the table is the data, and the test reads it.
- A template-owned page, checked against the upstream file (chosen).

## Consequences

- Registering is answering: the page gives each default, its Check rows say what to look at, and on GitHub it holds
  the prompt that registers the project and answers the criteria in Claude in Chrome.
- The template repository's CI reads raw.githubusercontent.com, and fails when ossf/best-practices-badge changes its
  criteria, until the page follows.
- The Gap rows name what the template doesn't provide yet; they stay Unmet until a project or a later template
  version closes them.
- `SECURITY.md`, `CONTRIBUTING.md`, `docs/assurance-case.md`, `docs/best-practices-badge.md` and `README.md` are
  project-owned: an existing project sees the template's changes to them on its next update and takes what fits.

## References

- `docs/best-practices-criteria.md`, `docs/best-practices-badge.md`
- [ADR-0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md): a project ships the
  documents the Best Practices silver level asks for.
- [OpenSSF Best Practices](https://www.bestpractices.dev), and `criteria/criteria.yml` in
  [ossf/best-practices-badge](https://github.com/ossf/best-practices-badge)
- [NCodeGroup/project-template#398](https://github.com/NCodeGroup/project-template/issues/398)
