<#
.SYNOPSIS
    Update this project to the newest version of the project template it was created from, and commit it.

.DESCRIPTION
    Every project carries this script as .template\Update-Project.ps1. The template owns it, so an update replaces
    it like the template's other files. It updates the project it sits in, or -Path.

    It clones the template from the address in .copier-answers.yml, so no template clone is needed. When the
    target version's copy of this script differs from this one, it hands the update over to that copy: the
    newest update logic always does the update, including replacing this file.

    Every file in the project has one of three owners (copier.yml, and the template README's "What a Project Gets"):

      Template-owned  Replaced by the new version. A local edit to one is discarded and a deleted one is
                      restored; the script lists both. A template-owned file that an earlier version's
                      _skip_if_exists kept from updates, such as docs/decisions/project-template/README.md, is
                      reset first, discarding a local edit, and listed as refreshed.
                      A file the update leaves as the project had it, such as an edit the new version also
                      makes or a deleted file it also deletes, is listed under none of these.
      Merged          Files whose tools can't include a second file (copier.yml's _merged_files). The template's
                      changes are merged with the project's edits, like a Git merge; the same lines changed on
                      both sides conflict. An adopted project's own lines at the end of .gitignore,
                      .gitattributes and .editorconfig are set aside while Copier merges, so they never
                      conflict. A catch-all text rule among .gitattributes' kept lines, such as * text eol=lf,
                      is left out, as the template's Adopt-Project.ps1 leaves it out, and the script says so: it came after
                      the template's binary rules and overrode them.
      Project-owned   Written once, never touched again (copier.yml's _skip_if_exists). When the template changed
                      one since the project's version, the script shows the template's change, rendered with
                      the project's answers, leaving out what the project's file already has.
                      The render takes pre_release from AGENTS.md's Landing section, untracked only
                      with - pre-release: untracked, else tracked, and release_policy from its release line.
                      A merged or template-owned file that the project's project_owned_files answer names
                      is project-owned too; a project that replaced one sets it with -Data @{ project_owned_files
                      = '.github/workflows/ci.yml', 'build/dod.ps1' }. The answer can name any file the version
                      the update moves to renders, except .copier-answers.yml, .template-version and the files in
                      .template/; any other name stops the update before it changes anything.
                      When the template changed a merged file that shares less than half its lines with the
                      template's version, the script suggests that answer, with the -Data to run;
                      it doesn't stop the update. When Copier can't render the project's version, it warns
                      with Copier's error and says it skipped the suggestion.
                      When the update stops on a conflict in a file the version it moves to owns, such
                      as a build/dod.ps1 of the project's own that the template now adds, it suggests
                      the answer the same way, naming that file too.

    The update runs `copier recopy` at the project's current template version, which resets the template-owned
    files and shows any local edits, then `copier update --defaults` to the newest version (or -Ref), which merges
    the merged files and adds and deletes template files as the template did. Copier runs from PATH or through uvx.

    Without conflicts the result is one commit, "Update from project-template <version>"; -NoCommit stages it
    instead. Either way it exits 0, as it does when there is nothing to update. With a conflict it commits nothing,
    says how to finish or abandon the update, and exits 1; every change outside the conflicting files is then staged
    as it is in the working tree, deletions included, so staging the resolved files stages the whole update. On any
    other failure the project is reset to where it was.

    It stages only what the update changed: the tracked files with git add -u and each file it added by name, never
    with git add -A. From the reset to the end of the run, .git/info/exclude holds a copy of the project's whole
    .gitignore, an adopted project's kept lines included, so a folder the project ignores, such as node_modules/, is
    never staged, also when the updated .gitignore doesn't ignore it. The run then restores .git/info/exclude.

    On the default branch (the branch origin/HEAD names) it refuses to run unless -NoCommit is given, because it
    commits. It enables the template's hooks (core.hooksPath .githooks) unless the clone already has a hooks path,
    hooks in .git/hooks, or uses pre-commit; then it leaves them as they are and warns, naming the command that runs
    the template's hooks, or says nothing when core.hooksPath names a .githooks folder. With protect: hook in
    AGENTS.md's Landing section, it also says how to run .githooks/pre-push there.

    When the project has version tags merged into HEAD that the changelog (AGENTS.md's Landing changelog key,
    CHANGELOG.md by default) has no section for, it ends with a /plan-migration request to backfill them.

    For a .NET library, it names the AssemblyVersion to set in Directory.Build.props when a release of the current
    major was built with a higher one than build/ProjectTemplate.props pins (the library's ADR-0136). It leaves
    .github/workflows/publish.yml out of a library whose own workflow already runs nuget push, and warns when both
    publish. When the project has .github/workflows/tag-release.yml and a publish.yml without a workflow_dispatch
    trigger, it says what to add. When tag-release.yml runs ci.yml and ci.yml has no workflow_call trigger, it says
    to add one. When a job of ci.yml, or its top-level permissions, asks for more than contents: read, which
    tag-release.yml grants it, it names each and says to move such a job into a workflow of its own (the template's
    ADR-0026). When it has release-provenance.yml or raise-baseline.yml and a publish.yml that
    doesn't call one of them, or either, it warns that no release gets its provenance or baseline pull request (the template's
    ADR-0151). When the project has version.json, it also warns about a publish.yml or a
    .gitlab-ci.yml that packs without -p:PublicRelease=true and, when build/ProjectTemplate.props references
    Nerdbank.GitVersioning, as a library's does, names the files that reference it themselves (the template's ADR-0138).
    It also names each job of the project's own workflows, other than publish.yml, that builds, packs, tests without --no-build or pushes packages with an actions/checkout step without fetch-depth: 0 whose folder holds such a step's working directory (its working-directory:, or the job's or the workflow's defaults:), on which Nerdbank.GitVersioning fails the build, or with a -p:Version argument, which it ignores; when it can't tell which checkout a step runs in, it names each shallow checkout of the job.
    Such a library's nuget_publisher answer names its own publishing workflows, so the update also leaves out or deletes release-provenance.yml and raise-baseline.yml, and its documents name those workflows instead of publish.yml.
    When -Data turns csharp_format on, it counts the C# files that don't start with the header .editorconfig sets and names the first five, because IDE0073 then fails the build for each (the template's ADR-0148); it rewrites none.
    An existing project keeps off the checks a new one starts with until -Data turns them on: csharp_format, node_lint, coverage_gate and file_headers are false when its answers lack them.
    A NuGet package needs a license: when the answers and -Data give a library none, it stops before it changes
    anything and prints the -Data @{ license = '<SPDX expression, such as MIT or Apache-2.0>' } to run.
    When the project has no LICENSE or COPYING file at its root, it warns, also when the project is already up to
    date: for a blank license answer it prints the -Data @{ license = 'Apache-2.0' } that adds one, and for any other
    answer it says to add that license's text (the template's ADR-0024). A project from before the license question
    gets a blank answer, never copier.yml's Apache-2.0 default.
    When best_practices_project is set and README.md doesn't show the project's OpenSSF Best Practices badge, it prints
    the line to add, also when the project is already up to date.
    When SECURITY.md, or the template's change to it that the update shows, says releases are immutable and a workflow
    in .github/workflows runs on a release event or uploads release assets, whose releases the template's
    tools/Set-RepositoryBaseline.ps1 leaves mutable, it says to delete or leave out that paragraph, also when the
    project is already up to date (the template's ADR-0028).
    With NCode Review, when .github/workflows/code-review.yml pins NCodeGroup/code-review before v1.5.0, passes no approve: true or passes approve: true without approver-private-key, or AGENTS.md's Landing section doesn't say draft: yes, it says what to change, to store the approver's key with tools/Enable-CodeReview.ps1 -ApproverPrivateKeyPath from NCodeGroup/code-review and to run the template's tools/Set-RepositoryBaseline.ps1 again, also when the project is already up to date (the template's ADR-0022 and ADR-0036). It says that v1.5.0, published on 2026-11-21, waits out the three-day cooldown of the template's ADR-0009 until 2026-11-24, or is adopted sooner only by that record's commented exception.

    On Windows it stops before it changes anything when the project's template version has a path Git for Windows
    refuses to check out, such as the uses_node file names of v0.69.0 to v0.75.0, because the update starts with
    copier recopy at that version; it names the path and says to update once from Linux, macOS or WSL.

    Every version of this script accepts -Path, -Ref, -Data, -NoCommit, -TemplateClone and -HandedOff, so any
    version can hand over to any later one.

.EXAMPLE
    .\.template\Update-Project.ps1
    .\.template\Update-Project.ps1 -NoCommit
    .\.template\Update-Project.ps1 -Ref v0.3.0
#>
[CmdletBinding()]
param(
    [string]$Path = (Split-Path $PSScriptRoot),
    [string]$Ref,
    [hashtable]$Data = @{},
    [switch]$NoCommit,
    # Set by the version that hands the update over; not for direct use.
    [string]$TemplateClone,
    [switch]$HandedOff
)
$ErrorActionPreference = 'Stop'

# Copier says "Make sure Git >= 2.24 is installed" whenever Git rejects `diff-tree --inter-hunk-context=-1`,
# which Git 2.55 and later always do (copier-org/copier#2759), so the filter swaps in an accurate note.
function Invoke-Copier {
    $gitNote = 'Copier''s "Make sure Git >= 2.24 is installed" is a false alarm: Git 2.55 and later reject an option its update uses, so Copier falls back, and nearby changes to a merged file may conflict as one block (copier-org/copier#2759).'
    $ErrorActionPreference = 'Continue'   # Copier's stderr lines arrive as error records through 2>&1
    $prefix = @()
    $command = if (Get-Command copier -ErrorAction SilentlyContinue) { 'copier' }
    elseif (Get-Command uvx -ErrorAction SilentlyContinue) { $prefix = '--from', 'copier', 'copier'; 'uvx' }
    else { throw 'Copier not found. Install it with: uv tool install copier' }
    & $command @prefix @args 2>&1 | ForEach-Object { if ("$_" -match 'Make sure Git >= 2\.24 is installed') { $gitNote } else { "$_" } }
    if ($LASTEXITCODE) { throw "copier $($args[0]) exited with code $LASTEXITCODE." }
}

# Copier reads --data values as YAML, so a list answer goes as a flow sequence: [claude, copilot].
function ConvertTo-CopierValue($Value) {
    if ($Value -is [Collections.IList]) { '[' + ($Value -join ', ') + ']' } else { "$Value" }
}

# The pre_release answer, which Copier never records (copier.yml says when: false): untracked when AGENTS.md's Landing
# section has "- pre-release: untracked", else tracked, as .template/Get-PlanState.ps1 reads it.
function Get-PreReleaseAnswer {
    $agents = Join-Path $project 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $agents -PathType Leaf)) { return 'tracked' }
    $inLanding = $false
    foreach ($line in Get-Content -LiteralPath $agents) {
        if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
        if ($inLanding -and $line -match '^## ') { break }
        if ($inLanding -and $line -match '^- pre-release:\s*(\S+)') { return $(if ($Matches[1] -eq 'untracked') { 'untracked' } else { 'tracked' }) }
    }
    'tracked'
}

# The release_policy answer, which Copier never records (copier.yml says when: false): the value of the "- release:" line of
# AGENTS.md's Landing section, none without the line, as .template/Publish-Release.ps1 reads it. Nothing for a value that
# script refuses or without AGENTS.md, so the answer's default applies. Runs in the project folder.
# tools/adopt/Adoption.psm1 has a deliberate copy of this function for the template's Adopt-Project.ps1. Change both together; tests/Test-UpdateNotes.ps1 runs both copies through the same cases.
function Get-ReleasePolicyAnswer {
    $agents = 'AGENTS.md'
    if (-not (Test-Path -LiteralPath $agents -PathType Leaf)) { return }
    $value = 'none'
    $inLanding = $false
    foreach ($line in Get-Content -LiteralPath $agents) {
        if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
        if ($inLanding -and $line -match '^## ') { break }
        if ($inLanding -and $line -match '^- release:\s*(.*?)\s*$') { $value = $Matches[1] -replace '^"(.*)"$', '$1'; break }
    }
    if ($value -in 'none', 'tag', 'tag+release') { $value }
}

# The checks a new project starts with and an existing one keeps off until it opts in (the template's ADR-0148,
# ADR-0201, ADR-0026 and ADR-0027): --data <answer>=false for each the answers file lacks and -Data doesn't set.
function Get-OptInData {
    foreach ($key in 'csharp_format', 'node_lint', 'coverage_gate', 'file_headers') {
        if (-not (Get-Answer $key) -and -not $Data.ContainsKey($key)) { '--data', "$key=false" }
    }
}

# A project from before the license question has no license answer: an update keeps it without a license rather than giving it copier.yml's Apache-2.0 default (ADR-0024); -Data sets one.
function Get-LicenseData {
    if (-not $Data.ContainsKey('license') -and -not (Select-String -LiteralPath .copier-answers.yml -Pattern '^license:' -Quiet)) { '--data', 'license=' }
}

# No license (ADR-0024): no LICENSE or COPYING file at the project's root. A blank answer gets the command that adds Apache-2.0; any other answer, one the template writes no file for or whose LICENSE.txt was deleted, is told to add that license's text. The template's Adopt-Project.ps1 prints the same lines; tests/Test-LicenseWarning.ps1 keeps them equal.
function Get-LicenseWarning {
    if (Get-ChildItem -LiteralPath $project -File | Where-Object Name -Match '^(LICEN[CS]E|COPYING)(\.\w+)?$') { return }
    $licenseAnswer = "$(Get-Answer 'license')".Trim()
    ''
    if (-not $licenseAnswer) {
        'No license: the license answer is blank and the repository has no LICENSE or COPYING file, so no one else may use, copy or change its code, and the OpenSSF Best Practices criteria for a license can''t be met. To license it under Apache-2.0, NCode Group''s license, which adds LICENSE.txt and COPYRIGHT.txt, run the command below in the repository; for another SPDX license expression, such as MIT, put it in place of Apache-2.0:'
        "  .\.template\Update-Project.ps1 -Data @{ license = 'Apache-2.0' }"
    }
    else {
        "No license file: the license answer is $licenseAnswer, but the repository has no LICENSE or COPYING file, so no one else may use, copy or change its code, and the OpenSSF Best Practices criteria for a license can't be met. Add the text of the $licenseAnswer license as LICENSE.txt at the repository's root."
    }
}

