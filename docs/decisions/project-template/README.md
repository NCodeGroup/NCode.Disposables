# Project Template Decisions

These records hold the decisions behind the project template's rules: why its instruction files, build settings,
workflow and repository setup are the way they are. They are template-owned: every template update replaces this
folder, so never edit a file in it. A project that decides otherwise writes its own record in `docs/decisions/` that
supersedes the template's by number (for example, "Supersedes: project-template ADR-0004"), and lists the override in
`AGENTS.md`, so every agent follows the project's decision instead of the template's.

Their numbers are below 1000: 0001–0099 for every project and 0100–0999 for the records of a stack or an option. A
project numbers its own records from 1000, so an ADR number names one record across both folders, and a template update
never brings a number the project already uses
([ADR-0012](0012-the-templates-records-are-numbered-below-1000-a-projects-from-1000.md)).

## Every Project

| ADR | Decision |
| --- | --- |
| [0001](0001-conventions-and-lessons-live-in-the-repository.md) | Conventions, decisions and lessons live in versioned files in the repository, not in an agent's memory |
| [0002](0002-commits-are-attributed-to-people-not-agents.md) | A commit records the people who wrote it; an AI agent is never an author, committer or co-author (superseded by [0018](0018-agent-attribution-is-avoided-not-enforced.md)) |
| [0003](0003-agent-work-is-planned-on-a-frontier-model-and-executed-on-a-cheaper-one.md) | A frontier model plans and re-plans; a cheaper model executes one verified step at a time and escalates |
| [0004](0004-trunk-based-development-on-the-default-branch.md) | One trunk, `main`, with short-lived branches; no long-lived integration branch |
| [0005](0005-the-default-branch-accepts-changes-only-through-pull-requests.md) | The default branch takes changes only through pull requests whose required checks pass (amended by 0018, 0022, 0030, 0034, 0040 and 0044) |
| [0006](0006-a-person-merges-every-pull-request.md) | A person decides every merge; an agent never merges, marks ready or turns on auto-merge (amended by 0022, 0034 and 0041) |
| [0007](0007-pending-work-is-tracked-as-issues.md) | Pending work is one issue per item on the repository host, closed by the pull request that completes it (amended by 0040 and 0042) |
| [0008](0008-external-inputs-are-pinned-and-kept-current.md) | Packages, actions, SDKs, images and the template version are pinned exactly and moved by reviewed updates (amended by 0040) |
| [0009](0009-new-dependency-versions-wait-a-cooldown.md) | A new dependency version is adopted only once it is at least three days old (amended by 0040) |
| [0010](0010-a-change-consumers-can-observe-is-decided-before-it-is-built.md) | A change consumers can observe starts as an issue, and its decision is recorded before or with the pull request |
| [0011](0011-agents-treat-existing-code-as-evidence-and-report-done-only-when-done.md) | Existing code is evidence, not a constraint; pins come from upstream; done means done in every tracker |
| [0012](0012-the-templates-records-are-numbered-below-1000-a-projects-from-1000.md) | The template's records are numbered below 1000 and a project's from 1000, so an ADR number names one record |
| [0013](0013-pull-requests-merge-with-a-merge-commit-or-by-rebasing.md) | A pull request merges with a merge commit or by rebasing; a project that squash-merges supersedes this record alone (amended by 0018 and 0040) |
| [0014](0014-every-pull-request-discussion-is-remediated-and-replied-to-before-it-merges.md) | Every pull request discussion is fixed, or answered with why not, and replied to before the merge; the person resolves it (amended by 0033 and 0040) |
| [0015](0015-a-pre-release-project-describes-the-product-as-it-is.md) | Until its first stable release, a project with `pre-release: untracked` describes the product as it is, in its changelog too, and keeps no compatibility |
| [0016](0016-releases-are-tagged-and-published-when-their-pull-request-merges.md) | On GitHub, CI tags and publishes a release when its pull request merges, from the changelog or the pull request's `Release:` line; `/ship-plan` is the fallback (amended by 0026, 0028, 0032 and 0040) |
| [0017](0017-cloud-sessions-bootstrap-their-tools-through-ensure-scripts.md) | A Claude Code cloud session bootstraps its tools through `.claude/hooks/ensure-*.sh` scripts that `session-start.sh` runs; a project adds a tool by adding a script |
| [0018](0018-agent-attribution-is-avoided-not-enforced.md) | Agents avoid adding attribution to commits and pull requests, and nothing enforces it: no hook, CI check or read-back fails, rewrites or reports work over it (supersedes 0002, amends 0005 and 0013) |
| [0019](0019-nothing-requires-the-code-review-check-the-person-who-merges-waits-for-it.md) | No ruleset requires NCode Review's "Code review" check; the person who merges waits for the review before merging or turning on auto-merge (amended by 0022, 0034 and 0043) |
| [0020](0020-github-projects-are-scored-by-openssf-scorecard.md) | A GitHub project runs the OpenSSF Scorecard workflow; the record lists what the template controls and what needs a person (amended by 0022, 0025, 0034, 0035, 0037, 0041 and 0044) |
| [0021](0021-tooling-is-written-in-nodejs-then-go-then-powershell.md) | Tooling is written in Node.js first, then Go, then PowerShell 7; Python is left out of the toolchain, except for Python programs such as Copier |
| [0022](0022-ncode-reviews-approval-gates-the-merge.md) | Where `code-review.yml` passes NCode Review's `approve: true`, the ruleset requires one approval and approval of the last push, never the latter alone, and a new such project opens every pull request as a draft (amends 0005, 0006, 0019, 0020 and, where a project has it, 0150; amended by 0031, 0034, 0036, 0041, 0043 and 0044) |
| [0023](0023-a-project-ships-the-documents-the-best-practices-silver-level-asks-for.md) | Every project ships the governance, conduct, roadmap, architecture and assurance documents the OpenSSF Best Practices silver level asks for, with a map from each criterion to its evidence (amended by 0025 and 0040) |
| [0024](0024-a-project-is-licensed-under-apache-2-0-unless-it-answers-otherwise.md) | A project's `license` answer defaults to Apache-2.0; an update or an adoption never chooses a license for existing code, and both warn when the repository has none |
| [0025](0025-every-best-practices-criterion-is-mapped-in-a-template-owned-page.md) | Every OpenSSF Best Practices criterion of the passing, silver and gold levels is mapped to the answer to expect and its evidence in the template-owned `docs/best-practices-criteria.md`, which a test checks against the badge's own list (amends 0023 and 0020; amended by 0037) |
| [0026](0026-tests-gate-coverage-at-the-gold-thresholds-and-a-release-waits-for-them.md) | With `coverage_gate`, CI fails below 90% of the statements or 80% of the branches, and a GitHub release is tagged only after `tag-release.yml` runs CI again on its commit (amends 0016; amended by 0040) |
| [0028](0028-github-releases-are-immutable-and-their-tags-signed.md) | GitHub releases are immutable, outside a repository with a workflow that runs on a release event or uploads release assets, and a public repository's release tags are signed with gitsign; `SECURITY.md` says how to verify both (amends 0016) |
| [0030](0030-the-code-scanning-rule-is-added-only-where-codeql-analyzes-a-language.md) | The repository baseline adds the code-scanning rule only where CodeQL's default setup analyzes a language, switches no setup's suite where it analyzes no language, and warns where a kept rule waits on a setup that analyzes none (amends 0005; amended by 0044) |
| [0031](0031-the-baseline-turns-off-approval-of-the-last-push-where-no-approval-is-required.md) | The repository baseline turns off approval of the last push in its own "Protect main" ruleset where no approval is required, since GitHub then waits for an approval no author can give, and keeps it where approvals are required (amends 0022; amended by 0034) |
| [0032](0032-a-plan-is-released-only-when-it-changes-what-a-release-ships.md) | A plan gets a `Release:` line, and its merge a tag and a release, only when it changes what a release ships: the product's code, a file its package carries or its build output; site pages, CI, decision records, tests and repository docs wait under `## [Unreleased]` (amends 0016) |
| [0033](0033-an-all-in-run-lifts-the-autopilots-limits-on-the-maintainers-word.md) | `/run-issues-all-in` takes every issue into one pull request with no question and no caps, fixes every follow-up and finding there, protected paths included for issues it names, and ends at the merge recommendation (amends 0014) |
| [0034](0034-with-one-writer-the-baseline-requires-no-approval.md) | Where one person has write access, the repository baseline requires no approval and no approval of the last push, even where NCode Review approves, since GitHub counts neither the author's approval nor NCode Review's; NCode Review keeps approving and gets no write access (amends 0005, 0006, 0019, 0020, 0022, 0031 and, where a project has it, 0150; amended by 0036) |
| [0035](0035-a-sole-maintainer-dismisses-the-scorecard-alerts-one-person-cannot-clear.md) | With one writer, the Scorecard alerts of Branch-Protection, CII-Best-Practices and Contributors are dismissed as won't fix with a standard comment, through the project template's tools/Dismiss-ScorecardAlerts.ps1, and /best-practices reports a dismissed alert as accepted (amends 0020 and 0037) |
| [0036](0036-a-required-approval-is-met-by-ncode-reviews-approver-app.md) | Where NCode Review's approver app is set up, the baseline requires one approval, of the last push, whatever the number of writers, since GitHub counts an approval from a reviewer that can write; Actions never approve pull requests, and the baseline pull request is opened from an issue's link (amends 0022, 0034 and, where a project has it, 0150; amended by 0041) |
| [0037](0037-best-practices-reports-scorecard-and-answers-for-the-default-branch.md) | `/best-practices` reports the Scorecard checks under 10 with their actions, finds an existing registration by URL, and answers for the default branch's tip (amends 0020 and 0025; amended by 0035) |
| [0038](0038-the-templates-scripts-carry-the-templates-copyright-and-license.md) | Every script the template owns, in `.template/`, `build/`, `.claude/hooks/` and `.githooks/`, carries the template's copyright line and `SPDX-License-Identifier: MIT`, which a project's header check leaves alone |
| [0039](0039-fork-pull-request-workflows-run-for-established-contributors.md) | The repository baseline and explicit organization policy require fork workflow approval only for contributors new to GitHub, preserving the stricter policy for projects that need it |
| [0040](0040-the-template-serves-ncodegroups-repositories-on-github-only.md) | The template serves NCodeGroup's repositories on GitHub only: it supports no other host, removes GitLab support without a deprecation phase, and leaves a GitLab template, if one is needed, to a separate template (supersedes 0029; amends 0005, 0007, 0008, 0009, 0013, 0014, 0016, 0023, 0026 and, where a project has them, 0027, 0135, 0150, 0154, 0200 and 0201) |
| [0041](0041-ncode-review-reviews-and-approves-dependabots-pull-requests.md) | `code-review.yml` reviews a pull request Dependabot opens from the repository when it opens and after each push Dependabot makes, so a clean review approves it and a minor or patch NCode Review pin bump merges with no person's action; another bot's pull request waits for a `/review` (amends 0006, 0020, 0022 and 0036) |
| [0042](0042-work-is-planned-as-epics-stories-and-plan-steps-on-a-kanban-flow.md) | Large work is an epic of stories, one plan each, whose parent and blocked-by links record their place and order; a story is planned once it meets the Definition of Ready, work flows through Kanban columns with at most two stories in progress and no sprints, and the organization supplies the issue forms (amends 0007) |
| [0043](0043-ncode-review-reviews-a-forks-ready-pull-request-and-never-approves-it.md) | `code-review.yml` passes NCode Review's `review-forks: true`, so a fork's pull request is reviewed once it isn't a draft and records its outcomes when it closes, and is never approved: a maintainer approves it (amends 0019 and 0022) |
| [0044](0044-a-fork-pull-request-merges-without-a-bypass.md) | Every GitHub project runs CodeQL in a project-owned workflow on every pull request, a fork's too, and the baseline turns CodeQL's default setup off where one runs; a fork's author brings its branch up to date, so its pull request merges with no bypass with one writer too (amends 0005, 0020, 0022 and 0030) |

