<#
.SYNOPSIS
    Raises PackageValidationBaselineVersion to a released version once nuget.org lists it, the template's ADR-0150.

.DESCRIPTION
    From the project template; template updates replace this file. Sets PackageValidationBaselineVersion in each
    Directory.Build.props and project file that already sets it, so package validation compares each package with the
    release just published. It never lowers the baseline: it changes nothing when no file sets the property, or when
    every file already holds the version or a later one. When it raises a project's baseline to a new major version, it
    removes the baseline suppressions (IsBaselineSuppression true, PKV006 and PKV007) from the
    CompatibilitySuppressions.xml beside the project, which match nothing once the baseline is that major's first
    release, keeps the others, and deletes the file when none is left. It adds a ### Changed entry under
    ## [Unreleased] of a Keep a Changelog file with a release section, in place of any baseline entry an earlier run
    left there, and never touches a release section. It prints the version it set, or nothing.

    A prerelease is never a baseline, so the version must be a stable one such as 1.4.0 or v1.4.0.

.PARAMETER Version
    The released version, such as 1.4.0 or v1.4.0.

.PARAMETER Root
    The repository root to search. Defaults to the folder above this script's.

.PARAMETER TimeoutMinutes
    How long to wait for nuget.org to list the version. Defaults to 30.

.PARAMETER SkipNugetCheck
    Raises the baseline without waiting for nuget.org.

.EXAMPLE
    ./build/Update-Baseline.ps1 -Version 1.4.0
#>
#Requires -Version 7.4
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Root = (Split-Path -Parent $PSScriptRoot),

    [int]$TimeoutMinutes = 30,

    [switch]$SkipNugetCheck
)

$ErrorActionPreference = 'Stop'

function Test-Bom([string]$Path) {
    $bytes = [IO.File]::ReadAllBytes($Path)
    $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
}

if ($Version -notmatch '^v?(\d+\.\d+\.\d+)$') {
    throw "'$Version' isn't a stable version such as 1.4.0 or v1.4.0: a prerelease is never a baseline."
}
$target = $Matches[1]
$targetVersion = [System.Management.Automation.SemanticVersion]$target

$candidates = @(
    Get-ChildItem -LiteralPath $Root -Recurse -File -Include Directory.Build.props, *.csproj |
        ForEach-Object {
            [pscustomobject]@{
                File     = $_
                Relative = [IO.Path]::GetRelativePath($Root, $_.FullName)
            }
        } |
        Where-Object { $_.Relative -notmatch '(^|[\\/])(bin|obj|node_modules|artifacts|\.git)[\\/]' } |
        Sort-Object -Property Relative
)

$pattern = '(<PackageValidationBaselineVersion>\s*)([^<]*?)(\s*</PackageValidationBaselineVersion>)'
$setterFiles = [Collections.Generic.List[IO.FileInfo]]::new()
$changed = [ordered]@{}
foreach ($candidate in $candidates) {
    $path = $candidate.File.FullName
    $relative = $candidate.Relative
    $text = [IO.File]::ReadAllText($path)
    if ($text -notmatch $pattern) { continue }
    $setterFiles.Add($candidate.File)
    $raise = @{ NewMajor = $false }
    $newText = [regex]::Replace($text, $pattern, {
            param($m)
            $value = $m.Groups[2].Value
            $parsed = $null
            if (-not [System.Management.Automation.SemanticVersion]::TryParse($value, [ref]$parsed)) {
                Write-Warning "${relative}: PackageValidationBaselineVersion '$value' isn't a version; left as it is."
                return $m.Value
            }
            if ($parsed -ge $targetVersion) { return $m.Value }
            if ($parsed.Major -lt $targetVersion.Major) { $raise.NewMajor = $true }
            return $m.Groups[1].Value + $target + $m.Groups[3].Value
        })
    if ($newText -ceq $text) { continue }
    $hadBom = Test-Bom $path
    $changed[$path] = @{ Text = $newText; HadBom = $hadBom; NewMajor = $raise.NewMajor }
}

