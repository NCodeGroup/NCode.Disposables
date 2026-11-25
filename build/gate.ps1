# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    The project's gate: the sample key check (build/Assert-SampleKeys.ps1, tested offline by build/Test-AssertSampleKeys.ps1), the README samples check (build/Assert-Markdown.ps1, tested offline by build/Test-AssertMarkdown.ps1) and the API map check (build/Assert-ApiMap.ps1, tested offline by build/Test-AssertApiMap.ps1), then the Definition of Done, build/dod.ps1, whose Format stage runs the CSharpier check (project-template ADR-0148, ADR-1000, ADR-1010).
.DESCRIPTION
    build/dod.ps1 is template-owned and has no stage for the samples, so this script runs those checks before it, as
    project-template ADR-0117 says. -Fast and -Filter are passed on to build/dod.ps1.
#>
[CmdletBinding()]
param([switch]$Fast, [string]$Filter)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    dotnet tool restore
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    # The READMEs and the site's pages take their samples by key from every C# file, so every marker has the one form
    # both tools read alike, a key is PascalCase and defined once, and only a site sample's key, in
    # tests/NCode.Disposables.Site.Tests, starts with Site (ADR-1010). The check's rules are tested offline first, so a
    # rule broken in the check fails here instead of passing every key.
    ./build/Test-AssertSampleKeys.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    ./build/Assert-SampleKeys.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    # The READMEs' C# samples are copied from the tests (ADR-1000), and MarkdownSnippets must leave the site's pages
    # alone (ADR-1010): build/Assert-Markdown.ps1 fails on a README or page dotnet mdsnippets changes. Its rules are
    # tested offline first, so a rule broken in the check fails here instead of passing every README.
    ./build/Test-AssertMarkdown.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    ./build/Assert-Markdown.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    # The API map check's rules are tested offline first, so a rule broken in build/Assert-ApiMap.ps1 fails here
    # instead of passing every map.
    ./build/Test-AssertApiMap.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    # The API map on /reference/api-map/ lists every line of src/NCode.Disposables/PublicAPI.Shipped.txt once.
    ./build/Assert-ApiMap.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    $dod = @{}
    if ($Fast) { $dod.Fast = $true }
    if ($Filter) { $dod.Filter = $Filter }
    & (Join-Path $PSScriptRoot 'dod.ps1') @dod
    exit $LASTEXITCODE
}
finally { Pop-Location }
