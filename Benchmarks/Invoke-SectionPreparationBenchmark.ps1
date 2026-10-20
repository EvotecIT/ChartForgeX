[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ProductBinaryRoot,
    [Parameter(Mandatory)][string] $OutputRoot,
    [ValidateSet('Shared', 'Cartesian')][string] $Matrix = 'Shared',
    [string] $BaselineBinaryRoot,
    [ValidateRange(0, 100)][int] $WarmupCount = 2,
    [ValidateRange(1, 1000)][int] $IterationCount = 9,
    [string[]] $Case,
    [string] $HostNoise = 'Normal development host; record observed before/after activity separately',
    [switch] $Plan,
    [switch] $SkipFixtureBuild
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$binaries = (Resolve-Path -LiteralPath $ProductBinaryRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
$repository = Split-Path $PSScriptRoot -Parent
$fixtureProject = Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkFixtures.csproj'
$fixtureOutput = Join-Path $output 'fixtures-current'
$fixture = Join-Path $fixtureOutput 'InteractivityBenchmarkFixtures.dll'
$baselineFixtureOutput = Join-Path $output 'fixtures-baseline'
$baselineFixture = Join-Path $baselineFixtureOutput 'InteractivityBenchmarkFixtures.dll'
if ($Matrix -eq 'Shared' -and -not $BaselineBinaryRoot) { throw 'Shared preparation requires BaselineBinaryRoot.' }
if ($BaselineBinaryRoot) { $BaselineBinaryRoot = (Resolve-Path -LiteralPath $BaselineBinaryRoot).Path }
if (-not $SkipFixtureBuild) {
    $builds = @(@{ Root = $binaries; Output = $fixtureOutput; Profile = 'SectionCurrent' })
    if ($Matrix -eq 'Shared') { $builds += @{ Root = $BaselineBinaryRoot; Output = $baselineFixtureOutput; Profile = 'SectionShared' } }
    foreach ($build in $builds) {
        & dotnet build $fixtureProject -c Release --nologo -o $build.Output "-p:ProductBinaryRoot=$($build.Root)" "-p:InteractivityBenchmarkProfile=$($build.Profile)" "-p:BaseIntermediateOutputPath=$($build.Output)/obj/"
        if ($LASTEXITCODE -ne 0) { throw "The $($build.Profile) section benchmark fixture build failed." }
    }
}
$variables = @{
    ProductBinaryRoot = $binaries
    FixtureAssemblyPath = (Resolve-Path -LiteralPath $fixture).Path
    BaselineBinaryRoot = if ($Matrix -eq 'Shared') { $BaselineBinaryRoot } else { '' }
    BaselineFixtureAssemblyPath = if ($Matrix -eq 'Shared') { (Resolve-Path -LiteralPath $baselineFixture).Path } else { '' }
    Matrix = $Matrix
    HostNoise = $HostNoise
    TokenPath = Join-Path $repository 'ChartForgeX/Themes/Tokens/evotec.chartforgex.tokens.json'
    FontPath = Join-Path $repository 'ChartForgeX.Examples/Fixtures/Fonts/Carlito/Carlito-Regular.ttf'
    BoldFontPath = Join-Path $repository 'ChartForgeX.Examples/Fixtures/Fonts/Carlito/Carlito-Bold.ttf'
}
Import-Module PSPublishModule -MinimumVersion 3.0.168 -ErrorAction Stop
try {
    $invoke = @{
        Path = Join-Path $PSScriptRoot 'section-preparation.benchmark.ps1'
        OutputRoot = $output; WarmupCount = $WarmupCount; IterationCount = $IterationCount
        RunMode = 'local'; RunOrder = 'Rotated'; OutlierMode = 'None'; Plan = $Plan; Variable = $variables
    }
    if ($Case) { $invoke.Case = $Case }
    $result = Invoke-BenchmarkSuite @invoke
    if (-not $Plan -and @($result.Summary | Where-Object { $_.FailureCount -gt 0 -or $_.Status -eq 'Failed' }).Count -gt 0) {
        throw 'Section preparation failed execution or native/source validation; retain the complete results.'
    }
    $result
} finally {
    if ('InteractivityBenchmarkLane' -as [type]) {
        try { if (-not $Plan) { [InteractivityBenchmarkLane]::WriteSectionReceipts((Join-Path $output 'receipts')) } }
        finally { [InteractivityBenchmarkLane]::DisposeAll() }
    }
}
