# Copyright (c) 2026 Marcin Polewski
# SPDX-License-Identifier: MIT

<#
.SYNOPSIS
    Tag and publish the release the changelog's newest section names, once the remote has no tag for it.

.DESCRIPTION
    .github/workflows/tag-release.yml, the Tag and publish release workflow, runs this on each push to main when
    AGENTS.md's Landing section says release: tag or tag+release, so a plan's release is tagged and published as soon as
    its pull request merges, with nothing left for a person or an agent to run. That includes a plan landed from a
    Claude Code cloud session, whose GitHub proxy refuses a tag push and GitHub's GraphQL API, which `gh release create`
    uses. It reads and publishes the release through GitHub's REST API, so it runs in such a session too.

    It reads the release, changelog and pre-release lines of the ## Landing section of AGENTS.md, as Get-PlanState.ps1
    does. release: none publishes nothing, tag pushes the tag alone, and tag+release also publishes the GitHub release.
    The version is the changelog's newest release heading, `## [<number>] - <date>`, with a v before the number, as
    /ship-plan names it, unless the remote's version tags have no v. Its section, without the heading, is the release's
    notes: up to the next `## ` heading or the link reference definitions that end a Keep a Changelog file. When the
    merged pull request's description also has a `Release:` line, which /ship-plan writes, that names another version,
    it fails. When the commit has a version.json whose version isn't the release's, or that Git can't read, it fails
    without tagging. With pre-release: untracked and no release heading, the version is the pull request's `Release:`
    line and the notes are the `## [Unreleased]` section. With no such line, or with pre-release: tracked and no
    release heading, it publishes nothing and leaves the release to /ship-plan. The merged pull request is the one
    GitHub's commits/<sha>/pulls endpoint returns whose merge_commit_sha is -Commit. A pull request without a Release:
    line, such as a plan's that changes nothing a release ships, tags and publishes nothing once the newest section's
    release is done, its tag on the remote and its GitHub release published, and its changelog entries wait under
    ## [Unreleased] for the next release. When that earlier release's tag or GitHub release is still missing, such as
    after its own run failed, a later merge's run tags or publishes it, as any run does.

    It tags -Commit, the commit the pull request merged as, with an annotated tag, and only when the version is newer
    than every version tag the remote has, so an edit to an older section tags nothing. It's safe to run again: a tag the
    remote has is never moved and a release GitHub has is never changed, so /ship-plan run after the merge finds both
    done, and a run that stopped after the tag publishes the release the next time. -WhatIf says what it would do and
    pushes and publishes nothing.

    A .NET library's publishing workflow runs on a published release, but a release made with the workflow's
    GITHUB_TOKEN starts no other workflow. So after it publishes a release, it starts each workflow the nuget_publisher
    answer in .copier-answers.yml names, a library's own, or else .github/workflows/publish.yml, when the project has
    it, with a workflow_dispatch event on the tag, passing the tag as the tag input when the workflow declares one. It
    fails, naming the trigger, when one of them has no workflow_dispatch trigger, and naming the answer when a workflow
    the answer names doesn't exist.

.PARAMETER Commit
    The commit to tag: in the workflow, the commit the push made main point to.

.PARAMETER Remote
    The remote whose tags count and that gets the tag.

.PARAMETER Gh
    The gh command that reads and publishes the release; the tests pass a stand-in.

.PARAMETER Policy
    Print the release policy, none, tag or tag+release, from the release line of the ## Landing section of AGENTS.md,
    none when AGENTS.md or the line is missing, and exit without reading the changelog, calling gh or tagging. It fails
    on any other value. .github/workflows/tag-release.yml runs it in a job that can only read, and runs the job that
    tags and publishes only for tag or tag+release.

