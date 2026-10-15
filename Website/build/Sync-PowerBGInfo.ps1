#Requires -Version 7.0
<#
.SYNOPSIS
    Snapshots the PowerBGInfo wallpaper showcase for chartforgex.com/powerbginfo/.

.DESCRIPTION
    Reads the manifest.json that PowerBGInfo's Examples/Run.BGInfo.Showcase.ps1 writes next to its renders, copies the
    renders to Website/static/powerbginfo/ and writes Website/data/powerbginfo-showcase.json with each scenario's
    script, read from the PowerBGInfo repository at the commit that produced the renders. The commit is recorded in the
    data file, so the page can link every script to the exact source it shows.

    The script owns static/powerbginfo/ and clears its PNG files on each run. It never changes the PowerBGInfo
    repository; it only reads files with git show.

.EXAMPLE
    pwsh ./Website/build/Sync-PowerBGInfo.ps1 -RepositoryPath C:\Support\GitHub\PowerBGInfo `
        -Ref 4016776e2c2d7e4b381c7b057cd190fc16afa572 -RenderPath C:\Temp\Showcase
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RepositoryPath,
    [Parameter(Mandatory)] [string] $Ref,
    [Parameter(Mandatory)] [string] $RenderPath
)

$ErrorActionPreference = 'Stop'
$website = Split-Path -Parent $PSScriptRoot
$repository = 'EvotecIT/PowerBGInfo'

$commit = (git -C $RepositoryPath rev-parse --verify "$Ref^{commit}").Trim()
if ($LASTEXITCODE -ne 0 -or -not $commit) { throw "Cannot resolve '$Ref' in $RepositoryPath." }

function Get-SourceText([string] $Path) {
    $text = git -C $RepositoryPath show "${commit}:$Path"
    if ($LASTEXITCODE -ne 0) { throw "$Path does not exist at $commit." }
    # git returns lines; rejoin them with LF and drop a leading byte-order mark.
    return (($text -join "`n").TrimStart([char] 0xFEFF)).TrimEnd() + "`n"
}

function Get-PngSize([string] $Path) {
    $bytes = [System.IO.File]::ReadAllBytes($Path)
    # Width and height are the big-endian integers of the IHDR chunk; widen each byte before shifting it.
    $read = { param($offset) ([int] $bytes[$offset] -shl 24) -bor ([int] $bytes[$offset + 1] -shl 16) -bor ([int] $bytes[$offset + 2] -shl 8) -bor [int] $bytes[$offset + 3] }
    return @{ Width = & $read 16; Height = & $read 20; Bytes = $bytes.Length }
}

$manifest = Get-Content -LiteralPath (Join-Path $RenderPath 'manifest.json') -Raw | ConvertFrom-Json
if ($manifest.generator -ne 'PowerBGInfo') { throw "$RenderPath/manifest.json was not written by the PowerBGInfo showcase." }

$target = Join-Path $website 'static/powerbginfo'
New-Item -ItemType Directory -Force -Path $target | Out-Null
Get-ChildItem -LiteralPath $target -Filter '*.png' -File | Remove-Item -Force

$scenarios = [ordered]@{}
foreach ($render in $manifest.renders) {
    $source = Join-Path $RenderPath $render.file
    $size = Get-PngSize $source
    if ($size.Width -ne $render.width -or $size.Height -ne $render.height) {
        throw "$($render.file) is $($size.Width)x$($size.Height); the manifest says $($render.width)x$($render.height)."
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $target $render.file) -Force

    if (-not $scenarios.Contains($render.scenario)) {
        $script = Get-SourceText $render.script
        $lines = $script.TrimEnd().Split("`n")
        # The excerpt is the New-BGInfo call: the part of the script that places what the wallpaper shows.
        $start = [array]::FindIndex($lines, [Predicate[string]] { param($line) $line -match '^New-BGInfo\b' })
        if ($start -lt 0) { throw "$($render.script) has no top-level New-BGInfo call." }
        $scenarios[$render.scenario] = [ordered]@{
            id          = $render.scenario
            title       = $render.title
            purpose     = $render.purpose
            script      = $render.script
            file        = Split-Path -Leaf $render.script
            url         = "https://github.com/$repository/blob/$commit/$($render.script)"
            lines       = $lines.Count
            excerptFrom = $start + 1
            excerpt     = ($lines[$start..($lines.Count - 1)] -join "`n") + "`n"
            source      = $script
            features    = @($render.chartForgeXFeatures)
            renders     = [ordered]@{}
        }
    }
    $scenarios[$render.scenario].renders[$render.theme] = [ordered]@{
        url    = "/powerbginfo/$($render.file)"
        file   = $render.file
        width  = $size.Width
        height = $size.Height
        kb     = [math]::Round($size.Bytes / 1KB)
    }
}

# Import-PowerShellDataFile reads the manifest as data only; it needs a file, so stage the committed text in TEMP.
$manifestCopy = Join-Path ([System.IO.Path]::GetTempPath()) "PowerBGInfo-$($commit.Substring(0, 7)).psd1"
Set-Content -LiteralPath $manifestCopy -Value (Get-SourceText 'PowerBGInfo.psd1') -Encoding utf8NoBOM
try { $module = Import-PowerShellDataFile -LiteralPath $manifestCopy } finally { Remove-Item -LiteralPath $manifestCopy -Force }
$theme = Get-SourceText 'Examples/Showcase/HudTheme.ps1'

$data = [ordered]@{
    generatedBy = 'Website/build/Sync-PowerBGInfo.ps1'
    source      = [ordered]@{
        repository = $repository
        url        = "https://github.com/$repository"
        commit     = $commit
        short      = $commit.Substring(0, 7)
        runner     = 'Examples/Run.BGInfo.Showcase.ps1'
        theme      = 'Examples/Showcase/HudTheme.ps1'
        themeUrl   = "https://github.com/$repository/blob/$commit/Examples/Showcase/HudTheme.ps1"
        themeLines = $theme.TrimEnd().Split("`n").Count
    }
    module      = [ordered]@{
        cmdletCount       = @($module.CmdletsToExport).Count
        powerShellVersion = $module.PowerShellVersion
        dotNetFramework   = $module.DotNetFrameworkVersion
    }
    scenarios   = @($scenarios.Values)
}

$dataPath = Join-Path $website 'data/powerbginfo-showcase.json'
Set-Content -LiteralPath $dataPath -Value (($data | ConvertTo-Json -Depth 8).Replace("`r`n", "`n") + "`n") -NoNewline -Encoding utf8NoBOM
$total = (Get-ChildItem -LiteralPath $target -Filter '*.png' -File | Measure-Object Length -Sum).Sum
Write-Host "PowerBGInfo $($commit.Substring(0, 7)): $($scenarios.Count) scenarios, $($manifest.renders.Count) renders, $([math]::Round($total / 1KB)) KB -> $dataPath"
