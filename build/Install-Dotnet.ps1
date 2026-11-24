<#
.SYNOPSIS
    Installs the .NET SDK global.json selects and the runtimes build/Get-TestRuntimes.ps1 names.

.DESCRIPTION
    From the project template; template updates replace this file. Takes each download from Microsoft's release
    metadata at https://builds.dotnet.microsoft.com/dotnet/release-metadata/ and installs nothing unless its SHA-512
    equals the one the metadata gives. A request gives up after 30 seconds without an answer, or 60 seconds without
    more of a download. A failed one is tried twice more, after 5 and then 10 seconds, or after the Retry-After a 429
    or 503 gives, up to 60 seconds, each plus up to a second at random so that clients don't retry in step; a client
    error (4xx) other than 408 or 429 fails at once. Needs PowerShell 7.4 or later.

    The SDK is the one global.json selects, as actions/setup-dotnet reads it on GitHub: the version it names, or, under
    the rollForward policy latestPatch, latestFeature, latestMinor or latestMajor, the newest release that policy
    allows. Every file and folder of its archive that the installation lacks is added, and none is replaced, so the
    SDKs, runtimes and host already there stay. Skips an SDK that is already there.

    Then, for each channel build/Get-TestRuntimes.ps1 prints, takes the ASP.NET Core runtime archive of the channel's
    latest release for this machine; it holds the .NET runtime too. Adds only its shared/<framework>/<version> folders,
    and skips a version that is already there.

    .gitlab-ci.yml runs it before the build; on GitHub, the .github/actions/install-dotnet action installs the same SDK
    and runtimes with actions/setup-dotnet. Run it as an administrator or with sudo when .NET is installed system-wide.

.PARAMETER Channel
    The runtime channels to install, such as 8.0.x. Defaults to what build/Get-TestRuntimes.ps1 prints once the SDK is
    installed.

.PARAMETER GlobalJson
    The global.json that names the SDK. Defaults to the one at the repository root.

.PARAMETER InstallDir
    The .NET installation to add to. Defaults to the installation that dotnet --list-runtimes shows for
    Microsoft.NETCore.App.

.PARAMETER SkipSdk
    Installs only the runtimes.

.EXAMPLE
    ./build/Install-Dotnet.ps1

.EXAMPLE
    ./build/Install-Dotnet.ps1 -InstallDir ~/.dotnet

.EXAMPLE
    ./build/Install-Dotnet.ps1 -SkipSdk -Channel 8.0.x
