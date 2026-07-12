param(
    [switch] $Check
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot

$assets = @(
    @{
        Source = 'ChartForgeX.Interactivity.Html/Assets/topology-interaction.source'
        Target = 'ChartForgeX.Interactivity.Html/Assets/topology-interaction.js'
    },
    @{
        Source = 'ChartForgeX.Interactivity.Html/Assets/interactive.source'
        Target = 'ChartForgeX.Interactivity.Html/Assets/interactive.js'
    }
)

function Join-AssetSource {
    param([string] $SourcePath)

    $parts = Get-ChildItem -Path $SourcePath -Filter '*.js' -File |
        Sort-Object Name

    if ($parts.Count -eq 0) {
        throw "No JavaScript source fragments found in '$SourcePath'."
    }

    $content = foreach ($part in $parts) {
        [System.IO.File]::ReadAllText($part.FullName).Replace("`r`n", "`n").Replace("`r", "`n").TrimEnd("`n")
    }

    ($content -join "`n") + "`n"
}

foreach ($asset in $assets) {
    $sourcePath = Join-Path $root $asset.Source
    $targetPath = Join-Path $root $asset.Target
    $generated = Join-AssetSource -SourcePath $sourcePath

    if ($Check) {
        $current = [System.IO.File]::ReadAllText($targetPath).Replace("`r`n", "`n").Replace("`r", "`n")
        if ($current -ne $generated) {
            throw "Generated asset is out of date: $($asset.Target). Run Build/Sync-InteractiveAssets.ps1."
        }

        continue
    }

    [System.IO.File]::WriteAllText($targetPath, $generated, [System.Text.UTF8Encoding]::new($false))
}
