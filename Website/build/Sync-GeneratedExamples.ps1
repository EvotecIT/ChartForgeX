param(
    [string] $DestinationRoot = (Join-Path $PSScriptRoot '..\static\examples\generated'),
    [string] $GalleryPath = (Join-Path $PSScriptRoot '..\data\gallery.json'),
    [string] $SourceRoot = (Join-Path $PSScriptRoot '..\..\ChartForgeX.Examples\bin\Release\net8.0\output-v2'),
    [string] $ScenarioSourceRoot = (Join-Path $PSScriptRoot '..\..\ChartForgeX.Examples\bin\Release\net8.0\output'),
    [string] $PromotedCasesPath = (Join-Path $PSScriptRoot '..\static\examples\promoted-cases.json')
)

$ErrorActionPreference = 'Stop'

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

function Get-NormalizedDirectoryPath {
    param([string] $Path)

    $fullPath = [IO.Path]::GetFullPath($Path)
    if ($fullPath.Length -gt [IO.Path]::GetPathRoot($fullPath).Length) {
        return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    }
    return $fullPath
}

function Normalize-GeneratedTextArtifacts {
    param([string] $Root, [string[]] $Assets)

    $textExtensions = @('.svg', '.html', '.json', '.txt', '.css', '.js')
    foreach ($relative in $Assets) {
        $file = Get-Item -LiteralPath (Join-Path $Root $relative)
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

$v2ManifestPath = Join-Path $SourceRoot 'manifest.json'
if (-not (Test-Path -LiteralPath $v2ManifestPath -PathType Leaf)) {
    throw 'The gallery sync requires fresh prepared-scene output. Run ChartForgeX.Examples with --v2-only --v2-curated first.'
}
$v2Source = Get-NormalizedDirectoryPath -Path (Resolve-Path -LiteralPath $SourceRoot).Path
$v2Destination = Get-NormalizedDirectoryPath -Path $DestinationRoot
$pathComparison = if ([IO.Path]::DirectorySeparatorChar -eq '\') { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
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
    if ([IO.Path]::IsPathRooted($relative) -or -not $resolvedAsset.StartsWith($v2Source.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, $pathComparison) -or
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
$promoted = Read-Utf8Text -Path $PromotedCasesPath | ConvertFrom-Json
$scenarioFiles = @($promoted.cases | ForEach-Object {
    foreach ($property in $_.artifacts.PSObject.Properties) {
        if ($property.Value -is [string]) {
            $relative = ([string] $property.Value).Replace('/examples/generated/', '')
            if (-not ([string] $property.Value).StartsWith('/examples/generated/', [StringComparison]::Ordinal) -or
                [IO.Path]::GetFileName($relative) -ne $relative -or
                -not (Test-Path -LiteralPath (Join-Path $ScenarioSourceRoot $relative) -PathType Leaf)) {
                throw "A promoted example requires fresh scenario output: $($property.Value)"
            }
            $relative
        }
    }
} | Select-Object -Unique)
$previousAssets = @()
if (Test-Path -LiteralPath (Join-Path $v2Destination 'manifest.json') -PathType Leaf) {
    $previousAssets = @((Read-Utf8Text -Path (Join-Path $v2Destination 'manifest.json') | ConvertFrom-Json).assets)
}
if (Test-Path -LiteralPath $GalleryPath -PathType Leaf) {
    $previousAssets += @((Read-Utf8Text -Path $GalleryPath | ConvertFrom-Json).assets |
        Where-Object { $_.StartsWith('/examples/generated/', [StringComparison]::Ordinal) } |
        ForEach-Object { $_.Substring('/examples/generated/'.Length) })
}
$ownedAssets = @(@($declaredAssets) + @($scenarioFiles) | Select-Object -Unique)
$retiredAssets = @($previousAssets | Select-Object -Unique | Where-Object { $_ -notin $ownedAssets })
# Validate all writes and retirements before copying; only inspect the selected output boundary.
foreach ($relative in @($ownedAssets) + @($retiredAssets)) {
    $target = [IO.Path]::GetFullPath((Join-Path $v2Destination $relative))
    if ([IO.Path]::IsPathRooted($relative) -or
        -not $target.StartsWith($v2Destination.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, $pathComparison)) {
        throw "Invalid gallery output asset: $relative"
    }
    for ($entryPath = $target; ; $entryPath = [IO.Path]::GetDirectoryName($entryPath)) {
        $entry = Get-Item -LiteralPath $entryPath -Force -ErrorAction SilentlyContinue
        if ($null -ne $entry -and ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Gallery output cannot pass through a filesystem link: $relative"
        }
        if ($entryPath.Equals($v2Destination, $pathComparison)) { break }
    }
}
# Keep ownership recoverable if a refresh stops while copying promoted files.
$pendingOwnership = if (Test-Path -LiteralPath $GalleryPath -PathType Leaf) {
    Read-Utf8Text -Path $GalleryPath | ConvertFrom-Json
} else { [pscustomobject][ordered]@{ categories = @(); items = @(); assets = @() } }
$pendingOwnership | Add-Member -MemberType NoteProperty -Name assets -Force -Value @(
    @($previousAssets) + @($ownedAssets) | Select-Object -Unique | ForEach-Object { '/examples/generated/' + $_ }
)
$pendingOwnership | ConvertTo-Json -Depth 8 | ForEach-Object { Write-Utf8NoBom -Path $GalleryPath -Text $_ }
New-Item -ItemType Directory -Force -Path $v2Destination | Out-Null
if (-not $v2Source.Equals($v2Destination, $pathComparison)) {
    foreach ($relative in $declaredAssets) {
        $target = [IO.Path]::GetFullPath((Join-Path $v2Destination $relative))
        if (-not $target.StartsWith($v2Destination.TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar, $pathComparison)) {
            throw "Gallery output must stay within its destination: $relative"
        }
        [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
        Copy-Item -LiteralPath (Join-Path $v2Source $relative) -Destination $target -Force
    }
}
foreach ($relative in $scenarioFiles) {
    Copy-Item -LiteralPath (Join-Path $ScenarioSourceRoot $relative) -Destination (Join-Path $v2Destination $relative) -Force
}
# Prune only previously published files; unrelated files retain their original bytes.
foreach ($relative in $retiredAssets) {
    $target = [IO.Path]::GetFullPath((Join-Path $v2Destination $relative))
    if (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -ErrorAction Stop }
}
Normalize-GeneratedTextArtifacts -Root $v2Destination -Assets $ownedAssets
$urlRoot = '/examples/generated/'
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
    assets = @($ownedAssets | ForEach-Object { $urlRoot + $_ })
    items = @($items)
} | ConvertTo-Json -Depth 8 | ForEach-Object { Write-Utf8NoBom -Path $GalleryPath -Text $_ }
Write-Host "Synced $($primary.Count) primary gallery examples from $v2Source"
