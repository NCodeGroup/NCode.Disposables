# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    Tests build/Assert-ApiMap.ps1 against fixture folders, offline.
.DESCRIPTION
    Covers a map that lists every shipped line once, whatever the file's comment and empty lines; a shipped line no
    entry lists; a line an entry lists that is not shipped; a line two entries list; no map file; and a file that
    holds no shipped line. Each fixture is a folder under the temp path, not a repository, that holds
    PublicAPI.Shipped.txt and the map. Each case checks the exit code and every line the check prints. Exits 1 on the
    first failed check. build/gate.ps1 runs it before the check.
#>
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'Assert-ApiMap.ps1'
$utf8 = [Text.UTF8Encoding]::new($false)
$shippedPath = 'src/NCode.Disposables/PublicAPI.Shipped.txt'
$mapPath = 'docs/site/_components/data/api-map.json'
$type = 'NCode.Disposables.DisposableFlag'
$member = 'NCode.Disposables.DisposableFlag.IsDisposed.get -> bool'

# An entry of the map, which lists the lines it covers.
function Entry([string]$Id, [string[]]$Api) { @{ id = $Id; api = @($Api) } }

# $Entries is the map's entries, or $null for no map file. The shipped file holds a comment, an empty line and two
# lines, the second with trailing white space.
function Invoke-Case {
    param(
        [string]$Name,
        [hashtable[]]$Entries,
        [string[]]$Problems = @(),
        [switch]$NoShippedLines
    )
    $root = Join-Path ([IO.Path]::GetTempPath()) "api-map-test-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $root | Out-Null
    try {
        $shipped = Join-Path $root $shippedPath
        New-Item -ItemType Directory -Path (Split-Path -Parent $shipped) -Force | Out-Null
        [IO.File]::WriteAllText($shipped, $(if ($NoShippedLines) { "#nullable enable`n" } else { "#nullable enable`n`n$type`n$member  `n" }), $utf8)
        if ($null -ne $Entries) {
            $map = Join-Path $root $mapPath
            New-Item -ItemType Directory -Path (Split-Path -Parent $map) -Force | Out-Null
            $json = @{ groups = @(); entries = @($Entries) } | ConvertTo-Json -Depth 5
            [IO.File]::WriteAllText($map, "$json`n", $utf8)
        }

        # A child pwsh, as build/Test-AssertMarkdown.ps1 runs its check: the check's exit, a throw and its preferences
        # stay out of this session, and its exit code is the process's.
        $printed = @(pwsh -NoProfile -NonInteractive -File $script -Root $root 2>&1 | ForEach-Object { "$_" })

        $code = $LASTEXITCODE
        $expectedCode = if ($Problems) { 1 } else { 0 }
        if ($code -ne $expectedCode) { throw "exited $code instead of $expectedCode" }
        $expected = if ($Problems) { @('API map: FAILED.') + $Problems } else { @() }
        if (($printed -join "`n") -cne ($expected -join "`n")) { throw "printed:`n$($printed -join "`n")" }
    }
    catch { Write-Host "FAILED: $Name`n$_"; exit 1 }
    finally { Remove-Item -LiteralPath $root -Recurse -Force }
    Write-Host "ok: $Name"
}

Invoke-Case 'clean map' -Entries (Entry 'disposable-flag' @($type)), (Entry 'disposable-flag-isdisposed' @($member))
Invoke-Case 'a shipped line no entry lists' -Entries (Entry 'disposable-flag' @($type)) `
    -Problems "API map: no entry for '$member'"
Invoke-Case 'a line that is not shipped' -Entries (Entry 'disposable-flag' @($type)), (Entry 'disposable-flag-isdisposed' @($member, 'NCode.Disposables.DisposableFlag.Gone() -> void')) `
    -Problems "API map: 'NCode.Disposables.DisposableFlag.Gone() -> void' (entry disposable-flag-isdisposed) is not in PublicAPI.Shipped.txt"
Invoke-Case 'a line two entries list' -Entries (Entry 'disposable-flag' @($type, $member)), (Entry 'disposable-flag-isdisposed' @($member)) `
    -Problems "API map: '$member' is listed 2 times: disposable-flag, disposable-flag-isdisposed"
Invoke-Case 'no map file' -Entries $null -Problems "API map: $mapPath does not exist"
Invoke-Case 'no shipped line' -Entries @() -NoShippedLines -Problems "API map: $shippedPath holds no shipped line"
# Every case passed. The last one left the check's exit code, 1, in $LASTEXITCODE, which a caller may read.
exit 0
