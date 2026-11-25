# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    Tests build/Assert-SampleKeys.ps1 against fixture repositories, offline.
.DESCRIPTION
    Covers a clean set of keys; lines neither MarkdownSnippets nor the site's build reads as a marker, which define no
    key; lines either tool takes for a marker in another form, which the check reports; markers after tabs and before
    trailing white space, which define a key; files the check must read, outside tests/ and under a name git quotes,
    and files it must not; and each rule it enforces: a key that is not PascalCase letters and digits, a key defined
    twice, also in another letter case, a site sample's key without the Site prefix, any other key with it, and no key
    at all. How each tool reads each fixture's marker lines was tested with MarkdownSnippets 29.0.0 and the site's
    build (ADR-1010). Each failing fixture breaks one rule, and each case checks the exit code and every line the check
    prints. Exits 1 on the first failed check. build/gate.ps1 runs it before the check.
#>
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'Assert-SampleKeys.ps1'
$utf8 = [Text.UTF8Encoding]::new($false)
$tab = "`t"
$readme = 'tests/NCode.Disposables.Tests/ReadmeSamplesTests.cs'
$signatures = 'tests/NCode.Disposables.Tests/ReadmeSamples/Signatures.cs'
$other = 'tests/NCode.Disposables.Tests/OtherTests.cs'
$more = 'tests/NCode.Disposables.Tests/MoreTests.cs'
$quickStart = 'tests/NCode.Disposables.Site.Tests/Start/QuickStartSamples.cs'
$guide = 'tests/NCode.Disposables.Site.Tests/Guides/VerifyAMacSamples.cs'
# A folder whose name only starts with the site project's holds no site samples.
$beside = 'tests/NCode.Disposables.Site.Tests.Extra/ExtraSamples.cs'
# MarkdownSnippets reads every C# file in the repository, not only the tests.
$outside = 'src/NCode.Disposables/Samples.cs'
# A name with a byte above 0x7F, which git quotes unless it is asked not to.
$quoted = 'tests/NCode.Disposables.Tests/Straße.cs'
# A plan's worktree is a copy of the repository, which git ignores.
$worktree = ".claude/worktrees/plan/$readme"

