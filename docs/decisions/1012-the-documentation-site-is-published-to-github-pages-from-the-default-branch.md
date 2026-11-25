# 1012. The documentation site is published to GitHub Pages from the default branch

- **Status:** Accepted
- **Date:** 2026-11-25
- **Deciders:** NCode.Disposables maintainers

## Context

[ADR-1009](1009-the-documentation-site-is-narrative-guides-built-by-a-shared-toolchain-outside-this-repository.md)
keeps the site's content in `docs/site/` and its build in the shared toolchain, NCodeGroup/docs, whose reusable
workflow builds a library's site and publishes it to that library's GitHub Pages when its `deploy` input is true (the
toolchain's `README.md` at v0.18.0, read 2026-11-24). The site has hand-written reference pages, one of them for every
public member, and a generated reference, if one is ever wanted, belongs in the toolchain, which builds the site of
every NCode library.

The toolchain's `README.md` lists a one-time setup: the repository's Pages source is GitHub Actions, and the
`github-pages` environment takes deployments from `main` only. The setup is the maintainers' to do in the repository's
settings; the workflow in this repository does not change it.

## Decision

**`.github/workflows/docs.yml` publishes the site to GitHub Pages from every run on the default branch, and the site
has no generated API reference.**

1. The workflow passes `deploy: ${{ github.ref == format('refs/heads/{0}', github.event.repository.default_branch) }}`
   to the toolchain, as the toolchain's `README.md` shows: a push to `main` builds and deploys the site, and a pull
   request only builds it.
2. The reference pages are written by hand, as ADR-1009's first rule says. A generated API reference is a change to
   the toolchain, not to this repository.
3. `README.md`, which nuget.org shows, links to the site by its absolute URL, and the repository's website is the
   site.

## Options considered

- **Leave the site unpublished.** Rejected: a reader sees it only by downloading a workflow run's artifact.
- **Publish only from a release tag.** Rejected: the toolchain deploys from the default branch only, and the
  `github-pages` environment accepts no other ref. A page merged into `main` is live once the workflow's deploy job
  finishes, and the site and the newest package rarely differ.
- **An API reference generated with DocFX.** Rejected for the reason ADR-1009 gives: DocFX 2.81.0 lists no members for
  the C# 14 extension blocks that hold a large part of the public API.
- **Publish from the default branch, with hand-written reference pages** (chosen).

## Consequences

- The site follows `main`: a page merged there is live once the workflow's deploy job finishes.
- A deployment runs the toolchain's deploy job with the `pages: write` and `id-token: write` permissions `docs.yml`
  grants; that job runs none of this repository's code.
- The site's address comes from the Pages settings, so a custom domain needs no change here
  ([ADR-1013](1013-the-documentation-site-is-served-at-disposables-ncode-io.md)).
- The first run on `main` fails until a maintainer sets the Pages source to GitHub Actions and limits the
  `github-pages` environment to `main`.

## References

- [ADR-1009](1009-the-documentation-site-is-narrative-guides-built-by-a-shared-toolchain-outside-this-repository.md)
- [ADR-1013](1013-the-documentation-site-is-served-at-disposables-ncode-io.md)
- `.github/workflows/docs.yml`, `docs/site/site.config.json`
- [NCodeGroup/docs](https://github.com/NCodeGroup/docs): its `README.md`, sections Workflow and One-time setup
