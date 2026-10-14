#Requires -Version 7.0
<#
.SYNOPSIS
    Snapshots the ImagePlayground commands powered by ChartForgeX for chartforgex.com.

.DESCRIPTION
    Reads CmdletsToExport from an ImagePlayground module manifest, groups the ChartForgeX-powered commands into
    families and writes Website/data/imageplayground.json. Point it at the same published module version that
    Render-PowerShellExamples.ps1 runs, so the command list and the examples describe one release.

.EXAMPLE
    pwsh ./Website/build/Sync-ImagePlayground.ps1

.EXAMPLE
    pwsh ./Website/build/Sync-ImagePlayground.ps1 -ModulePath D:\Modules\ImagePlayground\3.2.7\ImagePlayground.psd1
#>
[CmdletBinding()]
param(
    [string] $ModulePath
)

$ErrorActionPreference = 'Stop'
$website = Split-Path -Parent $PSScriptRoot
if (-not $ModulePath) {
    $installed = Get-Module ImagePlayground -ListAvailable | Sort-Object Version -Descending | Select-Object -First 1
    if (-not $installed) { throw 'ImagePlayground is not installed. Run Install-Module ImagePlayground or pass -ModulePath.' }
    $ModulePath = $installed.Path
}

$manifest = Import-PowerShellDataFile -LiteralPath $ModulePath
$cmdlets = @($manifest.CmdletsToExport | Sort-Object -Unique)
$families = @(
    [ordered]@{ id = 'charts'; title = 'Charts'; pattern = '^New-ImageChart'; description = 'Line, area, bar, donut, radar, heatmap, treemap, waterfall, word cloud and more, as PNG, SVG, JPEG or HTML.' }
    [ordered]@{ id = 'topology'; title = 'Topology and org charts'; pattern = 'Topology|Organization'; description = 'Service maps, groups, ports, node details, geographic layout, scenarios, route motion and organization charts.' }
    [ordered]@{ id = 'dashboards'; title = 'Dashboards and canvases'; pattern = '^New-Image(Canvas|VisualGrid|MetricCard|ListBlock|TableBlock|TimelineBlock|TimelineItem)'; description = 'Metric cards, tables, lists and timelines in one grid, plus fixed-size social cards and wallpapers.' }
    [ordered]@{ id = 'stories'; title = 'Stories, motion and export'; pattern = 'ConsoleStory|ImageStory|VisualStory|VisualMotionCue|VisualWatermark|VisualArtifact|StorySource'; description = 'Animated terminal sessions, source-to-result stories, motion cues, watermarks and visual artifacts.' }
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

$catalog = [ordered]@{
    generatedBy = 'Website/build/Sync-ImagePlayground.ps1'
    module = [ordered]@{
        name = 'ImagePlayground'
        version = $manifest.ModuleVersion
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