.PARAMETER Sign
    Sign the tag, with git tag -s and the signing program Git is set up with, such as the gitsign that tag-release.yml
    sets up in a public repository (the template's ADR-0028). Without it the tag is annotated and unsigned.

.EXAMPLE
    ./.template/Publish-Release.ps1 -Commit $env:GITHUB_SHA

.EXAMPLE
    ./.template/Publish-Release.ps1 -WhatIf

.EXAMPLE
    ./.template/Publish-Release.ps1 -Policy
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Commit = 'HEAD',
    [string]$Remote = 'origin',
    [string]$Gh = 'gh',
    [switch]$Policy,
    [switch]$Sign
)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# The value of each `- <key>: <value>` line in the ## Landing section of AGENTS.md, without the quotes around a value.
function Get-LandingPolicy([string[]]$Lines) {
    $landing = @{}
    $inLanding = $false
    foreach ($line in $Lines) {
        if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
        if ($inLanding -and $line -match '^## ') { break }
        if ($inLanding -and $line -match '^- (?<key>[a-z-]+):\s*(?<value>.*?)\s*$') {
            $landing[$Matches.key] = $Matches.value -replace '^"(.*)"$', '$1'
        }
    }
    $landing
}

# The changelog's newest release section: its number and its notes, without the heading and the blank lines around them.
function Get-NewestRelease([string[]]$Lines) {
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -notmatch '^## \[(?<number>[^\]]+)\]' -or $Matches.number -eq 'Unreleased') { continue }
        $number = $Matches.number
        $notes = [Collections.Generic.List[string]]::new()
        for ($j = $i + 1; $j -lt $Lines.Count -and $Lines[$j] -notmatch '^## ' -and $Lines[$j] -notmatch '^\[[^\]]+\]:\s'; $j++) {
            $notes.Add($Lines[$j])
        }
        return [pscustomobject]@{ Number = $number; Notes = (($notes -join "`n").Trim()) }
    }
    $null
}

# The lines after `## [Unreleased]` up to the next `## ` heading or the link reference definitions, without the blank lines around them.
function Get-UnreleasedNotes([string[]]$Lines) {
    for ($i = 0; $i -lt $Lines.Count; $i++) {
        if ($Lines[$i] -notmatch '^## \[Unreleased\]') { continue }
        $notes = [Collections.Generic.List[string]]::new()
        for ($j = $i + 1; $j -lt $Lines.Count -and $Lines[$j] -notmatch '^## ' -and $Lines[$j] -notmatch '^\[[^\]]+\]:\s'; $j++) {
            $notes.Add($Lines[$j])
        }
        return (($notes -join "`n").Trim())
    }
    ''
}

# The pull request that merged as $Sha and the version of its `Release:` line, or $null when it has none.
function Get-PlannedRelease([string]$Sha) {
    $output = (& $Gh api "repos/{owner}/{repo}/commits/$Sha/pulls" 2>&1 | ForEach-Object { "$_" }) -join "`n"
    if ($LASTEXITCODE) { throw "Can't read the pull request that merged as ${Sha}: $output" }
    $pull = @(@(ConvertFrom-Json $output) | Where-Object { $_.PSObject.Properties['merge_commit_sha'] -and $_.merge_commit_sha -eq $Sha }) | Select-Object -First 1
    if (-not $pull -or -not $pull.PSObject.Properties['body'] -or -not $pull.body) { return $null }
    if ($pull.body -notmatch '(?m)^Release:[ \t]*(?<version>v?[0-9]+\.[0-9]+\.[0-9]+)[ \t]*\r?$') { return $null }
    [pscustomobject]@{ Number = $pull.number; Version = $Matches.version }
}

# The tag names the remote has, or an error when it can't be read.
function Get-RemoteTag([string]$Name) {
    $refs = @(& git ls-remote --tags --refs $Name)
    if ($LASTEXITCODE) { throw "Can't read the tags of $Name (git ls-remote exited $LASTEXITCODE)." }
    @($refs | ForEach-Object { if ($_ -match '\trefs/tags/(?<tag>.+)$') { $Matches.tag } })
}

