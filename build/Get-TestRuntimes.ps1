<#
.SYNOPSIS
    Names the .NET runtimes the test projects target besides the SDK's own.

.DESCRIPTION
    From the project template; template updates replace this file. Reads global.json for the SDK's version, lists the
    projects of every solution at the repository root, and keeps those named <Name>.Tests. Asks MSBuild for the
    TargetFramework and TargetFrameworks of each, so a value that Directory.Build.props or build/ProjectTemplate.props
    sets counts, and keeps the net and netcoreapp frameworks.

    Prints one channel per line, such as 8.0.x, lowest first, for every such framework other than the SDK's own version;
    actions/setup-dotnet's dotnet-version and build/Install-Dotnet.ps1 take them. Prints nothing when every test
    project targets the SDK's own version. On GitHub, the .github/actions/install-dotnet action, which ci.yml,
    copilot-setup-steps.yml and publish.yml call, installs what it prints with actions/setup-dotnet; on GitLab,
    .gitlab-ci.yml runs build/Install-Dotnet.ps1, which installs it beside the SDK. To run every test locally, run
    build/Install-Dotnet.ps1 too.

.EXAMPLE
    ./build/Get-TestRuntimes.ps1
#>
[CmdletBinding()]
param()
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Push-Location $root
try {
    $env:DOTNET_NOLOGO = '1'
    $globalJson = Join-Path $root 'global.json'
    $sdk = $null
    if (Test-Path -LiteralPath $globalJson) {
        $sdkVersion = (Get-Content -Raw -LiteralPath $globalJson | ConvertFrom-Json).sdk.version
        if ($sdkVersion -match '^(\d+\.\d+)') { $sdk = $Matches[1] }
    }

    $projects = @()
    foreach ($solution in @(Get-ChildItem -LiteralPath $root -File | Where-Object { $_.Extension -in '.sln', '.slnx' })) {
        $listed = @(dotnet sln $solution.FullName list 2>&1)
        if ($LASTEXITCODE) { throw "dotnet sln $($solution.Name) list failed: $($listed -join "`n")" }
        $projects += @($listed | ForEach-Object { "$_".Trim() } | Where-Object { $_.EndsWith('.Tests.csproj') })
    }

    $channels = @()
    foreach ($project in @($projects | Select-Object -Unique)) {
        $properties = @(dotnet msbuild $project -getProperty:TargetFramework -getProperty:TargetFrameworks 2>&1)
        if ($LASTEXITCODE) { throw "dotnet msbuild $project failed: $($properties -join "`n")" }
        $text = $properties -join "`n"
        $json = ConvertFrom-Json $text.Substring($text.IndexOf('{'), $text.LastIndexOf('}') - $text.IndexOf('{') + 1)
        foreach ($framework in "$($json.Properties.TargetFrameworks);$($json.Properties.TargetFramework)" -split ';') {
            if ($framework -match '^net(?:coreapp)?(\d+)\.(\d+)') { $channels += "$($Matches[1]).$($Matches[2])" }
        }
    }

    $channels | Where-Object { $_ -ne $sdk } | Select-Object -Unique | Sort-Object { [version]$_ } | ForEach-Object { "$_.x" }
}
finally { Pop-Location }
