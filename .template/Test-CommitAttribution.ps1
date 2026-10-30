<#
.SYNOPSIS
    Fail when a commit in a range is attributed to an AI agent.

.DESCRIPTION
    Inspects every commit in Base..Head (merge commits included) and fails when the author or the committer is an AI
    agent account, or when the commit message carries agent attribution.

    An AI agent account has a name that is copilot-swe-agent (with or without the [bot] suffix), Copilot or
    GitHub Copilot, or an email that ends with +Copilot@users.noreply.github.com or @anthropic.com. Agent attribution
    is a Co-authored-by line that names such an account, a Claude-Session line, or a line that starts with
    "Generated with [Claude Code", after any leading symbols. Matching is case-insensitive. Dependabot and the
    GitHub <noreply@github.com> committer are not AI agents and pass. Git's output is read as UTF-8 whatever the console's code page.

    When Base is empty, is all zeros, or is not a commit in the clone (a new branch, or a force-push), only Head is
    checked, so the check never fails for a reason other than attribution.

    On failure each finding is written as "<short sha> <subject>: <finding>", followed by the remedy: re-author the
    commits with git rebase <base> --exec "git commit --amend --no-edit --reset-author", then git push
    --force-with-lease. When GITHUB_ACTIONS is true each finding is also written as a workflow error annotation.

    This file comes from the project template. Template updates replace it, so a local edit is lost.

.PARAMETER Base
    The revision that excludes the commits already accepted. Commits in Base..Head are checked. May be empty.

.PARAMETER Head
    The revision whose history is checked. Defaults to HEAD.

.PARAMETER Root
    The project root. Defaults to the parent of the .template directory.

.EXAMPLE
    .\.template\Test-CommitAttribution.ps1 -Base origin/main

.EXAMPLE
    .\.template\Test-CommitAttribution.ps1 -Base origin/main~20 -Head origin/main
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [AllowEmptyString()]
    [string]$Base,
    [string]$Head = 'HEAD',
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = [IO.Path]::GetFullPath($Root)
$outputEncoding = [Console]::OutputEncoding
$accountName = '^(copilot-swe-agent(\[bot\])?|(GitHub )?Copilot)$'
$accountEmail = '(\+Copilot@users\.noreply\.github\.com|@anthropic\.com)$'
$attributionLines = @(
    '^\s*co-authored-by:.*(copilot|@anthropic\.com)',
    '^\s*claude-session:',
    '^\W*generated with \[?claude code'
)

function Test-AgentAccount([string]$Name, [string]$Email) {
    return ($Name -match $accountName) -or ($Email -match $accountEmail)
}

try {
    # Read Git's output as UTF-8, so the emoji before "Generated with" stays a symbol whatever the console's code page.
    try { [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false) } catch { }

    $headCommit = & git -C $Root rev-parse --verify "$Head^{commit}" 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "Head '$Head' is not a commit in $Root."
    }

    $baseEmpty = [string]::IsNullOrWhiteSpace($Base)
    $baseKnown = $false
    if (-not $baseEmpty -and $Base -notmatch '^0+$') {
        $null = & git -C $Root rev-parse --verify --quiet "$Base^{commit}" 2>$null
        $baseKnown = $LASTEXITCODE -eq 0
    }
    $remedyBase = if ($baseKnown) { $Base } else { '<base>' }

    if ($baseKnown) {
        $commits = [string[]]@(& git -C $Root rev-list "$Base..$Head")
        if ($LASTEXITCODE -ne 0) {
            throw "git rev-list $Base..$Head failed."
        }
    }
    else {
        if ($baseEmpty) {
            Write-Output "Commit attribution: no base given; checking $Head only."
        }
        else {
            Write-Output "Commit attribution: base $Base is not in this clone; checking $Head only."
        }
        $commits = [string[]]@($headCommit)
    }

    $findings = [System.Collections.Generic.List[string]]::new()
    foreach ($commit in $commits) {
        $fields = (@(& git -C $Root log -1 '--format=%h%x00%s%x00%an%x00%ae%x00%cn%x00%ce' $commit) -join "`n") -split "`0"
        if ($LASTEXITCODE -ne 0 -or $fields.Count -ne 6) {
            throw "git log failed for commit $commit."
        }
        $message = [string[]]@(& git -C $Root log -1 '--format=%B' $commit)
        if ($LASTEXITCODE -ne 0) {
            throw "git log failed for commit $commit."
        }

        $prefix = "$($fields[0]) $($fields[1])"
        if (Test-AgentAccount $fields[2] $fields[3]) {
            $findings.Add("${prefix}: author $($fields[2]) $($fields[3]) is an AI agent account")
        }
        if (Test-AgentAccount $fields[4] $fields[5]) {
            $findings.Add("${prefix}: committer $($fields[4]) $($fields[5]) is an AI agent account")
        }
        foreach ($line in $message) {
            foreach ($pattern in $attributionLines) {
                if ($line -match $pattern) {
                    $findings.Add("${prefix}: message has agent attribution: $($line.Trim())")
                    break
                }
            }
        }
    }

    if ($findings.Count -eq 0) {
        Write-Output "Commit attribution: $($commits.Count) commit(s) checked; none is attributed to an AI agent."
        exit 0
    }

    foreach ($finding in $findings) {
        Write-Output $finding
    }
    Write-Output "Re-author these commits before merging: git rebase $remedyBase --exec ""git commit --amend --no-edit --reset-author"", then git push --force-with-lease."
    if ($env:GITHUB_ACTIONS -eq 'true') {
        foreach ($finding in $findings) {
            Write-Output "::error title=Commit attribution::$finding"
        }
    }
    exit 1
}
finally {
    try { [Console]::OutputEncoding = $outputEncoding } catch { }
}
