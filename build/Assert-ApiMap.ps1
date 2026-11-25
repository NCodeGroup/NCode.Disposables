# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    Checks the API map against PublicAPI.Shipped.txt: every shipped line is in exactly one entry of the map, and every
    line the map lists is shipped.
.DESCRIPTION
    The API map on /reference/api-map/ is the data in docs/site/_components/data/api-map.json, which ApiMap.astro
    renders. Each of its entries lists in `api` the lines of src/NCode.Disposables/PublicAPI.Shipped.txt it covers.
    The shipped lines are those the file holds, without their trailing white space, leaving out empty lines and lines
    that start with #. Lines are compared as ordinal strings.

    Reports, in this order: the map file is missing, which stops the check; each shipped line no entry lists, in file
    order; each line an entry lists that is not shipped, in entry order; each line listed more than once, in the order
    it is first listed, with the id of each entry that lists it. Prints nothing when none holds; otherwise prints
    'API map: FAILED.' and each problem, and exits 1. build/gate.ps1 runs it, after build/Test-AssertApiMap.ps1, which
    tests it.
.PARAMETER Root
    The repository root; build/Test-AssertApiMap.ps1 points it at a fixture folder.
#>
[CmdletBinding()]
param([string]$Root = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $Root
try {
    $shippedPaths = @('src/NCode.Disposables/PublicAPI.Shipped.txt')
    $mapPath = 'docs/site/_components/data/api-map.json'
    if (-not (Test-Path -LiteralPath $mapPath -PathType Leaf)) {
        'API map: FAILED.'
        "API map: $mapPath does not exist"
        exit 1
    }
    $shipped = [Collections.Generic.List[string]]::new()
    foreach ($shippedPath in $shippedPaths) {
        foreach ($line in [IO.File]::ReadLines([IO.Path]::Combine($PWD.Path, $shippedPath))) {
            $text = $line.TrimEnd()
            if ($text.Length -and -not $text.StartsWith('#', [StringComparison]::Ordinal)) { $shipped.Add($text) }
        }
    }
    # A misread file yields no lines, and a map checked against nothing would pass.
    if ($shipped.Count -eq 0) {
        'API map: FAILED.'
        "API map: $($shippedPaths[0]) holds no shipped line"
        exit 1
    }
    $map = Get-Content -LiteralPath $mapPath -Raw | ConvertFrom-Json -AsHashtable
    # The map's lines in entry order, and the ids of the entries that list each one, in the order it is first listed.
    $listings = [Collections.Generic.List[object]]::new()
    $ids = [Collections.Generic.Dictionary[string, [Collections.Generic.List[string]]]]::new([StringComparer]::Ordinal)
    $first = [Collections.Generic.List[string]]::new()
    foreach ($entry in $map['entries']) {
        if (-not $entry.ContainsKey('api')) { continue }
        foreach ($line in $entry['api']) {
            $listings.Add([pscustomobject]@{ Line = $line; Id = $entry['id'] })
            if (-not $ids.ContainsKey($line)) {
                $ids[$line] = [Collections.Generic.List[string]]::new()
                $first.Add($line)
            }
            $ids[$line].Add($entry['id'])
        }
    }
    $shippedSet = [Collections.Generic.HashSet[string]]::new($shipped, [StringComparer]::Ordinal)
    $problems = @(
        $shipped | Where-Object { -not $ids.ContainsKey($_) } | ForEach-Object { "API map: no entry for '$_'" }
        $listings | Where-Object { -not $shippedSet.Contains($_.Line) } | ForEach-Object { "API map: '$($_.Line)' (entry $($_.Id)) is not in PublicAPI.Shipped.txt" }
        $first | Where-Object { $ids[$_].Count -gt 1 } | ForEach-Object { "API map: '$_' is listed $($ids[$_].Count) times: $($ids[$_] -join ', ')" }
    )
    if ($problems) { 'API map: FAILED.'; $problems; exit 1 }
}
finally { Pop-Location }
