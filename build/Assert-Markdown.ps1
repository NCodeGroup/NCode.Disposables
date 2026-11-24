# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7

<#
.SYNOPSIS
    The README samples check: runs dotnet mdsnippets, which copies the C# samples from the tests into the READMEs (ADR-1000), and fails when it changed a Markdown file or failed.
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Push-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
try {
    # -z prints each path as it is, because git quotes a name with a byte above 0x7F and the quoted name matches no file.
    function Get-MarkdownState {
        $output = (git ls-files -z --cached --others --exclude-standard -- '*.md') -join "`n"
        if ($LASTEXITCODE) { throw "git ls-files failed with exit code $LASTEXITCODE." }
        $output.Split([char]0, [StringSplitOptions]::RemoveEmptyEntries) |
            Where-Object { Test-Path -LiteralPath $_ } |
            ForEach-Object { "$((Get-FileHash -LiteralPath $_).Hash) $_" }
    }

    $before = @(Get-MarkdownState)
    dotnet mdsnippets
    if ($LASTEXITCODE) {
        Write-Host 'README samples: FAILED. dotnet mdsnippets could not merge the samples.'
        exit 1
    }

    $after = @(Get-MarkdownState)
    $changed = @(Compare-Object $after $before | ForEach-Object { ($_.InputObject -split ' ', 2)[1] } | Select-Object -Unique)
    if ($changed.Count -gt 0) {
        Write-Host "README samples: FAILED. dotnet mdsnippets updated $($changed -join ', ') from the tests; commit the result."
        exit 1
    }

    Write-Host 'README samples: PASSED.'
    exit 0
}
catch {
    Write-Host "README samples: FAILED. $($_.Exception.Message)"
    exit 1
}
finally {
    Pop-Location
}
