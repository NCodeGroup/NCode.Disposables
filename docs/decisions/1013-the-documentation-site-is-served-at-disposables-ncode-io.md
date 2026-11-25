# 1013. The documentation site is served at disposables.ncode.io

- **Status:** Accepted
- **Date:** 2026-11-25
- **Deciders:** NCode.Disposables maintainers

## Context

[ADR-1012](1012-the-documentation-site-is-published-to-github-pages-from-the-default-branch.md) publishes the site to
GitHub Pages from the default branch. Without a custom domain, GitHub serves it under its own domain,
`https://ncodegroup.github.io/NCode.Disposables/`. NCode Group owns `ncode.io` and gives it to documentation alone:
every NCodeGroup documentation site is served under it, and products and marketing use other domains. The domain's
DNS is hosted on Cloudflare, and the NCodeGroup organization has verified `ncode.io` for GitHub Pages, so no other
GitHub account can serve a site under it.

The toolchain's deploying run reads the site's URL from the repository's Pages settings, through
`actions/configure-pages`, and builds the site for that URL and its base path; a pull request's build reads `site.url`
in `docs/site/site.config.json` (NCodeGroup/docs `.github/workflows/site.yml` at v0.18.0). The package's project
website comes from the same `site.url` (project-template ADR-0152).

## Decision

**The site is served at https://disposables.ncode.io/, a subdomain of `ncode.io` named for the library, from the root
path.**

1. `disposables.ncode.io` is a CNAME record to `ncodegroup.github.io`, DNS only, so GitHub Pages serves the site and
   issues its certificate.
2. The repository's Pages settings name `disposables.ncode.io` as the custom domain, with HTTPS enforced.
3. `site.url` in `docs/site/site.config.json` is `https://disposables.ncode.io`, so pull request builds, the package's
   project website and the README's links name the address the site is served at.
4. The repository holds no `CNAME` file: a GitHub Actions deployment takes its custom domain from the Pages settings,
   and the toolchain serves no file at a fixed URL (its `CONTRACT.md`, section 6).

## Options considered

- **Keep `https://ncodegroup.github.io/NCode.Disposables/`.** Rejected: NCode Group's documentation lives under its own
  domain.
- **A path under `ncode.io`, such as `https://ncode.io/NCode.Disposables/`.** GitHub Pages serves a project site there
  once the organization's own Pages site claims `ncode.io`. Rejected: the path is always the repository's name, and
  every library's site depends on that one repository and shares one origin with the others.
- **A subdomain per library** (chosen): each site is set up on its own, has its own origin, and is served from its
  root path, and `ncode.io` itself stays free for a page that lists every library's site.

## Consequences

- GitHub Pages redirects `https://ncodegroup.github.io/NCode.Disposables/` and every page under it to the same page at
  `https://disposables.ncode.io/`.
- A change to the custom domain takes effect on the site with the next deploying run of `docs.yml`.
- `build/ProjectTemplate.props` sets each package's `PackageProjectUrl` from `site.url`, so nuget.org shows the site
  from the next published release
  ([project-template ADR-0152](project-template/0152-a-packages-project-page-is-its-documentation-site.md)).
- Another NCodeGroup library's site is a sibling subdomain, such as `<library>.ncode.io`, set up the same way.

## References

- [ADR-1012](1012-the-documentation-site-is-published-to-github-pages-from-the-default-branch.md)
- [project-template ADR-0152](project-template/0152-a-packages-project-page-is-its-documentation-site.md)
- `.github/workflows/docs.yml`, `docs/site/site.config.json`
