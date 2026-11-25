# Documentation

The documents that describe NCode.Disposables: what it decided, how larger work is designed, and the contracts it
offers. Every document in `docs/` has a line here.

## What Lives Where

- [architecture.md](architecture.md) and [assurance-case.md](assurance-case.md): the components and the rules the design
  keeps, and why the design meets its security requirements.
- [best-practices-badge.md](best-practices-badge.md): where the repository meets each OpenSSF Best Practices criterion,
  for registering at bestpractices.dev.
- [best-practices-criteria.md](best-practices-criteria.md): every OpenSSF Best Practices criterion of the three levels,
  with the answer to expect, its evidence and the prompt that registers the project; the template owns it.
- [decisions/](decisions/README.md): architecture decision records, one short file per decision, with the project
  template's own decisions in [decisions/project-template/](decisions/project-template/README.md).
- [site/](site/index.mdx): the content of the documentation site, its pages as `.mdx` files, its components and data in
  `_components/`, and its settings in `site.config.json`. The shared toolchain in
  [NCodeGroup/docs](https://github.com/NCodeGroup/docs) builds it
  ([ADR-1009](decisions/1009-the-documentation-site-is-narrative-guides-built-by-a-shared-toolchain-outside-this-repository.md)),
  and the samples its pages show are tests in `tests/NCode.Disposables.Site.Tests`
  ([ADR-1010](decisions/1010-site-samples-are-compiled-tests-in-a-project-of-their-own-included-by-key-when-the-site-is-built.md)).
  `.github/workflows/docs.yml` builds the site on every pull request and publishes it from every push to the default
  branch to https://disposables.ncode.io/
  ([ADR-1012](decisions/1012-the-documentation-site-is-published-to-github-pages-from-the-default-branch.md),
  [ADR-1013](decisions/1013-the-documentation-site-is-served-at-disposables-ncode-io.md)); `build/site.mjs` builds,
  layout-checks and serves it locally.
- [designs/](designs/_template.md): design documents for work bigger than one plan, started from
  [designs/_template.md](designs/_template.md). Each keeps its status current, and its decisions become records in
  `decisions/` as its phases land.
- [metrics.md](metrics.md): the metrics and traces catalog, every meter, instrument, attribute, activity source and
  span the project defines, which dashboards and alerts are built against.
- [api-compatibility.md](api-compatibility.md): how the public API of the published packages evolves and is checked, and how the package validation baseline is kept.

## Rules

- A document changes in the same pull request as the code it describes. A pull request that changes a documented
  behavior, contract or procedure and leaves its document unchanged is incomplete.
- A document claims only what is built. Planned work is named with the issue that tracks it, never described as if it
  existed.
- A document links to a decision record rather than restating its reasoning, so the reasoning has one home.
- A new document gets a line in this index, in the same change.
