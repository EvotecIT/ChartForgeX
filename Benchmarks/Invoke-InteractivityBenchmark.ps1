[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ProductBinaryRoot,
    [Parameter(Mandatory)][string] $OutputRoot,
    [string] $BaselineBinaryRoot,
    [string] $BaselineAcquisitionSourcePath,
    [ValidateRange(1, 1000)][int] $Queries = 16,
    [ValidateRange(0, 100)][int] $WarmupCount = 2,
    [ValidateRange(1, 1000)][int] $IterationCount = 5,
    [string] $HostNoise = 'Normal development host; placement unchanged',
    [switch] $Plan,
    [switch] $SkipFixtureBuild
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$binaries = (Resolve-Path -LiteralPath $ProductBinaryRoot).Path
if ([string]::IsNullOrWhiteSpace($BaselineBinaryRoot) -ne [string]::IsNullOrWhiteSpace($BaselineAcquisitionSourcePath)) {
    throw 'A paired comparison requires both BaselineBinaryRoot and BaselineAcquisitionSourcePath.'
}
$output = [IO.Path]::GetFullPath($OutputRoot)
$fixtureOutput = Join-Path $output 'fixtures'
$fixture = Join-Path $fixtureOutput 'InteractivityBenchmarkFixtures.dll'
if (-not $SkipFixtureBuild) {
    & dotnet build (Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkFixtures.csproj') -c Release --nologo -o $fixtureOutput "-p:ProductBinaryRoot=$binaries" "-p:BaseIntermediateOutputPath=$output/obj/"
    if ($LASTEXITCODE -ne 0) { throw 'The isolated interactivity benchmark fixture build failed.' }
}
Import-Module PSPublishModule -MinimumVersion 3.0.151 -ErrorAction Stop
try {
    $result = Invoke-BenchmarkSuite -Path (Join-Path $PSScriptRoot 'interactivity.benchmark.ps1') -OutputRoot $output -WarmupCount $WarmupCount -IterationCount $IterationCount -RunMode local -Plan:$Plan -Variable @{
        ProductBinaryRoot = $binaries
        FixtureAssemblyPath = $fixture
        AcquisitionSourcePath = Join-Path $PSScriptRoot '../ChartForgeX.Interactivity.Html/Assets/interactive.source/45-crosshair-reveal.js'
        Queries = $Queries
        HostNoise = $HostNoise
        BaselineBinaryRoot = if ($BaselineBinaryRoot) { (Resolve-Path -LiteralPath $BaselineBinaryRoot).Path } else { '' }
        BaselineAcquisitionSourcePath = if ($BaselineAcquisitionSourcePath) { (Resolve-Path -LiteralPath $BaselineAcquisitionSourcePath).Path } else { '' }
    }
    if (-not $Plan -and @($result.Summary | Where-Object { $_.FailureCount -gt 0 -or $_.Status -eq 'Failed' }).Count -gt 0) {
        throw "Interactivity benchmark run $($result.RunId) failed validation."
    }
    if (-not $Plan) { [InteractivityBenchmarkLane]::BrowserVersions() | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'browser-versions.json') }
    $result
} finally {
    if ('InteractivityBenchmarkLane' -as [type]) { [InteractivityBenchmarkLane]::DisposeAll() }
}