#>
#Requires -Version 7.4
[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+(\.x)?$')]
    [string[]]$Channel,
    [string]$GlobalJson = (Join-Path (Split-Path -Parent $PSScriptRoot) 'global.json'),
    [string]$InstallDir,
    [switch]$SkipSdk
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$metadataRoot = 'https://builds.dotnet.microsoft.com/dotnet/release-metadata'
# Invoke-ReleaseRequest's limits: the seconds a request waits for an answer and for each next part of a download, the
# tries it makes, the back-off base, which it multiplies by the tries made, and the longest wait between tries.
$connectionTimeoutSeconds = 30
$operationTimeoutSeconds = 60
$maxAttempts = 3
$retryDelaySeconds = 5
$maxRetryDelaySeconds = 60
$channelGiven = $PSBoundParameters.ContainsKey('Channel')

if ($SkipSdk -and -not $channelGiven) { $Channel = @(& (Join-Path $PSScriptRoot 'Get-TestRuntimes.ps1')) }
if ($SkipSdk -and -not $Channel) { return }

if (-not $InstallDir) {
    foreach ($line in @(dotnet --list-runtimes)) {
        if ($line -match '^Microsoft\.NETCore\.App \S+ \[(.+)\]$') { $InstallDir = Split-Path -Parent (Split-Path -Parent $Matches[1]); break }
    }
    if (-not $InstallDir) { throw 'dotnet --list-runtimes shows no Microsoft.NETCore.App: pass -InstallDir.' }
}

$os = if ($IsWindows) { 'win' } elseif ($IsMacOS) { 'osx' } elseif (Test-Path -LiteralPath '/etc/alpine-release') { 'linux-musl' } else { 'linux' }
$arch = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString().ToLowerInvariant()
$suffix = "$os-$arch$(if ($IsWindows) { '.zip' } else { '.tar.gz' })"
$scratch = Join-Path $InstallDir ".install-dotnet-$PID"

# Reads release metadata, or downloads a file to OutFile, waiting at most $connectionTimeoutSeconds for an answer and
# $operationTimeoutSeconds for each next part of a download, so a download that stalls partway fails too. Makes up to
# $maxAttempts tries, but stops at a client error (4xx) other than a request timeout (408) or a rate limit (429).
# Between tries it waits the Retry-After a 429 or 503 gives, in seconds or as a date, or else $retryDelaySeconds times
# the tries made; at most $maxRetryDelaySeconds, plus up to a second at random, so that clients that failed together
# don't retry together.
function Invoke-ReleaseRequest([string]$Uri, [string]$OutFile) {
    $timeouts = @{ ConnectionTimeoutSeconds = $connectionTimeoutSeconds; OperationTimeoutSeconds = $operationTimeoutSeconds }
    for ($attempt = 1; ; $attempt++) {
        try {
            if ($OutFile) { return Invoke-WebRequest -Uri $Uri -OutFile $OutFile @timeouts }
            return Invoke-RestMethod -Uri $Uri @timeouts
        }
        catch {
            $response = $_.Exception.Response
            $status = [int]$response.StatusCode
            if ($attempt -ge $maxAttempts -or ($status -ge 400 -and $status -lt 500 -and $status -notin 408, 429)) { throw }
            $retryAfter = if ($status -in 429, 503) { $response.Headers.RetryAfter }
            $delay = if ($null -ne $retryAfter.Delta) {
                $retryAfter.Delta.TotalSeconds
            }
            elseif ($null -ne $retryAfter.Date) {
                ($retryAfter.Date - [DateTimeOffset]::UtcNow).TotalSeconds
            }
            else {
                $retryDelaySeconds * $attempt
            }
            $delay = [Math]::Clamp($delay, 0, $maxRetryDelaySeconds)
            Start-Sleep -Milliseconds ([int](1000 * $delay) + (Get-Random -Maximum 1000))
        }
    }
}

# Downloads a file the release metadata lists into the scratch folder, checks its SHA-512 and extracts it; returns the
# folder it was extracted to.
function Expand-Release($File, [string]$Label) {
    if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $scratch | Out-Null
    $archive = Join-Path $scratch $File.name
    Invoke-ReleaseRequest $File.url $archive
    if ((Get-FileHash -LiteralPath $archive -Algorithm SHA512).Hash -ne $File.hash) {
        throw "$($File.name) for $Label doesn't match the SHA-512 in Microsoft's release metadata; nothing was installed."
    }
    $content = Join-Path $scratch 'content'
    if ($IsWindows) {
        Expand-Archive -LiteralPath $archive -DestinationPath $content
    }
    else {
        New-Item -ItemType Directory -Force -Path $content | Out-Null
        tar -xzf $archive -C $content
        if ($LASTEXITCODE) { throw "tar -xzf $($File.name) failed with exit code $LASTEXITCODE." }
    }
    $content
}

# Moves in each file and folder the target lacks, by a rename on the same volume, which keeps Unix file modes. The sdk
# folder moves last, so an interrupted install never looks complete.
function Merge-Folder([string]$From, [string]$To) {
    New-Item -ItemType Directory -Force -Path $To | Out-Null
    foreach ($item in @(Get-ChildItem -LiteralPath $From -Force | Sort-Object { $_.Name -eq 'sdk' })) {
        $target = Join-Path $To $item.Name
        if (-not (Test-Path -LiteralPath $target)) { Move-Item -LiteralPath $item.FullName -Destination $target }
        elseif ($item.PSIsContainer -and (Test-Path -LiteralPath $target -PathType Container)) { Merge-Folder $item.FullName $target }
    }
}

try {
    if (-not $SkipSdk) {
        $sdk = (Get-Content -Raw -LiteralPath $GlobalJson | ConvertFrom-Json).sdk
        if ("$($sdk.version)" -notmatch '^(\d+)\.(\d+)\.([1-9])\d{2,}(-.+)?$') {
            throw "$GlobalJson names no full SDK version, such as 10.0.100, in sdk.version."
        }
        $pinned = $sdk.version
        $major = [int]$Matches[1]
        $band = "$($Matches[1]).$($Matches[2]).$($Matches[3])"
        $policy = if ($Matches[4]) { '' } else { "$($sdk.rollForward)" }
        $sdkChannel = "$($Matches[1]).$($Matches[2])"
        if ($policy -in 'latestMinor', 'latestMajor') {
            $sdkChannel = (Invoke-ReleaseRequest "$metadataRoot/releases-index.json").'releases-index' | Where-Object {
                $channelMajor = [int]($_.'channel-version' -split '\.')[0]
                $_.'latest-sdk' -notmatch '-' -and ($channelMajor -eq $major -or ($policy -eq 'latestMajor' -and $channelMajor -gt $major))
            } | Sort-Object { [version]$_.'latest-sdk' } | Select-Object -Last 1 -ExpandProperty 'channel-version'
        }
        $sdks = @()
        if ($sdkChannel) { $sdks = @((Invoke-ReleaseRequest "$metadataRoot/$sdkChannel/releases.json").releases | ForEach-Object { $_.sdks }) }
        $selected = if ($policy -in 'latestPatch', 'latestFeature', 'latestMinor', 'latestMajor') {
            $sdks | Where-Object {
                $_.version -notmatch '-' -and [version]$_.version -ge [version]$pinned -and
                ($policy -ne 'latestPatch' -or $_.version.StartsWith($band))
            } | Sort-Object { [version]$_.version } | Select-Object -Last 1
        }
        else {
            $sdks | Where-Object { $_.version -eq $pinned } | Select-Object -First 1
        }
        if (-not $selected) {
            throw "Microsoft's release metadata lists no .NET SDK that $GlobalJson selects: $pinned$(if ($policy) { " with rollForward $policy" })."
        }

        $sdkVersion = $selected.version
        if (Test-Path -LiteralPath (Join-Path $InstallDir "sdk/$sdkVersion")) {
            ".NET SDK $sdkVersion is already in $InstallDir"
        }
        else {
            $file = @($selected.files | Where-Object { $_.name -eq "dotnet-sdk-$suffix" })[0]
            if (-not $file) { throw "Microsoft publishes no dotnet-sdk-$suffix for .NET SDK $sdkVersion." }
            Merge-Folder (Expand-Release $file ".NET SDK $sdkVersion") $InstallDir
            Remove-Item -LiteralPath $scratch -Recurse -Force
            "Installed .NET SDK $sdkVersion in $InstallDir"
        }

        if (-not $channelGiven) {
            # build/Get-TestRuntimes.ps1 asks MSBuild, so it runs this installation's dotnet, which has the SDK.
            $env:PATH = "$InstallDir$([IO.Path]::PathSeparator)$env:PATH"
            $Channel = @(& (Join-Path $PSScriptRoot 'Get-TestRuntimes.ps1'))
        }
    }

    foreach ($c in $Channel) {
        $number = $c -replace '\.x$', ''
        $metadata = Invoke-ReleaseRequest "$metadataRoot/$number/releases.json"
        $release = @($metadata.releases | Where-Object { $_.'release-version' -eq $metadata.'latest-release' })[0]
        $version = $release.'release-version'
        $runtimeVersion = $release.runtime.version
        $aspNetVersion = $release.'aspnetcore-runtime'.version
        if ((Test-Path -LiteralPath (Join-Path $InstallDir "shared/Microsoft.NETCore.App/$runtimeVersion")) -and
            (Test-Path -LiteralPath (Join-Path $InstallDir "shared/Microsoft.AspNetCore.App/$aspNetVersion"))) {
            "Microsoft.NETCore.App $runtimeVersion and Microsoft.AspNetCore.App $aspNetVersion are already in $InstallDir"
            continue
        }

        $file = @($release.'aspnetcore-runtime'.files | Where-Object { $_.name -eq "aspnetcore-runtime-$suffix" })[0]
        if (-not $file) { throw "Microsoft publishes no aspnetcore-runtime-$suffix for .NET $version." }
        $shared = Join-Path (Expand-Release $file ".NET $version") 'shared'
        foreach ($framework in @(Get-ChildItem -LiteralPath $shared -Directory)) {
            foreach ($versionDir in @(Get-ChildItem -LiteralPath $framework.FullName -Directory)) {
                $target = Join-Path $InstallDir "shared/$($framework.Name)/$($versionDir.Name)"
                if (Test-Path -LiteralPath $target) { continue }
                New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null
                Move-Item -LiteralPath $versionDir.FullName -Destination $target
                "Installed $($framework.Name) $($versionDir.Name) in $InstallDir"
            }
        }
        Remove-Item -LiteralPath $scratch -Recurse -Force
    }
}
finally {
    if (Test-Path -LiteralPath $scratch) { Remove-Item -LiteralPath $scratch -Recurse -Force }
}
