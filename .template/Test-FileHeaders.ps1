# Copyright (c) 2026 Marcin Polewski
# SPDX-License-Identifier: MIT

<#
.SYNOPSIS
    Fail on a source file that doesn't start with the copyright line and the SPDX license line.

.DESCRIPTION
    From the project template; template updates replace this file (the template's ADR-0027). It reads author_name and
    license from .copier-answers.yml. Every tracked source file outside .template/, .claude/, .github/, .githooks/,
    .vscode/ and any node_modules/ or vendor/ folder, which hold other authors' code, must hold a
    "Copyright <author_name>" comment line and, when license isn't blank, a "SPDX-License-Identifier: <license>"
    comment line within its first 10 lines. The comment follows the file's extension: "#" for scripts, "//" for the C
    family and JavaScript, "/* ... */" on each line for .css, .scss and .less, and "//" inside the frontmatter for
    .astro, whose lines must sit between its leading "---" lines so nothing reaches the built page. Files of other
    kinds, and names ending in .min.js, aren't checked. Nor does it check the six build scripts the template owns
    (build/dod.ps1, Update-PublicApi.ps1, Get-TestRuntimes.ps1, Install-Dotnet.ps1, Test-DotnetSupport.ps1 and
    Update-Baseline.ps1), which updates replace and which carry the template's own copyright and license lines (the
    template's ADR-0038). A hand-written declaration file (.d.ts) is checked like any TypeScript file; a generated
    one, like other build output, sits in a git-ignored folder, which git ls-files leaves out.
    Prints each miss and exits 1 when there is one, else 0. With -Fix, it adds the missing lines instead, after any #!
    line or an .astro file's opening "---" line, giving an .astro file without frontmatter a "---" block that holds
    them, keeping the file's line endings and byte order mark, and exits 0. Without an author_name answer there is no
    header to check.

.PARAMETER Root
    The repository root. Defaults to the folder above this script's.

.PARAMETER Fix
    Add the missing header lines to each failing file.

.EXAMPLE
    ./.template/Test-FileHeaders.ps1

.EXAMPLE
    ./.template/Test-FileHeaders.ps1 -Fix
#>
[CmdletBinding()]
param(
    [string]$Root = (Split-Path -Parent $PSScriptRoot),
    [switch]$Fix
)
$ErrorActionPreference = 'Stop'
$Root = [IO.Path]::GetFullPath($Root)

# An answer from .copier-answers.yml, read as Update-Project.ps1 reads it.
function Get-Answer([string]$Key) {
    $answers = Join-Path $Root '.copier-answers.yml'
    if (-not (Test-Path -LiteralPath $answers)) { return }
    $match = Select-String -LiteralPath $answers -Pattern "^${Key}:\s*(.+?)\s*$" | Select-Object -First 1
    if ($match) { $match.Matches[0].Groups[1].Value.Trim("'", '"') }
}

$author = Get-Answer 'author_name'
$license = Get-Answer 'license'
if (-not $author) {
    'No author_name answer, so there is no header to check.'
    exit 0
}

$copyrightLine = "Copyright $([char]0x00A9) $author"
$spdxLine = if ($license) { "SPDX-License-Identifier: $license" }

$hashMarker = '.ps1', '.psm1', '.py', '.sh', '.bash', '.rb', '.tf'
$slashMarker = '.js', '.mjs', '.cjs', '.jsx', '.ts', '.mts', '.cts', '.tsx', '.go', '.rs', '.java', '.kt', '.kts', '.swift', '.c', '.h', '.cc', '.cpp', '.hpp', '.cs', '.scala', '.dart', '.groovy', '.astro'
$blockMarker = '.css', '.scss', '.less'
# Scripts the template owns and updates replace: they carry the template's copyright and license lines, not the
# project's (the template's ADR-0038).
$templateOwned = 'build/dod.ps1', 'build/Update-PublicApi.ps1', 'build/Get-TestRuntimes.ps1', 'build/Install-Dotnet.ps1', 'build/Test-DotnetSupport.ps1', 'build/Update-Baseline.ps1'

$listed = git -C $Root ls-files -z
if ($LASTEXITCODE) { throw "git ls-files exited with code $LASTEXITCODE." }
$paths = @("$($listed -join '')" -split "`0" | Where-Object { $_ })

$utf8 = [Text.UTF8Encoding]::new($false)
$checked = 0
$failing = 0
foreach ($path in $paths) {
    if ($path -match '^(\.template|\.claude|\.github|\.githooks|\.vscode)/' -or $path -match '(^|/)(node_modules|vendor)/' -or $path -match '\.min\.js$' -or $templateOwned -contains $path) { continue }
    $extension = [IO.Path]::GetExtension($path).ToLowerInvariant()
    $open, $close = if ($hashMarker -contains $extension) { '#', '' } elseif ($slashMarker -contains $extension) { '//', '' } elseif ($blockMarker -contains $extension) { '/*', '*/' } else { continue }
    $file = Join-Path $Root $path
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { continue }
    $checked++

    $bytes = [IO.File]::ReadAllBytes($file)
    $hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
    $text = if ($hasBom) { $utf8.GetString($bytes, 3, $bytes.Length - 3) } else { $utf8.GetString($bytes) }
    $eol = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    $lines = @($text -split '\r?\n')

    # An Astro component's header belongs in its frontmatter, which the build drops: a comment outside it, even an
    # HTML one, reaches the built page. So only the lines between its leading "---" lines count; a frontmatter that
    # never closes, which Astro rejects, runs to the end of the file, so -Fix still puts the header after its "---".
    $isAstro = $extension -eq '.astro'
    $frontmatter = $isAstro -and $lines[0].Trim() -eq '---'
    $first = 0
    $last = [Math]::Min(10, $lines.Count)
    if ($frontmatter) {
        for ($end = 1; $end -lt $lines.Count -and $lines[$end].Trim() -ne '---'; $end++) {}
        $first = 1
        $last = [Math]::Min($last, $end)
    } elseif ($isAstro) {
        $last = 0
    }

    $escapedOpen = [regex]::Escape($open)
    $closePattern = if ($close) { "\s*$([regex]::Escape($close))" } else { '' }
    $copyrightPattern = "^\s*$escapedOpen.*\bCopyright\b.*$([regex]::Escape($author))"
    $spdxPattern = if ($license) { "^\s*$escapedOpen\s*SPDX-License-Identifier:\s*$([regex]::Escape($license))$closePattern\s*$" }
    $copyrightAt = -1
    $spdxAt = -1
    for ($i = $first; $i -lt $last; $i++) {
        if ($copyrightAt -lt 0 -and $lines[$i] -match $copyrightPattern) { $copyrightAt = $i }
        if ($spdxAt -lt 0 -and $spdxPattern -and $lines[$i] -match $spdxPattern) { $spdxAt = $i }
    }
    $missingCopyright = $copyrightAt -lt 0
    $missingSpdx = [bool]$spdxLine -and $spdxAt -lt 0
    if (-not $missingCopyright -and -not $missingSpdx) { continue }
    $failing++

    if (-not $Fix) {
        if ($missingCopyright) { "${path}: no copyright line" }
        if ($missingSpdx) { "${path}: no SPDX-License-Identifier line" }
        continue
    }

    $suffix = if ($close) { " $close" } else { '' }
    $copyrightText = "$open $copyrightLine$suffix"
    $spdxText = if ($spdxLine) { "$open $spdxLine$suffix" }
    if ($missingCopyright -and $missingSpdx) {
        $header = @($copyrightText) + @($spdxText | Where-Object { $_ })
        if ($isAstro -and -not $frontmatter) {
            $header = @('---') + $header + @('---')
            $at = 0
        } else {
            $at = if ($frontmatter -or $lines[0] -like '#!*') { 1 } else { 0 }
            # A blank line sets the header apart from the code, but a frontmatter's closing line needs none.
            if (-not ($frontmatter -and $at -lt $lines.Count -and $lines[$at].Trim() -eq '---')) { $header += '' }
        }
    } elseif ($missingSpdx) {
        $header = @($spdxText)
        $at = $copyrightAt + 1
    } else {
        $header = @($copyrightText)
        $at = $spdxAt
    }
    $newLines = [Collections.Generic.List[string]]::new([string[]]$lines)
    $newLines.InsertRange($at, [string[]]$header)
    $body = $utf8.GetBytes(($newLines -join $eol))
    $out = if ($hasBom) { [byte[]](0xEF, 0xBB, 0xBF) + $body } else { $body }
    [IO.File]::WriteAllBytes($file, $out)
    "${path}: header added"
}

if ($Fix) { exit 0 }
if ($failing) {
    'Run ./.template/Test-FileHeaders.ps1 -Fix to add them.'
    "File headers: FAILED ($failing files)"
    exit 1
}
"File headers: PASSED ($checked files)"
exit 0
