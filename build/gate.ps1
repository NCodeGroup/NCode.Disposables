# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7

<#
.SYNOPSIS
    The project's gate: the README samples check's self-test, build/Test-AssertMarkdown.ps1, the check itself, build/Assert-Markdown.ps1 (ADR-1000), then the Definition of Done, build/dod.ps1.
.DESCRIPTION
    build/dod.ps1 is template-owned and has no stage for the samples, so this script runs that check before it, as project-template ADR-0117 says. -Fast and -Filter are passed on to build/dod.ps1.
#>
[CmdletBinding()]
param([switch]$Fast, [string]$Filter)
$ErrorActionPreference = 'Stop'
Push-Location (Split-Path -Parent $PSScriptRoot)
try {
    dotnet tool restore
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    ./build/Test-AssertMarkdown.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    ./build/Assert-Markdown.ps1
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
    $dod = @{}
    if ($Fast) { $dod.Fast = $true }
    if ($Filter) { $dod.Filter = $Filter }
    & (Join-Path $PSScriptRoot 'dod.ps1') @dod
    exit $LASTEXITCODE
}
finally { Pop-Location }
