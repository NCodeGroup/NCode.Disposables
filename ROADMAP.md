# Roadmap

What NCode.Disposables intends to do over the next year. Open issues hold the detail and the current state; this page
is the direction. It is reviewed at least once a year and when a release changes it.

## Scope

NCode.Disposables covers what [README.md](README.md) describes. A feature outside that starts as an issue, where the
maintainers say whether it fits.

## Next Twelve Months

- **Target framework.** Keep the one target framework on the oldest .NET LTS release Microsoft supports
  ([ADR-1003](docs/decisions/1003-the-package-targets-the-oldest-supported-lts-release.md)): `net10.0`, supported
  until November 2028, so no change is due this year.
- **Compatible releases.** Add features in minor releases, validated against the last published package
  ([ADR-0131](docs/decisions/project-template/0131-public-api-is-tracked-and-evolves-compatibly.md),
  [ADR-0147](docs/decisions/project-template/0147-packages-are-validated-against-the-last-published-package.md)); a
  member is removed only in a major release, after a minor release marks it obsolete.
- **Documentation site.** Keep [disposables.ncode.io](https://disposables.ncode.io/) in step with the code: a page
  changes in the same pull request as the behavior it describes, and every sample on it is a test
  ([ADR-1010](docs/decisions/1010-site-samples-are-compiled-tests-in-a-project-of-their-own-included-by-key-when-the-site-is-built.md)).
- **Tests.** Keep the tests above 90% of the statements and 80% of the branches, with property tests of the rules that
  hold for any input, such as reverse-order disposal and disposing once.
- **Supply chain.** Keep external inputs pinned and current
  ([ADR-0008](docs/decisions/project-template/0008-external-inputs-are-pinned-and-kept-current.md)),
  and raise the OpenSSF Scorecard score where the repository controls it
  ([ADR-0020](docs/decisions/project-template/0020-github-projects-are-scored-by-openssf-scorecard.md)).
- **OpenSSF Best Practices.** Keep the passing level and reach silver, which needs a second owner for continuity
  ([GOVERNANCE.md](GOVERNANCE.md#continuity), [docs/best-practices-badge.md](docs/best-practices-badge.md)).

## Not Planned

- **Finalizers.** A disposable never disposes what its caller doesn't, so nothing depends on the garbage collector
  ([docs/assurance-case.md](docs/assurance-case.md#security-requirements)).
- **Package dependencies.** The package depends on no other package ([docs/architecture.md](docs/architecture.md)).
- **More target frameworks.** A second one is added only when the library needs an API the targeted one lacks
  ([ADR-1003](docs/decisions/1003-the-package-targets-the-oldest-supported-lts-release.md)).
- **Idempotent structs.** Value disposables, leases and lock scopes stay structs that release on every dispose
  ([ADR-1006](docs/decisions/1006-value-disposables-and-lock-scopes-are-non-idempotent-structs.md)).
