# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    Checks the markers and keys of the C# samples: both tools read each marker alike, each key is PascalCase letters
    and digits and is defined once, and only a site sample's key starts with Site (ADR-1010).
.DESCRIPTION
    The READMEs and the site's pages take their samples by key from every C# file, so a key is PascalCase letters and
    digits and is defined once, keys that differ only in letter case counting as one, and only a site sample's key, in
    tests/NCode.Disposables.Site.Tests, starts with Site.

    The two tools read different lines as markers. The site's build takes a key only from a line that holds the begin
    marker, '// begin-snippet: ', and the key after its indentation, and it reports any other line that starts with
    // and begin-snippet in some letter case. MarkdownSnippets, which copies the README samples, reads a marker in any
    line that holds 'begin-snippet: ', in lower case, within the first 25 characters after its indentation, after code
    or another comment's opening too, and reads a #region whose name has no space as a sample. So a key is the rest of
    the first kind of line, without the white space after it, and every other line either tool takes for a marker is
    a problem, whatever key it names.

    Reads every C# file git lists, committed or not, and none it ignores, such as a plan's worktree. Prints nothing when
    every marker and key holds; otherwise prints 'Sample keys: FAILED.' and each problem, and exits 1. Finding no key
    at all is a problem too: the READMEs always have samples, so a broken listing or pattern fails instead of passing.
    build/gate.ps1 runs it.
.PARAMETER Root
    The repository root; build/Test-AssertSampleKeys.ps1 points it at a fixture repository.
#>
[CmdletBinding()]
param([string]$Root = (Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath $Root
try {
    $siteSamples = 'tests/NCode.Disposables.Site.Tests/'
    $marker = '// begin-snippet: '
    $write = "write '$marker<Key>' alone on its line"
    # -z prints each path as it is: without it, git quotes a name that holds a byte above 0x7F, and the quoted name
    # matches no file.
    $listed = (git ls-files -z --cached --others --exclude-standard -- '*.cs') -join "`n"
    if ($LASTEXITCODE) { "Sample keys: FAILED. git ls-files failed with exit code $LASTEXITCODE."; exit 1 }
    $sources = @($listed.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries) | Where-Object { Test-Path -LiteralPath $_ })
    $keys = [Collections.Generic.List[object]]::new()
    $markers = [Collections.Generic.List[string]]::new()
    foreach ($source in $sources) {
        $number = 0
        foreach ($line in [IO.File]::ReadLines([IO.Path]::Combine($PWD.Path, $source))) {
            $number++
            $text = $line.Trim()
            $at = "${source}:$number"
            if ($text.StartsWith($marker, [StringComparison]::Ordinal)) {
                $keys.Add([pscustomobject]@{ Key = $text.Substring($marker.Length); At = $at })
            }
            # MarkdownSnippets reads the first pattern, the site's build reports the second.
            elseif ($text -cmatch '^.{0,10}begin-snippet: ' -or $text -match '^//\s*begin-snippet') {
                $markers.Add("${at}: '$text' is a malformed marker: $write")
            }
            elseif ($text.StartsWith('#region ', [StringComparison]::Ordinal) -and -not $text.Substring(8).Trim().Contains(' ')) {
                $markers.Add("${at}: '$text' is a sample to MarkdownSnippets: give the region a name with a space")
            }
        }
    }
    $problems = @(
        $markers
        if (-not $keys.Count) { 'No C# file git lists defines a sample.' }
        $keys | Where-Object { $_.Key -cnotmatch '^[A-Z][A-Za-z0-9]*\z' } | ForEach-Object { "$($_.At): '$($_.Key)' is not PascalCase letters and digits" }
        $keys | Group-Object Key | Where-Object Count -GT 1 | ForEach-Object { "$($_.Name) is defined $($_.Count) times: $($_.Group.At -join ', ')" }
        $keys | Where-Object { $_.At.StartsWith($siteSamples, [StringComparison]::Ordinal) -and $_.Key -cnotlike 'Site*' } | ForEach-Object { "$($_.At): $($_.Key) is a site sample, so its key starts with Site" }
        $keys | Where-Object { -not $_.At.StartsWith($siteSamples, [StringComparison]::Ordinal) -and $_.Key -clike 'Site*' } | ForEach-Object { "$($_.At): $($_.Key) starts with Site, as only a site sample's key does" }
    )
    if ($problems) { 'Sample keys: FAILED.'; $problems; exit 1 }
}
finally { Pop-Location }
