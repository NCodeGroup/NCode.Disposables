# 1000. README samples are compiled tests that MarkdownSnippets copies into the READMEs

- **Status:** Accepted
- **Date:** 2026-11-24
- **Deciders:** NCode Group

## Context

A sample copied by hand into a README drifts from the API, and nothing notices a renamed member or a sample that never
compiled. Two READMEs show samples: `README.md`, the full guide on GitHub, and `src/NCode.Disposables/README.md`, the
package readme nuget.org renders, where a relative link doesn't resolve.

## Decision

**Every README sample is a compiled xUnit test, MarkdownSnippets copies it into the READMEs, and `build/gate.ps1` fails
when a README differs from its test.**

1. The samples live in `tests/NCode.Disposables.Tests/ReadmeSamplesTests.cs`, one fact per sample named
   `<Sample>Sample_Runs` that asserts what the sample's comments claim, between `// begin-snippet: <Sample>` and
   `// end-snippet`. The file departs from the test naming and region conventions because it tests the READMEs.
2. MarkdownSnippets.Tool, pinned in `.config/dotnet-tools.json` with `rollForward: false`, copies them with the
   `InPlaceOverwrite` convention and `OmitSnippetLinks` from `mdsnippets.json`.
3. `build/Assert-Markdown.ps1` runs `dotnet mdsnippets` and fails when it changes a Markdown file or fails.
   `build/Test-AssertMarkdown.ps1` proves both failures and a pass on a temporary Git repository with a fake
   `dotnet mdsnippets`. `build/gate.ps1` runs the self-test, then the check, before the template-owned `build/dod.ps1`,
   and CI and the Landing section's `gate` run `build/gate.ps1`.
4. A sample is edited in its test, never in a README.

## Options considered

- Hand-copied samples: rejected, because nothing checks them.
- A custom test that parses README code blocks: rejected, because it re-implements MarkdownSnippets.
- Snippet-source links: rejected, because repository-relative links break on nuget.org.
- MarkdownSnippets `InPlaceOverwrite`: chosen. The READMEs keep their names and the package README stays the package
  readme. NCode.PropertyBag's ADR-1003 decides the same.

## Consequences

- The samples show CSharpier's formatting, so a long line wraps in the README.
- An edit inside a `<!-- snippet: -->` block is overwritten by the next `dotnet mdsnippets`.
- Dependabot's `nuget` ecosystem updates the tool with the other packages.
- A `#region` name in the test needs a space, or MarkdownSnippets reads it as a snippet.

## References

- `tests/NCode.Disposables.Tests/ReadmeSamplesTests.cs`, `mdsnippets.json`, `build/Assert-Markdown.ps1`,
  `build/Test-AssertMarkdown.ps1`, `build/gate.ps1`
- [MarkdownSnippets](https://github.com/SimonCropp/MarkdownSnippets)
- [NCode.PropertyBag ADR-1003](https://github.com/NCodeGroup/PropertyBag/blob/main/docs/decisions/1003-readme-samples-are-compiled-tests-that-markdownsnippets-copies-into-the-readmes.md)
