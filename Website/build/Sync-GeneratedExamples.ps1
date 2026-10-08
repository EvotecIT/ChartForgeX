param(
    [string] $SourceRoot = (Join-Path $PSScriptRoot '..\..\ChartForgeX.Examples\bin\Release\net8.0\output'),
    [string] $DestinationRoot = (Join-Path $PSScriptRoot '..\static\examples\generated'),
    [string] $GalleryPath = (Join-Path $PSScriptRoot '..\data\gallery.json'),
    [string] $V2SourceRoot = (Join-Path $PSScriptRoot '..\..\ChartForgeX.Examples\bin\Release\net8.0\output-v2'),
    [string] $V2DestinationRoot = (Join-Path $PSScriptRoot '..\static\examples\generated-v2')
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

$v2ManifestPath = Join-Path $V2SourceRoot 'manifest.json'
if (Test-Path -LiteralPath $v2ManifestPath -PathType Leaf) {
    $v2Source = (Resolve-Path -LiteralPath $V2SourceRoot).Path
    $v2Destination = [IO.Path]::GetFullPath($V2DestinationRoot)
    $manifest = Read-Utf8Text -Path $v2ManifestPath | ConvertFrom-Json
    if ($manifest.schemaVersion -ne 1 -or $manifest.pipeline -ne 'model-to-prepared-scene-to-svg-or-raster') {
        throw 'The gallery sync requires the ChartForgeX prepared-scene catalog manifest.'
    }
    $primary = @($manifest.artifacts | Where-Object { $_.primary -and $_.theme -eq 'light' })
    if ($primary.Count -eq 0) { throw 'The gallery manifest has no primary examples.' }
    if (@($primary.family | Select-Object -Unique).Count -ne $primary.Count) {
        throw 'The gallery manifest must select one primary light example per family.'
    }
    $declaredAssets = @($manifest.assets)
    if ($declaredAssets.Count -eq 0) { throw 'The gallery manifest must declare its linked presentation assets.' }
    foreach ($relative in $declaredAssets) {
        $relative = [string] $relative
        $resolvedAsset = [IO.Path]::GetFullPath((Join-Path $v2Source $relative))
        if ([IO.Path]::IsPathRooted($relative) -or -not $resolvedAsset.StartsWith($v2Source.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) -or
            -not (Test-Path -LiteralPath $resolvedAsset -PathType Leaf)) {
            throw "The gallery manifest references a missing or invalid presentation asset: $relative"
        }
    }
    foreach ($artifact in @($manifest.artifacts)) {
        foreach ($property in @('svg', 'png', 'html', 'source', 'thumbnail', 'thumbnailPng')) {
            $relative = [string] $artifact.$property
            if ([string]::IsNullOrWhiteSpace($relative) -or [IO.Path]::GetFileName($relative) -ne $relative -or
                -not (Test-Path -LiteralPath (Join-Path $v2Source $relative) -PathType Leaf)) {
                throw "The gallery manifest references a missing or invalid $property file: $relative"
            }
        }
    }
    New-Item -ItemType Directory -Force -Path $v2Destination | Out-Null
    if (-not $v2Source.Equals($v2Destination, [StringComparison]::OrdinalIgnoreCase)) {
        foreach ($relative in $declaredAssets) {
            $target = [IO.Path]::GetFullPath((Join-Path $v2Destination $relative))
            if (-not $target.StartsWith($v2Destination.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
                throw "Gallery output must stay within its destination: $relative"
            }
            [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
            Copy-Item -LiteralPath (Join-Path $v2Source $relative) -Destination $target -Force
        }
    }
    Normalize-GeneratedTextArtifacts -Root $v2Destination
    $urlRoot = '/examples/generated-v2/'
    $items = foreach ($artifact in $primary) {
        $exampleKey = $artifact.id.Substring(0, $artifact.id.Length - $artifact.theme.Length - 1)
        $dark = @($manifest.artifacts | Where-Object { $_.id -eq "$exampleKey-dark" })
        if ($dark.Count -ne 1) { throw "A primary example requires its matching dark output: $($artifact.id)" }
        $compact = @($manifest.artifacts | Where-Object { $_.family -eq $artifact.family -and $_.variant -eq 'compact' -and $_.theme -eq 'light' } |
            Sort-Object @{ Expression = { if ($_.id.StartsWith('family-', [StringComparison]::Ordinal)) { 0 } else { 1 } } }, id | Select-Object -First 1)
        $darkCompact = @()
        if ($compact.Count -eq 1) {
            $compactKey = $compact[0].id.Substring(0, $compact[0].id.Length - $compact[0].theme.Length - 1)
            $darkCompact = @($manifest.artifacts | Where-Object { $_.id -eq "$compactKey-dark" })
            if ($darkCompact.Count -ne 1) { throw "A compact example requires its matching dark output: $($compact[0].id)" }
        }
        [pscustomobject][ordered]@{
            category = $artifact.group
            familyLabel = $artifact.familyLabel
            title = $artifact.title
            body = "$($artifact.familyLabel). SVG, PNG and C# source with matching light and dark themes."
            tags = @(@($artifact.seriesKinds) + @($artifact.familyLabel) | Select-Object -Unique)
            image = $urlRoot + $artifact.svg
            thumbnail = $urlRoot + $artifact.thumbnailPng
            source = $urlRoot + $artifact.source
            html = $urlRoot + $artifact.html
            darkImage = $urlRoot + $dark[0].svg
            darkThumbnail = $urlRoot + $dark[0].thumbnailPng
            darkHtml = $urlRoot + $dark[0].html
            darkSource = $urlRoot + $dark[0].source
            compactHtml = if ($compact.Count -eq 1) { $urlRoot + $compact[0].html } else { $null }
            darkCompactHtml = if ($darkCompact.Count -eq 1) { $urlRoot + $darkCompact[0].html } else { $null }
        }
    }
    [pscustomobject][ordered]@{
        categories = @($manifest.groups | ForEach-Object { [pscustomobject][ordered]@{ id = $_.id; label = $_.label } })
        assets = @($declaredAssets | ForEach-Object { $urlRoot + $_ })
        items = @($items)
    } | ConvertTo-Json -Depth 8 | ForEach-Object { Write-Utf8NoBom -Path $GalleryPath -Text $_ }
    Write-Host "Synced $($primary.Count) primary gallery examples from $v2Source"
    return
}

$source = Resolve-Path -LiteralPath $SourceRoot -ErrorAction SilentlyContinue
if (-not $source) {
    Write-Warning "Example output folder not found: $SourceRoot"
    return
}

New-Item -ItemType Directory -Force -Path $DestinationRoot | Out-Null
foreach ($file in Get-ChildItem -LiteralPath $source.Path -File -Include '*.svg', '*.png', '*.gif', '*.apng', '*.html', '*.json', '*.csharp.txt', '*.powershell.txt' -Recurse) {
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
