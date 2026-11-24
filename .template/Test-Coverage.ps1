<#
.SYNOPSIS
    Fail when the tests cover less than 90% of the statements or 80% of the branches.

.DESCRIPTION
    From the project template; template updates replace this file (the template's ADR-0026). It reads Cobertura and LCOV
    reports, merges them line by line, and checks the result against the OpenSSF Best Practices gold thresholds, which
    count statement and branch coverage. A line counts as a statement. A report with no branch data counts as fully
    covered, since Pester measures no branches.
    Each -Report entry is a file, or a folder searched recursively for *.cobertura.xml, coverage.xml,
    cobertura-coverage.xml and lcov.info. Without -Report, it takes those of coverage.xml, lcov.info, coverage/ and
    artifacts/test-results/ under -Root that exist. A file whose first non-blank character is "<" is Cobertura; any
    other is LCOV.
    Test code and the template's own files are left out: a file under a folder named test, tests, bin, obj,
    node_modules, .template, .claude, .github or .githooks, or under a folder ending in .Tests or .Test, or named
    test_*.py, *_test.py, conftest.py, *.Tests.ps1, *.test.* or *.spec.*.
    Prints the statement and branch coverage, and with a failure the five files with the most uncovered lines. Exits 1
    when no report is found or a minimum is missed, else 0; reports that measure no code pass.

.PARAMETER Report
    Coverage report files, or folders to search for them. Defaults to the reports a project's tests usually write.

.PARAMETER Statements
    The minimum percentage of statements the tests must cover. Defaults to 90.

.PARAMETER Branches
    The minimum percentage of branches the tests must cover. Defaults to 80.

.PARAMETER Root
    The repository root, which report file names are made relative to. Defaults to the folder above this script's.

.EXAMPLE
    ./.template/Test-Coverage.ps1

.EXAMPLE
    ./.template/Test-Coverage.ps1 -Report artifacts/test-results -Branches 70
#>
[CmdletBinding()]
param(
    [string[]]$Report,
    [double]$Statements = 90,
    [double]$Branches = 80,
    [string]$Root = (Split-Path -Parent $PSScriptRoot)
)
$ErrorActionPreference = 'Stop'
$culture = [Globalization.CultureInfo]::InvariantCulture
$Root = [IO.Path]::GetFullPath($Root)
$reportNames = '*.cobertura.xml', 'coverage.xml', 'cobertura-coverage.xml', 'lcov.info'
$skipFolders = 'test', 'tests', 'bin', 'obj', 'node_modules', '.template', '.claude', '.github', '.githooks'
$skipFiles = 'test_*.py', '*_test.py', 'conftest.py', '*.Tests.ps1', '*.test.*', '*.spec.*'

# The report files an entry names: the file itself, or the reports found in a folder.
function Get-ReportFile([string]$Entry) {
    if (Test-Path -LiteralPath $Entry -PathType Leaf) { (Resolve-Path -LiteralPath $Entry).ProviderPath; return }
    if (-not (Test-Path -LiteralPath $Entry -PathType Container)) { return }
    foreach ($name in $reportNames) {
        Get-ChildItem -LiteralPath $Entry -Recurse -File -Filter $name | ForEach-Object FullName
    }
}

# A file name from a report made relative to the root, with / separators, or $null when the file is test code or the template's.
function ConvertTo-CoveragePath([string]$Name, [string]$Source) {
    $path = $Name.Trim()
    if (-not $path) { return $null }
    if (-not [IO.Path]::IsPathRooted($path) -and $Source) { $path = Join-Path $Source $path }
    if ([IO.Path]::IsPathRooted($path)) { $path = [IO.Path]::GetRelativePath($Root, $path) }
    $path = $path.Replace('\', '/')
    $parts = $path -split '/'
    $folders = if ($parts.Count -gt 1) { $parts[0..($parts.Count - 2)] } else { @() }
    foreach ($folder in $folders) {
        if ($folder -in $skipFolders -or $folder -like '*.Tests' -or $folder -like '*.Test') { return $null }
    }
    foreach ($pattern in $skipFiles) { if ($parts[-1] -like $pattern) { return $null } }
    $path
}

$files = [Collections.Generic.List[string]]::new()
$seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$entries = if ($Report) { $Report } else { 'coverage.xml', 'lcov.info', 'coverage', 'artifacts/test-results' | ForEach-Object { Join-Path $Root $_ } }
foreach ($entry in $entries) {
    foreach ($file in @(Get-ReportFile $entry)) { if ($seen.Add($file)) { $files.Add($file) } }
}
if (-not $files.Count) {
    'No coverage report: the tests wrote no coverage.xml, lcov.info, coverage/ or artifacts/test-results/ report. Write one, as CONTRIBUTING.md says for this stack.'
    exit 1
}

# Line key -> the file and whether any report covers it; branch key -> the most covered and valid branches any report gives.
$lines = [Collections.Generic.Dictionary[string, object]]::new()
$branchCounts = [Collections.Generic.Dictionary[string, int[]]]::new()
function Add-Line([string]$File, [string]$Key, [bool]$Covered) {
    if ($lines.ContainsKey($Key)) { if ($Covered) { $lines[$Key].Covered = $true } }
    else { $lines[$Key] = [pscustomobject]@{ File = $File; Covered = $Covered } }
}
function Add-Branch([string]$Key, [int]$Covered, [int]$Valid) {
    if ($branchCounts.ContainsKey($Key)) {
        $current = $branchCounts[$Key]
        $branchCounts[$Key] = @([Math]::Max($current[0], $Covered), [Math]::Max($current[1], $Valid))
    }
    else { $branchCounts[$Key] = @($Covered, $Valid) }
}

$settings = [Xml.XmlReaderSettings]@{ DtdProcessing = [Xml.DtdProcessing]::Ignore; XmlResolver = $null }
foreach ($file in $files) {
    $text = [IO.File]::ReadAllText($file)
    if ($text.TrimStart().StartsWith('<', [StringComparison]::Ordinal)) {
        $xml = [Xml.XmlDocument]::new()
        $reader = [Xml.XmlReader]::Create($file, $settings)
        try { $xml.Load($reader) } finally { $reader.Dispose() }
        $sourceNode = $xml.SelectSingleNode('//sources/source')
        $source = if ($sourceNode) { $sourceNode.InnerText.Trim() } else { '' }
        foreach ($class in $xml.SelectNodes('//class')) {
            $name = ConvertTo-CoveragePath $class.GetAttribute('filename') $source
            if (-not $name) { continue }
            foreach ($line in $class.SelectNodes('lines/line')) {
                $number = $line.GetAttribute('number')
                $hits = 0L
                [void][long]::TryParse($line.GetAttribute('hits'), [Globalization.NumberStyles]::Integer, $culture, [ref]$hits)
                Add-Line $name "${name}:$number" ($hits -gt 0)
                $condition = [regex]::Match($line.GetAttribute('condition-coverage'), '\((\d+)/(\d+)\)')
                if ($line.GetAttribute('branch') -ieq 'true' -and $condition.Success) {
                    Add-Branch "${name}:$number" ([int]$condition.Groups[1].Value) ([int]$condition.Groups[2].Value)
                }
            }
        }
    }
    else {
        $name = $null
        foreach ($row in $text -split '\r?\n') {
            if ($row.StartsWith('SF:', [StringComparison]::Ordinal)) { $name = ConvertTo-CoveragePath $row.Substring(3) $Root; continue }
            if ($row -eq 'end_of_record') { $name = $null; continue }
            if (-not $name) { continue }
            if ($row.StartsWith('DA:', [StringComparison]::Ordinal)) {
                $parts = $row.Substring(3).Split(',')
                if ($parts.Count -lt 2) { continue }
                $hits = 0L
                [void][long]::TryParse($parts[1], [Globalization.NumberStyles]::Integer, $culture, [ref]$hits)
                Add-Line $name "${name}:$($parts[0])" ($hits -gt 0)
            }
            elseif ($row.StartsWith('BRDA:', [StringComparison]::Ordinal)) {
                $parts = $row.Substring(5).Split(',')
                if ($parts.Count -lt 4) { continue }
                $taken = 0L
                $covered = [long]::TryParse($parts[3], [Globalization.NumberStyles]::Integer, $culture, [ref]$taken) -and $taken -gt 0
                Add-Branch "${name}:$($parts[0]):$($parts[1]):$($parts[2])" ([int]$covered) 1
            }
        }
    }
}

if (-not $lines.Count) { 'The coverage reports measure no code yet.'; exit 0 }

$lineTotal = $lines.Count
$lineCovered = @($lines.Values | Where-Object Covered).Count
$statementPercent = $lineCovered * 100.0 / $lineTotal
$failed = $statementPercent -lt $Statements
[string]::Format($culture, 'Statement coverage: {0:N1}% ({1} of {2} lines), minimum {3}%', $statementPercent, $lineCovered, $lineTotal, $Statements)

$branchTotal = 0
$branchCovered = 0
foreach ($counts in $branchCounts.Values) { $branchCovered += $counts[0]; $branchTotal += $counts[1] }
if ($branchTotal) {
    $branchPercent = $branchCovered * 100.0 / $branchTotal
    if ($branchPercent -lt $Branches) { $failed = $true }
    [string]::Format($culture, 'Branch coverage: {0:N1}% ({1} of {2} branches), minimum {3}%', $branchPercent, $branchCovered, $branchTotal, $Branches)
}
else { 'Branch coverage: no branches measured' }

if ($failed) {
    'Least covered:'
    $lines.Values | Group-Object File | ForEach-Object {
        [pscustomobject]@{ File = $_.Name; Total = $_.Count; Uncovered = @($_.Group | Where-Object { -not $_.Covered }).Count }
    } | Where-Object Uncovered | Sort-Object @{ Expression = 'Uncovered'; Descending = $true }, File | Select-Object -First 5 | ForEach-Object {
        "  $($_.File): $($_.Uncovered) of $($_.Total) lines uncovered"
    }
    'Coverage: FAILED'
    exit 1
}
'Coverage: PASSED'
exit 0
