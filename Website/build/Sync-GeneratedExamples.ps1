param(
    [string] $SourceRoot = (Join-Path $PSScriptRoot '..\..\ChartForgeX.Examples\bin\Release\net8.0\output'),
    [string] $DestinationRoot = (Join-Path $PSScriptRoot '..\static\examples\generated'),
    [string] $GalleryPath = (Join-Path $PSScriptRoot '..\data\gallery.json')
)

$ErrorActionPreference = 'Stop'

function ConvertTo-Title {
    param([string] $Slug)

    $text = $Slug -replace '[-_]+', ' '
    $culture = [Globalization.CultureInfo]::GetCultureInfo('en-US')
    return $culture.TextInfo.ToTitleCase($text)
}

function Get-Category {
    param([string] $Slug)

    if ($Slug -match 'topology|replication|dependency|connectivity|site') { return 'topology' }
    if ($Slug -match '(^|-)map($|-)|travel|geo|viewport|nuts|states') { return 'maps' }
    if ($Slug -match 'dashboard|grid|scorecard|summary|kpi') { return 'dashboards' }
    if ($Slug -match 'theme|palette|style|font') { return 'themes' }
    if ($Slug -match 'pictorial|word-cloud|people|infographic') { return 'infographics' }
    return 'charts'
}

function Get-InferredTags {
    param([string] $Slug)

    $rules = [ordered]@{
        '(^|-)line($|-)|trend|sparkline|step-line' = 'Line'
        'area|stacked-area' = 'Area'
        '(^|-)bar($|-)|bars|stacked-bar|stacked-column|column|lollipop|pareto' = 'Bar'
        'combo' = 'Combo'
        'scatter|observed-remediation-trend' = 'Scatter'
        'bubble|clusters' = 'Bubble'
        'heatmap|matrix' = 'Heatmap'
        'calendar' = 'CalendarHeatmap'
        'histogram' = 'Histogram'
        'boxplot|box-plot' = 'BoxPlot'
        'candlestick' = 'Candlestick'
        'ohlc' = 'OHLC'
        'gauge' = 'Gauge'
        'radar' = 'Radar'
        'polar-area' = 'PolarArea'
        'pie' = 'Pie'
        'donut' = 'Donut'
        'treemap' = 'Treemap'
        'funnel' = 'Funnel'
        'waterfall' = 'Waterfall'
        'sankey' = 'Sankey'
        '(^|-)map($|-)|geo|travel|viewport|nuts|states' = 'Map'
        'timeline' = 'Timeline'
        'gantt|schedule' = 'Gantt'
        'word-cloud' = 'WordCloud'
        'pictorial|isotype' = 'Pictorial'
    }

    $tags = [System.Collections.Generic.List[string]]::new()
    foreach ($entry in $rules.GetEnumerator()) {
        if ($Slug -match $entry.Key -and -not $tags.Contains($entry.Value)) {
            $tags.Add($entry.Value)
        }
    }

    if ($tags.Count -eq 0) {
        return ,@()
    }

    return ,$tags.ToArray()
}

function Merge-Tags {
    param(
        [string] $Slug,
        [object[]] $ExistingTags,
        [object[]] $InferredTags
    )

    $merged = [System.Collections.Generic.List[string]]::new()
    foreach ($tag in @($ExistingTags) + @($InferredTags)) {
        if ($null -eq $tag) {
            continue
        }

        $text = [string] $tag
        if ([string]::IsNullOrWhiteSpace($text) -or $merged.Contains($text)) {
            continue
        }

        if ($text -eq 'Map' -and $Slug -notmatch '(^|-)map($|-)|geo|travel|viewport|nuts|states') {
            continue
        }

        if ($text -eq 'Bar' -and $Slug -match 'errorbar|radialbar|stacked-area') {
            continue
        }

        if ($text -eq 'Line' -and $Slug -match 'timeline') {
            continue
        }

        $merged.Add($text)
        if ($text -like '*Combo*' -and -not $merged.Contains('Combo')) {
            $merged.Add('Combo')
        }
    }

    if ($merged.Count -eq 0) {
        return ,@()
    }

    return ,$merged.ToArray()
}

