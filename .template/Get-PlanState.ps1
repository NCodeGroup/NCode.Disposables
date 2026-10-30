<#
.SYNOPSIS
    Print where the plan in a folder stands, and the workflow command to run next.

.DESCRIPTION
    Reads the Git repository that contains the folder and prints one "key: value" line per fact, in a fixed order:
    folder, worktree, branch, default, host, plan, issue, refs, release, steps, reviewed, landing, protected, ahead, behind, landed,
    pr, merge, tag, renumber, conflicts, changelog-format, changelog-version, changelog-backfill, published, issue-state, issue-states, unfiled, dirty, plans and next. A fact that does not apply prints
    "-". With an origin remote, the script first fetches it, ignoring a failure, unless -NoFetch is given. landed is
    yes when HEAD is on the default branch, or when any of the branch's merged pull requests or merge requests has its
    merge commit there and its head holds HEAD or has HEAD's tree. pr names that request, or else the newest one.
    merge is the commit that request merged as, when it merged. issue is the plan's Issue: line, which names one
    issue or up to three separated by spaces; issue-states gives each one's state as <reference>=<state>, and
    issue-state is OPEN while any of them is open, CLOSED when all are closed, and unknown otherwise. refs is the plan's Refs: line, the issues it relates to without resolving them; the script reads no state for them.
    landing gives each key of AGENTS.md's Landing section that the workflow reads as key=value, with its default when
    the section leaves it out; a value may be written in backticks, and a value with a space is printed in double quotes.
    gate is the command /ship-plan runs before it pushes, or none.
    pr-format is <file>#<heading>, the section that sets a pull request's title and description, or none.
    renumber is the version the plan moves to when another plan has landed its Release: version, or a later one, first: the newest version tag on origin (the clone's tags when origin's can't be read) or newest release heading of the changelog, ## [<version>] - <date> or ## <version>, on origin's default branch, raised as the plan's version was raised (patch, else minor, else major). It is - when that version is older, release is none, or the plan has landed. next is ship-plan rather than wait while a pull request is open and renumber isn't -, so /ship-plan renumbers the plan.
    tag is present when origin has the plan's Release: version tag, local when only this clone has it, such as one a failed push left, absent when neither has it, and unknown when git ls-remote can't read origin's tags. Without an origin remote, it reads the clone's tags: present or absent.
    conflicts names the files that conflict when origin's default branch merges into HEAD, joined by ", ", from git merge-tree --write-tree, which changes neither the index nor the working tree; none when the merge is clean, unknown when Git can't say, such as Git older than 2.38, and - on the default branch or once the plan has landed. next is ship-plan rather than wait while a pull request is open and conflicts names files, so /ship-plan adds a step that merges the default branch in.
    changelog-format is keep-a-changelog when the changelog has a ## [Unreleased] or ## [<version>] heading, other when it has neither, and missing without the file; changelog-version is its newest release, with a leading v when the plan's Release: version has one, or -. changelog-backfill names the version tags merged into HEAD, shaped like v1.2.3 or 1.2.3, that have no release heading in the changelog, as <oldest>..<newest>, the tag alone when there is one, or - when there is none.
    unfiled counts the lines of PLAN.md's ## Follow-ups section, up to the next ## heading, that start with "- " or "* " and don't end with a mark /ship-plan appends: an em dash, then "filed as #<number>" or "already tracked as #<number>", either with <owner/repo> before the #, or "dropped". A line that says only None or Nothing doesn't count. It is 0 without the section and - without a plan. next is ship-plan rather than finish-plan for a landed plan while unfiled is above 0 and the Landing section's follow-ups isn't none, so /ship-plan files them.
    protected is hook when protect is hook and Git runs .githooks/pre-push, host when GitHub or GitLab requires pull
    requests into the default branch, no when it does not, unknown when the host or its CLI cannot say, and - without
    a default branch.

.PARAMETER Path
    A folder inside the repository. Defaults to the current folder.

.PARAMETER NoFetch
    Skip the fetch of origin, and read ahead, behind and landed from the remote refs the last fetch left. Every
    worktree of a repository shares those refs, so a second call for another worktree needs no fetch of its own.

.EXAMPLE
    .\Get-PlanState.ps1

.EXAMPLE
    .\Get-PlanState.ps1 -Path .claude/worktrees/csv-export -NoFetch
#>
[CmdletBinding()]
param([string]$Path = '.', [switch]$NoFetch)
$ErrorActionPreference = 'Stop'

function Read-Lines([string]$File) {
    ([IO.File]::ReadAllText($File) -split "`r?`n")
}

function Get-LandingValue([string]$Text, [switch]$Spaced) {
    $value = $Text.Trim()
    if ($value -match '^`(.+)`$' -or $value -match '^"(.+)"$') { $value = $Matches[1].Trim() }
    if (-not $Spaced) { $value = ($value -split '\s+')[0] }
    $value
}

function ConvertTo-PlanVersion([string]$Text) {
    if ($Text -match '^v?(\d+)\.(\d+)\.(\d+)$') { return [version]::new([int]$Matches[1], [int]$Matches[2], [int]$Matches[3]) }
    $null
}

# The version of the first release heading in a changelog, as ## [1.2.3] - 2026-01-31 or ## 1.2.3, or $null.
function Get-ChangelogVersion([string[]]$Lines) {
    foreach ($line in $Lines) {
        if ("$line" -match '^## \[?(v?\d+\.\d+\.\d+)\]?(?: - \d{4}-\d{2}-\d{2})?(?: \[YANKED\])?\s*$') { return ConvertTo-PlanVersion $Matches[1] }
    }
    $null
}

# keep-a-changelog when a changelog has a ## [Unreleased] or ## [<version>] heading, else other.
function Get-ChangelogFormat([string[]]$Lines) {
    foreach ($line in $Lines) {
        if ("$line" -match '^## \[(Unreleased|v?\d+\.\d+\.\d+)\]') { return 'keep-a-changelog' }
    }
    'other'
}

# The version tags merged into HEAD that have no release heading in a changelog: <oldest>..<newest>, the tag alone, or -.
function Get-ChangelogBackfill([string[]]$Lines) {
    $listed = @(foreach ($line in $Lines) { if ("$line" -match '^## \[?(v?\d+\.\d+\.\d+)\]?(?: - \d{4}-\d{2}-\d{2})?(?: \[YANKED\])?\s*$') { ConvertTo-PlanVersion $Matches[1] } })
    $missing = @(foreach ($t in @(& git tag --list --merged HEAD 2>$null)) { $v = ConvertTo-PlanVersion "$t"; if ($null -ne $v -and $v -notin $listed) { [pscustomobject]@{ Tag = "$t"; Version = $v } } })
    $missing = @($missing | Sort-Object Version)
    if ($missing.Count -eq 0) { '-' } elseif ($missing.Count -eq 1) { $missing[0].Tag } else { "$($missing[0].Tag)..$($missing[-1].Tag)" }
}

# The number of follow-ups in a plan's ## Follow-ups section that /ship-plan has not filed, tracked or dropped.
function Get-UnfiledFollowUps([string[]]$Lines) {
    $count = 0; $inSection = $false
    foreach ($line in $Lines) {
        if ("$line" -match '^## ') { $inSection = ("$line" -match '^## Follow-ups\s*$'); continue }
        if ($inSection -and "$line" -match '^[-*] \S' -and "$line" -notmatch '^[-*] (?:none|nothing)\.?\s*$' -and "$line" -notmatch '\s\u2014\s(?:(?:filed as|already tracked as)\s+\S*#\d+|dropped)\s*$') { $count++ }
    }
    $count
}

# The tag names origin has, from git ls-remote, or $null when origin can't be read.
function Get-OriginTags {
    $out = @(& git ls-remote --tags --refs origin 2>$null)
    if ($LASTEXITCODE) { return $null }
    , [string[]]@(foreach ($line in $out) { if ("$line" -match '\trefs/tags/(.+)$') { $Matches[1] } })
}

function Get-IssueState([string]$Ref, [string]$HostName) {
    $issueState = 'unknown'
    $numbers = [regex]::Matches($Ref, '\d+')
    if ($HostName -in 'github', 'gitlab' -and $numbers.Count -gt 0) {
        $issueNumber = $numbers[$numbers.Count - 1].Value
        $repoArgs = @()
        if ($Ref -match '^(?<r>[^\s/#:]+/[^\s/#:]+)#\d+') { $repoArgs = @('--repo', $Matches['r']) }
        elseif ($Ref -match '^https?://[^/\s]+/(?<r>.+?)/(?:-/)?issues/\d+') { $repoArgs = @('--repo', $Matches['r']) }
        $cli = if ($HostName -eq 'github') { 'gh' } else { 'glab' }
        if (Get-Command $cli -ErrorAction SilentlyContinue) {
            if ($HostName -eq 'github') {
                $out = (& gh issue view $issueNumber @repoArgs --json state --jq .state 2>$null | Out-String).Trim()
                if ($LASTEXITCODE -eq 0 -and $out) { $issueState = $out.ToUpperInvariant() }
            }
            else {
                $out = & glab issue view $issueNumber @repoArgs --output json 2>$null | Out-String
                if ($LASTEXITCODE -eq 0) {
                    try {
                        $issueState = switch ("$(($out | ConvertFrom-Json -ErrorAction Stop).state)") { 'opened' { 'OPEN' } 'closed' { 'CLOSED' } default { 'unknown' } }
                    }
                    catch { $issueState = 'unknown' }
                }
            }
        }
    }
    $issueState
}

function Test-RequestLanded([string]$Merge, [string]$Head, [string]$Default) {
    & git merge-base --is-ancestor $Merge "origin/$Default" 2>$null | Out-Null
    $mergeOnDefault = ($LASTEXITCODE -eq 0)
    & git merge-base --is-ancestor HEAD $Head 2>$null | Out-Null
    $headInPr = ($LASTEXITCODE -eq 0)
    $prTree = (& git rev-parse -q --verify "${Head}^{tree}" 2>$null | Out-String).Trim()
    $headTreeNow = (& git rev-parse -q --verify 'HEAD^{tree}' 2>$null | Out-String).Trim()
    $mergeOnDefault -and ($headInPr -or ($prTree -and $headTreeNow -and $prTree -eq $headTreeNow))
}

function Get-Protected([string]$HostName, [string]$Default, [string]$Protect) {
    if ($Protect -eq 'hook') {
        $h = (& git rev-parse --git-path hooks 2>$null | Out-String).Trim()
        if ($h -and ((((Split-Path -Leaf $h) -eq '.githooks') -and (Test-Path -LiteralPath "$h/pre-push")) -or [bool]('pre-push', 'pre-push.legacy' | Where-Object { Select-String -LiteralPath "$h/$_" -SimpleMatch '.githooks/pre-push' -Quiet -ErrorAction Ignore }))) { return 'hook' }
    }
    if ($Default -eq '-') { return '-' }
    if ($HostName -notin 'github', 'gitlab') { return 'unknown' }
    $cli = if ($HostName -eq 'github') { 'gh' } else { 'glab' }
    if (-not (Get-Command $cli -ErrorAction SilentlyContinue)) { return 'unknown' }
    if ($HostName -eq 'github') {
        $out = & gh api "repos/{owner}/{repo}/rules/branches/$Default" 2>$null | Out-String
        if ($LASTEXITCODE -eq 0) {
            try {
                if (@($out | ConvertFrom-Json -ErrorAction Stop | Where-Object { $_.type -eq 'pull_request' }).Count -gt 0) { return 'host' }
            }
            catch { }
        }
        & gh api "repos/{owner}/{repo}/branches/$Default/protection/required_pull_request_reviews" 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) { return 'host' }
        return 'no'
    }
    $out = & glab api "projects/:id/protected_branches/$Default" 2>$null | Out-String
    if ($LASTEXITCODE -eq 0) {
        try {
            if (@(($out | ConvertFrom-Json -ErrorAction Stop).push_access_levels | Where-Object { $_.access_level -ne 0 }).Count -eq 0) { return 'host' }
        }
        catch { }
    }
    'no'
}

