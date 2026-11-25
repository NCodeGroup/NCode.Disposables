# 1009. The documentation site is narrative guides, built by a shared toolchain outside this repository

- **Status:** Accepted
- **Date:** 2026-11-25
- **Deciders:** NCode.Disposables maintainers

## Context

NCode.Disposables' full guide is its README, and nuget.org shows a shorter one;
NCode.Buffers and NCode.PropertyBag publish documentation sites built by one shared toolchain. A generated API
reference is not used: DocFX 2.81.0 lists no members for C# 14 extension blocks (NCodeGroup/Buffers ADR-1009, read
2026-11-15), and `DisposableExtensions`, `AsyncDisposableExtensions`, `SemaphoreSlimExtensions` and
`ReaderWriterLockSlimExtensions` are extension blocks. The guides do not depend on DocFX.

A site generator for the guides is a Node.js project. Where its package manifest and lockfile live matters here:

- Each release carries an SBOM that `.github/workflows/release-provenance.yml` exports from GitHub's dependency graph
  of this repository. The graph lists the packages of the repository's manifests and lockfiles, so a documentation
  toolchain's packages would be in the SBOM of a library whose packages have almost no dependencies.
- OpenSSF Scorecard scores the repository (project-template ADR-0020). Its Vulnerabilities check scans the repository
  recursively for lockfiles (`Recursive: true` in `clients/osv.go`) and takes one point off for each open advisory
  (`score := checker.MaxResultScore - numVulnsFound` in `checks/evaluation/vulnerabilities.go`), in Scorecard 5.6.0
  (read 2026-11-15).
- A lockfile for Astro Starlight holds several hundred packages: 394 in the toolchain's own (counted 2026-11-15).

## Decision

**The site is narrative guides written by hand, built with Astro Starlight by the shared toolchain in
[NCodeGroup/docs](https://github.com/NCodeGroup/docs), and this repository holds the site's content and samples
only.**

1. The site has a landing page, a quick start, guides, concept pages and a hand-written API map at
   `/reference/api-map/`, whose data `build/Assert-ApiMap.ps1` checks against `src/NCode.Disposables/PublicAPI.Shipped.txt`,
   a picker at `/start/which-disposable/` and a behavior reference at `/reference/behavior/`, both drawn from
   `docs/site/_components/data/disposables.json`, so the two cannot disagree. It has no generated API reference.
2. Every guide has a `## Pitfalls` section, which `requiredHeadings` in `site.config.json` requires.
3. The toolchain repository holds everything that builds a site: the Starlight project, the theme, the plugins, the
   package manifest and its lockfile, and the reusable workflow that builds and publishes a library's site. Its
   `CONTRACT.md` defines what a library holds and what stops a build.
4. This repository holds `docs/site/`: the pages, as `.mdx` files, `site.config.json`, and the site's own components
   in `_components/`; an image, when a page needs one, goes in `_assets/`. The samples the pages show are compiled tests
   ([ADR-1010](1010-site-samples-are-compiled-tests-in-a-project-of-their-own-included-by-key-when-the-site-is-built.md)).
5. This repository has no `package.json`, no lockfile, no Node.js code and no Astro or Starlight configuration. A page
   needs nothing the toolchain does not provide; a need for more is a change to the toolchain.
6. The site is built from a clone of the toolchain beside the main checkout through `build/site.mjs`, a copy of the
   toolchain's launcher (NCodeGroup/docs ADR-1016), which writes nothing here.

## Options considered

- **DocFX for the whole site.** Rejected for the guides: it ties them to an API reference that cannot list the members
  of extension blocks.
- **Astro Starlight in a folder of this repository.** Rejected: the lockfile puts the toolchain's packages in each
  release's SBOM and each of their advisories in the Scorecard result, whether or not the advisory applies to a
  static site.
- **Fumadocs, a Next.js static export.** Rejected: in prototypes built on 2026-11-15 it sent 309 to 344 kB of
  compressed JavaScript to a documentation page where Starlight sent 31 kB, and it makes the site a React application
  to maintain (NCodeGroup/Buffers ADR-1009's finding).
- **VitePress.** Not chosen; it was assessed less than the other two, with no prototype. Its stable line is 1.6.4 and
  2.0 is a pre-release (npm, read 2026-11-15, NCodeGroup/Buffers ADR-1009's finding).
- **A hosted documentation product.** Rejected: the site is published to GitHub Pages from this organization's
  repositories.
- **The pages in the toolchain repository too.** Rejected: a page changes in the same pull request as the code it
  describes ([docs/README.md](../README.md)), and its samples are tests of this repository.
- **Astro Starlight in a shared toolchain repository, with the content here** (chosen).

## Consequences

- The npm dependency graph, its advisories and its updates are in one repository for every NCode library.
- A change to the site's look, components or build is made in the toolchain and reaches this site when the pinned
  toolchain commit moves.
- A contributor who previews the site needs Node.js and a clone of the toolchain. A contributor who changes only the
  library needs neither.
- `.github/workflows/docs.yml` calls the toolchain's reusable workflow at a pinned commit and builds the site on every
  pull request and push to the default branch. Publishing it is
  [ADR-1012](1012-the-documentation-site-is-published-to-github-pages-from-the-default-branch.md).
- A change that ships a public member adds its entry to the API map, which the gate checks.
- A change to a disposable's behavior changes its outcome in `disposables.json`.

## References

- `docs/site/`, `.github/workflows/docs.yml`, `.github/workflows/release-provenance.yml`, `build/Assert-ApiMap.ps1`
- [NCodeGroup/docs](https://github.com/NCodeGroup/docs): the toolchain, its `README.md` and `CONTRACT.md`
- [Project-template ADR-0020](project-template/0020-github-projects-are-scored-by-openssf-scorecard.md)
- [OpenSSF Scorecard 5.6.0, `checks/evaluation/vulnerabilities.go`](https://github.com/ossf/scorecard/blob/v5.6.0/checks/evaluation/vulnerabilities.go)
- [Astro Starlight](https://starlight.astro.build)