## .NET

| ADR | Decision |
| --- | --- |
| [0100](0100-warnings-are-errors-in-every-project.md) | Every project, tests included, builds with zero warnings |
| [0101](0101-central-package-management.md) | Package versions live in `Directory.Packages.props` through Central Package Management |
| [0102](0102-disallowed-apis-are-banned-at-build-time.md) | Sync-over-async, `JsonDocument` parsing and Newtonsoft.Json are banned at build time |
| [0103](0103-behavior-is-a-service-a-strategy-collection-or-a-mediator.md) | Behavior is a service by default, a strategy collection when open, a mediator only at a pipeline seam; observers are fault-isolated |
| [0104](0104-no-cross-assembly-internalsvisibleto.md) | Production assemblies collaborate through public contracts; `InternalsVisibleTo` is for tests only |
| [0105](0105-the-canonical-implementation-is-named-defaultfoo.md) | The one canonical implementation of `IFoo` is the internal `DefaultFoo` |
| [0106](0106-configuration-binds-strictly-and-is-validated-at-startup.md) | An unknown configuration key or an invalid value stops the host at startup |
| [0107](0107-filtered-reads-push-filtering-to-the-database.md) | A filtered read runs its filter in the database, through an intent-named query |
| [0108](0108-persisted-documents-are-versioned-and-additive.md) | Persisted JSON documents are versioned envelopes that refuse any other document, change in place before a pre-release project's first stable release and additively after it, and are upcast step by step |
| [0109](0109-a-lost-concurrency-race-is-a-store-neutral-exception.md) | A lost optimistic-concurrency race surfaces as one store-neutral exception |
| [0110](0110-shared-state-is-changed-by-read-modify-write.md) | Shared stored state changes through one read-modify-write operation that retries a lost race |
| [0111](0111-failures-carry-a-machine-readable-code.md) | A failure that callers act on carries a stable machine-readable code |
| [0112](0112-binary-data-uses-spans-pooled-buffers-and-cleared-secrets.md) | Binary data uses spans, caller-owned writers, pooled buffers and cleared secrets (amended by 0156) |
| [0113](0113-metrics-have-one-owner-per-signal-and-bounded-dimensions.md) | Each metric signal has one owner, and every attribute comes from a bounded vocabulary |
| [0114](0114-one-activity-source-named-like-its-meter.md) | A project traces only its own mechanisms, through one activity source named like its meter |
| [0115](0115-a-host-logs-to-standard-output.md) | A host logs to standard output, as one JSON line per entry where a collector reads it |
| [0116](0116-visibility-starts-at-the-narrowest-that-works.md) | Every type and member starts at the narrowest visibility that works; `public` needs a reason |
| [0117](0117-one-definition-of-done-gates-every-change.md) | `build/dod.ps1` is the one Definition of Done that people, agents and CI run (amended by 0148) |
| [0118](0118-the-null-forgiving-operator-is-banned-outside-tests.md) | The null-forgiving operator `!` fails the build outside test projects |
| [0119](0119-production-refuses-development-only-settings.md) | Development conveniences are inert unless configured, and Production refuses them, listing every finding at once |
| [0140](0140-replicas-converge-through-reconciliation.md) | Caches expire absolutely, change notices are hints, and derived state is reconciled under a lease |
| [0141](0141-side-effects-after-a-commit-go-through-an-outbox.md) | An effect that must follow a commit is written to an outbox with it and delivered at least once |
| [0142](0142-data-that-grows-with-use-is-pruned-in-bounded-batches.md) | Data that grows with use has a retention period and is pruned in bounded batches |
| [0143](0143-migrations-run-before-the-rollout-and-only-expand.md) | Migrations run in a step before the rollout, never at startup, and only expand until a later release contracts; a pre-release project keeps one initial migration per context |
| [0144](0144-correctness-guards-fail-closed-capacity-guards-fail-open.md) | A correctness guard fails closed with `503`; a capacity guard fails open behind a timeout and a breaker |
| [0145](0145-outbound-calls-are-bounded-and-caller-supplied-urls-are-guarded.md) | Outbound calls are bounded; caller-supplied URLs go through a client that refuses private addresses |
| [0146](0146-secrets-are-never-logged-compared-in-constant-time-and-shown-once.md) | Secrets never reach diagnostics, are compared in constant time, and are shown once and stored hashed |
| [0148](0148-csharp-is-formatted-in-the-gate-and-files-carry-the-spdx-header.md) | CSharpier checks C# in the gate when the project's tool manifest names it, and a file without the SPDX header fails the build (amends 0117) |
| [0155](0155-dotnet-code-targets-only-lts-releases-and-prose-names-no-dotnet-version.md) | Target frameworks and the SDK are LTS releases Microsoft supports, a project moves to a new LTS before the older one's end of support, and only files that pin a .NET version name one (extends 0008) |
| [0156](0156-byte-arrays-are-the-exception-and-secrets-live-in-pinned-pooled-buffers.md) | Bytes travel as spans and memory, with `byte[]` only where an API or a framework needs one; a secret lives in a pinned, pooled buffer zeroed on dispose, through NCode.Buffers (amends 0112) |