# The files that conflict when origin/<Default> merges into HEAD, from git merge-tree, which changes no file: their
# paths joined by ', ', none when the merge is clean, or unknown when Git can't say (such as Git older than 2.38).
function Get-Conflicts([string]$Default) {
    $out = @(& git -c core.quotePath=false merge-tree --write-tree --name-only --no-messages "origin/$Default" HEAD 2>$null)
    $code = $LASTEXITCODE
    if ($code -eq 0) { return 'none' }
    if ($code -eq 1 -and $out.Count -gt 1 -and "$($out[0])" -match '^[0-9a-f]{40,64}$') {
        $files = @($out | Select-Object -Skip 1 | ForEach-Object { "$_" } | Where-Object { $_ })
        if ($files.Count) { return ($files -join ', ') }
    }
    'unknown'
}

Push-Location -LiteralPath $Path
try {
    $folder = (& git rev-parse --show-toplevel 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -or -not $folder) { throw "Not a Git repository: $Path" }
    $folder = $folder -replace '\\', '/'

    $worktree = if ($folder -match '/\.claude/worktrees/([^/]+)$') { $Matches[1] } else { '-' }

    $branch = (& git branch --show-current 2>$null | Out-String).Trim()
    if (-not $branch) { $branch = '-' }

    $originUrl = (& git remote get-url origin 2>$null | Out-String).Trim()
    if ($LASTEXITCODE -or -not $originUrl) {
        $hostName = 'none'
    }
    else {
        $hostName = if ($originUrl -match 'gitlab') { 'gitlab' } elseif ($originUrl -match 'github\.com') { 'github' } else { 'other' }
        if (-not $NoFetch) { & git fetch origin --prune --tags --quiet 2>$null | Out-Null }
    }

    if ($hostName -eq 'none') { $default = '-' }
    else {
        $ref = (& git symbolic-ref --short refs/remotes/origin/HEAD 2>$null | Out-String).Trim()
        $default = if ($LASTEXITCODE -or -not $ref) { 'main' } else { $ref -replace '^origin/', '' }
    }

    # PLAN.md
    $plan = '-'; $issue = '-'; $related = '-'; $release = '-'; $steps = '-'; $reviewed = '-'; $unfiled = '-'
    $done = 0; $total = 0; $open = 0; $escalated = @(); $failed = @()
    $planFile = Join-Path $folder 'PLAN.md'
    if (Test-Path -LiteralPath $planFile) {
        $lines = Read-Lines $planFile
        foreach ($line in $lines) { if ($line -match '^# Plan:\s*(.+)$') { $plan = $Matches[1].Trim(); break } }
        foreach ($line in $lines) { if ($line -match '^Issue:\s*(\S.*)$') { $issue = $Matches[1].Trim(); break } }
        foreach ($line in $lines) { if ($line -match '^Refs:\s*(\S.*)$') { $related = $Matches[1].Trim(); break } }
        foreach ($line in $lines) { if ($line -match '^Release:\s*(\S+)') { $release = $Matches[1]; break } }
        $unfiled = Get-UnfiledFollowUps $lines

        $done = 0; $total = 0; $open = 0; $escalated = @(); $failed = @()
        foreach ($line in $lines) {
            if ($line -match '^### \[([ xX!?])\] (\d+)\.') {
                $total++
                switch -CaseSensitive ($Matches[1]) {
                    { $_ -in 'x', 'X' } { $done++ }
                    ' ' { $open++ }
                    '?' { $escalated += $Matches[2] }
                    '!' { $failed += $Matches[2] }
                }
            }
        }
        $escText = if ($escalated) { $escalated -join ',' } else { '-' }
        $failText = if ($failed) { $failed -join ',' } else { '-' }
        $steps = "$done/$total open=$open escalated=$escText failed=$failText"

        foreach ($line in $lines) {
            if ($line -match '^Reviewed:\s*([0-9a-f]{7,40})\s*$') {
                $sha = $Matches[1]
                $reviewTree = (& git rev-parse "${sha}^{tree}" 2>$null | Out-String).Trim()
                $reviewOk = -not $LASTEXITCODE
                $headTree = (& git rev-parse 'HEAD^{tree}' 2>$null | Out-String).Trim()
                $reviewed = if ($reviewOk -and $reviewTree -eq $headTree) { 'current' } else { 'stale' }
                break
            }
        }
    }

    # Landing policy from AGENTS.md
    # Each key the workflow reads, with its default, in the order the landing line prints them.
    $policy = [ordered]@{ integrate = 'pull-request'; release = 'none'; changelog = 'CHANGELOG.md'; draft = 'no'; 'follow-ups' = 'issues'; 'follow-up-label' = 'follow-up'; gate = 'none'; protect = 'none'; 'pr-format' = 'none' }
    $spacedKeys = @('follow-up-label', 'gate', 'pr-format')   # keys whose value keeps its spaces
    $agents = Join-Path $folder 'AGENTS.md'
    if (Test-Path -LiteralPath $agents) {
        $inLanding = $false
        foreach ($line in (Read-Lines $agents)) {
            if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
            if ($inLanding -and $line -match '^## ') { break }
            if ($inLanding -and $line -match '^- ([a-z-]+):\s*(\S.*)$' -and $policy.Contains($Matches[1])) {
                $key = $Matches[1]; $policy[$key] = Get-LandingValue $Matches[2] -Spaced:($key -in $spacedKeys)
            }
        }
    }
    $integrate = $policy['integrate']; $releasePolicy = $policy['release']
    $landing = @(foreach ($key in $policy.Keys) { if ($policy[$key] -match '\s') { "$key=`"$($policy[$key])`"" } else { "$key=$($policy[$key])" } }) -join ' '
    $protected = Get-Protected $hostName $default $policy['protect']

    # Position against the default branch
    $ahead = '-'; $behind = '-'; $landed = '-'
    if ($default -ne '-') {
        $ahead = (& git rev-list --count "origin/$default..HEAD" 2>$null | Out-String).Trim()
        $behind = (& git rev-list --count "HEAD..origin/$default" 2>$null | Out-String).Trim()
        if (-not $ahead) { $ahead = '-' }
        if (-not $behind) { $behind = '-' }
        & git merge-base --is-ancestor HEAD "origin/$default" 2>$null | Out-Null
        $landed = if ($LASTEXITCODE -eq 0) { 'yes' } else { 'no' }
    }

    $tag = '-'
    $originTags = $null
    if ($hostName -ne 'none' -and $release -ne '-') { $originTags = Get-OriginTags }
    if ($release -ne '-') {
        & git rev-parse -q --verify "refs/tags/$release" 2>$null | Out-Null
        $localTag = ($LASTEXITCODE -eq 0)
        if ($hostName -eq 'none') { $tag = if ($localTag) { 'present' } else { 'absent' } }
        elseif ($null -eq $originTags) { $tag = 'unknown' }
        elseif ($originTags -ccontains $release) { $tag = 'present' }
        elseif ($localTag) { $tag = 'local' }
        else { $tag = 'absent' }
    }

    # Pull request or merge request
    $pr = '-'; $prNumber = ''; $prState = ''; $prMerge = ''; $prHead = ''; $prBase = ''
    if ($hostName -in 'github', 'gitlab' -and $branch -ne '-' -and $branch -ne $default) {
        $cli = if ($hostName -eq 'github') { 'gh' } else { 'glab' }
        $pr = 'unknown'
        if (Get-Command $cli -ErrorAction SilentlyContinue) {
            $json = if ($hostName -eq 'github') {
                & gh pr list --head $branch --state all --limit 100 --json number,state,mergeCommit,headRefOid,baseRefName 2>$null | Out-String
            }
            else {
                & glab mr list --source-branch $branch --all --output json 2>$null | Out-String
            }
            if ($LASTEXITCODE -eq 0) {
                try {
                    $items = @($json | ConvertFrom-Json -ErrorAction Stop)
                    $requests = @(foreach ($item in $items) {
                        if ($hostName -eq 'github') {
                            [pscustomobject]@{
                                Number = "$($item.number)"; State = "$($item.state)".ToUpperInvariant()
                                Merge = "$($item.mergeCommit.oid)"; Head = "$($item.headRefOid)"; Base = "$($item.baseRefName)"
                            }
                        }
                        else {
                            $mergeSha = "$($item.merge_commit_sha)"
                            if (-not $mergeSha) { $mergeSha = "$($item.squash_commit_sha)" }
                            [pscustomobject]@{
                                Number = "$($item.iid)"
                                State = switch ("$($item.state)") { 'opened' { 'OPEN' } 'merged' { 'MERGED' } 'closed' { 'CLOSED' } default { "$($item.state)".ToUpperInvariant() } }
                                Merge = $mergeSha; Head = "$($item.sha)"; Base = "$($item.target_branch)"
                            }
                        }
                    })
                    if ($requests.Count -eq 0) { $pr = '-' }
                    else {
                        $chosen = $requests[0]
                        foreach ($request in $requests) {
                            if ($request.State -eq 'MERGED' -and $request.Merge -and $request.Head -and (Test-RequestLanded $request.Merge $request.Head $default)) { $landed = 'yes'; $chosen = $request; break }
                        }
                        $prNumber = $chosen.Number; $prState = $chosen.State; $prMerge = $chosen.Merge; $prHead = $chosen.Head; $prBase = $chosen.Base
                        $pr = "$prNumber $prState"
                    }
                }
                catch { $pr = 'unknown'; $prNumber = ''; $prState = ''; $prMerge = ''; $prHead = ''; $prBase = '' }
            }
        }
    }
    $merge = if ($prState -eq 'MERGED' -and $prMerge) { $prMerge } else { '-' }

    # Changelog in this checkout
    $changelogFormat = 'missing'; $changelogVersion = '-'
    $changelogFile = Join-Path $folder $policy['changelog']
    $changelogLines = @()
    if (Test-Path -LiteralPath $changelogFile -PathType Leaf) {
        $changelogLines = Read-Lines $changelogFile
        $changelogFormat = Get-ChangelogFormat $changelogLines
        $v = Get-ChangelogVersion $changelogLines
        if ($null -ne $v) {
            $prefix = if ("$release".StartsWith('v')) { 'v' } else { '' }
            $changelogVersion = "$prefix$($v.Major).$($v.Minor).$($v.Build)"
        }
    }
    $changelogBackfill = Get-ChangelogBackfill $changelogLines

    # Version still free for the plan
    $renumber = '-'
    $planVersion = ConvertTo-PlanVersion $release
    if ($releasePolicy -ne 'none' -and $default -ne '-' -and $landed -ne 'yes' -and $null -ne $planVersion) {
        $found = @()
        $tagNames = if ($null -ne $originTags) { $originTags } else { @(& git tag --list 2>$null) }
        foreach ($tagName in $tagNames) {
            $v = ConvertTo-PlanVersion "$tagName"
            if ($null -ne $v) { $found += $v }
        }
        $headLines = @(& git show "origin/${default}:$($policy['changelog'])" 2>$null)
        if ($LASTEXITCODE -eq 0) {
            $v = Get-ChangelogVersion $headLines
            if ($null -ne $v) { $found += $v }
        }
        if ($found.Count -gt 0) {
            $newest = $found | Sort-Object | Select-Object -Last 1
            if ($newest -ge $planVersion) {
                $raised = if ($planVersion.Build -gt 0) { [version]::new($newest.Major, $newest.Minor, $newest.Build + 1) }
                elseif ($planVersion.Minor -gt 0) { [version]::new($newest.Major, $newest.Minor + 1, 0) }
                else { [version]::new($newest.Major + 1, 0, 0) }
                $prefix = if ($release.StartsWith('v')) { 'v' } else { '' }
                $renumber = "$prefix$($raised.Major).$($raised.Minor).$($raised.Build)"
            }
        }
    }

    # Files that conflict with the default branch
    $conflicts = '-'
    if ($default -ne '-' -and $branch -ne $default -and $landed -eq 'no') { $conflicts = if ($behind -eq '0') { 'none' } else { Get-Conflicts $default } }

    # Release published on the host
    $published = '-'
    if ($releasePolicy -eq 'tag+release' -and $tag -eq 'present') {
        $published = 'unknown'
        if ($hostName -in 'github', 'gitlab') {
            $cli = if ($hostName -eq 'github') { 'gh' } else { 'glab' }
            if (Get-Command $cli -ErrorAction SilentlyContinue) {
                if ($hostName -eq 'github') { & gh release view $release --json tagName 2>$null | Out-Null }
                else { & glab release view $release 2>$null | Out-Null }
                $published = if ($LASTEXITCODE -eq 0) { 'yes' } else { 'no' }
            }
        }
    }

    # State of the linked issue
    $issueState = '-'; $issueStates = '-'
    if ($issue -ne '-') {
        $refs = @($issue -split '[\s,]+' | Where-Object { $_ })
        $states = @(foreach ($ref in $refs) { Get-IssueState $ref $hostName })
        $issueStates = @(for ($i = 0; $i -lt $refs.Count; $i++) { "$($refs[$i])=$($states[$i])" }) -join ' '
        $issueState = if ($states -contains 'OPEN') { 'OPEN' } elseif (@($states | Where-Object { $_ -ne 'CLOSED' }).Count -eq 0) { 'CLOSED' } else { 'unknown' }
    }

    $status = (& git status --porcelain 2>$null | Out-String).Trim()
    $dirty = if ($status) { 'yes' } else { 'no' }

    # Other plan worktrees
    $others = @()
    foreach ($line in (& git worktree list --porcelain 2>$null)) {
        if ("$line" -match '^worktree (.+)$') {
            $wtPath = $Matches[1] -replace '\\', '/'
            if ($wtPath -match '/\.claude/worktrees/([^/]+)$') {
                $name = $Matches[1]
                $wtPlan = Join-Path $wtPath 'PLAN.md'
                if ($name -ne $worktree -and (Test-Path -LiteralPath $wtPlan)) {
                    $title = '-'
                    foreach ($l in (Read-Lines $wtPlan)) { if ($l -match '^# Plan:\s*(.+)$') { $title = $Matches[1].Trim(); break } }
                    $others += "$name=$title"
                }
            }
        }
    }
    $plans = if ($others) { $others -join '; ' } else { '-' }

    # Next command
    $next = $null
    if ($dirty -eq 'yes') { $next = 'none uncommitted changes: commit or discard them first' }
    elseif ($plan -ne '-') {
        if ($escalated.Count -or $failed.Count) {
            $lowest = ($escalated + $failed | ForEach-Object { [int]$_ } | Sort-Object | Select-Object -First 1)
            $next = "replan-step $lowest"
        }
        elseif ($open -gt 0) { $next = 'orchestrate-plan' }
        elseif ($total -eq 0) { $next = 'none the plan has no steps' }
        elseif ($reviewed -ne 'current') { $next = 'review-plan-diff' }
        elseif ($landed -ne 'yes') {
            if ($hostName -eq 'none') { $next = 'none no origin remote to land on' }
            elseif ($prState -eq 'OPEN' -and ($renumber -ne '-' -or $conflicts -notin '-', 'none', 'unknown')) { $next = 'ship-plan' }
            elseif ($prState -eq 'OPEN') { $next = "wait pull request #$prNumber is open: run ship-plan again once it merges" }
            elseif ($prState -eq 'MERGED' -and $integrate -eq 'direct') { $next = 'ship-plan' }
            elseif ($prState -eq 'MERGED' -and $prBase -and $prBase -ne $default) { $next = "none pull request #$prNumber merged into $prBase, not ${default}: this branch lands once $prBase reaches $default; run ship-plan again then" }
            elseif ($prState -eq 'MERGED') { $next = "none pull request #$prNumber merged without this branch's HEAD: land the commits after it from a new branch" }
            else { $next = 'ship-plan' }
        }
        elseif (($releasePolicy -ne 'none' -and $tag -ne 'present') -or $published -eq 'no' -or $issueState -eq 'OPEN' -or ($policy['follow-ups'] -ne 'none' -and $unfiled -gt 0)) { $next = 'ship-plan' }
        else { $next = 'finish-plan' }
    }
    else {
        if ($branch -ne '-' -and $branch -ne $default -and $landed -eq 'yes') { $next = 'finish-plan' }
        elseif ($plans -ne '-') {
            $names = $others | ForEach-Object { ($_ -split '=', 2)[0] }
            $next = "none no plan in this folder; open plans: $($names -join ', ')"
        }
        else { $next = 'none no plan in this folder' }
    }

    Write-Output "folder: $folder"
    Write-Output "worktree: $worktree"
    Write-Output "branch: $branch"
    Write-Output "default: $default"
    Write-Output "host: $hostName"
    Write-Output "plan: $plan"
    Write-Output "issue: $issue"
    Write-Output "refs: $related"
    Write-Output "release: $release"
    Write-Output "steps: $steps"
    Write-Output "reviewed: $reviewed"
    Write-Output "landing: $landing"
    Write-Output "protected: $protected"
    Write-Output "ahead: $ahead"
    Write-Output "behind: $behind"
    Write-Output "landed: $landed"
    Write-Output "pr: $pr"
    Write-Output "merge: $merge"
    Write-Output "tag: $tag"
    Write-Output "renumber: $renumber"
    Write-Output "conflicts: $conflicts"
    Write-Output "changelog-format: $changelogFormat"
    Write-Output "changelog-version: $changelogVersion"
    Write-Output "changelog-backfill: $changelogBackfill"
    Write-Output "published: $published"
    Write-Output "issue-state: $issueState"
    Write-Output "issue-states: $issueStates"
    Write-Output "unfiled: $unfiled"
    Write-Output "dirty: $dirty"
    Write-Output "plans: $plans"
    Write-Output "next: $next"
}
finally { Pop-Location }
