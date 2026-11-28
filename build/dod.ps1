# Copyright (c) 2026 Marcin Polewski
# SPDX-License-Identifier: MIT

<#
.SYNOPSIS
    The Definition of Done: the one gate a change passes before it counts as done, run the same way by people, agents
    and CI.

.DESCRIPTION
    From the project template; template updates replace this file. Runs these stages from the repository root and stops
    at the first that fails, with its name; prints "Definition of Done: PASSED" as its last line, and exits 0, when every
    stage passes. A script that runs it tests the exit code rather than that line, which build output can follow in a
    merged stream.

    1. Tools: dotnet tool restore, when .config/dotnet-tools.json exists.
    2. Format: dotnet csharpier check ., when .config/dotnet-tools.json names CSharpier; fails with the command that fixes it.
    3. Decisions: .template/Test-AdrIndex.ps1 checks that docs/decisions/README.md indexes every decision record.
    4. Build: restore and build the solution at the repository root, in Release, with warnings as errors, the analyzers
       and the banned APIs that build/ProjectTemplate.props sets up, and for a library its public API checked. NuGet
       vulnerability advisories are errors here, as in CI.
    5. Test: every test project, without building again, writing TRX results to artifacts/test-results; -Filter
       narrows the run, and a filter that matches no test fails it.
    6. Coverage: with the coverage_gate answer, the Test stage collects coverage, and .template/Test-Coverage.ps1 fails
       below 90% of the statements or 80% of the branches; skipped with -Filter and while no *.Tests project exists.
    7. Pack: for a library (the nuget_package answer), every packable project into artifacts/packages, with its symbols.

    -Fast is the local gate: Debug, no pack, no test results, and advisories as warnings; the Format stage runs there too. CI runs the full gate.

.PARAMETER Fast
    Build in Debug and skip the pack stage.

.PARAMETER Filter
    A dotnet test filter expression, such as FullyQualifiedName~OrderStore, to run only the tests it matches. A filter
    that matches no test fails the stage instead of passing with nothing run.

.EXAMPLE
    ./build/dod.ps1

.EXAMPLE
    ./build/dod.ps1 -Fast -Filter 'FullyQualifiedName~OrderStore'
#>
[CmdletBinding()]
param(
    [switch]$Fast,
    [string]$Filter
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $configuration = if ($Fast) { 'Debug' } else { 'Release' }
    $answers = if (Test-Path -LiteralPath .copier-answers.yml) { [IO.File]::ReadAllText((Join-Path $root '.copier-answers.yml')) } else { '' }
    $library = $answers -match '(?m)^nuget_package:\s*true\s*$'
    $coverage = $answers -match '(?m)^coverage_gate:\s*true\s*$' -and -not $Filter

    function Invoke-Stage([string]$Name, [scriptblock]$Command) {
        "== $Name"
        & $Command
        if ($LASTEXITCODE) {
            ''
            "Definition of Done: FAILED at $Name"
            exit 1
        }
    }

    if (Test-Path -LiteralPath .config/dotnet-tools.json) { Invoke-Stage 'Tools' { dotnet tool restore } }
    if ((Test-Path -LiteralPath .config/dotnet-tools.json) -and (Get-Content -Raw -LiteralPath .config/dotnet-tools.json) -match '(?i)"csharpier"') { Invoke-Stage 'Format' { dotnet csharpier check . ; if ($LASTEXITCODE) { 'Run: dotnet csharpier format .' } } }
    Invoke-Stage 'Decisions' { pwsh -NoProfile -File .template/Test-AdrIndex.ps1 }
    if (-not (Get-ChildItem -LiteralPath $root -File | Where-Object Extension -In '.sln', '.slnx')) {
        "No solution at the repository root yet, so nothing to build: create it with dotnet new sln when adding the first project."
        ''
        'Definition of Done: PASSED'
        exit 0
    }
    # The full gate fails on a NuGet vulnerability advisory, as CI does (build/ProjectTemplate.props).
    $buildArgs = @('--configuration', $configuration, '--nologo')
    if (-not $Fast) { $buildArgs += '-p:FailOnNuGetAdvisories=true' }
    Invoke-Stage 'Build' { dotnet build @buildArgs }
    $testArgs = @('--configuration', $configuration, '--no-build', '--nologo')
    if ($Filter) { $testArgs += '--filter', $Filter }
    $hasTests = [bool](Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.Tests.csproj' | Where-Object { [IO.Path]::GetRelativePath($root, $_.FullName) -notmatch '(^|[\\/])(bin|obj|artifacts|\.claude)[\\/]' } | Select-Object -First 1)
    $coverage = $coverage -and $hasTests
    if (-not $Fast) { $testArgs += '--logger', 'trx' }
    if ($coverage -or -not $Fast) { $testArgs += '--results-directory', 'artifacts/test-results' }
    if ($coverage) {
        # Microsoft.CodeCoverage, which Microsoft.NET.Test.Sdk brings, writes one Cobertura report per test project; an earlier run's would count again.
        $testArgs += '--collect', 'Code Coverage;Format=cobertura'
        Get-ChildItem -LiteralPath artifacts/test-results -Recurse -File -Filter '*.cobertura.xml' -ErrorAction SilentlyContinue | Remove-Item -Force
    }
    Invoke-Stage 'Test' {
        dotnet test @testArgs 2>&1 | Tee-Object -Variable testOutput
        # A filter that matches no test exits 0, which would pass the stage before its test exists. Each test project
        # reports its own total, and a filter usually matches tests in only one of them, so count across the run.
        if (-not $LASTEXITCODE -and $Filter -and -not ($testOutput | Select-String -Pattern 'Total:\s*[1-9]' -Quiet)) {
            "The filter '$Filter' matches no test."
            $global:LASTEXITCODE = 1
        }
    }
    if ($coverage) { Invoke-Stage 'Coverage' { pwsh -NoProfile -File .template/Test-Coverage.ps1 -Report artifacts/test-results } }
    if ($library -and -not $Fast) { Invoke-Stage 'Pack' { dotnet pack --configuration $configuration --no-build --nologo --output artifacts/packages } }
    ''
    'Definition of Done: PASSED'
    exit 0
}
finally { Pop-Location }