# The events a workflow's top-level on: key names, in order: one event (on: push), a list in brackets, the keys or items
# indented below it, or - items at its own indent, with quotes, comments and blank lines allowed. A mapping in braces, or
# no on: key, gives none.
# A copy of .template/Update-Project.ps1's, itself a copy of tools/Set-RepositoryBaseline.ps1's; tests/Test-PublishRelease.ps1 checks they match.
function Get-WorkflowEvents([string]$Text) {
    $lines = @($Text -split '\r?\n')
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^(on|"on"|''on''):(.*)$') { continue }
        $rest = ($Matches[2] -replace '(^|\s)#.*$', '').Trim()
        $names = if ($rest.StartsWith('[')) { $rest.Trim('[', ']') -split ',' }
        elseif ($rest.StartsWith('{')) { @() }
        elseif ($rest) { @($rest) }
        else {
            $indent = -1
            @(for ($j = $i + 1; $j -lt $lines.Count; $j++) {
                $line = $lines[$j]
                if ($line -match '^\s*(#.*)?$') { continue }
                $null = $line -match '^(\s*)(.*)$'
                $lead = $Matches[1].Length
                $item = $Matches[2]
                # A line at column 0 ends the block, unless it is a - item of a sequence at the key's own indent.
                if (-not $lead -and -not ($indent -le 0 -and $item -match '^-(\s|$)')) { break }
                if ($indent -lt 0) { $indent = $lead }
                if ($lead -ne $indent) { continue }
                if ($item -match '^-\s*([^\s#]+)') { $Matches[1] }
                elseif ($item -match '^([^\s:#]+)\s*:') { $Matches[1] }
            })
        }
        return @($names | ForEach-Object { $_.Trim().Trim('"', "'") } | Where-Object { $_ -match '^[A-Za-z_]+$' })
    }
    @()
}

# Whether a workflow's workflow_dispatch block declares a tag input, as the template's publish.yml does: only then does the dispatch pass the tag as inputs[tag], since GitHub refuses an input the workflow doesn't declare.
function Test-DispatchTagInput([string]$Text) {
    $lines = @($Text -split '\r?\n')
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -notmatch '^(\s+)workflow_dispatch\s*:\s*(#.*)?$') { continue }
        $indent = $Matches[1].Length
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            $line = $lines[$j]
            if ($line -match '^\s*(#.*)?$') { continue }
            $null = $line -match '^(\s*)'
            if ($Matches[1].Length -le $indent) { break }
            if ($line -match '^\s+tag\s*:') { return $true }
        }
        return $false
    }
    $false
}