# $Samples maps each fixture file to the key of its one sample, whose indented begin marker is on line 3; $Lines maps a
# file to the lines it holds from line 3 on instead. A $Staged file is added to the index, as a committed one is, and
# the others stay untracked, so the check must read both. A $Deleted file is added to the index and then deleted, as
# one deleted but not committed is. Every fixture repository ignores .claude/worktrees/, as this one does.
function Invoke-Case {
    param(
        [string]$Name,
        [hashtable]$Samples = @{},
        [string[]]$Problems = @(),
        [hashtable]$Lines = @{},
        [string[]]$Staged = @(),
        [string[]]$Deleted = @()
    )
    $root = Join-Path ([IO.Path]::GetTempPath()) "sample-keys-test-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $root | Out-Null
    try {
        git init -q $root
        if ($LASTEXITCODE) { throw 'git init failed' }
        [IO.File]::WriteAllText((Join-Path $root '.gitignore'), ".claude/worktrees/`n", $utf8)
        $third = @{}
        foreach ($path in $Samples.Keys) { $third[$path] = "    // begin-snippet: $($Samples[$path])" }
        foreach ($path in $Lines.Keys) { $third[$path] = @($Lines[$path]) -join "`n" }
        foreach ($path in $third.Keys) {
            $file = Join-Path $root $path
            New-Item -ItemType Directory -Path (Split-Path -Parent $file) -Force | Out-Null
            [IO.File]::WriteAllText($file, "// A fixture.`n`n$($third[$path])`n    _ = 0;`n    // end-snippet`n", $utf8)
        }
        foreach ($path in $Staged + $Deleted) {
            git -C $root add -- $path
            if ($LASTEXITCODE) { throw "git add $path failed" }
        }
        foreach ($path in $Deleted) { Remove-Item -LiteralPath (Join-Path $root $path) }

        # A child pwsh, as build/Test-AssertMarkdown.ps1 runs its check: the check's exit, a throw and its preferences
        # stay out of this session, and its exit code is the process's.
        $printed = @(pwsh -NoProfile -NonInteractive -File $script -Root $root 2>&1 | ForEach-Object { "$_" })

        $code = $LASTEXITCODE
        $expectedCode = if ($Problems) { 1 } else { 0 }
        if ($code -ne $expectedCode) { throw "exited $code instead of $expectedCode" }
        $expected = if ($Problems) { @('Sample keys: FAILED.') + $Problems } else { @() }
        if (($printed -join "`n") -cne ($expected -join "`n")) { throw "printed:`n$($printed -join "`n")" }
    }
    catch { Write-Host "FAILED: $Name`n$_"; exit 1 }
    finally { Remove-Item -LiteralPath $root -Recurse -Force }
    Write-Host "ok: $Name"
}

# A line the check reports, with what it says about it.
function Malformed([string]$Text) { [pscustomobject]@{ Text = $Text; Says = "is a malformed marker: write '// begin-snippet: <Key>' alone on its line" } }
function Region([string]$Text) { [pscustomobject]@{ Text = $Text; Says = 'is a sample to MarkdownSnippets: give the region a name with a space' } }

Invoke-Case 'clean set' @{ $readme = 'ZeroedAfterUse'; $quickStart = 'SiteWatchAKeyDisappear'; $guide = 'SiteVerifyMacWebhook'; $beside = 'Extra' }

# Neither tool reads these as markers. After the line's indentation, the marker's text starts past the eleventh
# character, or follows code in other letter case or without the space after its colon; a region's name has a space,
# or a tab follows the directive.
$neither = @(
    '    string s = "// begin-snippet: ZeroedAfterUse";'
    '    xx = 1; // begin-snippet: ZeroedAfterUse'
    '    x = 1; // Begin-Snippet: ZeroedAfterUse'
    '    x = 1; // begin-snippet:ZeroedAfterUse'
    '    #region Zeroed After Use'
    "    #region${tab}ZeroedAfterUse"
)
Invoke-Case 'lines neither tool reads as a marker' @{ $readme = 'ZeroedAfterUse' } -Lines @{ $signatures = $neither }

# Each tool takes these for a marker, in a form the other reads differently or not at all. MarkdownSnippets reads the
# first five as markers, of which the site's build reports the third and skips the rest, and the last three as samples,
# which the site's build skips: only a space in a region's name stops MarkdownSnippets, not a tab. The site's build
# reports the four between as malformed, and MarkdownSnippets skips them. The check reports each line, and none
# defines a key.
$taken = @(
    Malformed '_ = "// begin-snippet: Zeroed";'
    Malformed 'x = 1; // begin-snippet: Zeroed'
    Malformed '//begin-snippet: Zeroed'
    Malformed '/// begin-snippet: Zeroed'
    Malformed '/* begin-snippet: Zeroed */'
    Malformed '// Begin-Snippet: Zeroed'
    Malformed '//Begin-Snippet: Zeroed'
    Malformed '// begin-snippet:Zeroed'
    Malformed '// begin-snippet:'
    Region '#region Zeroed'
    Region '#region  Zeroed'
    Region "#region Zeroed${tab}Tests"
)
$reported = for ($i = 0; $i -lt $taken.Count; $i++) { "${signatures}:$($i + 3): '$($taken[$i].Text)' $($taken[$i].Says)" }
Invoke-Case 'lines either tool takes for a marker' @{ $readme = 'Zeroed' } -Lines @{ $signatures = $taken.ForEach({ "    $($_.Text)" }) } `
    -Problems $reported

# Both tools read a marker after tabs, and drop the white space after its key.
Invoke-Case 'markers after tabs and before white space' @{ $readme = 'Zeroed' } `
    -Lines @{ $signatures = "$tab$tab// begin-snippet: Zeroed"; $other = "    // begin-snippet: Zeroed$tab"; $more = '    // begin-snippet: Zeroed   ' } `
    -Problems "Zeroed is defined 4 times: ${more}:3, ${other}:3, ${signatures}:3, ${readme}:3"
Invoke-Case 'a file outside tests' @{ $readme = 'ZeroedAfterUse'; $outside = 'ZeroedAfterUse' } `
    -Problems "ZeroedAfterUse is defined 2 times: ${outside}:3, ${readme}:3"
Invoke-Case 'a file whose name git quotes' @{ $readme = 'ZeroedAfterUse'; $quoted = 'ZeroedAfterUse' } `
    -Problems "ZeroedAfterUse is defined 2 times: ${readme}:3, ${quoted}:3"
Invoke-Case 'a file git ignores' @{ $readme = 'ZeroedAfterUse'; $worktree = 'ZeroedAfterUse' }
Invoke-Case 'a file deleted from the work tree' @{ $readme = 'ZeroedAfterUse'; $signatures = 'ZeroedAfterUse' } -Deleted $signatures

Invoke-Case 'key that is not PascalCase' @{ $readme = 'zeroedAfterUse'; $other = 'Zeroed_After'; $more = '9Lives' } `
    -Lines @{ $signatures = '    // begin-snippet: Zeroed After Use' } `
    -Problems "${more}:3: '9Lives' is not PascalCase letters and digits", `
    "${other}:3: 'Zeroed_After' is not PascalCase letters and digits", `
    "${signatures}:3: 'Zeroed After Use' is not PascalCase letters and digits", `
    "${readme}:3: 'zeroedAfterUse' is not PascalCase letters and digits"
# git lists untracked files first, then the ones in the index.
Invoke-Case 'key defined twice' @{ $signatures = 'Zeroed'; $readme = 'Zeroed'; $quickStart = 'SiteZeroed' } -Staged $readme `
    -Problems "Zeroed is defined 2 times: ${signatures}:3, ${readme}:3"
Invoke-Case 'keys that differ only in case' @{ $signatures = 'ZeroedAfterUse'; $readme = 'ZeroedAfteruse' } `
    -Problems "ZeroedAfterUse is defined 2 times: ${signatures}:3, ${readme}:3"
# The prefix is Site in that letter case, as the tools match keys.
Invoke-Case 'site key without Site' @{ $readme = 'Zeroed'; $quickStart = 'ZeroedAfterUse'; $guide = 'SITEVerifyMacWebhook' } `
    -Problems "${guide}:3: SITEVerifyMacWebhook is a site sample, so its key starts with Site", `
    "${quickStart}:3: ZeroedAfterUse is a site sample, so its key starts with Site"
Invoke-Case 'other key with Site' @{ $readme = 'SiteZeroed'; $signatures = 'SITEMap'; $quickStart = 'SiteZeroedAfterUse'; $beside = 'SiteExtra' } `
    -Problems "${beside}:3: SiteExtra starts with Site, as only a site sample's key does", `
    "${readme}:3: SiteZeroed starts with Site, as only a site sample's key does"
Invoke-Case 'no key at all' -Lines @{ $readme = '    _ = 1;' } -Problems 'No C# file git lists defines a sample.'
# Every case passed. The last one left the check's exit code, 1, in $LASTEXITCODE, which build/gate.ps1 reads.
exit 0