if ($setterFiles.Count -eq 0) {
    Write-Host 'No Directory.Build.props or project file sets PackageValidationBaselineVersion: set it once by hand, as the template''s ADR-0147 says, and this script raises it from then on.'
    return
}
if ($changed.Count -eq 0) {
    Write-Host "PackageValidationBaselineVersion is already $target or later."
    return
}

$suppressionPattern = '(?s)[ \t]*<Suppression>.*?</Suppression>[ \t]*(\r?\n)?'
$baselineEntry = '<IsBaselineSuppression>\s*true\s*</IsBaselineSuppression>|<DiagnosticId>\s*PKV00[67]\s*</DiagnosticId>'
$suppressions = [ordered]@{}
foreach ($candidate in $candidates) {
    $file = $candidate.File
    if ($file.Extension -ne '.csproj') { continue }
    $folder = $file.DirectoryName
    $setter = if ($setterFiles.FullName -contains $file.FullName) { $file } else {
        $setterFiles | Where-Object { $_.Name -eq 'Directory.Build.props' -and ($folder -eq $_.DirectoryName -or $folder.StartsWith($_.DirectoryName + [IO.Path]::DirectorySeparatorChar)) } |
            Sort-Object { $_.DirectoryName.Length } -Descending | Select-Object -First 1
    }
    if (-not $setter -or -not $changed.Contains($setter.FullName) -or -not $changed[$setter.FullName].NewMajor) { continue }
    $path = Join-Path $folder 'CompatibilitySuppressions.xml'
    if ($suppressions.Contains($path) -or -not (Test-Path -LiteralPath $path -PathType Leaf)) { continue }
    $text = [IO.File]::ReadAllText($path)
    $kept = [regex]::Replace($text, $suppressionPattern, { param($m) if ($m.Value -match $baselineEntry) { '' } else { $m.Value } })
    if ($kept -ceq $text) { continue }
    $hadBom = Test-Bom $path
    $keptText = if ($kept -match '<Suppression>') { $kept } else { $null }
    $suppressions[$path] = @{ Text = $keptText; HadBom = $hadBom; Relative = [IO.Path]::GetRelativePath($Root, $path) }
}

if (-not $SkipNugetCheck) {
    $ids = @(
        foreach ($candidate in $candidates) {
            $file = $candidate.File
            if ($file.Extension -ne '.csproj' -or $file.Name -like '*.Tests.csproj') { continue }
            $projectText = [IO.File]::ReadAllText($file.FullName)
            if ($projectText -match '<IsPackable>\s*false') { continue }
            $idMatch = [regex]::Match($projectText, '<PackageId>\s*([^<]*?)\s*</PackageId>')
            if ($idMatch.Success -and $idMatch.Groups[1].Value) { $idMatch.Groups[1].Value } else { $file.BaseName }
        }
    )
    if (-not $ids.Count) { throw 'No packable project found: nothing to wait for on nuget.org.' }

    $lastError = @{}
    function Test-Listed([string]$Id) {
        try {
            $listed = @((Invoke-RestMethod -Uri "https://api.nuget.org/v3-flatcontainer/$($Id.ToLowerInvariant())/index.json" -TimeoutSec 30).versions) -contains $target
            $lastError.Remove($Id)
            $listed
        }
        catch {
            Write-Warning "nuget.org gave no answer for ${Id}: $($_.Exception.Message)"
            $response = $_.Exception.PSObject.Properties['Response']
            $notFound = $response -and $response.Value -and [int]$response.Value.StatusCode -eq 404
            if ($notFound) { $lastError.Remove($Id) } else { $lastError[$Id] = $_.Exception.Message }
            $false
        }
    }
    $pending = $ids
    $slept = 0
    $delay = 15
    while ($true) {
        $pending = @($pending | Where-Object { -not (Test-Listed $_) })
        if (-not $pending.Count) { break }
        if ($slept -ge $TimeoutMinutes * 60) {
            $errors = @($pending | Where-Object { $lastError.ContainsKey($_) } | ForEach-Object { "${_}: $($lastError[$_])" })
            $detail = if ($errors.Count) { " The last error from nuget.org: $($errors -join '; ')." } else { '' }
            throw "nuget.org doesn't list $($pending -join ', ') at $target after $TimeoutMinutes minutes: run this again once it does.$detail"
        }
        $wait = $delay + (Get-Random -Maximum 5)
        Write-Host "Waiting $wait seconds for nuget.org to list $($pending -join ', ') at $target."
        Start-Sleep -Seconds $wait
        $slept += $wait
        $delay = [Math]::Min($delay * 2, 120)
    }
}

