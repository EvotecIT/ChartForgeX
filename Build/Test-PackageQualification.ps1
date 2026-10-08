param(
    [Parameter(Mandatory = $true)]
    [string] $PackageRoot,

    [string] $ReportOutput,

    [string] $Version
)

$ErrorActionPreference = 'Stop'
Import-Module PSPublishModule -MinimumVersion 3.0.158 -ErrorAction Stop
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$packageRootResolved = (Resolve-Path -LiteralPath $PackageRoot).Path
$reportRoot = if ($ReportOutput) { [IO.Path]::GetFullPath($ReportOutput) } else { Join-Path $packageRootResolved 'qualification' }
New-Item -ItemType Directory -Path $reportRoot -Force | Out-Null

# This is an explicit local qualification route. It neither signs nor publishes packages.
$metadataParameters = @{
    ConfigPath = Join-Path $PSScriptRoot 'PackageValidation/packages.json'
    ProjectRoot = $repositoryRoot
    Variables = @{ PackageRoot = $packageRootResolved }
}
if ($Version) { $metadataParameters.Version = $Version }
$metadata = Invoke-ReleaseValidation @metadataParameters
$metadata | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $reportRoot 'packages.json') -Encoding utf8

$sourceRoot = Join-Path $PSScriptRoot 'PackageConsumers'
$compileRoot = Join-Path ([IO.Path]::GetTempPath()) ('ChartForgeX-package-compile-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $compileRoot | Out-Null
try {
    foreach ($lane in @('Core', 'Visuals', 'Stories', 'Adapters')) {
        $contractPath = Join-Path $PSScriptRoot "PackageValidation/$($lane.ToLowerInvariant()).json"
        $laneRoot = Join-Path $compileRoot $lane
        $destination = Join-Path $laneRoot $lane
        $feed = Join-Path $laneRoot 'feed'
        New-Item -ItemType Directory -Path $destination, $feed | Out-Null
        Copy-Item -LiteralPath (Join-Path $sourceRoot 'PackageAssertions.cs') -Destination $laneRoot
        Copy-Item -LiteralPath (Join-Path $sourceRoot "$lane/${lane}PackageProbe.csproj") -Destination $destination
        Copy-Item -LiteralPath (Join-Path $sourceRoot "$lane/Program.cs") -Destination $destination
        $contract = Get-Content -LiteralPath $contractPath -Raw | ConvertFrom-Json
        foreach ($package in $contract.Packages.Items) {
            Copy-Item -LiteralPath (Join-Path $packageRootResolved "$($package.Id).$($metadata.Version).nupkg") -Destination $feed
        }
        $config = Get-Content -LiteralPath (Join-Path $sourceRoot 'NuGet.config.template') -Raw
        $config.Replace('{PackageRoot}', [Security.SecurityElement]::Escape($feed)) |
            Set-Content -LiteralPath (Join-Path $laneRoot 'NuGet.Config') -Encoding utf8
        $result = Invoke-ReleaseValidation -ConfigPath $contractPath `
            -ProjectRoot $repositoryRoot -Version $metadata.Version -Variables @{
                PackageRoot = $packageRootResolved
                CompileRoot = $laneRoot
            }
        $result | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $reportRoot "$($lane.ToLowerInvariant()).json") -Encoding utf8
    }
} finally {
    # Only the fresh copy/cache created above is disposable; package and report inputs stay intact.
    $resolvedCompileRoot = [IO.Path]::GetFullPath($compileRoot)
    $tempPrefix = [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedCompileRoot.StartsWith($tempPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Package compilation cleanup target is outside the selected temporary root.'
    }
    if (Test-Path -LiteralPath $resolvedCompileRoot) {
        $linked = (@(Get-Item -LiteralPath $resolvedCompileRoot) + @(Get-ChildItem -LiteralPath $resolvedCompileRoot -Recurse -Force)) |
            Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }
        if ($linked) { throw 'Package compilation cleanup target contains a linked path.' }
        Remove-Item -LiteralPath $resolvedCompileRoot -Recurse -ErrorAction Stop
    }
}

Write-Output "Packed package qualification passed. Reports: $reportRoot"