# The workflows that publish a .NET library's packages: those the nuget_publisher answer in .copier-answers.yml names, a library's own, whether or not they exist, or else the template's publish.yml when the project has it.
function Get-PackagePublisher([string]$Root) {
    $answers = Join-Path $Root '.copier-answers.yml'
    if (Test-Path -LiteralPath $answers) {
        foreach ($line in Get-Content -LiteralPath $answers) {
            if ($line -notmatch '^nuget_publisher:\s*(?<value>.*?)\s*$') { continue }
            $named = @($Matches.value.Trim("'", '"') -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            if ($named.Count) { return $named }
            break
        }
    }
    @('.github/workflows/publish.yml' | Where-Object { Test-Path -LiteralPath (Join-Path $Root $_) -PathType Leaf })
}

$root = (& git rev-parse --show-toplevel)
if ($LASTEXITCODE) { throw 'Run this in a clone of the repository.' }
$agents = Join-Path $root 'AGENTS.md'
if (-not (Test-Path -LiteralPath $agents)) { if ($Policy) { 'none' } else { 'AGENTS.md is missing, so there is no release policy: nothing to publish.' }; exit 0 }
$landing = Get-LandingPolicy @(Get-Content -LiteralPath $agents)
$release = if ($landing.ContainsKey('release')) { $landing['release'] } else { 'none' }
if ($release -notin 'none', 'tag', 'tag+release') { throw "AGENTS.md's Landing section says release: $release; use none, tag or tag+release." }
if ($Policy) { $release; exit 0 }
if ($release -eq 'none') { "AGENTS.md's Landing section says release: none: nothing to publish."; exit 0 }

$changelogName = if ($landing.ContainsKey('changelog') -and $landing['changelog']) { $landing['changelog'] } else { 'CHANGELOG.md' }
$changelog = Join-Path $root $changelogName
if (-not (Test-Path -LiteralPath $changelog)) { "$changelogName is missing: nothing to publish."; exit 0 }
$lines = @(Get-Content -LiteralPath $changelog)
$sha = (& git rev-parse --verify --quiet "$Commit^{commit}")
if ($LASTEXITCODE -or -not $sha) { throw "$Commit isn't a commit in this clone." }
$planned = Get-PlannedRelease $sha
$untracked = $landing.ContainsKey('pre-release') -and $landing['pre-release'] -eq 'untracked'

$newest = Get-NewestRelease $lines
if ($newest) {
    if ($newest.Number -notmatch '^[0-9]+\.[0-9]+\.[0-9]+\z') { "$changelogName's newest release, $($newest.Number), isn't shaped like 1.2.3: tag it with /ship-plan."; exit 0 }
    if ($planned -and ($planned.Version -replace '^v') -ne $newest.Number) {
        throw "Pull request #$($planned.Number) says Release: $($planned.Version), but $changelogName's newest release is $($newest.Number): make them agree, then run the workflow again."
    }
    $number = $newest.Number
    $notes = $newest.Notes
    $source = "its $changelogName section"
} elseif ($untracked -and $planned) {
    $number = $planned.Version -replace '^v'
    $notes = Get-UnreleasedNotes $lines
    $source = "the [Unreleased] section of $changelogName"
} elseif ($untracked) {
    "$changelogName has no release section and the merged pull request has no Release: line: nothing to publish."
    exit 0
} else {
    "$changelogName has no release section yet: nothing to publish."
    exit 0
}

$tags = Get-RemoteTag $Remote
$versionTags = @($tags | Where-Object { $_ -match '^v?[0-9]+\.[0-9]+\.[0-9]+\z' })
$prefix = if ($versionTags.Count -and -not @($versionTags | Where-Object { $_ -like 'v*' }).Count) { '' } else { 'v' }
$version = "$prefix$number"

if ($tags -ccontains $version) {
    "$Remote has the tag $version already."
} else {
    $newer = @($versionTags | Where-Object { [version]($_ -replace '^v') -ge [version]$number })
    if ($newer.Count) {
        "The release is $version, but $Remote has $(($newer | Sort-Object { [version]($_ -replace '^v') } | Select-Object -Last 1)) already: nothing to tag."
        exit 0
    }
    # A .NET library's packages take their version from version.json (ADR-0138): a tag whose commit holds another would publish none.
    # Only a commit without the file skips the check; one whose file Git can't list or read stops the release, because a tag is never moved.
    $listed = @(& git ls-tree --name-only $sha -- version.json)
    if ($LASTEXITCODE) { throw "git ls-tree couldn't list $sha (exit $LASTEXITCODE), so version.json can't be checked: nothing is tagged." }
    if ($listed -contains 'version.json') {
        $versionJson = & git show "${sha}:version.json" 2>$null
        if ($LASTEXITCODE) { throw "version.json at $sha can't be read (git show exited $LASTEXITCODE): nothing is tagged." }
        $held = try { "$((($versionJson -join "`n") | ConvertFrom-Json).version)" } catch { '' }
        if ($held -ne $number) { throw "version.json at $sha holds version '$held', but the release is ${number}: set its version to $number in a pull request, whose merge tags the release." }
    }
    if ($PSCmdlet.ShouldProcess("$sha on $Remote", "Tag $version")) {
        $tagFlag = if ($Sign) { '-s' } else { '-a' }
        & git tag $tagFlag $version -m $version $sha
        if ($LASTEXITCODE) { throw "git tag $tagFlag $version exited $LASTEXITCODE." }
        & git push $Remote "refs/tags/$version"
        if ($LASTEXITCODE) {
            & git tag -d $version | Out-Null
            if ($LASTEXITCODE) { Write-Warning "Couldn't delete the local tag $version; delete it with git tag -d $version." }
            # Another run, or /ship-plan, may have pushed the same tag a moment ago; the release below still needs it.
            if (-not ((Get-RemoteTag $Remote) -ccontains $version)) { throw "Pushing the tag $version to $Remote failed." }
            "$Remote got the tag $version from another run meanwhile."
        } else {
            "Tagged $sha as $version$(if ($Sign) { ', signed,' }) and pushed it to $Remote."
        }
    }
}

$publishedNow = $false
if ($release -eq 'tag+release') {
    # REST, not `gh release view` and `gh release create`, which use GraphQL: a Claude Code cloud session can run this too.
    $lookup = (& $Gh api "repos/{owner}/{repo}/releases/tags/$version" --silent 2>&1 | ForEach-Object { "$_" }) -join "`n"
    $lookupCode = $LASTEXITCODE
    # Only a 404 means there is no release; any other failure, such as a bad token or an outage, stops here with gh's message.
    if ($lookupCode -and $lookup -notmatch 'HTTP 404') { throw "Can't tell whether GitHub has the release ${version}: $lookup" }
    if (-not $lookupCode) {
        "GitHub has the release $version already."
    } elseif ($PSCmdlet.ShouldProcess($version, 'Publish the GitHub release')) {
        $notesFile = New-TemporaryFile
        try {
            Set-Content -LiteralPath $notesFile -Value $notes -Encoding utf8NoBOM
            & $Gh api 'repos/{owner}/{repo}/releases' -f "tag_name=$version" -f "name=$version" -F "body=@$notesFile" --silent
            if ($LASTEXITCODE) { throw "Publishing the GitHub release $version failed (gh api exited $LASTEXITCODE)." }
            $publishedNow = $true
            "Published the GitHub release $version from $source."
        } finally {
            Remove-Item -LiteralPath $notesFile -ErrorAction SilentlyContinue
        }
    }
}

# A release made with the workflow's GITHUB_TOKEN starts no other workflow, so a .NET library's publishing workflows, which run on a published release, are started through workflow_dispatch (the template's ADR-0137).
if ($publishedNow) {
    $untriggered = [Collections.Generic.List[string]]::new()
    $missing = [Collections.Generic.List[string]]::new()
    foreach ($workflow in @(Get-PackagePublisher $root)) {
        $path = Join-Path $root $workflow
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { $missing.Add($workflow); continue }
        $text = Get-Content -LiteralPath $path -Raw
        if (@(Get-WorkflowEvents $text) -notcontains 'workflow_dispatch') { $untriggered.Add($workflow); continue }
        $name = Split-Path -Leaf $workflow
        if ($PSCmdlet.ShouldProcess($version, "Start $name")) {
            $dispatch = @('api', "repos/{owner}/{repo}/actions/workflows/$name/dispatches", '-f', "ref=$version")
            if (Test-DispatchTagInput $text) { $dispatch += '-f', "inputs[tag]=$version" }
            & $Gh @dispatch --silent
            if ($LASTEXITCODE) { throw "Starting $name for $version failed (gh api exited $LASTEXITCODE): run it from the Actions tab on the tag $version." }
            "Started $name for $version."
        }
    }
    $messages = @($missing | ForEach-Object { "Published the release $version, but $_, which the nuget_publisher answer in .copier-answers.yml names as this library's publishing workflow, doesn't exist, so nothing publishes its packages from it. Correct the answer, which .\.template\Update-Project.ps1 sets to the workflows that run nuget push, or restore the workflow, then run it from the Actions tab on the tag $version." }) + @($untriggered | ForEach-Object {
            if ($_ -eq '.github/workflows/publish.yml') {
                "Published the release $version, but .github/workflows/publish.yml has no workflow_dispatch trigger, so nothing publishes its packages: a release made with the workflow's token starts no other workflow. Add the trigger with a tag input, as the project template's publish.yml has it, then run Publish packages from the Actions tab with the tag $version."
            } else {
                "Published the release $version, but $_, which the nuget_publisher answer in .copier-answers.yml names as this library's publishing workflow, has no workflow_dispatch trigger, so nothing publishes its packages: a release made with the workflow's token starts no other workflow. Add a workflow_dispatch trigger to it, reading the tag from inputs.tag or github.ref_name when the run has no release, then run it from the Actions tab on the tag $version."
            }
        })
    if ($messages.Count) { throw ($messages -join ' ') }
}