## ASP.NET Core

| ADR | Decision |
| --- | --- |
| [0120](0120-request-delegates-are-generated-at-compile-time.md) | Minimal-API request delegates are generated at compile time |
| [0121](0121-html-pages-have-a-strict-content-security-policy.md) | Every HTML page has a strict Content-Security-Policy and is framed only by design |
| [0122](0122-strict-transport-security-outside-development.md) | The application sends Strict-Transport-Security outside Development |
| [0123](0123-resource-ids-are-server-generated-and-opaque.md) | Resource identifiers are minted by the server and opaque |
| [0124](0124-collection-endpoints-use-keyset-pagination.md) | Collection endpoints page by keyset cursor, with no query protocol |
| [0125](0125-a-write-that-loses-a-race-is-answered-412.md) | An API write that loses a concurrency race is answered `412 Precondition Failed` |
| [0126](0126-published-urls-come-from-a-configured-origin.md) | Published URLs come from a configured origin, never the request's `Host` header |
| [0127](0127-the-data-protection-key-ring-is-shared-and-encrypted-at-rest.md) | The Data Protection key ring is shared, encrypted at rest, and checked when the host starts |
| [0128](0128-liveness-never-checks-dependencies-readiness-does.md) | Liveness never checks dependencies; readiness does, and neither shows details publicly |
| [0129](0129-browser-facing-requests-are-safe-by-construction.md) | Local return URLs, no state change on `GET`, antiforgery, `__Host-` cookies, no overposting, `404` for the unseen |

