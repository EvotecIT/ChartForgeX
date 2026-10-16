#Requires -Version 7.0
<#
.SYNOPSIS
    Snapshots the PowerBGInfo wallpaper showcase and feature renders for chartforgex.com/powerbginfo/.

.DESCRIPTION
    Reads the manifest.json files that PowerBGInfo's Examples/Run.BGInfo.Showcase.ps1 (-RenderPath) and
    Examples/Run.BGInfo.Features.ps1 (-FeaturePath) write next to their renders, copies the renders the page uses to
    Website/static/powerbginfo/ and Website/static/powerbginfo/features/, and writes
    Website/data/powerbginfo-showcase.json with each scenario's script, read from the PowerBGInfo repository at the
    commit that produced the renders. The commit is recorded in the data file, so the page can link every script to
    the exact source it shows.

    From the scenario renders the page uses the 1920 x 1080 render of each scenario in both themes, plus the
    -ScreenSizes renders (the ultrawide and portrait screens) of the scenarios that have them. The 2560 x 1440 renders
    are skipped: the page shows every wallpaper at most 1240 pixels wide, so they would only add weight. Each
    scenario's script is also written to static/powerbginfo/scripts/<file>.txt for the scenario viewer, which slices
    the presentation and data lines out of it.

    From the feature renders the page uses the 1920 x 1080 render of each widget in both themes, every theme preset,
    and the region example at 1920 x 1080, 3440 x 1440 and 1080 x 1920.

    The script owns the PNG files in static/powerbginfo/ and static/powerbginfo/features/ and the scripts in
    static/powerbginfo/scripts/, and clears them on each run. It never changes the PowerBGInfo repository; it only
    reads files with git show.

