# Copyright © NCode Group
# SPDX-License-Identifier: Apache-2.0

#requires -version 7
<#
.SYNOPSIS
    Tests build/Assert-Markdown.ps1 against fixture repositories, offline.
.DESCRIPTION
    Each case builds a git repository with a README and a site page, runs the check with a fake samples command in
    place of dotnet mdsnippets, and checks its exit code and output: an unchanged tree passes; a changed README, a
    changed README under a name git quotes, a rewritten site page and a failing samples command fail; and a folder that
    is not a repository fails instead of reading as "nothing changed". Exits 1 on the first failed check.
    build/gate.ps1 runs it before the check.
#>
$ErrorActionPreference = 'Stop'
$script = Join-Path $PSScriptRoot 'Assert-Markdown.ps1'
$utf8 = [Text.UTF8Encoding]::new($false)
# A name with a byte above 0x7F, which git quotes unless it is asked not to.
$quoted = 'docs/Straße.md'

# $Fake is the body of the samples command, run in the fixture's root; $Expect is the exit code; $Says is text the
# output must hold. With -NoRepository the fixture is a plain folder.
function Invoke-Case {
    param([string]$Case, [string]$Fake, [int]$Expect, [string]$Says = '', [switch]$NoRepository)
    $root = Join-Path ([IO.Path]::GetTempPath()) "markdown-test-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $root | Out-Null
    try {
        if (-not $NoRepository) {
            git init -q $root
            if ($LASTEXITCODE) { throw 'git init failed' }
        }
        foreach ($path in 'README.md', 'docs/site/index.mdx', $quoted) {
            $file = Join-Path $root $path
            New-Item -ItemType Directory -Path (Split-Path -Parent $file) -Force | Out-Null
            [IO.File]::WriteAllText($file, "# $path`n", $utf8)
        }
        $command = "& '$script' -Root '$root' -Snippets { $Fake }"
        $output = @(pwsh -NoProfile -NonInteractive -Command $command 2>&1 | ForEach-Object { "$_" })
        $code = $LASTEXITCODE
        if ($code -ne $Expect) { "FAILED: $Case exited $code instead of $Expect. Output: $($output -join ' | ')"; exit 1 }
        if ($Says -and -not (($output -join "`n").Contains($Says))) { "FAILED: $Case did not print '$Says'. Output: $($output -join ' | ')"; exit 1 }
        "ok: $Case"
    }
    finally { Remove-Item -LiteralPath $root -Recurse -Force }
}

Invoke-Case 'an unchanged tree' -Fake '$global:LASTEXITCODE = 0' -Expect 0
Invoke-Case 'a changed README' -Fake "Add-Content -LiteralPath README.md -Value 'sample'; `$global:LASTEXITCODE = 0" -Expect 1 -Says 'README samples: FAILED. dotnet mdsnippets updated README.md'
Invoke-Case 'a changed README under a name git quotes' -Fake "Add-Content -LiteralPath '$quoted' -Value 'sample'; `$global:LASTEXITCODE = 0" -Expect 1 -Says "updated $quoted"
Invoke-Case 'a rewritten site page' -Fake "Add-Content -LiteralPath docs/site/index.mdx -Value '<!-- snippet -->'; `$global:LASTEXITCODE = 0" -Expect 1 -Says 'Site pages: FAILED. dotnet mdsnippets rewrote docs/site/index.mdx'
Invoke-Case 'a failing samples command' -Fake '$global:LASTEXITCODE = 3' -Expect 1 -Says 'could not merge the samples'
Invoke-Case 'a folder that is not a repository' -Fake '$global:LASTEXITCODE = 0' -Expect 1 -Says 'git ls-files failed' -NoRepository
# Every case passed.
exit 0
