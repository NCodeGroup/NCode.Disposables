# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7

<#
.SYNOPSIS
    The README samples check's self-test: runs build/Assert-Markdown.ps1 on a temporary Git repository with a fake dotnet mdsnippets, and fails unless the check passes an unchanged README and fails a changed README and a failed merge.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$assert = Join-Path $PSScriptRoot 'Assert-Markdown.ps1'
$tempPath = [IO.Path]::GetTempPath()
$repo = Join-Path $tempPath "assert-markdown-$([Guid]::NewGuid().ToString('N'))"
$readme = Join-Path $repo 'README.md'
$failures = [System.Collections.Generic.List[string]]::new()

function Test-Case([string]$Name, [scriptblock]$Snippets, [int]$ExpectedCode, [string]$ExpectedText) {
    $output = & $assert -Root $repo -Snippets $Snippets *>&1
    $code = $LASTEXITCODE
    $text = $output -join "`n"
    if ($code -ne $ExpectedCode -or -not $text.Contains($ExpectedText)) {
        $failures.Add("${Name}: expected exit code $ExpectedCode and '$ExpectedText', got exit code $code and '$text'.")
    }
}

try {
    New-Item -ItemType Directory -Path $repo | Out-Null
    git -c init.defaultBranch=main init -q $repo
    if ($LASTEXITCODE) { throw "git init failed with exit code $LASTEXITCODE." }
    Set-Content -LiteralPath $readme -Value '# Sample'
    git -C $repo add README.md
    if ($LASTEXITCODE) { throw "git add failed with exit code $LASTEXITCODE." }

    Test-Case 'An unchanged README' { } 0 'README samples: PASSED.'
    $rewrite = { Set-Content -LiteralPath $readme -Value '# Changed' }.GetNewClosure()
    Test-Case 'A changed README' $rewrite 1 'from the tests'
    Test-Case 'A failed merge' { $global:LASTEXITCODE = 3 } 1 'could not merge'
}
finally {
    Remove-Item -LiteralPath $repo -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    foreach ($failure in $failures) { Write-Host "README samples self-test: FAILED. $failure" }
    exit 1
}

Write-Host 'README samples self-test: PASSED.'
exit 0