## Libraries

| ADR | Decision |
| --- | --- |
| [0130](0130-source-link-and-symbol-packages.md) | Packages are source-linked, deterministic and ship symbol packages |
| [0131](0131-public-api-is-tracked-and-evolves-compatibly.md) | The public API surface is tracked and evolves backward-compatibly |
| [0132](0132-implementation-packages-depend-only-on-abstractions.md) | Implementation packages reference only abstractions packages; the composition root wires them |
| [0133](0133-abstractions-are-framework-free-and-tiered.md) | Abstractions packages are framework-free, tiered by dependency, and hold no logic |
| [0134](0134-evolving-optional-parameters-are-a-dto.md) | A growing set of optional criteria is an init-only DTO, not positional parameters |
| [0135](0135-packages-are-published-from-a-github-release-through-trusted-publishing.md) | Packages are published from a GitHub release through nuget.org trusted publishing (amended by 0040, 0137, 0138 and 0139) |
| [0136](0136-packages-follow-semantic-versioning-and-the-assembly-version-moves-only-with-the-major.md) | Versions follow Semantic Versioning, from `version.json` (0138) or else the release tag; `AssemblyVersion` moves only with the major (amended by 0138) |
| [0137](0137-the-release-workflow-starts-package-publishing.md) | A release the Tag and publish release workflow publishes starts `publish.yml` through `workflow_dispatch` (amends 0135) |
| [0138](0138-package-versions-come-from-version-json-through-nerdbank-gitversioning.md) | Package versions come from `version.json` through Nerdbank.GitVersioning (amends 0135 and 0136) |
| [0139](0139-releases-carry-build-provenance.md) | Releases carry build provenance: `release-provenance.yml` attests the packages and attaches them to the release (amends 0135; amended by 0151) |
| [0147](0147-packages-are-validated-against-the-last-published-package.md) | Each pack is validated against the last published package, so a binary break fails before it ships (extends 0131; amended by 0150) |
| [0149](0149-stale-nuget-logistics-are-named-not-fixed.md) | Adoption and update name the stale NuGet logistics of an older library and fix none (extends 0147) |
| [0150](0150-a-workflow-raises-the-package-validation-baseline-after-each-release.md) | Once a release is on nuget.org, `build/Update-Baseline.ps1` raises the package validation baseline; on GitHub, `raise-baseline.yml` opens its pull request (amends 0147; amended by 0022, 0034, 0036, 0040 and 0151) |
| [0151](0151-release-follow-ups-are-jobs-publish-calls.md) | `publish.yml` calls `release-provenance.yml` and `raise-baseline.yml` as jobs after it publishes, since `workflow_run` never fires for the run `tag-release.yml` starts (amends 0139 and 0150) |
| [0152](0152-a-packages-project-page-is-its-documentation-site.md) | A package's project page is the documentation site `docs/site/site.config.json` names, else the repository, unless the project sets `PackageProjectUrl` (extends 0130) |
| [0154](0154-a-library-build-takes-nothing-from-the-clock-and-ci-builds-it-twice.md) | A library's `Copyright` names no year, and CI builds its packages twice and compares them entry by entry, so a tag rebuilds to the same packages in any year (extends 0147; amended by 0040) |