foreach ($path in $changed.Keys) {
    [IO.File]::WriteAllText($path, $changed[$path].Text, [Text.UTF8Encoding]::new($changed[$path].HadBom))
}
foreach ($path in $suppressions.Keys) {
    $s = $suppressions[$path]
    if ($null -eq $s.Text) {
        Remove-Item -LiteralPath $path
        Write-Host "Deleted $($s.Relative): its suppressions all compared with the previous major version."
    }
    else {
        [IO.File]::WriteAllText($path, $s.Text, [Text.UTF8Encoding]::new($s.HadBom))
        Write-Host "Removed the suppressions that compared with the previous major version from $($s.Relative)."
    }
}

$changelog = 'CHANGELOG.md'
$agentsPath = Join-Path $Root 'AGENTS.md'
if (Test-Path -LiteralPath $agentsPath -PathType Leaf) {
    $agents = [IO.File]::ReadAllText($agentsPath)
    $landing = [regex]::Match($agents, '(?ms)^## Landing[ \t]*\r?$(.*?)(?=^## |\z)')
    if ($landing.Success) {
        $named = [regex]::Match($landing.Groups[1].Value, '(?m)^- changelog:[ \t]*(.+?)[ \t]*\r?$')
        if ($named.Success) { $changelog = $named.Groups[1].Value.Trim('`', '"', "'") }
    }
}
$changelogPath = Join-Path $Root $changelog
if (Test-Path -LiteralPath $changelogPath -PathType Leaf) {
    $original = [IO.File]::ReadAllText($changelogPath)
    $crlf = $original.Contains("`r`n")
    $text = $original.Replace("`r`n", "`n")
    if ($text -match '(?m)^## \[Unreleased\][ \t]*$' -and $text -match '(?m)^## \[\d+\.\d+\.\d+[^\]]*\] - ') {
        $entry = "- Package validation compares each package with the published $target package."
        $unreleased = [regex]::Match($text, '(?s)## \[Unreleased\]\n(.*?)(?=\n## \[|\n\[unreleased\]:)')
        if ($unreleased.Success) {
            $body = $unreleased.Groups[1].Value
            $baseline = 'Package validation compares each package with the published \d+\.\d+\.\d+ package\.'
            $earlier = [regex]::Match($body, '(?m)^- ' + $baseline + '$')
            if ($earlier.Success) {
                $rest = [regex]::Replace($body.Substring($earlier.Index + $earlier.Length), '\n- ' + $baseline + '(?=\n|\z)', '')
                $newBody = $body.Substring(0, $earlier.Index) + $entry + $rest
            }
            elseif ($body -match '(?m)^### Changed\n') {
                $newBody = [regex]::Replace($body, '(?m)^### Changed\n\n?', "### Changed`n`n$entry`n", 1)
            }
            elseif (($later = [regex]::Match($body, '(?m)^### (Deprecated|Removed|Fixed|Security)\n')).Success) {
                $newBody = $body.Insert($later.Index, "### Changed`n`n$entry`n`n")
            }
            else {
                $newBody = "`n" + ($body.Trim("`n") + "`n`n### Changed`n`n$entry").TrimStart("`n") + "`n"
            }
            $group = $unreleased.Groups[1]
            $newText = $text.Substring(0, $group.Index) + $newBody + $text.Substring($group.Index + $group.Length)
            if ($crlf) { $newText = $newText.Replace("`n", "`r`n") }
            $hadBom = Test-Bom $changelogPath
            [IO.File]::WriteAllText($changelogPath, $newText, [Text.UTF8Encoding]::new($hadBom))
        }
    }
    else {
        Write-Host "Left $changelog as it is: it has no [Unreleased] and release sections to add the entry to."
    }
}

$target
