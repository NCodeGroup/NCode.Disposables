# 0039. Fork pull request workflows run for established contributors

- **Status:** Accepted
- **Date:** 2026-11-27

## Context

GitHub can require a maintainer's approval before workflows run on a pull request from a fork. Requiring approval for
every first-time contributor to a repository delays feedback for contributors already established on GitHub
([#518](https://github.com/NCodeGroup/project-template/issues/518)).

## Decision

1. The repository baseline sets the fork pull request workflow approval policy to
   `first_time_contributors_new_to_github`. Only contributors new to GitHub need approval, so established contributors
   can get workflow feedback without waiting for a maintainer.
2. The baseline sets the organization default to the same policy only when run with `-OrganizationPolicy`. It
   preserves the stricter `all_external_contributors` policy at both organization and repository level.
3. Projects with self-hosted runners or `pull_request` workflows that use secrets must retain
   `all_external_contributors`. The automation does not inspect runners or workflows for these risks; where it raises
   a policy, it prints a notice that such a project must keep the stricter one.

## Options considered

- Require approval for every first-time contributor to the repository. Rejected: established GitHub contributors
  wait for a maintainer before workflows run.
- Require approval only for contributors new to GitHub (chosen): established contributors get feedback sooner, while
  projects needing stricter protection keep `all_external_contributors`.
- Require approval for all external contributors everywhere. Rejected: this adds a maintainer wait even where the
  stricter policy is not needed.

## Consequences

- Established GitHub contributors' fork pull request workflows can run without maintainer approval under the chosen
  policy.
- Maintainers must retain the stricter policy when using self-hosted runners or secret-bearing `pull_request`
  workflows; the automation does not check for either risk, and prints a notice where it raises a policy.

## References

- `tools/Set-RepositoryBaseline.ps1` in the project template's repository
- [NCodeGroup/project-template#518](https://github.com/NCodeGroup/project-template/issues/518)