.EXAMPLE
    pwsh ./Website/build/Sync-PowerBGInfo.ps1 -RepositoryPath C:\Support\GitHub\PowerBGInfo `
        -Ref cd0c215997fc4c9dcd170d6b49d79edd2d27b74e -RenderPath C:\Temp\Showcase -FeaturePath C:\Temp\Features
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $RepositoryPath,
    [Parameter(Mandatory)] [string] $Ref,
    [Parameter(Mandatory)] [string] $RenderPath,
    [Parameter(Mandatory)] [string] $FeaturePath,
    # Scenario renders shown besides the 1920 x 1080 ones, to show that one script fits any screen.
    [string[]] $ScreenSizes = @('3440x1440', '1080x1920')
)

$ErrorActionPreference = 'Stop'
$website = Split-Path -Parent $PSScriptRoot
$repository = 'EvotecIT/PowerBGInfo'
# Widgets shown in the capabilities grid, plus the examples the logic section uses.
$widgetIds = 'metrics', 'tables', 'lists', 'calendar-heatmap', 'timeline', 'treemap', 'mermaid', 'qrcode', 'system-info'
$regionSizes = '1920x1080', '3440x1440', '1080x1920'

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

function Read-Manifest([string] $Path) {
    $manifest = Get-Content -LiteralPath (Join-Path $Path 'manifest.json') -Raw | ConvertFrom-Json
    if ($manifest.generator -ne 'PowerBGInfo') { throw "$Path/manifest.json was not written by a PowerBGInfo runner." }
    return $manifest
}

# Copies one render after checking its size against the manifest, and returns what the page needs to show it.
function Copy-Render($Render, [string] $From, [string] $To, [string] $Url) {
    $source = Join-Path $From $Render.file
    $size = Get-PngSize $source
    if ($size.Width -ne $Render.width -or $size.Height -ne $Render.height) {
        throw "$($Render.file) is $($size.Width)x$($size.Height); the manifest says $($Render.width)x$($Render.height)."
    }
    Copy-Item -LiteralPath $source -Destination (Join-Path $To $Render.file) -Force
    return [ordered] @{ url = "$Url/$($Render.file)"; file = $Render.file; width = $size.Width; height = $size.Height; kb = [math]::Round($size.Bytes / 1KB) }
}

function Clear-Renders([string] $Path, [string] $Filter = '*.png') {
    New-Item -ItemType Directory -Force -Path $Path | Out-Null
    Get-ChildItem -LiteralPath $Path -Filter $Filter -File | Remove-Item -Force
}

$manifest = Read-Manifest $RenderPath
$target = Join-Path $website 'static/powerbginfo'
$scriptTarget = Join-Path $target 'scripts'
Clear-Renders $target
Clear-Renders $scriptTarget '*.txt'

$scenarios = [ordered]@{}
foreach ($render in $manifest.renders) {
    $size = "$($render.width)x$($render.height)"
    if ($size -ne '1920x1080' -and $size -notin $ScreenSizes) { continue }
    $copied = Copy-Render $render $RenderPath $target '/powerbginfo'
    if (-not $scenarios.Contains($render.scenario)) {
        $script = Get-SourceText $render.script
        $lines = $script.TrimEnd().Split("`n")
        # The excerpt is the New-BGInfo call: the part of the script that places what the wallpaper shows.
        $start = [array]::FindIndex($lines, [Predicate[string]] { param($line) $line -match '^New-BGInfo\b' })
        if ($start -lt 0) { throw "$($render.script) has no top-level New-BGInfo call." }
        # The data block is the part a reader replaces with their own sources: whole lines from #region Data to the
        # #endregion that closes it.
        $dataFrom = [array]::FindIndex($lines, [Predicate[string]] { param($line) $line -match '^#region Data\b' })
        $dataTo = if ($dataFrom -ge 0) { [array]::FindIndex($lines, $dataFrom, [Predicate[string]] { param($line) $line -match '^#endregion\b' }) } else { -1 }
        # Newer runners name the preset in the manifest; older ones only in the script.
        $preset = if ($render.preset) { "$($render.preset)" } else {
            $match = [regex]::Match($script, 'New-BGInfoTheme\s+(?:-Preset\s+)?(\w+)')
            if ($match.Success) { $match.Groups[1].Value } else { $null }
        }
        $file = Split-Path -Leaf $render.script
        Set-Content -LiteralPath (Join-Path $scriptTarget "$file.txt") -Value $script -NoNewline -Encoding utf8NoBOM
        $scenarios[$render.scenario] = [ordered]@{
            id          = $render.scenario
            title       = $render.title
            purpose     = $render.purpose
            script      = $render.script
            file        = $file
            url         = "https://github.com/$repository/blob/$commit/$($render.script)"
            text        = "/powerbginfo/scripts/$file.txt"
            lines       = $lines.Count
            theme       = $preset
            excerptFrom = $start + 1
            excerpt     = ($lines[$start..($lines.Count - 1)] -join "`n") + "`n"
            dataFrom    = if ($dataTo -ge 0) { $dataFrom + 1 } else { $null }
            dataTo      = if ($dataTo -ge 0) { $dataTo + 1 } else { $null }
            data        = if ($dataTo -ge 0) { ($lines[$dataFrom..$dataTo] -join "`n") + "`n" } else { $null }
            source      = $script
            features    = @($render.chartForgeXFeatures)
            renders     = [ordered]@{}
            screens     = [ordered]@{}
        }
    }
    $scenario = $scenarios[$render.scenario]
    if ($size -eq '1920x1080') {
        $scenario.renders[$render.theme] = $copied
    } else {
        if (-not $scenario.screens.Contains($size)) {
            $scenario.screens[$size] = [ordered]@{ id = $size; width = $render.width; height = $render.height; renders = [ordered]@{} }
        }
        $scenario.screens[$size].renders[$render.theme] = $copied
    }
}
foreach ($scenario in $scenarios.Values) {
    foreach ($mode in 'light', 'dark') {
        if (-not $scenario.renders.Contains($mode)) { throw "$RenderPath has no 1920x1080 $mode render of '$($scenario.id)'." }
    }
}