function Write-Utf8NoBom {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path,
        [Parameter(Mandatory = $true)]
        [string] $Text
    )

    $encoding = [System.Text.UTF8Encoding]::new($false)
    $normalized = $Text -replace "`r`n", "`n"
    if (-not $normalized.EndsWith("`n")) {
        $normalized += "`n"
    }

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $directory = [System.IO.Path]::GetDirectoryName($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($directory) -and -not (Test-Path -LiteralPath $directory)) {
        New-Item -ItemType Directory -Force -Path $directory | Out-Null
    }

    [System.IO.File]::WriteAllText($fullPath, $normalized, $encoding)
}

function Read-Utf8Text {
    param(
        [Parameter(Mandatory = $true)]
        [string] $Path
    )

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $text = [System.IO.File]::ReadAllText($fullPath, [System.Text.Encoding]::UTF8)
    if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
        return $text.Substring(1)
    }

    return $text
}

function Normalize-GeneratedTextArtifacts {
    param([string] $Root)

    $textExtensions = @('.svg', '.html', '.json', '.txt', '.css', '.js')
    foreach ($file in Get-ChildItem -LiteralPath $Root -File -Recurse) {
        if ($textExtensions -notcontains $file.Extension.ToLowerInvariant()) {
            continue
        }

        $text = [System.IO.File]::ReadAllText($file.FullName, [System.Text.Encoding]::UTF8)
        if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) {
            $text = $text.Substring(1)
        }

        Write-Utf8NoBom -Path $file.FullName -Text $text
    }
}

$source = Resolve-Path -LiteralPath $SourceRoot -ErrorAction SilentlyContinue
if (-not $source) {
    Write-Warning "Example output folder not found: $SourceRoot"
    return
}

New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source.Path -File -Include '*.svg', '*.png', '*.html', '*.csharp.txt', '*.powershell.txt' -Recurse) {
    $relativePath = [System.IO.Path]::GetRelativePath($source.Path, $file.FullName)
    $targetName = $file.Name
    if ($relativePath -match '[\\/]' -and $file.Name -ieq 'index.html') {
        $parent = Split-Path -Parent $relativePath
        $targetName = ($parent -replace '[\\/]+', '-') + '.html'
    }

    Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $DestinationRoot $targetName) -Force
}
Normalize-GeneratedTextArtifacts -Root $DestinationRoot

$existingByImage = @{}
if (Test-Path -LiteralPath $GalleryPath) {
    $existing = Read-Utf8Text -Path $GalleryPath | ConvertFrom-Json
    foreach ($item in @($existing.items)) {
        if ($item.image) {
            $existingByImage[$item.image] = $item
        }
    }
}

$items = foreach ($svg in Get-ChildItem -LiteralPath $DestinationRoot -File -Filter '*.svg' | Sort-Object Name) {
    $slug = [IO.Path]::GetFileNameWithoutExtension($svg.Name)
    $image = "/examples/generated/$($svg.Name)"
    $inferredTags = Get-InferredTags -Slug $slug
    if ($existingByImage.ContainsKey($image)) {
        $item = $existingByImage[$image]
        $mergedTags = Merge-Tags -Slug $slug -ExistingTags @($item.tags) -InferredTags $inferredTags
        if ($item.PSObject.Properties.Name -contains 'tags') {
            $item.tags = $mergedTags
        } else {
            $item | Add-Member -MemberType NoteProperty -Name tags -Value $mergedTags
        }

        $item
        continue
    }

    [pscustomobject]@{
        category = Get-Category -Slug $slug
        title = ConvertTo-Title -Slug $slug
        body = 'Generated ChartForgeX visual output.'
        tags = Merge-Tags -Slug $slug -ExistingTags @() -InferredTags $inferredTags
        image = $image
    }
}

$categories = @(
    [pscustomobject]@{ id = 'charts'; label = 'Charts' }
    [pscustomobject]@{ id = 'dashboards'; label = 'Dashboards' }
    [pscustomobject]@{ id = 'topology'; label = 'Topology' }
    [pscustomobject]@{ id = 'maps'; label = 'Maps' }
    [pscustomobject]@{ id = 'infographics'; label = 'Infographics' }
    [pscustomobject]@{ id = 'themes'; label = 'Themes' }
)

[pscustomobject]@{
    categories = $categories
    items = @($items)
} | ConvertTo-Json -Depth 8 | ForEach-Object {
    Write-Utf8NoBom -Path $GalleryPath -Text $_
}

Write-Host "Synced $(@($items).Count) gallery item(s) from $($source.Path)"
