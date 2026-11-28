# Copyright (c) 2026 Marcin Polewski
# SPDX-License-Identifier: MIT

<#
.SYNOPSIS
    Checks that the .NET SDK global.json pins and every target framework are LTS releases Microsoft supports.

.DESCRIPTION
    From the project template; template updates replace this file. Reads the SDK version global.json pins and lists the
    projects of every solution at the repository root, asking MSBuild for the TargetFramework and TargetFrameworks of
    each, so a value that Directory.Build.props or build/ProjectTemplate.props sets counts. Looks up the channel of the
    SDK and of every net and netcoreapp framework in Microsoft's release metadata (releases-index.json).

    Fails when a channel is missing from the metadata, is a Standard Term Support release, has reached its end of
    support, or is in a phase other than active or maintenance. Warns, without failing, when a channel is in
    maintenance, that is, when its end of support is near. Passes when every channel is an LTS release Microsoft
    supports. Prints one line per channel, then PASSED or FAILED, and exits 1 on a failure.

    CI runs it once it has installed .NET: a step of .github/workflows/ci.yml on GitHub, and .gitlab-ci.yml on GitLab.
    On GitHub Actions the lines are workflow annotations. The policy is ADR-0155.

.PARAMETER ReleasesIndex
    The path of a copy of releases-index.json to read instead of downloading Microsoft's; for tests and offline runs.

.PARAMETER Root
    The repository root to check. Defaults to the folder above this script's.

.PARAMETER WarnOnly
    Reports every failure as a warning and exits 0, for a project that can't migrate yet (ADR-0155).

.EXAMPLE
    ./build/Test-DotnetSupport.ps1
#>
#Requires -Version 7.4
[CmdletBinding()]
param(
    [string]$ReleasesIndex,
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [switch]$WarnOnly
)
$ErrorActionPreference = 'Stop'
Push-Location $Root
try {
    $env:DOTNET_NOLOGO = '1'
    $sources = [ordered]@{}
    $addSource = {
        param([string]$Channel, [string]$Source)
        if (-not $sources.Contains($Channel)) { $sources[$Channel] = [System.Collections.Generic.List[string]]::new() }
        if (-not $sources[$Channel].Contains($Source)) { $sources[$Channel].Add($Source) }
    }

    $globalJson = Join-Path $Root 'global.json'
    if (Test-Path -LiteralPath $globalJson) {
        $sdkVersion = (Get-Content -Raw -LiteralPath $globalJson | ConvertFrom-Json).sdk.version
        if ($sdkVersion -match '^(\d+)\.(\d+)') { & $addSource "$($Matches[1]).$($Matches[2])" 'global.json' }
    }

    $projects = @()
    foreach ($solution in @(Get-ChildItem -LiteralPath $Root -File | Where-Object { $_.Extension -in '.sln', '.slnx' })) {
        $listed = @(dotnet sln $solution.FullName list 2>&1)
        if ($LASTEXITCODE) { throw "dotnet sln $($solution.Name) list failed: $($listed -join "`n")" }
        $projects += @($listed | ForEach-Object { ("$_".Trim()) -replace '\\', '/' } | Where-Object { $_ -match '\.(cs|fs|vb)proj$' })
    }

    foreach ($project in @($projects | Select-Object -Unique)) {
        $properties = @(dotnet msbuild $project -getProperty:TargetFramework -getProperty:TargetFrameworks 2>&1)
        if ($LASTEXITCODE) { throw "dotnet msbuild $project failed: $($properties -join "`n")" }
        # MSBuild writes its warnings to stderr and the JSON to stdout: parse only stdout.
        $text = @($properties | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }) -join "`n"
        $start = $text.IndexOf('{')
        $end = $text.LastIndexOf('}')
        if ($start -lt 0 -or $end -lt $start) { throw "dotnet msbuild $project printed no JSON properties: $($properties -join "`n")" }
        $json = ConvertFrom-Json $text.Substring($start, $end - $start + 1)
        foreach ($framework in "$($json.Properties.TargetFrameworks);$($json.Properties.TargetFramework)" -split ';') {
            if ($framework -match '^net(?:coreapp)?(\d+)\.(\d+)') { & $addSource "$($Matches[1]).$($Matches[2])" $project }
        }
    }
}
finally { Pop-Location }

$index = @()
if ($sources.Count -gt 0) {
    if ($ReleasesIndex) {
        $index = (Get-Content -Raw -LiteralPath $ReleasesIndex | ConvertFrom-Json).'releases-index'
    }
    else {
        $index = (Invoke-RestMethod -Uri 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/releases-index.json' -ConnectionTimeoutSeconds 30 -OperationTimeoutSeconds 60 -MaximumRetryCount 2 -RetryIntervalSec 5).'releases-index'
    }
}

$errorPrefix = if ($env:GITHUB_ACTIONS -eq 'true') { '::error::' } else { 'Error: ' }
$warningPrefix = if ($env:GITHUB_ACTIONS -eq 'true') { '::warning::' } else { 'Warning: ' }
if ($WarnOnly) { $errorPrefix = $warningPrefix }

$failures = 0
foreach ($channel in @($sources.Keys | Sort-Object { [version]$_ })) {
    $source = $sources[$channel] -join ', '
    $release = @($index | Where-Object { $_.'channel-version' -eq $channel }) | Select-Object -First 1
    if (-not $release) {
        "$errorPrefix$("Microsoft's release metadata lists no .NET $channel ($source).")"
        if (-not $WarnOnly) { $failures++ }
    }
    elseif ($release.'release-type' -ne 'lts') {
        "${errorPrefix}.NET $channel ($source) is a Standard Term Support release: target an LTS release and pin its SDK in global.json (ADR-0155)."
        if (-not $WarnOnly) { $failures++ }
    }
    elseif ($release.'support-phase' -eq 'eol') {
        "${errorPrefix}.NET $channel ($source) reached its end of support on $($release.'eol-date'): move to the newest LTS with /plan-migration (ADR-0155)."
        if (-not $WarnOnly) { $failures++ }
    }
    elseif ($release.'support-phase' -eq 'maintenance') {
        "${warningPrefix}.NET $channel ($source) ends its support on $($release.'eol-date'): move to the newest LTS with /plan-migration before then (ADR-0155)."
    }
    elseif ($release.'support-phase' -eq 'active') {
        "$channel ($source): LTS, supported until $($release.'eol-date')"
    }
    else {
        "${errorPrefix}.NET $channel ($source) is a $($release.'support-phase') release, not one Microsoft supports (ADR-0155)."
        if (-not $WarnOnly) { $failures++ }
    }
}

if ($failures -gt 0) {
    ".NET support: FAILED ($failures)"
    exit 1
}
'.NET support: PASSED'
exit 0