# documentation_achievements asks for the badge in README.md within 48 hours of earning a level, and README.md is
# project-owned, so recording best_practices_project never adds it there (the template's ADR-0025).
function Get-BadgeReminder {
    $number = "$(Get-Answer 'best_practices_project')".Trim()
    if (-not $number) { return }
    $badge = "https://www.bestpractices.dev/projects/$number/badge"
    $readme = Join-Path $project 'README.md'
    if ((Test-Path -LiteralPath $readme) -and (Get-Content -LiteralPath $readme -Raw).Contains($badge)) { return }
    ''
    "README.md doesn't show the OpenSSF Best Practices badge of project $number, which documentation_achievements asks for within 48 hours of earning a level. Add this line near its top (the template's ADR-0025, in docs/decisions/project-template/):"
    "  [![OpenSSF Best Practices]($badge)](https://www.bestpractices.dev/projects/$number)"
}

# tag-release.yml runs ci.yml as a reusable workflow before it tags a release (ADR-0026): without the trigger, no release is tagged.
# tools/adopt/Adoption.psm1 has a deliberate copy of this function for the template's Adopt-Project.ps1. Change both together; tests/Test-UpdateNotes.ps1 runs both copies through the same cases.
function Get-CiCallNote {
    $release = '.github/workflows/tag-release.yml'
    if (-not (Test-Path -LiteralPath $release) -or (Get-Content -LiteralPath $release -Raw) -notmatch '(?m)^\s+uses:\s*\./\.github/workflows/ci\.yml\s*$') { return }
    $ci = '.github/workflows/ci.yml'
    if ((Test-Path -LiteralPath $ci) -and (Get-Content -LiteralPath $ci -Raw) -match '(?m)^\s+workflow_call:') { return }
    ''
    '.github/workflows/ci.yml is missing or has no workflow_call trigger. .github/workflows/tag-release.yml runs it as a reusable workflow before it tags a release, so until it has that trigger, no release is tagged or published. Add workflow_call: to its on: section, as the ci.yml the project template gives a GitHub project does (the template''s ADR-0026, in docs/decisions/project-template/).'
}

# The events a workflow's top-level on: key names, in order: one event (on: push), a list in brackets, the keys or items
# indented below it, or - items at its own indent, with quotes, comments and blank lines allowed. A mapping in braces, or
# no on: key, gives none.
# A copy of tools/Set-RepositoryBaseline.ps1's; the copies must match.
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

# Whether a workflow's text runs on a release event, or uploads release assets with gh, an upload action or the API
# on a line that isn't a comment.
# A copy of tools/Set-RepositoryBaseline.ps1's; the copies must match.
function Test-ReleaseAssetWorkflow([string]$Text) {
    $code = $Text -replace '(?m)^\s*#.*$', ''
    [bool](((Get-WorkflowEvents $Text) -contains 'release') -or ($code -match 'gh\s+release\s+upload|upload-release-asset|uploadReleaseAsset|action-gh-release'))
}

# SECURITY.md's Verify a Release section says releases are immutable, which the template's
# tools/Set-RepositoryBaseline.ps1 leaves off where a workflow runs on a release event or uploads release assets
# (ADR-0028).
# The template's Adopt-Project.ps1 has a copy of this function; tests/Test-UpdateNotes.ps1 keeps the copies equal.
function Get-ImmutableReleaseNote([bool]$Offered) {
    $says = (Test-Path -LiteralPath SECURITY.md) -and (Get-Content -LiteralPath SECURITY.md -Raw) -match '(?m)^Releases are immutable'
    if (-not ($says -or $Offered) -or -not (Test-Path -LiteralPath .github/workflows)) { return }
    $files = @(Get-ChildItem -LiteralPath .github/workflows -File | Where-Object { $_.Name -match '\.ya?ml$' -and (Test-ReleaseAssetWorkflow (Get-Content -LiteralPath $_.FullName -Raw)) } | ForEach-Object { ".github/workflows/$($_.Name)" })
    if (-not $files) { return }
    ''
    "$(if ($says) { 'SECURITY.md says' } else { 'The template''s change to SECURITY.md above says' }) releases are immutable, but $($files -join ', ') run on a release event or upload release assets, which an immutable release refuses once it is published, so the template's tools/Set-RepositoryBaseline.ps1 leaves this repository's releases mutable (the template's ADR-0028, in docs/decisions/project-template/). $(if ($says) { 'Delete' } else { 'Leave out' }) the paragraph of its Verify a Release section that starts `"Releases are immutable`"."
}

# tag-release.yml grants ci.yml only contents: read (ADR-0026). GitHub checks every job of a called workflow against that
# as the run starts, whatever the job's if: says, and fails the run when one asks for more: no release is tagged.
# tools/adopt/Adoption.psm1 has a deliberate copy of this function for the template's Adopt-Project.ps1. Change both together; tests/Test-UpdateNotes.ps1 runs both copies through the same cases.
function Get-CiPermissionNote {
    $release = '.github/workflows/tag-release.yml'
    if (-not (Test-Path -LiteralPath $release) -or (Get-Content -LiteralPath $release -Raw) -notmatch '(?m)^\s+uses:\s*\./\.github/workflows/ci\.yml\s*$') { return }
    $ci = '.github/workflows/ci.yml'
    if (-not (Test-Path -LiteralPath $ci)) { return }
    # Each permissions: block of ci.yml, the workflow's own (Owner '') or a job's, with its scope: level pairs.
    $blocks = [Collections.Generic.List[object]]::new()
    $inJobs = $false
    $jobIndent = -1
    $keyIndent = -1
    $job = ''
    $block = $null
    $blockIndent = -1
    foreach ($line in Get-Content -LiteralPath $ci) {
        if ($line -notmatch '^(\s*)([\w-]+):\s*(.*?)\s*(?:#.*)?$') { continue }
        $indent, $key, $value = $Matches[1].Length, $Matches[2], $Matches[3].Trim('''', '"')
        if ($block -and $indent -gt $blockIndent) { $block.Pairs += "${key}: $value"; continue }
        $block = $null
        if ($indent -eq 0) { $inJobs = $key -eq 'jobs'; $jobIndent = -1 }
        elseif ($inJobs -and ($jobIndent -lt 0 -or $indent -le $jobIndent)) { $jobIndent = $indent; $job = $key; $keyIndent = -1; continue }
        elseif ($inJobs -and $keyIndent -lt 0) { $keyIndent = $indent }
        $isJob = $inJobs -and $indent -eq $keyIndent
        if ($key -ne 'permissions' -or -not ($indent -eq 0 -or $isJob)) { continue }
        $block = [pscustomobject]@{ Owner = $(if ($isJob) { $job } else { '' }); Pairs = @() }
        $blockIndent = $indent
        $blocks.Add($block)
        if ($value) {
            $block.Pairs = @($value.Trim('{', '}', ' ') -split ',' | ForEach-Object { ($_ -replace '[''"]', '').Trim() } | Where-Object { $_ })
            $block = $null
        }
    }
    $findings = foreach ($b in $blocks) {
        $extra = @($b.Pairs | Where-Object { $_ -notmatch '^contents:\s*read$' -and $_ -notmatch '^[\w-]+:\s*none$' })
        if (-not $extra.Count) { continue }
        if ($b.Owner) { "job $($b.Owner) asks for $($extra -join ', ')" } else { "its top-level permissions ask for $($extra -join ', ')" }
    }
    if (-not $findings) { return }
    ''
    ".github/workflows/ci.yml asks for more than contents: read: $($findings -join '; '). .github/workflows/tag-release.yml runs ci.yml as a reusable workflow before it tags a release and grants it only contents: read, and GitHub fails that run as it starts when a job of ci.yml asks for more, whatever the job's if: says, so no release is tagged or published. Move each such job into a workflow of its own, such as .github/workflows/coverage.yml, and keep ci.yml's top-level permissions at contents: read (the template's ADR-0026, in docs/decisions/project-template/)."
}

# NCode Review approves a clean pull request only with approve: true, and from NCodeGroup/code-review v1.5.0 on submits the
# approval as the NCode Review Approver, which meets a required approval, when approver-private-key is passed; code-review.yml
# and AGENTS.md are project-owned, so the update can only say what they still lack (the template's ADR-0022 and ADR-0036),
# and when ADR-0009's cooldown lets it adopt v1.5.0.
function Get-CodeReviewNote {
    $workflow = '.github/workflows/code-review.yml'
    if (-not (Test-Path -LiteralPath $workflow -PathType Leaf)) { return }
    $text = Get-Content -LiteralPath $workflow -Raw
    $versions = @(foreach ($m in [regex]::Matches($text, '(?im)^\s*(?:-\s+)?uses:\s*[''"]?NCodeGroup/code-review(?:/[\w./-]+)?@([^\s''"#]+)[''"]?[ \t]*(?:#[ \t]*(\S+))?')) {
        if ($m.Groups[2].Success -and $m.Groups[2].Value -match '^v?(\d+\.\d+\.\d+)$') { [version]$Matches[1] }
        elseif ($m.Groups[1].Value -match '^v?(\d+\.\d+\.\d+)$') { [version]$Matches[1] }
    })
    $findings = [Collections.Generic.List[string]]::new()
    if ($versions.Count) {
        $lowest = @($versions | Sort-Object)[0]
        if ($lowest -lt [version]'1.5.0') { $findings.Add(".github/workflows/code-review.yml pins NCodeGroup/code-review v$lowest") }
    }
    if ($text -notmatch '(?m)^\s+approve:\s*[''"]?true[''"]?\s*(?:#.*)?$') { $findings.Add('.github/workflows/code-review.yml passes no approve: true') }
    elseif ($text -notmatch '(?m)^\s+approver-private-key:\s*\S') { $findings.Add('.github/workflows/code-review.yml has no approver-private-key input') }
    if (Test-Path -LiteralPath AGENTS.md) {
        $draft = $null
        $inLanding = $false
        foreach ($line in Get-Content -LiteralPath AGENTS.md) {
            if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
            if ($inLanding -and $line -match '^## ') { break }
            if ($inLanding -and $line -match '^- draft:\s*`?([^`\s]+)`?\s*$') { $draft = $Matches[1] }
        }
        if ($draft -ne 'yes') { $findings.Add($(if ($null -eq $draft) { 'AGENTS.md''s Landing section has no draft line' } else { "AGENTS.md's Landing section says draft: $draft" })) }
    }
    if (-not $findings.Count) { return }
    ''
    "NCode Review approves a clean pull request only with approve: true, and from NCodeGroup/code-review v1.5.0 on, given approver-private-key, submits the approval as the NCode Review Approver, an app that can write, so the approval meets a required approval (the template's ADR-0022, ADR-0034 and ADR-0036, in docs/decisions/project-template/): $($findings -join '; '). Pin v1.5.0 or later (published on 2026-11-21: the three-day cooldown of the template's ADR-0009 adopts it from 2026-11-24, and sooner only by that record's commented exception) and pass approve: true and approver-private-key: `${{ secrets.NCODE_REVIEW_APPROVER_PRIVATE_KEY }} in the review job of .github/workflows/code-review.yml, as the template's code-review.yml does, store the approver's key in the code-review environment with tools/Enable-CodeReview.ps1 -ApproverPrivateKeyPath from NCodeGroup/code-review, set - draft: yes in AGENTS.md's Landing section, so a pull request opens as a draft and marking it ready is the maintainer's review, then run the template's tools/Set-RepositoryBaseline.ps1 again, so the default branch dismisses stale approvals and requires one approval, of the last push, where the approver is set up or a second person has write access."
}

