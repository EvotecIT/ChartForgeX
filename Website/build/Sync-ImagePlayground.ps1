#Requires -Version 7.0
<#
.SYNOPSIS
    Snapshots the ImagePlayground command catalog and ChartForgeX sample outputs for chartforgex.com.

.DESCRIPTION
    Reads CmdletsToExport from ImagePlayground.psd1, groups the ChartForgeX-powered commands into families and writes
    Website/data/imageplayground.json. When -SamplesPath is supplied, copies the selected example outputs into
    Website/static/powershell/. Produce the samples by running the ImagePlayground example scripts against a current
    build, for example:

        $env:IMAGEPLAYGROUND_DEVELOPMENT = '1'
        ./Examples/Charts.ChartForgeX.Showcase.ps1 -OutputDirectory <samples> -Theme Dark

.EXAMPLE
    pwsh ./Website/build/Sync-ImagePlayground.ps1 -ImagePlaygroundRoot C:\Support\GitHub\ImagePlayground -SamplesPath D:\Scratch\ip-samples
#>
[CmdletBinding()]
param(
    [string] $ImagePlaygroundRoot = (Join-Path ($env:EVOTEC_GITHUB_ROOT ?? 'C:\Support\GitHub') 'ImagePlayground'),
    [string] $SamplesPath
)

$ErrorActionPreference = 'Stop'
$website = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $ImagePlaygroundRoot 'ImagePlayground.psd1'
if (-not (Test-Path -LiteralPath $manifestPath)) {
    throw "ImagePlayground.psd1 was not found under '$ImagePlaygroundRoot'. Pass -ImagePlaygroundRoot."
}

$manifest = Import-PowerShellDataFile -LiteralPath $manifestPath
$cmdlets = @($manifest.CmdletsToExport | Sort-Object -Unique)
$families = @(
    [ordered]@{ id = 'charts'; title = 'Charts'; pattern = '^New-ImageChart'; description = 'Line, area, bar, donut, radar, heatmap, treemap, waterfall, word cloud and more, as PNG, SVG or HTML.' }
    [ordered]@{ id = 'topology'; title = 'Topology and org charts'; pattern = 'Topology|Organization'; description = 'Service maps, dependencies, ports, scenarios, route motion and organization charts.' }
    [ordered]@{ id = 'dashboards'; title = 'Dashboards and canvases'; pattern = '^New-Image(Canvas|VisualGrid|MetricCard|ListBlock|TableBlock|TimelineBlock|TimelineItem)'; description = 'Metric cards, tables, lists, timelines and fixed-size social cards or wallpapers.' }
    [ordered]@{ id = 'stories'; title = 'Stories and animation'; pattern = 'ConsoleStory|ImageStory|VisualStory|VisualMotionCue|VisualWatermark|VisualArtifact|StorySource'; description = 'Animated terminal sessions and source-to-result stories as SVG, HTML, GIF or APNG.' }
)

$familyResults = foreach ($family in $families) {
    $names = @($cmdlets | Where-Object { $_ -match $family.pattern })
    [ordered]@{
        id = $family.id
        title = $family.title
        description = $family.description
        commandCount = $names.Count
        commands = $names
    }
}

$commit = (git -C $ImagePlaygroundRoot rev-parse --short HEAD 2>$null) ?? ''
$catalog = [ordered]@{
    generatedBy = 'Website/build/Sync-ImagePlayground.ps1'
    module = [ordered]@{
        name = 'ImagePlayground'
        version = $manifest.ModuleVersion
        commit = $commit
        commandCount = $cmdlets.Count
        chartForgeXCommandCount = [int](@($familyResults | ForEach-Object { $_.commandCount }) | Measure-Object -Sum).Sum
        install = 'Install-Module -Name ImagePlayground -Scope CurrentUser'
        powerShell = 'Windows PowerShell 5.1 and PowerShell 7+'
    }
    families = @($familyResults)
}
$catalogPath = Join-Path $website 'data/imageplayground.json'
$catalog | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $catalogPath -Encoding utf8NoBOM
Write-Host "ImagePlayground $($manifest.ModuleVersion): $($cmdlets.Count) commands, $($catalog.module.chartForgeXCommandCount) powered by ChartForgeX -> $catalogPath"

if ($SamplesPath) {
    $target = Join-Path $website 'static/powershell'
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    $samples = 'social-preview-dark.png', 'service-health-dark.png', 'service-map-dark.png', 'service-map-dark.svg',
        'powershell-console-story-dark.gif', 'ChartsChartForgeXTrend.png', 'ChartsChartForgeXTrend.svg',
        'ChartsChartForgeXDonut.png', 'ChartsChartForgeXWordCloud.png'
    foreach ($sample in $samples) {
        $source = Join-Path $SamplesPath $sample
        if (-not (Test-Path -LiteralPath $source)) { throw "Missing sample '$source'. Run the ImagePlayground examples first." }
        Copy-Item -LiteralPath $source -Destination (Join-Path $target $sample) -Force
    }
    Write-Host "Copied $($samples.Count) samples to $target"
}
