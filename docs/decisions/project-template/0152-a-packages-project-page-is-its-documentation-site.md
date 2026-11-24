# 0152. A package's project page is its documentation site, else its repository

- **Status:** Accepted
- **Date:** 2026-11-18

## Context

This decision applies to projects published as NuGet packages.

nuget.org shows a package's `PackageProjectUrl` as its "Project website", next to its "Source repository".
`build/ProjectTemplate.props` set it to the repository Source Link finds, so a library with a documentation site
showed the same link twice, and nothing told it to set its own: NCodeGroup/Buffers found this when its site went live
and set `PackageProjectUrl` by hand (NCodeGroup/Buffers#103). A library's site built with the NCodeGroup/docs
toolchain is configured in `docs/site/site.config.json`, whose required `site.url` is the site's address with its
base path and no trailing slash.

## Decision

- When the project hasn't set `PackageProjectUrl` and `docs/site/site.config.json` exists at the repository root,
  `build/ProjectTemplate.props` reads `site.url` from it with `File::ReadAllText` and a regular expression, and makes
  it the package's project page, with a trailing `/`.
- Otherwise the project page stays the repository, and the release notes still link the changelog there.
- A value the project sets wins: in a project file, in `Directory.Build.props` after the import of the template's
  settings, or on the command line. A site elsewhere, or from another toolchain, is set that way.

## Options considered

- Derive it from `docs/site/site.config.json` (chosen). The site's own config is the one place its address lives, so
  the package follows a site that moves. It ties the default to one toolchain's layout, which every NCode library with
  a site uses; any other site is one property away.
- A Copier question, such as `docs_url`. Rejected: it repeats `site.url` and goes stale when the site moves.
- Document it only. Rejected: every library with a site would still have to find the step, as Buffers did.

## Consequences

- A library that adds a site with the NCodeGroup/docs toolchain gets it as its project page on its next pack.
- A library that already sets `PackageProjectUrl` keeps its value.
- A config whose `site.url` the regular expression doesn't find, such as one with escaped slashes, gives the
  repository, as no config does.

## References

- `build/ProjectTemplate.props`
- [ADR-0130](0130-source-link-and-symbol-packages.md): packages are source-linked and ship symbol packages.
- NCodeGroup/docs, `schema/site.config.schema.json`: the site configuration's schema.