# Nerdbank.GitVersioning, which version.json turns on (the template's ADR-0138), fails a build on a shallow clone and ignores -p:Version: name each job of the project's own workflows, other than publish.yml, which has its own check, that builds, packs, tests without --no-build or pushes packages in a shallow actions/checkout's folder, by the step's working-directory: or the job's or workflow's defaults:, or with a -p:Version argument.
# tools/adopt/Adoption.psm1 has a deliberate copy of this function for the template's Adopt-Project.ps1. Change both together; tests/Test-UpdateNotes.ps1 runs both copies through the same cases.
function Get-GitVersioningWorkflowNote {
    if (-not (Test-Path -LiteralPath version.json) -or -not (Test-Path -LiteralPath .github/workflows -PathType Container)) { return }
    $findings = [Collections.Generic.List[string]]::new()
    # A folder as the workspace-relative path actions/checkout's path: or a working-directory: names it: '' for the workspace, $null for one this check can't place.
    $folderOf = {
        param([string]$Value)
        $v = ($Value -replace '\s+#.*$', '').Trim().Trim('''', '"').Trim()
        $v = $v -replace '^\$\{\{\s*github\.workspace\s*\}\}/?', '' -replace '^(?:\./)+', '' -replace '/+$', ''
        if ($v -eq '.') { $v = '' }
        if ($v -match '\$\{\{|^/|^[A-Za-z]:|(?:^|/)\.\.(?:/|$)') { return $null }
        $v
    }
    # The first capture of Pattern in Lines, or $null when no line matches.
    $valueIn = { param([string[]]$Lines, [string]$Pattern) foreach ($l in $Lines) { if ($l -match $Pattern) { return $Matches[1] } } }
    $isBuild = { param([string]$Line) $Line -match '(?i)\bdotnet\s+(?:build|pack)\b' -or ($Line -match '(?i)\bdotnet\s+test\b' -and $Line -notmatch '--no-build') -or $Line -match '(?i)\bnuget(?:\.exe)?\s+push\b' }
    foreach ($file in @(Get-ChildItem -LiteralPath .github/workflows -File | Where-Object { $_.Name -match '\.ya?ml$' -and $_.Name -ne 'publish.yml' } | Sort-Object Name)) {
        # The lines of each job under the top-level jobs: key, by job name, and the lines of the top-level defaults: key.
        $jobs = [ordered]@{}
        $defaults = [Collections.Generic.List[string]]::new()
        $inJobs = $false
        $inDefaults = $false
        $jobIndent = -1
        $current = $null
        foreach ($line in Get-Content -LiteralPath $file.FullName) {
            if ($line -match '^\s*(#.*)?$') { continue }
            $indent = $line.Length - $line.TrimStart().Length
            if ($indent -eq 0) { $inJobs = $line -match '^jobs:\s*(#.*)?$'; $inDefaults = $line -match '^defaults:\s*(#.*)?$'; $jobIndent = -1; $current = $null; continue }
            if ($inDefaults) { $defaults.Add($line); continue }
            if (-not $inJobs) { continue }
            if ($jobIndent -lt 0) { $jobIndent = $indent }
            if ($indent -eq $jobIndent -and $line -match '^\s*([\w-]+):') {
                $current = $Matches[1]
                $jobs[$current] = [Collections.Generic.List[string]]::new()
                continue
            }
            if ($current) { $jobs[$current].Add($line) }
        }
        $workflowDefault = & $valueIn $defaults '^\s*working-directory:\s*(.*)$'
        foreach ($name in $jobs.Keys) {
            $lines = @($jobs[$name])
            if (-not @($lines | Where-Object { & $isBuild $_ }).Count) { continue }
            $stepAt = {
                param([int]$At)
                # The step starts at the nearest line at or above that starts a list item, and ends before the next one no deeper.
                $start = $At
                while ($start -ge 0 -and $lines[$start] -notmatch '^\s*-\s') { $start-- }
                if ($start -lt 0) { $start = $At }
                $startIndent = $lines[$start].Length - $lines[$start].TrimStart().Length
                $end = $lines.Count
                for ($j = $start + 1; $j -lt $lines.Count; $j++) {
                    if ($lines[$j] -match '^\s*-\s' -and ($lines[$j].Length - $lines[$j].TrimStart().Length) -le $startIndent) { $end = $j; break }
                }
                $lines[$start..($end - 1)]
            }
            # The job's own defaults: working-directory, else the workflow's.
            $keyIndent = $lines[0].Length - $lines[0].TrimStart().Length
            $jobDefaults = [Collections.Generic.List[string]]::new()
            $inJobDefaults = $false
            foreach ($l in $lines) {
                $lineIndent = $l.Length - $l.TrimStart().Length
                if ($lineIndent -le $keyIndent) { $inJobDefaults = $l -match '^\s*defaults:\s*(#.*)?$'; continue }
                if ($inJobDefaults) { $jobDefaults.Add($l) }
            }
            $jobDefault = & $valueIn $jobDefaults '^\s*working-directory:\s*(.*)$'
            if ($null -eq $jobDefault) { $jobDefault = $workflowDefault }
            $checkouts = @(for ($i = 0; $i -lt $lines.Count; $i++) {
                if ($lines[$i] -notmatch '\buses:\s*[''"]?actions/checkout@') { continue }
                $step = @(& $stepAt $i)
                [pscustomobject]@{ Step = $step; Folder = (& $folderOf "$(& $valueIn $step '(?:^\s*(?:-\s+)?|[{,]\s*)path:\s*([^,}]+)')"); Used = $false }
            })
            # Each build step uses the checkout whose folder holds its working directory most closely. When a folder can't be placed, or no checkout holds a build's working directory, every checkout of the job counts as used.
            $everyCheckout = @($checkouts | Where-Object { $null -eq $_.Folder }).Count -gt 0
            for ($i = 0; $i -lt $lines.Count -and -not $everyCheckout; $i++) {
                if (-not (& $isBuild $lines[$i])) { continue }
                $value = & $valueIn @(& $stepAt $i) '^\s*(?:-\s+)?working-directory:\s*(.*)$'
                if ($null -eq $value) { $value = $jobDefault }
                $folder = & $folderOf "$value"
                if ($null -eq $folder) { $everyCheckout = $true; break }
                $holder = @($checkouts | Where-Object { $_.Folder -eq '' -or $folder -eq $_.Folder -or $folder.StartsWith("$($_.Folder)/") } | Sort-Object { $_.Folder.Length } -Descending)
                if ($holder.Count) { $holder[0].Used = $true } else { $everyCheckout = $true }
            }
            foreach ($checkout in $checkouts) {
                if (-not ($everyCheckout -or $checkout.Used)) { continue }
                if (-not @($checkout.Step | Where-Object { $_ -match '(?<![\w-])fetch-depth:\s*[''"]?0[''"]?(?![\w.])' }).Count) {
                    $findings.Add(".github/workflows/$($file.Name), job ${name}: actions/checkout$(if ($checkout.Folder) { " (path: $($checkout.Folder))" }) without fetch-depth: 0")
                }
            }
            foreach ($m in [regex]::Matches(($lines -join "`n"), '(?i)(?<![\w-])[-/]p(?:roperty)?:Version=\S+')) {
                $findings.Add(".github/workflows/$($file.Name), job ${name}: $($m.Value)")
            }
        }
    }
    if (-not $findings.Count) { return }
    ''
    'These workflows build with Nerdbank.GitVersioning, which version.json turns on (the template''s ADR-0138, in docs/decisions/project-template/):'
    foreach ($finding in $findings) { "  - $finding" }
    'Nerdbank.GitVersioning fails the build on a shallow clone ("Shallow clone lacks the objects required to calculate version height"), which actions/checkout makes unless fetch-depth is 0, and it ignores -p:Version, so a release would carry version.json''s version, not the tag''s. Set fetch-depth: 0 on each actions/checkout step named, drop each -p:Version, and build a release with -p:PublicRelease=true, as the publish.yml the project template gives a .NET library on GitHub does.'
}

# Render the template at a version into a folder with the project's answers;
# false, with a warning that says why, when Copier can't.
function Invoke-TemplateRender([string]$Version, [string]$Folder) {
    $renderArgs = @('copy', '--defaults', '--quiet', '--vcs-ref', $Version, '--data-file', (Join-Path $project '.copier-answers.yml'))
    foreach ($k in $Data.Keys) { $renderArgs += '--data', "$k=$(ConvertTo-CopierValue $Data[$k])" }
    if (-not $Data.ContainsKey('pre_release')) { $renderArgs += '--data', "pre_release=$(Get-PreReleaseAnswer)" }
    $renderArgs += @(Get-OptInData)
    $renderArgs += @(Get-LicenseData)
    $copierOutput = [Collections.Generic.List[string]]::new()
    try { Invoke-Copier @renderArgs $TemplateClone $Folder | ForEach-Object { $copierOutput.Add("$_") }; $true }
    catch { Write-Warning ((@("Copier couldn't render template $Version with this project's answers: $_") + $copierOutput) -join "`n"); $false }
}

function Get-Answer([string]$Key) {
    $match = Select-String -LiteralPath .copier-answers.yml -Pattern "^${Key}:\s*(.+?)\s*$" | Select-Object -First 1
    if ($match) { $match.Matches[0].Groups[1].Value.Trim("'", '"') }
}

# A list answer from .copier-answers.yml, which Copier writes as "key:" and then one "- item" line each.
function Get-AnswerList([string]$Key) {
    $inList = $false
    foreach ($line in Get-Content -LiteralPath .copier-answers.yml) {
        if ($line -match "^${Key}:\s*$") { $inList = $true; continue }
        if ($inList -and $line -match '^\s*-\s+(.+?)\s*$') { $Matches[1].Trim("'", '"') }
        elseif ($inList) { break }
    }
}

# A file list from the template's copier.yml at a version, or at the clone's HEAD when that version isn't in it.
function Get-TemplateList([string]$Repo, [string]$Version, [string]$Key) {
    $yaml = git -C $Repo show "${Version}:copier.yml" 2>$null
    if ($LASTEXITCODE) { $yaml = git -C $Repo show HEAD:copier.yml }
    $inList = $false
    foreach ($line in $yaml) {
        if ($line -match "^${Key}:") { $inList = $true; continue }
        if ($inList -and $line -match '^\s+-\s+(.+?)\s*$') { $Matches[1].Trim("'", '"') }
        elseif ($inList -and $line -match '^\S') { break }
    }
}

# Whether a project file matches one of Copier's file patterns, which Copier reads like .gitignore. A pattern with a / names the file from the project root, and one without names a file of that name in any folder.
function Test-CopierPattern([string]$File, [string[]]$Pattern) {
    foreach ($p in $Pattern) {
        if ($p.Contains('/')) { if ($File -ceq $p.TrimStart('/')) { return $true } }
        elseif (($File -split '/')[-1] -ceq $p) { return $true }
    }
    $false
}

# A template source's name as in a project: without template/, each Jinja tag and the .jinja suffix.
function ConvertTo-ProjectName([string]$Source) {
    $Source -replace '^template/' -replace ('\{' + '%.*?%' + '\}') -replace '\.jinja$'
}

# Whether a project can own a file the template renders: any but the update's own files. tests/Test-Adoption.ps1 checks that Get-OwnableFile in tools/adopt/Adoption.psm1 follows the same rule.
function Test-OwnableName([string]$Name) {
    $Name -notmatch '^\.template/' -and -not $Name.Contains('{') -and $Name -notin '.copier-answers.yml', '.template-version'
}

# The files the template renders at a version, named as in a project. A file whose name holds a condition is listed whatever the answers.
function Get-RenderedFile([string]$Repo, [string]$Version) {
    foreach ($source in git -C $Repo ls-tree -r --name-only $Version -- template/) { ConvertTo-ProjectName $source }
}

# The newest version tag, as Copier picks it: v1.2.3 or 1.2.3, prereleases ignored.
function Get-NewestTag([string]$Repo) {
    git -C $Repo tag --list | Where-Object { $_ -match '^v?\d+(\.\d+){0,3}$' } |
        Sort-Object { $v = $_ -replace '^v'; if ($v -notmatch '\.') { $v += '.0' }; [version]$v } | Select-Object -Last 1
}

# Whether Windows can hold a file or folder name, by the rules tests/Test-WindowsFileNames.ps1 checks this repository's paths with; change both together.
function Test-WindowsName([string]$Name) {
    $Name -notmatch '[\x00-\x1F<>:"|?*\\]' -and $Name -notmatch '^(?i:CON|PRN|AUX|NUL|COM[0-9\xB9\xB2\xB3]|LPT[0-9\xB9\xB2\xB3]|CONIN\$|CONOUT\$)(\..*)?$' -and $Name -notmatch '[. ]$'
}

# The first path of the template at a version that Git for Windows refuses to check out, or nothing.
function Get-WindowsCheckoutProblem([string]$Repo, [string]$Version) {
    $listing = (git -C $Repo ls-tree -r -z --name-only $Version 2>$null) -join "`n"
    if ($LASTEXITCODE) { return }
    foreach ($path in $listing -split "`0" | Where-Object { $_ }) {
        if (@($path -split '/' | Where-Object { -not (Test-WindowsName $_) }).Count) { return $path }
    }
}

function Get-Change {
    foreach ($line in git status --porcelain --untracked-files=all) {
        $code = $line.Substring(0, 2)
        $file = $line.Substring(3)
        if ($file -eq '.copier-answers.yml') { continue }
        $kind = if ($code -match 'U' -or $code -in 'AA', 'DD') { 'conflict' }
        elseif ($code -eq '??' -or $code[0] -eq 'A') { 'added' }
        elseif ($code -match 'D') { 'deleted' }
        elseif ($code -match 'R') { 'renamed' }
        else { 'modified' }
        [pscustomobject]@{ Kind = $kind; File = $file }
    }
}

# Stage the files the update added, by name from a list file, so a long list can't overflow Windows' command line. Git leaves
# out every ignored path, and with the copy of the project's .gitignore in info/exclude also one the project ignored when the
# update started, so a folder only the project's own rules ignore, such as node_modules/, is never staged.
function Add-NewFile([string]$Folder) {
    $new = @((git -c core.quotePath=false ls-files -z --others --exclude-standard) -split "`0" | Where-Object { $_ })
    if (-not $new) { return }
    New-Item -ItemType Directory -Force $Folder | Out-Null
    $list = Join-Path $Folder 'new-files.txt'
    [IO.File]::WriteAllText($list, ($new -join "`0"), [Text.UTF8Encoding]::new($false))
    git --literal-pathspecs add --pathspec-from-file=$list --pathspec-file-nul
    if ($LASTEXITCODE) { throw 'Staging the files the update added failed.' }
}

# Stage the update's changes to tracked files, deletions included, and the files it added (Add-NewFile); never git add -A.
function Add-UpdateChange([string]$Folder) {
    git add -u
    if ($LASTEXITCODE) { throw 'Staging the update failed.' }
    Add-NewFile $Folder
}

# True when the project's file already has a hunk of the template's change: the hunk applies to it in reverse.
function Test-HunkApplied([string]$File, [string[]]$Hunk, [string]$Folder) {
    $patch = Join-Path $Folder 'hunk.patch'
    [IO.File]::WriteAllText($patch, ((@("--- a/$File", "+++ b/$File") + $Hunk) -join "`n") + "`n", [Text.UTF8Encoding]::new($false))
    git apply --check --reverse -- $patch 2>$null
    -not $LASTEXITCODE
}

# The fraction of the project file's distinct, trimmed, non-blank lines that the template's file also has; 1 for a file with none.
function Get-SharedLineFraction([string]$Project, [string]$Template) {
    $templateLines = [Collections.Generic.HashSet[string]]::new([string[]]@($Template -split '\r?\n' | ForEach-Object Trim | Where-Object { $_ }))
    $projectLines = @($Project -split '\r?\n' | ForEach-Object Trim | Where-Object { $_ } | Select-Object -Unique)
    if (-not $projectLines.Count) { return 1.0 }
    [double]@($projectLines | Where-Object { $templateLines.Contains($_) }).Count / $projectLines.Count
}

# The files, of those given, that share less than half their lines with the template's render of them in $Folder, leaving out the lines an adopted file keeps under its "# Kept from" comment.
function Get-RewrittenFile([string[]]$Files, [string]$Folder) {
    foreach ($file in $Files) {
        $rendered = Join-Path $Folder $file
        if (-not (Test-Path -LiteralPath $rendered -PathType Leaf)) { continue }
        $text = [regex]::Split([IO.File]::ReadAllText((Join-Path $project $file)), "(?m)^# Kept from this repository's own \S+ when it adopted the project template\r?$")[0]
        if ((Get-SharedLineFraction $text ([IO.File]::ReadAllText($rendered))) -lt 0.5) { $file }
    }
}

# How to run the template's .githooks in this clone: HooksPath when core.hooksPath can point at it, otherwise the
# Note to warn with. A hooks path turns off the hooks in .git/hooks.
# A hooks path the clone already has, from any Git config, is never changed: one that names a .githooks folder runs the
# template's hooks, and any other, such as husky's or a global hooks folder, gets a note naming the command to run.
# With "- protect: hook" in AGENTS.md's Landing section, it also says how to run .githooks/pre-push until something does.
# This is a deliberate copy of Get-GitHooksSetup in the template's tools/adopt/Adoption.psm1, not shared code: this
# script runs on its own, from an older template's copy. Change both together, or Update and Adopt can decide
# differently for the same clone; tests/Test-GitHooks.ps1 runs both copies through the same cases.
function Get-GitHooksSetup {
    $config = '.pre-commit-config.yaml'
    $hooksDir = git rev-parse --path-format=absolute --git-path hooks
    $hooksRel = git rev-parse --git-path hooks
    $ownHooks = @(Get-ChildItem -LiteralPath $hooksDir -File -ErrorAction SilentlyContinue | Where-Object Extension -NE '.sample')
    $preCommit = (Test-Path -LiteralPath $config) -or [bool]($ownHooks | Select-String -SimpleMatch 'File generated by pre-commit' -List)

    # The value of the "- protect:" line in AGENTS.md's Landing section, when it has one.
    $protect = $null
    if (Test-Path -LiteralPath AGENTS.md) {
        $inLanding = $false
        foreach ($line in Get-Content -LiteralPath AGENTS.md) {
            if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
            if ($inLanding -and $line -match '^## ') { break }
            if ($inLanding -and $line -match '^- protect:\s*(\S+)') { $protect = $Matches[1]; break }
        }
    }

    $current = git config --get core.hooksPath
    if ($current) {
        if ((Split-Path -Leaf $current.TrimEnd('\', '/')) -eq '.githooks') { return [pscustomobject]@{ HooksPath = $false; Note = $null } }
        $push = if ($protect -eq 'hook') { ' as AGENTS.md''s Landing section says (protect: hook)' } else { ' when AGENTS.md''s Landing section says protect: hook' }
        return [pscustomobject]@{ HooksPath = $false; Note = "Git hooks: .githooks not enabled. core.hooksPath is already $current (git config --show-origin core.hooksPath says where it's set), so Git never runs .githooks/pre-push, which refuses a push to the default branch$push. To run both, call .githooks/pre-push from $current/pre-push. To run the template's hooks instead, run: git config core.hooksPath .githooks" }
    }

    # True when the clone's pre-push hook, or the pre-push.legacy that pre-commit runs first, calls .githooks/pre-push.
    $pushRuns = $false
    foreach ($name in 'pre-push', 'pre-push.legacy') {
        $file = Join-Path $hooksDir $name
        if ((Test-Path -LiteralPath $file) -and (Select-String -LiteralPath $file -SimpleMatch '.githooks/pre-push' -Quiet)) { $pushRuns = $true }
    }

    $parts = @()
    if (-not $preCommit -and $ownHooks) {
        $parts += "Git hooks: .githooks not enabled. This clone has hooks of its own in .git/hooks ($(($ownHooks | ForEach-Object Name) -join ', ')), and core.hooksPath would turn them off. To run both, call .githooks/pre-push from your pre-push hook, or move your hooks into .githooks and run: git config core.hooksPath .githooks"
    }
    if (($preCommit -or $ownHooks) -and $protect -eq 'hook' -and -not $pushRuns) {
        $pushHook = Join-Path $hooksDir 'pre-push'
        $lead = 'Git hooks: AGENTS.md''s Landing section says protect: hook, but nothing runs .githooks/pre-push in this clone.'
        if ((Test-Path -LiteralPath $pushHook) -and -not (Select-String -LiteralPath $pushHook -SimpleMatch 'File generated by pre-commit' -Quiet)) {
            $parts += "$lead Call .githooks/pre-push from $hooksRel/pre-push with the same arguments and standard input, and refuse the push when it fails."
        }
        elseif ((Test-Path -LiteralPath $pushHook) -and (Test-Path -LiteralPath (Join-Path $hooksDir 'pre-push.legacy'))) {
            # pre-commit moved a hook of the clone's own aside as pre-push.legacy, so the wrapper must not replace it.
            $parts += "$lead Call .githooks/pre-push from $hooksRel/pre-push.legacy with the same arguments and standard input, and refuse the push when it fails."
        }
        else {
            $legacy = Test-Path -LiteralPath $pushHook
            $target = if ($legacy) { "$hooksRel/pre-push.legacy" } else { "$hooksRel/pre-push" }
            $aside = if ($legacy) { ' (pre-commit''s own pre-push hook runs it first, with the same arguments and refs)' } else { '' }
            $parts += @("$lead pre-commit's pre-push stage checks only one of the refs a push updates, so run the hook from Git: save these two lines as $target$aside and, on Linux or macOS, run: chmod +x $target",
                '#!/usr/bin/env sh', 'exec .githooks/pre-push "$@"') -join "`n"
        }
    }
    [pscustomobject]@{ HooksPath = -not $preCommit -and -not $ownHooks; Note = if ($parts) { $parts -join "`n" } else { $null } }
}

# What a project does after an update removes the Commit attribution check (the template's ADR-0018): a ruleset that still
# requires the check blocks every pull request, and a config or hook that still calls the removed commit-msg hook fails every commit.
function Get-AttributionCheckNote([object[]]$Changes) {
    if (-not ($Changes | Where-Object { $_.Kind -eq 'deleted' -and $_.File -eq '.template/Test-CommitAttribution.ps1' })) { return }
    ''
    'The template no longer has the Commit attribution check or the .githooks/commit-msg hook (the template''s ADR-0018). If the default branch''s ruleset or branch protection requires a check named "Commit attribution", remove it from the required checks, or every pull request waits for a check that never reports: on GitHub in Settings > Rules > Rulesets, or for a "Protect main" ruleset tools/Set-RepositoryBaseline.ps1 made, by running it again from the template''s clone with -RequireChecks naming the checks to keep, such as -RequireChecks ''build-and-test''.'
    $hooksDir = git rev-parse --path-format=absolute --git-path hooks
    $hooksRel = git rev-parse --git-path hooks
    $callers = @()
    if ((Test-Path -LiteralPath .pre-commit-config.yaml) -and (Select-String -LiteralPath .pre-commit-config.yaml -SimpleMatch '.githooks/commit-msg' -Quiet)) { $callers += '.pre-commit-config.yaml' }
    $callers += @(Get-ChildItem -LiteralPath $hooksDir -File -ErrorAction SilentlyContinue | Where-Object Extension -NE '.sample' |
            Where-Object { Select-String -LiteralPath $_.FullName -SimpleMatch '.githooks/commit-msg' -Quiet } |
            ForEach-Object { "$hooksRel/$($_.Name)" })
    if ($callers) { "These files still call .githooks/commit-msg, which this update removed, so every commit fails until you delete that call: $($callers -join ', ')" }
}

# The *.cs files under the current folder that don't start with the header .editorconfig's file_header_template sets,
# as relative paths with /: what IDE0073 fails once a project turns csharp_format on (the template's ADR-0148). Nothing
# when .editorconfig sets no header. Generated code is skipped, as IDE0073 skips it.
# tools/adopt/Adoption.psm1 in the template has a deliberate copy of this function, not shared code: this script runs on its own, from an older template's copy. Change both together; tests/Test-LibraryRelease.ps1 keeps the two equal.
function Get-CSharpFileWithoutHeader {
    if (-not (Test-Path -LiteralPath .editorconfig -PathType Leaf)) { return }
    $setting = [regex]::Match([IO.File]::ReadAllText((Join-Path (Get-Location).Path '.editorconfig')), '(?m)^file_header_template\s*=\s*(.+?)\r?$')
    if (-not $setting.Success) { return }
    $header = (($setting.Groups[1].Value -split '\\n') | ForEach-Object { "// $_" }) -join "`n"
    foreach ($file in Get-ChildItem -LiteralPath . -Filter *.cs -File -Recurse -ErrorAction SilentlyContinue) {
        if ($file.FullName -match '[\\/](bin|obj|node_modules|artifacts|\.git|Migrations)[\\/]' -or $file.Name -match '\.(g|generated|designer)\.cs$') { continue }
        $text = ([IO.File]::ReadAllText($file.FullName).TrimStart([char]0xFEFF)) -replace "`r`n", "`n"
        if (-not $text.StartsWith("$header`n")) { [IO.Path]::GetRelativePath((Get-Location).Path, $file.FullName) -replace '\\', '/' }
    }
}

# The changelog AGENTS.md's Landing section names (CHANGELOG.md by default) as File, and as Range the version tags merged into HEAD
# that have no release heading in it: <oldest>..<newest>, the tag alone, or -. .template/Get-PlanState.ps1 prints Range as changelog-backfill.
# With pre-release: untracked in the Landing section, a tag below 1.0.0 needs no section.
# This is a deliberate copy of that logic, not shared code: Update-Project.ps1 runs on its own, from an older template's copy, before any workflow script exists to dot-source.
# Change the release-heading pattern and the merged-tag filter in both places, or the request printed here can disagree with changelog-backfill.
function Get-ChangelogBackfill {
    $file = 'CHANGELOG.md'
    $untracked = $false
    if (Test-Path -LiteralPath AGENTS.md) {
        $inLanding = $false
        foreach ($line in Get-Content -LiteralPath AGENTS.md) {
            if ($line -match '^## Landing\s*$') { $inLanding = $true; continue }
            if ($inLanding -and $line -match '^## ') { break }
            if ($inLanding -and $line -match '^- changelog:\s*`?([^`\s]+)') { $file = $Matches[1] }
            if ($inLanding -and $line -match '^- pre-release:\s*`?untracked`?\s*$') { $untracked = $true }
        }
    }
    $listed = @(if (Test-Path -LiteralPath $file -PathType Leaf) { foreach ($line in Get-Content -LiteralPath $file) { if ($line -match '^## \[?v?(\d+\.\d+\.\d+)\]?(?: - \d{4}-\d{2}-\d{2})?(?: \[YANKED\])?\s*$') { [version]$Matches[1] } } })
    $missing = @(git tag --list --merged HEAD 2>$null | Where-Object { $_ -match '^v?(\d+\.\d+\.\d+)$' -and [version]$Matches[1] -notin $listed -and -not ($untracked -and ([version]$Matches[1]).Major -lt 1) } | Sort-Object { [version]($_ -replace '^v') })
    $range = if ($missing.Count -eq 0) { '-' } elseif ($missing.Count -eq 1) { $missing[0] } else { "$($missing[0])..$($missing[-1])" }
    [pscustomobject]@{ File = $file; Range = $range }
}

# Deliberate copies of tools/adopt/Adoption.psm1's functions, as for Get-ChangelogBackfill: change both, or tests\Test-LibraryRelease.ps1 fails.
function Get-NuGetPublisher {
    if (-not (Test-Path -LiteralPath .github/workflows -PathType Container)) { return }
    $found = foreach ($file in Get-ChildItem -LiteralPath .github/workflows -File) {
        if ($file.Extension -in '.yml', '.yaml' -and $file.Name -ne 'publish.yml' -and (Get-Content -LiteralPath $file.FullName -Raw) -match '(?i)\bnuget(?:\.exe)?\s+push\b') { ".github/workflows/$($file.Name)" }
    }
    @($found) | Sort-Object
}

# The nuget_publisher answer: the workflows other than publish.yml that run nuget push, joined with ", ", when the library has no publish.yml,
# else blank. tools/adopt/Adoption.psm1 has a deliberate copy of this function for the template's Adopt-Project.ps1. Change both together; tests/Test-UpdateNotes.ps1 runs both copies through the same cases.
function Get-NuGetPublisherAnswer {
    if (Test-Path -LiteralPath .github/workflows/publish.yml) { return '' }
    @(Get-NuGetPublisher) -join ', '
}

function Get-AssemblyVersionFloor {
    if (-not (Test-Path -LiteralPath build/ProjectTemplate.props -PathType Leaf)) { return }
    if ((Get-Content -LiteralPath build/ProjectTemplate.props -Raw) -notmatch 'Name="ProjectTemplatePinAssemblyVersion"') { return }
    if ((Test-Path -LiteralPath Directory.Build.props -PathType Leaf) -and (Get-Content -LiteralPath Directory.Build.props -Raw) -match '<AssemblyVersion[\s>]') { return }
    $tags = @(git tag --list --merged HEAD 2>$null | Where-Object { $_ -match '^v?(\d+\.\d+\.\d+)(?:-[0-9A-Za-z.-]+)?$' } |
        Sort-Object { [version]($_ -replace '^v' -replace '-.*$') })
    if ($tags.Count -eq 0) { return }
    $major = ([version]($tags[-1] -replace '^v' -replace '-.*$')).Major
    $best = $null
    $bestTag = $null
    foreach ($tag in $tags) {
        $number = [version]($tag -replace '^v' -replace '-.*$')
        if ($number.Major -ne $major) { continue }
        $shipped = $null
        $props = git show "${tag}:Directory.Build.props" 2>$null
        if ($LASTEXITCODE -eq 0 -and ($props -join "`n") -match '<AssemblyVersion>\s*(\d+(?:\.\d+){1,3})\s*</AssemblyVersion>') { $shipped = [version]$Matches[1] }
        if (-not $shipped) {
            $template = git show "${tag}:build/ProjectTemplate.props" 2>$null
            $shipped = if ($LASTEXITCODE -eq 0 -and ($template -join "`n") -match 'Name="ProjectTemplatePinAssemblyVersion"') { [version]::new($major, 0, 0, 0) } else { $number }
        }
        $shipped = [version]::new($shipped.Major, $shipped.Minor, [Math]::Max($shipped.Build, 0), [Math]::Max($shipped.Revision, 0))
        if (-not $best -or $shipped -gt $best) { $best = $shipped; $bestTag = $tag }
    }
    if ($best -gt [version]::new($major, 0, 0, 0)) { [pscustomobject]@{ Tag = $bestTag; AssemblyVersion = $best.ToString(); Major = $major } }
}

function Get-PublishedPackageVersion {
    param([scriptblock]$Fetch = { param($Uri) Invoke-RestMethod -Uri $Uri -TimeoutSec 20 })
    foreach ($file in Get-ChildItem -LiteralPath . -Filter *.csproj -File -Recurse -ErrorAction SilentlyContinue) {
        if ($file.FullName -match '[\\/](bin|obj|node_modules|artifacts|\.git)[\\/]' -or $file.Name -like '*.Tests.csproj') { continue }
        $text = Get-Content -LiteralPath $file.FullName -Raw
        if ($text -match '<IsPackable>\s*false') { continue }
        $id = if ($text -match '<PackageId>\s*([^<\s]+)\s*</PackageId>') { $Matches[1] } else { $file.BaseName }
        try { $response = & $Fetch "https://api.nuget.org/v3-flatcontainer/$($id.ToLowerInvariant())/index.json" } catch { continue }
        $stable = @($response.versions | Where-Object { $_ -and $_ -notmatch '-' } | Sort-Object { [version]$_ })
        if ($stable.Count -gt 0) { [pscustomobject]@{ Id = $id; Version = $stable[-1] } }
    }
}

# A deliberate copy of tools/adopt/Adoption.psm1's Get-PackageMetadataDrift: change both, or tests\Test-LibraryRelease.ps1 fails.
function Get-PackageMetadataDrift {
    param(
        [scriptblock]$Fetch = { param($Uri) Invoke-RestMethod -Uri $Uri -TimeoutSec 20 },
        [scriptblock]$Evaluate = {
            param($Project)
            try {
                $json = & dotnet msbuild $Project -getProperty:Description -getProperty:PackageTags -nologo 2>$null
                if ($LASTEXITCODE -ne 0) { return $null }
                $properties = ($json -join "`n" | ConvertFrom-Json).Properties
                @{ Description = $properties.Description; PackageTags = $properties.PackageTags }
            }
            catch { $null }
        }
    )
    $root = (Get-Location).ProviderPath
    foreach ($file in Get-ChildItem -LiteralPath . -Filter *.csproj -File -Recurse -ErrorAction SilentlyContinue) {
        if ($file.FullName -match '[\\/](bin|obj|node_modules|artifacts|\.git)[\\/]' -or $file.Name -like '*.Tests.csproj') { continue }
        $text = Get-Content -LiteralPath $file.FullName -Raw
        if ($text -match '<IsPackable>\s*false') { continue }
        $id = if ($text -match '<PackageId>\s*([^<\s]+)\s*</PackageId>') { $Matches[1] } else { $file.BaseName }
        $lower = $id.ToLowerInvariant()
        try {
            $response = & $Fetch "https://api.nuget.org/v3-flatcontainer/$lower/index.json"
            $stable = @($response.versions | Where-Object { $_ -and $_ -notmatch '-' } | Sort-Object { [version]$_ })
            if ($stable.Count -eq 0) { continue }
            $version = $stable[-1]
            $nuspec = & $Fetch "https://api.nuget.org/v3-flatcontainer/$lower/$version/$lower.nuspec"
            if ($nuspec -is [string]) { $nuspec = [xml]$nuspec }
            $metadata = $nuspec.package.metadata
            $publishedValues = @{ Description = [string]$metadata.description; PackageTags = [string]$metadata.tags }
            $local = & $Evaluate $file.FullName
        }
        catch { continue }
        if (-not $local) { continue }
        $relative = [IO.Path]::GetRelativePath($root, $file.FullName) -replace '\\', '/'
        foreach ($field in 'Description', 'PackageTags') {
            $publishedText = ($publishedValues[$field] -replace '\s+', ' ').Trim()
            $localText = ([string]$local[$field] -replace '\s+', ' ').Trim()
            if (-not $publishedText) { continue }
            if ($field -eq 'PackageTags') {
                $publishedKey = (@($publishedText -split '[;,\s]+' | Where-Object { $_ } | Sort-Object -Unique) -join ' ')
                $localKey = (@($localText -split '[;,\s]+' | Where-Object { $_ } | Sort-Object -Unique) -join ' ')
            }
            else { $publishedKey = $publishedText; $localKey = $localText }
            if ($publishedKey -ne $localKey) {
                [pscustomobject]@{ Id = $id; Version = $version; Field = $field; Message = "$relative would publish $field '$localText' where $id $version published '$publishedText': restore it in the project or Directory.Build.props (NCodeGroup/Buffers#33 lost both when a .props file stopped being imported)." }
            }
        }
    }
}

# A deliberate copy of tools/adopt/Adoption.psm1's Get-NuGetLogistics: change both, or tests\Test-NuGetLogistics.ps1 fails.
function Get-NuGetLogistics {
    $root = (Get-Location).ProviderPath
    $excluded = 'bin', 'obj', 'node_modules', 'artifacts', '.git'
    $found = [Collections.Generic.List[object]]::new()
    $pending = [Collections.Generic.Stack[string]]::new()
    $pending.Push($root)
    while ($pending.Count) {
        foreach ($entry in Get-ChildItem -LiteralPath $pending.Pop() -Force -ErrorAction SilentlyContinue) {
            if ($entry.PSIsContainer) {
                if ($entry.Name -notin $excluded) { $pending.Push($entry.FullName) }
                continue
            }
            $path = $entry.FullName.Substring($root.Length).TrimStart('\', '/').Replace('\', '/')
            $found.Add([pscustomobject]@{ Path = $path; Name = $entry.Name; Full = $entry.FullName })
        }
    }
    $files = @($found | Sort-Object { $_.Path } -CaseSensitive)
    $projectFiles = @($files | Where-Object { $_.Path -ne 'build/ProjectTemplate.props' -and $_.Name -match '\.(csproj|props)$' })
    $mentioning = @($files | Where-Object { $_.Name -match '\.(csproj|props|targets|slnx)$' } | ForEach-Object { [pscustomobject]@{ Path = $_.Path; Text = Get-Content -LiteralPath $_.Full -Raw } })

    foreach ($file in $files | Where-Object { $_.Name -like '*.props' -and $_.Name -notlike 'Directory.*.props' -and $_.Name -ne 'ProjectTemplate.props' }) {
        if ($mentioning | Where-Object { $_.Path -ne $file.Path -and $_.Text -and $_.Text.IndexOf($file.Name, [StringComparison]::OrdinalIgnoreCase) -ge 0 }) { continue }
        $text = Get-Content -LiteralPath $file.Full -Raw
        $settings = @('Description', 'PackageTags', 'Authors', 'PackageProjectUrl', 'PackageReleaseNotes', 'PackageIcon' | Where-Object { $text -match "<$_[\s>]" })
        $message = if ($settings.Count) { "$($file.Path) is imported by no project, so the $($settings -join ', ') it sets no longer reach the package: import it from Directory.Build.props, after the import of build/ProjectTemplate.props, or move its settings into the project file." }
        else { "$($file.Path) is imported by no project: import it or delete it." }
        [pscustomobject]@{ Kind = 'OrphanProps'; Message = $message }
    }

    foreach ($file in $projectFiles) {
        if ((Get-Content -LiteralPath $file.Full -Raw) -match '<GeneratePackageOnBuild>\s*true') {
            [pscustomobject]@{ Kind = 'GeneratePackageOnBuild'; Message = "$($file.Path) sets GeneratePackageOnBuild to true, which races with dotnet pack from a clean tree (NU5026): delete it; build/dod.ps1 packs on its own." }
        }
    }

    $elements = 'ImplicitUsings', 'Nullable', 'LangVersion', 'PublishRepositoryUrl', 'EmbedUntrackedSources', 'IncludeSymbols', 'SymbolPackageFormat', 'DocumentationFile'
    foreach ($file in $projectFiles | Where-Object { $_.Name -like '*.csproj' -or $_.Name -in 'Directory.Build.props', 'Directory.Packages.props' }) {
        $text = Get-Content -LiteralPath $file.Full -Raw
        $repeated = @($elements | Where-Object { $text -match "<$_[\s>]" })
        if ($text -match '<Package(Reference|Version)\b[^>]*\bInclude\s*=\s*"Microsoft\.SourceLink\.GitHub"') { $repeated += 'Microsoft.SourceLink.GitHub' }
        if ($repeated.Count) {
            [pscustomobject]@{ Kind = 'RepeatedSettings'; Message = "$($file.Path) repeats settings build/ProjectTemplate.props makes: $($repeated -join ', '). Delete them, or keep one only to differ from the template on purpose." }
        }
    }

    foreach ($file in $files | Where-Object { $_.Name -like '*.sln' }) {
        $folder = if ($file.Path.Contains('/')) { $file.Path.Substring(0, $file.Path.LastIndexOf('/') + 1) } else { '' }
        if (($file.Path.Split('/').Count -le 3) -and -not ($files | Where-Object { $_.Name -like '*.slnx' -and $_.Path -eq "$folder$($_.Name)" })) {
            $name = [IO.Path]::GetFileNameWithoutExtension($file.Name)
            [pscustomobject]@{ Kind = 'Solution'; Message = "$($file.Path) is a .sln where the template and current SDKs use .slnx: run dotnet sln $($file.Path) migrate, delete the .sln, and rename $name.sln.DotSettings to $name.slnx.DotSettings if it exists." }
        }
    }

    $bomExtensions = '.cs', '.csproj', '.props', '.targets', '.slnx', '.json', '.md', '.yml', '.yaml', '.xml', '.txt', '.editorconfig'
    $withBom = [Collections.Generic.List[string]]::new()
    foreach ($file in $files | Where-Object { [IO.Path]::GetExtension($_.Name) -in $bomExtensions }) {
        $bytes = [byte[]]::new(3)
        try {
            $stream = [IO.File]::Open($file.Full, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::ReadWrite)
            try { $read = $stream.Read($bytes, 0, 3) } finally { $stream.Dispose() }
        }
        catch [IO.IOException], [UnauthorizedAccessException] {
            Write-Verbose "Skipped $($file.Path), which can't be read for a byte order mark: $($_.Exception.Message)"
            continue
        }
        if ($read -eq 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF) { $withBom.Add($file.Path) }
    }
    if ($withBom.Count) {
        $listed = ($withBom | Select-Object -First 5) -join ', '
        if ($withBom.Count -gt 5) { $listed += ", and $($withBom.Count - 5) more" }
        [pscustomobject]@{ Kind = 'ByteOrderMark'; Message = "$($withBom.Count) file(s) start with a UTF-8 byte order mark, which the template's encoding rule (UTF-8 without BOM) forbids: $listed. Save them without it." }
    }

    $rootProjects = @($files | Where-Object { $_.Name -like '*.csproj' -and -not $_.Path.Contains('/') })
    if ($rootProjects.Count) {
        [pscustomobject]@{ Kind = 'RootProject'; Message = "$($rootProjects.Name -join ', ') $(if ($rootProjects.Count -eq 1) { 'is' } else { 'are' }) at the repository root: move each project into src/ (a test project into tests/), as the template's layout has it." }
    }
}

# Whether Directory.Build.props, or a project file under the current folder, already sets PackageValidationBaselineVersion.
# tools/adopt/Adoption.psm1 in the template has a deliberate copy of this function, not shared code: this script runs on its own, from an older template's copy. Change both together; tests/Test-LibraryRelease.ps1 keeps the two equal.
function Test-PackageValidationBaseline {
    if ((Test-Path -LiteralPath Directory.Build.props -PathType Leaf) -and (Get-Content -LiteralPath Directory.Build.props -Raw) -match 'PackageValidationBaselineVersion') { return $true }
    @(Get-ChildItem -LiteralPath . -Filter *.csproj -File -Recurse -ErrorAction SilentlyContinue | Where-Object { $_.FullName -notmatch '[\\/](bin|obj|node_modules|artifacts|\.git)[\\/]' -and (Get-Content -LiteralPath $_.FullName -Raw) -match 'PackageValidationBaselineVersion' }).Count -gt 0
}

# A deliberate copy of tools/adopt/Adoption.psm1's Update-VersionFile: change both, or tests\Test-LibraryRelease.ps1 fails.
function Update-VersionFile {
    $path = Join-Path (Get-Location) 'version.json'
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return }
    $text = [IO.File]::ReadAllText($path)
    $result = [pscustomobject]@{ Version = $null; Tag = $null; AssemblyVersion = $null; Precision = $null }
    $tag = @(git tag --list --merged HEAD 2>$null | Where-Object { $_ -match '^v?\d+\.\d+\.\d+$' } | Sort-Object { [version]($_ -replace '^v') }) | Select-Object -Last 1
    if ($tag) {
        $number = [version]($tag -replace '^v')
        $version = "$($number.Major).$($number.Minor).$($number.Build + 1)"
        $updated = [regex]::Replace($text, '(?m)^  "version": "[^"]*"', "  `"version`": `"$version`"")
        if ($updated -ne $text) { $text = $updated; $result.Version = $version; $result.Tag = $tag }
    }
    $shipped = $null
    if ((Test-Path -LiteralPath Directory.Build.props -PathType Leaf) -and (Get-Content -LiteralPath Directory.Build.props -Raw) -match '<AssemblyVersion>\s*(\d+(?:\.\d+){1,3})\s*</AssemblyVersion>') { $shipped = $Matches[1] }
    if (-not $shipped) { $shipped = (Get-AssemblyVersionFloor).AssemblyVersion }
    if ($shipped) {
        $number = [version]$shipped
        $parts = @($number.Major, $number.Minor, [Math]::Max($number.Build, 0), [Math]::Max($number.Revision, 0))
        if ($parts[3] -gt 0) { $assembly = $parts -join '.'; $precision = 'revision' }
        elseif ($parts[2] -gt 0) { $assembly = $parts[0..2] -join '.'; $precision = 'build' }
        else { $assembly = $parts[0..1] -join '.'; $precision = 'minor' }
        $updated = $text.Replace('"assemblyVersion": { "precision": "major" }', "`"assemblyVersion`": { `"version`": `"$assembly`", `"precision`": `"$precision`" }")
        if ($updated -ne $text) { $text = $updated; $result.AssemblyVersion = $assembly; $result.Precision = $precision }
    }
    if ($result.Version -or $result.AssemblyVersion) { [IO.File]::WriteAllText($path, $text, [Text.UTF8Encoding]::new($false)) }
    $result
}

# The files, other than the template's own, that reference Nerdbank.GitVersioning, when build/ProjectTemplate.props references it too, as a library's does once version.json exists (the template's ADR-0138). Without that reference, as in a project that isn't a library, the project's own reference is its only one: nothing to name.
function Get-GitVersioningReference {
    if (-not (Test-Path -LiteralPath build/ProjectTemplate.props -PathType Leaf) -or (Get-Content -LiteralPath build/ProjectTemplate.props -Raw) -notmatch '<PackageReference\s+Include="Nerdbank\.GitVersioning"') { return }
    $found = foreach ($file in @(git ls-files -- '*.props' '*.targets' '*.csproj') | Where-Object { $_ -ne 'build/ProjectTemplate.props' }) {
        $text = Get-Content -LiteralPath $file -Raw
        if ($file -eq 'Directory.Packages.props') {
            if ([regex]::Matches($text, '<PackageVersion\s+Include="Nerdbank\.GitVersioning"').Count -gt 1) { $file }
        }
        elseif ($text -match '<PackageReference\s+Include="Nerdbank\.GitVersioning"') { $file }
    }
    @($found) | Sort-Object
}

# The project's own build scripts that run CSharpier's check, which build/dod.ps1 runs in its Format stage once .config/dotnet-tools.json names CSharpier (ADR-0148): such a wrapper checks the formatting twice. Only a command that runs the check counts, found through PowerShell's parser, so a comment, comment-based help or a string that names it doesn't. A script with a syntax error is searched as far as the parser recovers, without a warning, since a broken wrapper fails on its own.
function Get-CSharpierWrapper([string[]]$TemplateFiles) {
    if (-not (Test-Path -LiteralPath .config/dotnet-tools.json -PathType Leaf) -or (Get-Content -LiteralPath .config/dotnet-tools.json -Raw) -notmatch '(?i)csharpier') { return }
    foreach ($file in @(Get-ChildItem -LiteralPath build -Filter *.ps1 -File -ErrorAction SilentlyContinue | Sort-Object Name)) {
        $relative = "build/$($file.Name)"
        if ($relative -in $TemplateFiles) { continue }
        $script = [Management.Automation.Language.Parser]::ParseFile($file.FullName, [ref]$null, [ref]$null)
        $run = $script.Find({
                param($node)
                if ($node -isnot [Management.Automation.Language.CommandAst]) { return $false }
                $words = @($node.CommandElements | ForEach-Object { if ($_ -is [Management.Automation.Language.StringConstantExpressionAst]) { $_.Value } else { '' } })
                $at = 0
                if ($words[0] -in 'dotnet', 'dotnet.exe') { $at = if ($words[1] -ceq 'tool' -and $words[2] -ceq 'run') { 3 } else { 1 } }
                $words.Count -ge $at + 2 -and $words[$at] -ceq 'csharpier' -and $words[$at + 1] -ceq 'check'
            }, $true)
        if ($run) { $relative }
    }
}

$project = (Resolve-Path -LiteralPath $Path).Path
$ownClone = -not $TemplateClone
$scratch = Join-Path ([IO.Path]::GetTempPath()) "project-template-update-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
$outputEncoding = [Console]::OutputEncoding
$restoreExclude = $false
Push-Location -LiteralPath $project
try {
    # Read Git's output as UTF-8, so file contents and diffs keep their characters.
    try { [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false) } catch { Write-Verbose "Couldn't set the console's output encoding: $_" }
    if (-not (Test-Path -LiteralPath .copier-answers.yml)) { throw "$project has no .copier-answers.yml, so it wasn't created from the template." }
    if (git status --porcelain) { throw "$project has uncommitted changes. Commit or stash them first." }
    # copier.yml's license validator refuses a NuGet library without a license: stop before anything changes, naming the -Data that gives one.
    $nugetAnswer = if ($Data.ContainsKey('nuget_package')) { "$($Data['nuget_package'])" } else { Get-Answer 'nuget_package' }
    $licenseAnswer = if ($Data.ContainsKey('license')) { "$($Data['license'])" } else { Get-Answer 'license' }
    if ($nugetAnswer -eq 'true' -and -not "$licenseAnswer".Trim()) { throw "A NuGet package needs a license, and $project publishes NuGet packages without a license answer. Run the update with the SPDX license expression its packages carry: .\.template\Update-Project.ps1 -Data @{ license = '<SPDX expression, such as MIT or Apache-2.0>' }" }
    $head = git rev-parse HEAD
    $from = Get-Answer '_commit'
    $src = Get-Answer '_src_path'
    # The template moved from polewskm/project-template to NCodeGroup/project-template. A project whose answers still name the old
    # address moves over with this update, so it stops depending on GitHub's redirect. Only the _src_path line changes, and the
    # update commits it with everything else. Get-TemplateUpdate.ps1 reads the same old address for the weekly notice;
    # change both together.
    $movedSource = 'https://github.com/NCodeGroup/project-template.git'
    if ($src -match '^https://github\.com/polewskm/project-template(?:\.git)?/?$') {
        $answersFile = Join-Path $project '.copier-answers.yml'
        $answersText = [IO.File]::ReadAllText($answersFile)
        [IO.File]::WriteAllText($answersFile, [regex]::Replace($answersText, '(?m)^_src_path:[^\r\n]*', "_src_path: $movedSource"), [Text.UTF8Encoding]::new($false))
        $src = $movedSource
        "The template moved to $movedSource; .copier-answers.yml now names it."
    }

    if ($ownClone) {
        $TemplateClone = Join-Path ([IO.Path]::GetTempPath()) "project-template-$([guid]::NewGuid().ToString('N').Substring(0, 8))"
        # A full clone: Copier renders versions from it and can't clone a blob-filtered clone; the template is small.
        git clone --quiet --no-checkout -- $src $TemplateClone 2>$null
        if ($LASTEXITCODE) { throw "Can't read the project template at $src. Updating needs read access to it; for its owner, Git's credentials (gh auth setup-git). Without access the project still works as it is: only template updates are unavailable." }
    }
    $to = if ($Ref) { $Ref } else { Get-NewestTag $TemplateClone }
    if (-not $to) { throw "The template at $src has no version tags." }

    # The update commits, and the default branch may accept changes only through pull requests.
    $branch = git branch --show-current
    $defaultRef = git symbolic-ref --quiet --short refs/remotes/origin/HEAD 2>$null
    $originBranch = if ($branch -in 'main', 'master') { git rev-parse --quiet --verify "refs/remotes/origin/$branch" 2>$null }
    $default = if ($defaultRef) { $defaultRef -replace '^origin/' } elseif ($originBranch) { $branch }
    if ($branch -and $branch -eq $default -and -not $NoCommit) { throw "$project is on $branch, its default branch, and the update commits. Update on a branch of its own: git switch -c build/project-template-$to — or stage the update here with -NoCommit." }

    # Hand over to the target version's copy of this script when it differs, so the newest update logic runs.
    # git show, not git checkout: checkout doesn't fetch the file in a blob-filtered clone, but show does.
    if (-not $HandedOff) {
        $nextText = (git -C $TemplateClone show "${to}:template/.template/Update-Project.ps1" 2>$null) -join "`n"
        if (-not $LASTEXITCODE -and $nextText -and $nextText.TrimEnd() -ne [IO.File]::ReadAllText($PSCommandPath).Replace("`r`n", "`n").TrimEnd()) {
            New-Item -ItemType Directory -Force $scratch | Out-Null
            $next = Join-Path $scratch 'Update-Project.ps1'
            [IO.File]::WriteAllText($next, "$nextText`n", [Text.UTF8Encoding]::new($false))
            "Handing the update over to the $to version of Update-Project.ps1."
            & $next -Path $project -Ref $to -Data $Data -NoCommit:$NoCommit -TemplateClone $TemplateClone -HandedOff
            exit $LASTEXITCODE
        }
    }

    # copier recopy and the renders below check out the project's version, which Git for Windows refuses when a name
    # in it is one Windows can't hold, such as the uses_node file names of v0.69.0 to v0.75.0: stop before any of them runs.
    if ($IsWindows) {
        $badPath = Get-WindowsCheckoutProblem $TemplateClone $from
        if ($badPath) { throw "This project is on template $from, which Git for Windows can't check out: it refuses the path $badPath. The update starts by resetting the template-owned files to $from, so it can't run on Windows. Update once from Linux, macOS or WSL, such as with pwsh ./.template/Update-Project.ps1 in WSL; later updates run on Windows again." }
    }

    $skipFrom = @(Get-TemplateList $TemplateClone $from '_skip_if_exists')
    $projectOwned = @($skipFrom | ForEach-Object { $_.TrimStart('/') })
    $skipTo = @(Get-TemplateList $TemplateClone $to '_skip_if_exists')
    $merged = @(Get-TemplateList $TemplateClone $from '_merged_files')
    # Files the project owns (its project_owned_files answer), merged or template-owned: updates leave them alone and show the template's change, as for project-owned files.
    # -Data wins, because this update records it, and a single file becomes the list the answer must be.
    if ($Data.ContainsKey('project_owned_files')) { $Data['project_owned_files'] = @($Data['project_owned_files'] | Where-Object { $_ } | Select-Object -Unique) }
    $ownedFiles = @(if ($Data.ContainsKey('project_owned_files')) { $Data['project_owned_files'] } else { Get-AnswerList 'project_owned_files' })
    # Checked against the version the update moves to, so a file that version first adds or merges can be owned from this update on; the update's own files can't be.
    $ownable = @(Get-RenderedFile $TemplateClone $to | Where-Object { Test-OwnableName $_ })
    $notOwnable = @($ownedFiles | Where-Object { $_ -notin $ownable })
    if ($notOwnable) { throw "project_owned_files names $($notOwnable -join ', '), which template $to doesn't render or the update itself needs. It can name any file template $to renders except .copier-answers.yml, .template-version and the files in .template/." }
    # The answers the template's documents and release workflows follow, in $Data so the update and every render take them; -Data wins.
    # nuget_publisher names a library's own NuGet publishing workflows, which keeps publish.yml, release-provenance.yml and raise-baseline.yml out.
    if ((Get-Answer 'nuget_package') -eq 'true' -and -not $Data.ContainsKey('nuget_publisher')) { $Data['nuget_publisher'] = Get-NuGetPublisherAnswer }
    $releasePolicy = Get-ReleasePolicyAnswer
    if ($releasePolicy -and -not $Data.ContainsKey('release_policy')) { $Data['release_policy'] = $releasePolicy }
    # A merged file the project rewrote shares few lines with the template's render of it, so the template's changes to it conflict in unrelated places (copier-org/copier#2759): suggest the project_owned_files answer. Only a file the template changed can conflict.
    $rewritten = @()
    $fromRendered = $null
    $renderedFrom = Join-Path $scratch 'rendered\from'
    if ($from -ne $to) {
        try {
            $changedMerged = @(foreach ($source in git -C $TemplateClone diff --name-only $from $to -- template/ 2>$null) {
                    $file = ConvertTo-ProjectName $source
                    if ($file -in $merged -and $file -notin $ownedFiles -and (Test-Path -LiteralPath $file -PathType Leaf)) { $file }
                })
            # A renamed template source, such as one that gained .jinja, lists its file under both names: keep each file once.
            $changedMerged = @($changedMerged | Select-Object -Unique)
            if ($changedMerged) { $fromRendered = Invoke-TemplateRender $from $renderedFrom }
            if ($fromRendered) { $rewritten = @(Get-RewrittenFile $changedMerged $renderedFrom) }
            elseif ($changedMerged) { Write-Warning "Skipped the rewritten-file suggestion: without template $from rendered, the update can't tell which merged files this project rewrote." }
        }
        catch {
            Write-Warning "Skipped the rewritten-file suggestion: checking for merged files this project rewrote failed: $_"
            $rewritten = @()
        }
    }
    # Copier writes the template's .gitignore over the project's before merging the project's edits back, then passes every path
    # Git reports as ignored to one `git apply` command, and the update sets an adopted .gitignore's kept lines aside while
    # Copier merges. Keep the project's whole .gitignore, kept lines included, in force through info/exclude from the reset
    # to the end of the run, or a folder only it ignores, such as node_modules/, is staged with the reset, or is listed file
    # by file and overflows Windows' command line. The copy sits between marker lines, so a run that was killed before
    # restoring info/exclude is cleaned up here; the outer finally restores it.
    $infoExclude = git rev-parse --path-format=absolute --git-path info/exclude
    $beginCopy = '# Update-Project.ps1: the project''s .gitignore, in force while Copier updates'
    $endCopy = '# Update-Project.ps1: end of the .gitignore copy'
    $excludeBefore = if (Test-Path -LiteralPath $infoExclude) {
        [regex]::Replace([IO.File]::ReadAllText($infoExclude), "(?s)\n$([regex]::Escape($beginCopy))\n.*?\n$([regex]::Escape($endCopy))\n", '')
    }
    $restoreExclude = $true
    $projectIgnore = Join-Path $project '.gitignore'
    if (Test-Path -LiteralPath $projectIgnore) {
        New-Item -ItemType Directory -Force (Split-Path $infoExclude) | Out-Null
        [IO.File]::WriteAllText($infoExclude, "$excludeBefore`n$beginCopy`n$([IO.File]::ReadAllText($projectIgnore))`n$endCopy`n")
    }
    $versionFileNew = $false
    try {
        # Reset the template-owned files to the project's current version; what changes was edited or deleted here. A file the project owns is left out, so its copy is never overwritten.
        $exclude = foreach ($file in $projectOwned + $merged + $ownedFiles) { '--exclude', "/$file" }
        $recopyData = @(Get-OptInData) + @(Get-LicenseData)
        Invoke-Copier recopy --force --quiet --vcs-ref :current: @recopyData @exclude
        $reset = @(Get-Change)
        # A template-owned file that the project's version skips only through a name without a /, which Copier matches in every folder,
        # and that the target version doesn't skip, was never updated, such as docs/decisions/project-template/README.md. Reset it to the
        # project's version, as the recopy does the other template-owned files; after $reset, so it is listed as refreshed, not as an edit.
        $stale = @(Get-RenderedFile $TemplateClone $from | Select-Object -Unique | Where-Object { (Test-CopierPattern $_ $skipFrom) -and $_ -notin $projectOwned -and -not (Test-CopierPattern $_ $skipTo) -and $_ -notin $merged -and $_ -notin $ownedFiles -and (Test-Path -LiteralPath $_ -PathType Leaf) })
        $refreshed = @()
        if ($stale) {
            if ($null -eq $fromRendered) { $fromRendered = Invoke-TemplateRender $from $renderedFrom }
            if ($fromRendered) {
                $refreshed = @(foreach ($file in $stale) {
                        $src = Join-Path $renderedFrom $file
                        $dst = Join-Path $project $file
                        if ((Test-Path -LiteralPath $src -PathType Leaf) -and [IO.File]::ReadAllText($src) -ne [IO.File]::ReadAllText($dst)) { Copy-Item -LiteralPath $src -Destination $dst -Force; $file }
                    })
            }
            else { Write-Warning "Skipped refreshing $($stale -join ', '): without template $from rendered, the update can't reset them." }
        }
        # The template's Adopt-Project.ps1 ends these files with the repository's own lines under a "# Kept from …" comment. Set them aside while
        # Copier merges, so a template change to the file's last lines can't conflict with them or take them into its conflict.
        $keptLines = @{}
        $droppedTextRules = @()
        foreach ($file in '.gitignore', '.gitattributes', '.editorconfig') {
            if ($file -notin $merged -or $file -in $ownedFiles -or -not (Test-Path -LiteralPath $file)) { continue }
            $text = [IO.File]::ReadAllText((Join-Path $project $file))
            $match = [regex]::Match($text, "(?m)^# Kept from this repository's own \S+ when it adopted the project template\r?$")
            if (-not $match.Success) { continue }
            $templatePart = $text.Substring(0, $match.Index).TrimEnd("`r", "`n")
            $keptLines[$file] = $text.Substring($templatePart.Length)
            # The template's Adopt-Project.ps1 leaves a catch-all text rule, such as "* text eol=lf", out of the lines it keeps (Merge-AdoptedFile in the template's
            # tools/adopt/Adoption.psm1), because Git uses the last rule that matches and the rule would override the template's binary rules. A project
            # adopted before it did still has the rule: leave it out here too, and say so below. A deliberate copy of that pattern; change both together.
            if ($file -eq '.gitattributes') {
                $keptSplit = $keptLines[$file] -split "`n"
                $catchAll = @($keptSplit | Where-Object { $_ -match '^\*\s+text(?:=auto)?(?:\s+eol=lf)?\s*$' })
                if ($catchAll) { $keptLines[$file] = ($keptSplit | Where-Object { $_ -notin $catchAll }) -join "`n"; $droppedTextRules = @($catchAll | ForEach-Object Trim) }
            }
            [IO.File]::WriteAllText((Join-Path $project $file), "$templatePart`n")
        }
        Add-UpdateChange $scratch
        $resetCommitted = [bool](git diff --cached --name-only)
        if ($resetCommitted) { git commit -q --no-verify -m 'Reset template-owned files'; if ($LASTEXITCODE) { throw 'Committing the reset failed.' } }

        $copierArgs = @('update', '--defaults', '--quiet', '--vcs-ref', $to)
        foreach ($k in $Data.Keys) { $copierArgs += '--data', "$k=$(ConvertTo-CopierValue $Data[$k])" }
        $copierArgs += @(Get-OptInData)
        $copierArgs += @(Get-LicenseData)
        # A project-owned file the project deleted stays deleted; Copier would otherwise create it again.
        $deleted = $projectOwned | Where-Object { -not (Test-Path -LiteralPath $_) -and (git log --diff-filter=D --format=%h -1 -- $_) }
        foreach ($file in $deleted) { $copierArgs += '--exclude', "/$file" }
        # A library whose own workflow already pushes NuGet packages gets no publish.yml, which would push them a second time.
        $publishers = @(if ((Get-Answer 'nuget_package') -eq 'true') { Get-NuGetPublisher })
        $publishLeftOut = $publishers.Count -gt 0 -and -not (Test-Path -LiteralPath .github/workflows/publish.yml) -and '.github/workflows/publish.yml' -notin @($deleted)
        if ($publishLeftOut) { $copierArgs += '--exclude', '/.github/workflows/publish.yml' }
        # A file the project owns, merged or template-owned, stays as it is; its template change is shown below.
        foreach ($file in $ownedFiles) { $copierArgs += '--exclude', "/$file" }
        $versionFileNew = -not (Test-Path -LiteralPath version.json) -and 'version.json' -notin @($deleted)
        Invoke-Copier @copierArgs

        # Put the kept lines back at the end, after any conflict.
        foreach ($file in $keptLines.Keys) {
            $full = Join-Path $project $file
            if (Test-Path -LiteralPath $full) { [IO.File]::WriteAllText($full, [IO.File]::ReadAllText($full).TrimEnd("`r", "`n") + $keptLines[$file]) }
        }

        # Drop the reset commit but keep its changes, and any conflict, in the index and the working tree.
        if ($resetCommitted) { git update-ref HEAD $head; if ($LASTEXITCODE) { throw 'Removing the reset commit failed.' } }
    }
    catch {
        git reset -q --hard $head; git clean -fdq
        throw
    }

    # version.json this update added starts after the newest release (Update-VersionFile, ADR-0138).
    $versionFile = if ($versionFileNew -and (Test-Path -LiteralPath version.json -PathType Leaf)) { Update-VersionFile }
    $conflicts = @(git diff --name-only --diff-filter=U)
    if (-not $conflicts) {
        Add-UpdateChange $scratch
        # Executable on Linux and macOS; Windows doesn't record the bit on its own.
        if (Test-Path -LiteralPath .githooks/pre-push) { git update-index --chmod=+x .githooks/pre-push }
    }
    else {
        # git add -A or git add -u would mark the conflicted files resolved, conflict markers and all, so stage every other change to a tracked file by name, as the update left it: the files the reset changed, whose index otherwise keeps the reset's version, and the files Copier changed or deleted, whose deletion a commit that stages only the files that still exist would otherwise leave out. A list file, as in Add-NewFile, so a long list can't overflow Windows' command line.
        $unmerged = @((git -c core.quotePath=false diff --name-only --diff-filter=U -z) -split "`0" | Where-Object { $_ })
        $restage = @((git -c core.quotePath=false diff --name-only --no-renames -z) -split "`0" | Where-Object { $_ -and $_ -notin $unmerged } | Select-Object -Unique)
        if ($restage) {
            New-Item -ItemType Directory -Force $scratch | Out-Null
            $list = Join-Path $scratch 'restage.txt'
            [IO.File]::WriteAllText($list, ($restage -join "`0"), [Text.UTF8Encoding]::new($false))
            git --literal-pathspecs add -u --pathspec-from-file=$list --pathspec-file-nul
            if ($LASTEXITCODE) { throw 'Staging the update around its conflicts failed.' }
        }
        # The files the update added are staged too, so git add -u after the conflicts are resolved stages the whole update.
        Add-NewFile $scratch
    }
    # Enable the template's versioned hooks in this clone, unless it already has a hooks path or runs hooks another way
    # (Get-GitHooksSetup): then warn with how to run them.
    if (Test-Path -LiteralPath .githooks) {
        $hooksSetup = Get-GitHooksSetup
        if ($hooksSetup.HooksPath) { git config core.hooksPath .githooks }
        elseif ($hooksSetup.Note) { Write-Warning $hooksSetup.Note }
    }
    $changes = @(Get-Change)
    $projectOwnedChanged = @(if ($from -ne $to) {
            $changedOwned = @(foreach ($source in git -C $TemplateClone diff --name-only $from $to -- template/ 2>$null) {
                    $file = ConvertTo-ProjectName $source
                    if (((Test-CopierPattern $file $skipTo) -or $file -in $ownedFiles) -and (Test-Path -LiteralPath $file)) { [pscustomobject]@{ File = $file; Source = $source } }
                })
            # A renamed template source lists its file twice: show the file once, with every source.
            foreach ($group in $changedOwned | Group-Object File) { [pscustomobject]@{ File = $group.Name; Source = @($group.Group | ForEach-Object Source) } }
        })

    # A success ends with exit 0, not return, which would hand a caller that runs this with & the exit code of the last git command, such as git apply --check's 1 for a hunk the project lacks.
    if (-not (git status --porcelain)) { "Already up to date: template $to."; Get-LicenseWarning; Get-BadgeReminder; Get-ImmutableReleaseNote $false; Get-CodeReviewNote; exit 0 }
    "Template $from -> ${to}:"
    $changes | Sort-Object Kind, File | ForEach-Object { '  {0,-9} {1}' -f $_.Kind, $_.File }
    # A file the update leaves as the project had it lost nothing, such as a local edit the new version also makes or a deleted file it also deletes: list only the files that differ from the commit the update started from.
    $differs = @((git -c core.quotePath=false diff --name-only --no-renames -z $head) -split "`0" | Where-Object { $_ })
    $edited = @($reset | Where-Object { $_.Kind -ne 'added' -and $_.File -in $differs } | ForEach-Object File)
    $restored = @($reset | Where-Object { $_.Kind -eq 'added' -and $_.File -in $differs } | ForEach-Object File)
    $refreshed = @($refreshed | Where-Object { $_ -in $differs })
    if ($edited) { ''; 'Local edits discarded — these files are the template''s; make the change in a project file instead:'; $edited | ForEach-Object { "  $_" } }
    if ($restored) { ''; 'Restored — these files are the template''s, so the project can''t delete them:'; $restored | ForEach-Object { "  $_" } }
    if ($droppedTextRules) { ''; "Left out of the lines .gitattributes kept from this repository at adoption: its catch-all text rule ($($droppedTextRules -join '; ')). Git uses the last rule that matches, so the rule overrode the template's binary rules, such as *.png binary and *.ico binary, and git add dropped the CR of each CR LF pair in a binary file it staged. Check the binary files committed while the rule applied, and replace any that no longer open." }
    if ($refreshed) { ''; 'Refreshed — these files are the template''s, but earlier template versions never updated them; a local edit to one is discarded, so make the change in a project file instead:'; $refreshed | ForEach-Object { "  $_" } }
    $shown = @()
    if ($projectOwnedChanged) {
        # Render both versions with the project's answers, so the diffs show what the project would get, not Jinja.
        New-Item -ItemType Directory -Force $scratch | Out-Null
        $renderedTo = Join-Path $scratch 'rendered\to'
        $rendered = $(if ($null -ne $fromRendered) { $fromRendered } else { Invoke-TemplateRender $from $renderedFrom }) -and (Invoke-TemplateRender $to $renderedTo)
        $empty = Join-Path $scratch 'empty'
        [IO.File]::WriteAllText($empty, '')
        $shown = foreach ($changed in $projectOwnedChanged) {
            if ($rendered) {
                $old = Join-Path $renderedFrom $changed.File
                $new = Join-Path $renderedTo $changed.File
                $diff = git diff --no-index --no-color --unified=1 -- $(if (Test-Path -LiteralPath $old) { $old } else { $empty }) $(if (Test-Path -LiteralPath $new) { $new } else { $empty })
                $title = "  $($changed.File):"
            }
            else {
                $diff = git -C $TemplateClone diff --no-color --unified=1 $from $to -- $changed.Source
                $title = "  $($changed.File), from the template source $($changed.Source -join ', '), which Copier couldn't render with this project's answers:"
            }
            # Leave out a hunk the project's file already has; a file left with none isn't shown.
            $hunk = @(
                $current = @()
                foreach ($line in @($diff | Where-Object { $_ -is [string] }) + '@@') {
                    if ($line.StartsWith('@@')) {
                        if ($current -and -not ($rendered -and (Test-HunkApplied $changed.File $current $scratch))) { $current | ForEach-Object { "    $_" } }
                        $current = @()
                    }
                    if ($current -or $line.StartsWith('@@')) { $current += $line }
                }
            )
            if ($hunk) { [pscustomobject]@{ Title = $title; Lines = $hunk; File = $changed.File } }
        }
        if ($shown) {
            ''
            "The template changed these project-owned files since ${from}. They weren't touched; copy over what you want:"
            foreach ($file in $shown) { ''; $file.Title; $file.Lines }
        }
    }

    # A conflict in a file the target version owns, neither merged nor project-owned there, comes from the project's own copy of it, such as a build/dod.ps1 the template now adds: the project_owned_files answer keeps that copy.
    $ownedConflicts = @()
    if ($conflicts) {
        $mergedTo = @(Get-TemplateList $TemplateClone $to '_merged_files')
        $ownedConflicts = @($conflicts | Where-Object { $_ -in $ownable -and $_ -notin $mergedTo -and -not (Test-CopierPattern $_ $skipTo) })
    }
    $ownedCommand = "  .\.template\Update-Project.ps1 -Data @{ project_owned_files = $((@($ownedFiles) + $rewritten + $ownedConflicts | Select-Object -Unique | ForEach-Object { "'$_'" }) -join ', ') }"
    if ($rewritten) {
        ''
        "These merged files share less than half their lines with the template's version, so the project seems to have replaced them, and the template's changes to them can conflict in unrelated places:"
        $rewritten | ForEach-Object { "  $_" }
        if (-not $ownedConflicts) { "To make them project-owned, so updates leave them alone and show the template's changes instead, run the update with (after abandoning it with git reset --hard; git clean -fd if it stops with a conflict below):"; $ownedCommand }
    }
    if ($ownedConflicts) {
        ''
        "These files are template-owned in ${to}, and this project has its own version of each, so they conflict:"
        $ownedConflicts | ForEach-Object { "  $_" }
        "To keep the project's versions, make them project-owned$(if ($rewritten) { ', with the merged files above,' }) so updates leave them alone and show the template's changes instead. Abandon this update with git reset --hard; git clean -fd, then run:"
        $ownedCommand
    }

    # A library release built with a higher AssemblyVersion than build/ProjectTemplate.props pins (Get-AssemblyVersionFloor), in a library without version.json, whose assemblyVersion holds it instead.
    $floor = if (-not (Test-Path -LiteralPath version.json)) { Get-AssemblyVersionFloor }
    if ($floor) {
        ''
        "$($floor.Tag) was released with AssemblyVersion $($floor.AssemblyVersion), and build/ProjectTemplate.props gives every $($floor.Major).x release $($floor.Major).0.0.0, which an application compiled against $($floor.AssemblyVersion) can't load. Until the next major version, set it in Directory.Build.props, after the import of build/ProjectTemplate.props (ADR-0136):"
        "  <PropertyGroup><AssemblyVersion>$($floor.AssemblyVersion)</AssemblyVersion></PropertyGroup>"
    }
    if ($versionFile) {
        ''
        $versionLine = 'Added version.json, which Nerdbank.GitVersioning now builds every assembly''s and package''s version from (the template''s ADR-0138, in docs/decisions/project-template/).'
        $versionLine += if ($versionFile.Version) { " Its version is $($versionFile.Version), the patch after $($versionFile.Tag): the next plan with a Release: line sets it to that release's version." } else { ' Its version is 0.1.0, as the project has no release tag yet.' }
        if ($versionFile.AssemblyVersion) { $versionLine += " Its assemblyVersion is $($versionFile.AssemblyVersion) with precision $($versionFile.Precision), the AssemblyVersion the current major version's releases need; the change that starts the next major version sets it to { `"precision`": `"major`" }." }
        $versionLine
    }
    if ((Test-Path -LiteralPath version.json) -and (Test-Path -LiteralPath Directory.Build.props -PathType Leaf) -and (Get-Content -LiteralPath Directory.Build.props -Raw) -match '<AssemblyVersion[\s>]') {
        ''
        'Directory.Build.props sets AssemblyVersion, which version.json''s assemblyVersion sets once Nerdbank.GitVersioning builds the project (the template''s ADR-0138): delete it from Directory.Build.props.'
    }
    # A library with its own NuGet publishing workflow (Get-NuGetPublisher).
    if ($publishLeftOut) {
        ''
        "Left out .github/workflows/publish.yml: $($publishers -join ', ') already push NuGet packages, and a published release would run both. To publish through publish.yml instead, with nuget.org trusted publishing, delete $($publishers -join ', ') and run the update again. The update also leaves out release-provenance.yml and raise-baseline.yml, which only publish.yml calls, deleting them where an earlier update added them, and renders the documents it adds, and the template changes it shows, with $($publishers -join ', ') in place of publish.yml (the nuget_publisher answer)."
    }
    elseif ($publishers.Count -gt 0 -and (Test-Path -LiteralPath .github/workflows/publish.yml)) {
        ''
        "Both .github/workflows/publish.yml and $($publishers -join ', ') push NuGet packages, so a published release runs both. Keep one: delete publish.yml, or move the publishing into it and delete the others."
    }
    # A library's publish.yml without the trigger tag-release.yml starts it with: a release that workflow publishes would ship no packages.
    if ((Test-Path -LiteralPath .github/workflows/tag-release.yml) -and (Test-Path -LiteralPath .github/workflows/publish.yml) -and (Get-Content -LiteralPath .github/workflows/publish.yml -Raw) -notmatch '(?m)^\s+workflow_dispatch:') {
        ''
        '.github/workflows/publish.yml has no workflow_dispatch trigger. .github/workflows/tag-release.yml publishes each release with the workflow''s token, which starts no other workflow, so it starts publish.yml through workflow_dispatch, and until publish.yml has that trigger, a release ships no packages. Add the trigger with a required tag input to its on: section, read the tag from inputs.tag when the run has no release, and check out refs/tags/<tag>, as the publish.yml the project template gives a .NET library on GitHub does (the template''s ADR-0137, which such a library has in docs/decisions/project-template/).'
    }
    Get-CiCallNote
    Get-CiPermissionNote
    $offered = [bool]@($shown | Where-Object { $_.File -eq 'SECURITY.md' } | ForEach-Object Lines | Where-Object { $_ -match '^\s*\+Releases are immutable' }).Count
    Get-ImmutableReleaseNote $offered
    Get-CodeReviewNote
    # A publish.yml that doesn't call one of the release follow-ups, or either (ADR-0151): they no longer start on
    # workflow_run, so a release misses what each one it doesn't call makes.
    $publishText = if (Test-Path -LiteralPath .github/workflows/publish.yml) { Get-Content -LiteralPath .github/workflows/publish.yml -Raw } else { '' }
    if ($publishText -and (((Test-Path -LiteralPath .github/workflows/release-provenance.yml) -and $publishText -notmatch '(?m)^\s+uses:\s*\./\.github/workflows/release-provenance\.yml\s*(#.*)?$') -or ((Test-Path -LiteralPath .github/workflows/raise-baseline.yml) -and $publishText -notmatch '(?m)^\s+uses:\s*\./\.github/workflows/raise-baseline\.yml\s*(#.*)?$'))) {
        ''
        '.github/workflows/publish.yml doesn''t call .github/workflows/release-provenance.yml or .github/workflows/raise-baseline.yml, or both, which no longer start on workflow_run, since GitHub starts none for the run tag-release.yml starts: until it calls each, no release gets the provenance or the baseline pull request the missing one makes. Add the provenance and baseline jobs of the template''s publish.yml (the template''s ADR-0151). To attest an earlier release while its Publish packages run''s artifact is kept (14 days): gh workflow run release-provenance.yml -f tag=<tag> -f run-id=<run id>'
    }
    # A library without a package validation baseline (ADR-0147): name the published version to set.
    if ((Get-Answer 'nuget_package') -eq 'true') {
        if (-not (Test-PackageValidationBaseline)) {
            foreach ($published in @(Get-PublishedPackageVersion)) {
                Write-Warning "$($published.Id) is published on nuget.org at $($published.Version). The template now validates every pack against the last published package (ADR-0147): set <PackageValidationBaselineVersion>$($published.Version)</PackageValidationBaselineVersion> in Directory.Build.props after the import, or in the project's file."
            }
        }
    }
    # The stale NuGet logistics of an older library (ADR-0149): named, never fixed.
    $logistics = @(if ((Get-Answer 'nuget_package') -eq 'true') { Get-NuGetLogistics })
    if ($logistics.Count) {
        ''
        'Stale NuGet logistics, which the template now makes or checks itself:'
        foreach ($finding in $logistics) { "  - $($finding.Message)" }
        'To fix them in the repository, run:'
        "  /plan-migration Bring the NuGet logistics up to the template: $(($logistics.Kind | Select-Object -Unique) -join ', ')"
    }
    $drifts = @(if ((Get-Answer 'nuget_package') -eq 'true') { Get-PackageMetadataDrift })
    if ($drifts.Count) {
        ''
        'Package metadata that differs from the published package:'
        foreach ($drift in $drifts) { "  - $($drift.Message)" }
    }
    # A project that has just turned the C# formatter on: the files IDE0073 now fails (Get-CSharpFileWithoutHeader).
    if ($Data.ContainsKey('csharp_format') -and (Get-Answer 'csharp_format') -eq 'true') {
        $headerless = @(Get-CSharpFileWithoutHeader)
        if ($headerless.Count -gt 0) {
            ''
            "csharp_format is on, and $($headerless.Count) C# file$(if ($headerless.Count -ne 1) { 's' }) don't start with the header .editorconfig sets (file_header_template): IDE0073 is a warning, which fails the build, for each. Rewrite each once, as the template's ADR-0148 describes: replace its old header, such as a #region Copyright Preamble block, with the header's lines as // comments, and drop a UTF-8 byte order mark. $(($headerless | Select-Object -First 5) -join ', ')$(if ($headerless.Count -gt 5) { ', ...' })"
        }
    }
    $wrappers = @(Get-CSharpierWrapper $ownable)
    if ($wrappers.Count -gt 0) {
        ''
        "These build scripts of the project's own run dotnet csharpier check, which build/dod.ps1 already runs in its Format stage now that .config/dotnet-tools.json names CSharpier (the template's ADR-0148): $($wrappers -join ', '). Drop that check from them, and update any comment or decision record of the project's that says build/dod.ps1 doesn't check the formatting."
    }
    if (Test-Path -LiteralPath version.json) {
        # Nerdbank.GitVersioning ignores -p:Version, and without PublicRelease it adds a -g<commit> suffix.
        if ((Test-Path -LiteralPath .github/workflows/publish.yml) -and (Get-Content -LiteralPath .github/workflows/publish.yml -Raw) -notmatch 'PublicRelease=true') {
            ''
            '.github/workflows/publish.yml packs without -p:PublicRelease=true. Nerdbank.GitVersioning takes each package''s version from version.json and ignores -p:Version, and without PublicRelease it adds a -g<commit> suffix, so the check that every package has the tag''s version stops each release before it publishes. Add -p:PublicRelease=true to its dotnet test and dotnet pack commands, as the publish.yml the project template gives a .NET library on GitHub does (the template''s ADR-0138, in docs/decisions/project-template/).'
        }
        $gitlabCi = if (Test-Path -LiteralPath .gitlab-ci.yml) { Get-Content -LiteralPath .gitlab-ci.yml -Raw } else { '' }
        if ($gitlabCi -match 'dotnet\s+pack\b|nuget(?:\.exe)?\s+push\b') {
            $missing = @()
            if ($gitlabCi -notmatch 'PublicRelease=true') { $missing += '-p:PublicRelease=true' }
            if ($gitlabCi -notmatch '(?m)GIT_DEPTH:[ \t]*["'']?0["'']?[ \t]*(?:#.*)?\r?$') { $missing += 'GIT_DEPTH: "0"' }
            if ($missing.Count) {
                ''
                ".gitlab-ci.yml packs or pushes packages without $($missing -join ' and '). Nerdbank.GitVersioning takes each package's version from version.json and ignores -p:Version, adds a -g<commit> suffix without PublicRelease, and fails on a shallow clone, which GitLab CI makes unless GIT_DEPTH is `"0`". Add -p:PublicRelease=true to its dotnet pack command and set GIT_DEPTH: `"0`" in its variables, as the publish.yml the project template gives a .NET library on GitHub does (the template's ADR-0138, in docs/decisions/project-template/)."
            }
        }
        Get-GitVersioningWorkflowNote
        $gitVersioning = @(Get-GitVersioningReference)
        if ($gitVersioning.Count -gt 0) {
            ''
            "These files reference Nerdbank.GitVersioning, which build/ProjectTemplate.props references too, as a library's does once version.json exists (the template's ADR-0138): $($gitVersioning -join ', '). A second reference fails the build (NU1504, NU1506): delete the project's own PackageReference of it, and its own PackageVersion of it in Directory.Packages.props, keeping the template's."
        }
    }

    Get-AttributionCheckNote $changes
    Get-LicenseWarning
    Get-BadgeReminder

    # Version tags the changelog has no section for, such as releases from before it started (Get-ChangelogBackfill).
    $backfill = Get-ChangelogBackfill
    if ($backfill.Range -ne '-') {
        ''
        "Version tags without a section in $($backfill.File): $($backfill.Range). To give the changelog the project's full release history, in the repository, run:"
        "  /plan-migration Backfill $($backfill.File) from the releases $($backfill.Range)"
    }
    $message = "Update from project-template $to"
    if ($conflicts) {
        ''
        "$($conflicts.Count) file(s) conflict: the template and this project changed the same lines. In each, keep the right lines and delete the <<<<<<<, ======= and >>>>>>> marker lines. Then:"
        '  git add -u'
        '  git diff --cached --check     # prints any marker you missed'
        "  git commit -m `"$message`""
        'To abandon the update instead: git reset --hard; git clean -fd'
        exit 1
    }
    if ($NoCommit) { ''; "Staged for review: git diff --cached, then git commit -m `"$message`""; exit 0 }
    git commit -q -m $message
    if ($LASTEXITCODE) { throw 'The commit failed.' }
    ''
    "Committed `"$message`". Review it with: git show --stat"
    exit 0
}
finally {
    if ($restoreExclude) {
        if ($null -eq $excludeBefore) { Remove-Item -LiteralPath $infoExclude -Force -ErrorAction SilentlyContinue }
        else { [IO.File]::WriteAllText($infoExclude, $excludeBefore) }
    }
    Pop-Location
    try { [Console]::OutputEncoding = $outputEncoding } catch { Write-Verbose "Couldn't restore the console's output encoding: $_" }
    if ($ownClone -and $TemplateClone -and (Test-Path -LiteralPath $TemplateClone)) { Remove-Item -LiteralPath $TemplateClone -Recurse -Force -ErrorAction SilentlyContinue }
    if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force -ErrorAction SilentlyContinue }
}
