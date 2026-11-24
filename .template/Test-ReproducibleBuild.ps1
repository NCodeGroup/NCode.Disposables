<#
.SYNOPSIS
    Packs the project twice from clean build folders and compares the packages entry by entry, the template's ADR-0154.

.DESCRIPTION
    From the project template; template updates replace this file. Proves a commit's packages depend on nothing the
    clock or the machine supplies. It deletes every bin and obj folder under the root, packs, deletes them again and
    packs a second time, then compares each package of the first build with the same-named package of the second:
    the same entry names and the same SHA-256 of each entry's contents. Zip timestamps are never compared.

    It prints one line per difference and exits 1, prints how many packages match and exits 0, or prints
    'No packages to compare.' and exits 0 when nothing was packed.

.PARAMETER Root
    The repository root to build. Defaults to the folder above this script's.

.PARAMETER Configuration
    The build configuration. Defaults to Release.

.EXAMPLE
    ./.template/Test-ReproducibleBuild.ps1
#>
#Requires -Version 7.4
[CmdletBinding()]
param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),

    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

$root = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Root)
$output = Join-Path $root 'artifacts/reproducible'

function Get-EntryHashes([string]$Path) {
    $hashes = @{}
    $zip = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        foreach ($entry in $zip.Entries) {
            $stream = $entry.Open()
            try {
                $hashes[$entry.FullName] = [BitConverter]::ToString([Security.Cryptography.SHA256]::HashData($stream))
            }
            finally {
                $stream.Dispose()
            }
        }
    }
    finally {
        $zip.Dispose()
    }
    $hashes
}

if (Test-Path -LiteralPath $output) {
    Remove-Item -LiteralPath $output -Recurse -Force
}

foreach ($pass in 'first', 'second') {
    $skipped = [IO.Path]::DirectorySeparatorChar + 'artifacts' + [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::DirectorySeparatorChar + 'node_modules' + [IO.Path]::DirectorySeparatorChar,
        [IO.Path]::DirectorySeparatorChar + '.git' + [IO.Path]::DirectorySeparatorChar
    Get-ChildItem -LiteralPath $root -Directory -Recurse -Force |
        Where-Object { $_.Name -in 'bin', 'obj' } |
        Where-Object {
            $full = $_.FullName.Substring($root.TrimEnd('\', '/').Length) + [IO.Path]::DirectorySeparatorChar
            -not ($skipped | Where-Object { $full.Contains($_) })
        } |
        ForEach-Object { if (Test-Path -LiteralPath $_.FullName) { Remove-Item -LiteralPath $_.FullName -Recurse -Force } }

    Push-Location -LiteralPath $root
    try {
        & dotnet pack --configuration $Configuration -p:PublicRelease=true -p:ContinuousIntegrationBuild=true --nologo --output (Join-Path $output $pass)
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet pack failed in the $pass build (exit code $LASTEXITCODE)."
        }
    }
    finally {
        Pop-Location
    }
}

$firstDir = Join-Path $output 'first'
$secondDir = Join-Path $output 'second'
$packages = @()
if (Test-Path -LiteralPath $firstDir) {
    $packages = @(Get-ChildItem -LiteralPath $firstDir -File | Where-Object { $_.Extension -in '.nupkg', '.snupkg' })
}

if ($packages.Count -eq 0) {
    'No packages to compare.'
    exit 0
}

$failed = $false
foreach ($package in $packages) {
    $other = Join-Path $secondDir $package.Name
    if (-not (Test-Path -LiteralPath $other)) {
        "Not reproducible: $($package.Name): the package is missing from the second build"
        $failed = $true
        continue
    }
    $a = Get-EntryHashes $package.FullName
    $b = Get-EntryHashes $other
    foreach ($name in $a.Keys | Sort-Object) {
        if (-not $b.ContainsKey($name)) {
            "Not reproducible: $($package.Name): $name is missing from the second build"
            $failed = $true
        }
        elseif ($a[$name] -ne $b[$name]) {
            "Not reproducible: $($package.Name): $name differs"
            $failed = $true
        }
    }
    foreach ($name in $b.Keys | Sort-Object) {
        if (-not $a.ContainsKey($name)) {
            "Not reproducible: $($package.Name): $name is missing from the first build"
            $failed = $true
        }
    }
}

if ($failed) {
    exit 1
}
"Reproducible: $($packages.Count) packages match entry by entry."
exit 0