$featureManifest = Read-Manifest $FeaturePath
$featureTarget = Join-Path $target 'features'
Clear-Renders $featureTarget
$widgets = [ordered] @{}
$themes = [ordered] @{}
$regions = [ordered] @{}
foreach ($render in $featureManifest.renders) {
    $size = "$($render.width)x$($render.height)"
    $entry = $null
    if ($render.feature -in $widgetIds -and $size -eq '1920x1080') {
        if (-not $widgets.Contains($render.feature)) {
            $widgets[$render.feature] = [ordered] @{
                id = $render.feature; script = $render.script; file = Split-Path -Leaf $render.script
                url = "https://github.com/$repository/blob/$commit/$($render.script)"; renders = [ordered] @{}
            }
        }
        $entry = $widgets[$render.feature]
    } elseif ($render.feature -eq 'themes' -and $size -eq '1920x1080') {
        # Theme renders are named themes-<preset>-<mode>-<width>.png.
        $preset = [regex]::Match($render.file, '^themes-([a-z]+)-').Groups[1].Value
        if (-not $themes.Contains($preset)) {
            $themes[$preset] = [ordered] @{ id = $preset; name = (Get-Culture).TextInfo.ToTitleCase($preset); renders = [ordered] @{} }
        }
        $entry = $themes[$preset]
    } elseif ($render.feature -eq 'regions' -and $size -in $regionSizes) {
        if (-not $regions.Contains($size)) {
            $regions[$size] = [ordered] @{ id = $size; width = $render.width; height = $render.height; renders = [ordered] @{} }
        }
        $entry = $regions[$size]
    }
    if ($entry) { $entry.renders[$render.theme] = Copy-Render $render $FeaturePath $featureTarget '/powerbginfo/features' }
}
foreach ($id in $widgetIds) { if (-not $widgets.Contains($id)) { throw "$FeaturePath has no 1920x1080 render of '$id'." } }
foreach ($id in $regionSizes) { if (-not $regions.Contains($id)) { throw "$FeaturePath has no $id region render." } }
$regionScript = 'Examples/Features/Feature.Regions.ps1'
$themeScript = 'Examples/Features/Feature.Themes.ps1'

# Import-PowerShellDataFile reads the manifest as data only; it needs a file, so stage the committed text in TEMP.
$manifestCopy = Join-Path ([System.IO.Path]::GetTempPath()) "PowerBGInfo-$($commit.Substring(0, 7)).psd1"
Set-Content -LiteralPath $manifestCopy -Value (Get-SourceText 'PowerBGInfo.psd1') -Encoding utf8NoBOM
try { $module = Import-PowerShellDataFile -LiteralPath $manifestCopy } finally { Remove-Item -LiteralPath $manifestCopy -Force }

$data = [ordered]@{
    generatedBy = 'Website/build/Sync-PowerBGInfo.ps1'
    source      = [ordered]@{
        repository  = $repository
        url         = "https://github.com/$repository"
        commit      = $commit
        short       = $commit.Substring(0, 7)
        runner      = 'Examples/Run.BGInfo.Showcase.ps1'
        features    = 'Examples/Run.BGInfo.Features.ps1'
        featuresUrl = "https://github.com/$repository/tree/$commit/Examples/Features"
    }
    module      = [ordered]@{
        version           = "$($module.ModuleVersion)"
        cmdletCount       = @($module.CmdletsToExport).Count
        powerShellVersion = $module.PowerShellVersion
        dotNetFramework   = $module.DotNetFrameworkVersion
    }
    scenarios   = @($scenarios.Values)
    themes      = [ordered] @{
        script  = $themeScript
        url     = "https://github.com/$repository/blob/$commit/$themeScript"
        presets = @($themes.Values)
    }
    regions     = [ordered] @{
        script = $regionScript
        url    = "https://github.com/$repository/blob/$commit/$regionScript"
        sizes  = @($regionSizes | ForEach-Object { $regions[$_] })
    }
    widgets     = $widgets
}

$dataPath = Join-Path $website 'data/powerbginfo-showcase.json'
Set-Content -LiteralPath $dataPath -Value (($data | ConvertTo-Json -Depth 8).Replace("`r`n", "`n") + "`n") -NoNewline -Encoding utf8NoBOM
$total = (@(Get-ChildItem -LiteralPath $target -Filter '*.png' -File) + @(Get-ChildItem -LiteralPath $featureTarget -Filter '*.png' -File) | Measure-Object Length -Sum).Sum
$screens = @($scenarios.Values | ForEach-Object { $_.screens.Count } | Measure-Object -Sum).Sum
Write-Host "PowerBGInfo $($commit.Substring(0, 7)): $($scenarios.Count) scenarios ($screens extra screen sizes), $($widgets.Count) widgets, $($themes.Count) themes, $($regions.Count) region sizes, $([math]::Round($total / 1KB)) KB -> $dataPath"
