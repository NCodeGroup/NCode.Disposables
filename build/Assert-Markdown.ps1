# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    The README samples check: runs dotnet mdsnippets, which copies the C# samples from the tests into the READMEs
    (ADR-1000), and fails when it changed a README, rewrote a site page (ADR-1010) or failed.
.DESCRIPTION
    Hashes every Markdown file (*.md) and site page (*.mdx) git tracks or would track, runs the samples command, then
    hashes them again. A changed README means a sample changed in only one place: the test and the README disagree. A
    changed page means MarkdownSnippets rewrote an .mdx file into HTML comments that MDX rejects, which mdsnippets.json
    prevents by excluding *.mdx. A failing git command fails the check instead of reading as "nothing changed".
    build/gate.ps1 runs it, after build/Test-AssertMarkdown.ps1, which tests it offline.
.PARAMETER Root
    The repository to check; the default is the repository this script is in.
.PARAMETER Snippets
    The command that merges the samples, run in -Root; the default runs dotnet mdsnippets. The offline test passes a
    fake.
#>
[CmdletBinding()]
param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [scriptblock]$Snippets = { dotnet mdsnippets }
)
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $Root
try {
    # -z prints each path as it is: without it, git quotes a name that holds a byte above 0x7F, and the quoted name
    # matches no file, so the file would drop out of the comparison.
    function Get-MarkdownState {
        $listed = (git ls-files -z --cached --others --exclude-standard -- '*.md' '*.mdx') -join "`n"
        if ($LASTEXITCODE) { throw "git ls-files failed with exit code $LASTEXITCODE." }
        $listed.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries) |
            Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { "$((Get-FileHash -LiteralPath $_).Hash) $_" }
    }

    $before = @(Get-MarkdownState)
    $global:LASTEXITCODE = 0
    & $Snippets
    if ($LASTEXITCODE) { 'README samples: FAILED. dotnet mdsnippets could not merge the samples.'; exit 1 }
    $after = @(Get-MarkdownState)
    $changed = @(Compare-Object $after $before | ForEach-Object { ($_.InputObject -split ' ', 2)[1] } | Select-Object -Unique)
    $pages = @($changed -like '*.mdx')
    if ($pages) { "Site pages: FAILED. dotnet mdsnippets rewrote $($pages -join ', '); restore it, and keep *.mdx in ExcludeMarkdownFiles of mdsnippets.json."; exit 1 }
    if ($changed) { "README samples: FAILED. dotnet mdsnippets updated $($changed -join ', ') from the tests; commit the result."; exit 1 }
    exit 0
}
catch {
    "README samples: FAILED. $($_.Exception.Message)"
    exit 1
}
finally { Pop-Location }
