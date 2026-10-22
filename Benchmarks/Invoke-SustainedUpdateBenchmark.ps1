#Requires -Version 7.4
<#
.SYNOPSIS
Measures the complete public rolling-source to native preparation to browser commit path through PowerForge.
.DESCRIPTION
Uses the existing isolated benchmark fixture project and HtmlTinkerX Chromium session. PowerForge owns timing,
warmups, iteration order and outputs. Builds fixtures only from frozen product binaries; does not build or alter
the product, install browsers, accept performance history, or update README results. Requires existing PSPublishModule.
.PARAMETER ProductBinaryRoot
Frozen net8 product/test output containing the product and existing HtmlTinkerX/Playwright runtime assemblies.
.PARAMETER OutputRoot
Task-owned output for isolated fixture compilation, PowerForge raw samples and first/final receipts.
.PARAMETER Matrix
Current runs 32 primary, 1000 secondary and 5000 dense secondary cases. Shared compares 32/1000 using the same
fixture source compiled independently against supplied compatible baseline binaries. An incompatible API fails compilation.
.PARAMETER BaselineBinaryRoot
Required only for Shared. No old-API bridge, current-binary surrogate or unrelated historical workload is substituted.
.PARAMETER UpdatesPerOperation
Number of sequential real source/preparation/browser commits in each measured operation, from one to ten.
.PARAMETER WarmupCount
PowerForge warmup count. Default two.
.PARAMETER IterationCount
PowerForge measured iteration count. Default nine.
.PARAMETER Case
Optional existing PowerForge case selector, useful for a one-case smoke or bounded per-case runs.
.PARAMETER HostNoise
Actual host activity and placement notes; this does not assert a quiet host.
.PARAMETER BaselineUnavailableReason
Explicit evidence boundary for a Current-only run. No historical speed comparison is emitted.
.PARAMETER Plan
Shows the PowerForge matrix without launching browsers. Fixture compilation still occurs unless SkipFixtureBuild is supplied.
.PARAMETER SkipFixtureBuild
Reuses already built isolated fixtures in this exact OutputRoot. Does not bypass source/binary metadata binding.
.EXAMPLE
./Benchmarks/Invoke-SustainedUpdateBenchmark.ps1 -ProductBinaryRoot /frozen/net8 -OutputRoot /task/update-smoke -Case points-32-primary -UpdatesPerOperation 1 -WarmupCount 0 -IterationCount 1
.EXAMPLE
./Benchmarks/Invoke-SustainedUpdateBenchmark.ps1 -ProductBinaryRoot /frozen/current -BaselineBinaryRoot /frozen/compatible -Matrix Shared -OutputRoot /task/update-pair
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ProductBinaryRoot,
    [Parameter(Mandatory)][string] $OutputRoot,
    [ValidateSet('Current', 'Shared')][string] $Matrix = 'Current',
    [string] $BaselineBinaryRoot,
    [ValidateRange(1, 10)][int] $UpdatesPerOperation = 10,
    [ValidateRange(0, 100)][int] $WarmupCount = 2,
    [ValidateRange(1, 1000)][int] $IterationCount = 9,
    [string[]] $Case,
    [string] $HostNoise = 'Normal development host; record actual activity and placement separately',
    [string] $BaselineUnavailableReason = 'Not supplied: this is a current-only full-path workload, without a qualified historical ranking.',
    [switch] $Plan,
    [switch] $SkipFixtureBuild
)
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$binaries = (Resolve-Path -LiteralPath $ProductBinaryRoot).Path
$output = [IO.Path]::GetFullPath($OutputRoot)
$repository = Split-Path $PSScriptRoot -Parent
if ($Matrix -eq 'Shared' -and -not $BaselineBinaryRoot) { throw 'Shared requires compatible BaselineBinaryRoot.' }
if ($Matrix -eq 'Current' -and $BaselineBinaryRoot) { throw 'Use Shared explicitly for a baseline/current comparison.' }
if ($Matrix -eq 'Current' -and [string]::IsNullOrWhiteSpace($BaselineUnavailableReason)) { throw 'Record the unavailable-baseline boundary for a current-only run.' }
if ($BaselineBinaryRoot) { $BaselineBinaryRoot = (Resolve-Path -LiteralPath $BaselineBinaryRoot).Path }
$fixtureProject = Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkFixtures.csproj'
$fixtureOutput = Join-Path $output 'fixtures-current'
$baselineFixtureOutput = Join-Path $output 'fixtures-baseline'
$fixture = Join-Path $fixtureOutput 'InteractivityBenchmarkFixtures.dll'
$baselineFixture = Join-Path $baselineFixtureOutput 'InteractivityBenchmarkFixtures.dll'
if (-not $SkipFixtureBuild) {
    $builds = @(@{ Root = $binaries; Output = $fixtureOutput })
    if ($Matrix -eq 'Shared') { $builds += @{ Root = $BaselineBinaryRoot; Output = $baselineFixtureOutput } }
    foreach ($build in $builds) {
        & dotnet build $fixtureProject -c Release --nologo -nodeReuse:false -p:UseSharedCompilation=false -o $build.Output "-p:ProductBinaryRoot=$($build.Root)" -p:InteractivityBenchmarkProfile=SustainedUpdate "-p:BaseIntermediateOutputPath=$($build.Output)/obj/"
        if ($LASTEXITCODE -ne 0) { throw 'The identical typed sustained-update fixture did not compile against its declared product binaries. No compatibility substitute is permitted.' }
    }
}
$variables = @{
    ProductBinaryRoot = $binaries
    FixtureAssemblyPath = (Resolve-Path -LiteralPath $fixture).Path
    BaselineBinaryRoot = if ($Matrix -eq 'Shared') { $BaselineBinaryRoot } else { '' }
    BaselineFixtureAssemblyPath = if ($Matrix -eq 'Shared') { (Resolve-Path -LiteralPath $baselineFixture).Path } else { '' }
    BaselineAvailability = if ($Matrix -eq 'Shared') { 'Supplied compatible baseline; identical fixture sources compile independently and their complete source/policy digests must match.' } else { $BaselineUnavailableReason }
    Matrix = $Matrix
    UpdatesPerOperation = $UpdatesPerOperation
    HostNoise = $HostNoise
    TokenPath = Join-Path $repository 'ChartForgeX/Themes/Tokens/evotec.chartforgex.tokens.json'
    FontPath = Join-Path $repository 'ChartForgeX.Examples/Fixtures/Fonts/Carlito/Carlito-Regular.ttf'
    BoldFontPath = Join-Path $repository 'ChartForgeX.Examples/Fixtures/Fonts/Carlito/Carlito-Bold.ttf'
}
Import-Module PSPublishModule -MinimumVersion 3.0.168 -ErrorAction Stop
try {
    $invoke = @{
        Path = Join-Path $PSScriptRoot 'sustained-update.benchmark.ps1'
        OutputRoot = $output
        WarmupCount = $WarmupCount
        IterationCount = $IterationCount
        RunMode = 'local'
        RunOrder = 'Rotated'
        OutlierMode = 'None'
        Plan = $Plan
        Variable = $variables
    }
    if ($Case) { $invoke.Case = $Case }
    $result = Invoke-BenchmarkSuite @invoke
    if (-not $Plan -and @($result.Summary | Where-Object { $_.FailureCount -gt 0 -or $_.Status -eq 'Failed' }).Count -gt 0) {
        throw 'The sustained-update benchmark failed full-path execution or source/native/browser/export validation; retain every result.'
    }
    if (-not $Plan) {
        [InteractivityBenchmarkLane]::SustainedBrowserVersions() | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $output 'browser-versions.json')
    }
    $result
} finally {
    if ('InteractivityBenchmarkLane' -as [type]) {
        try {
            if (-not $Plan) { [InteractivityBenchmarkLane]::WriteSectionReceipts((Join-Path $output 'receipts')) }
        } finally {
            [InteractivityBenchmarkLane]::DisposeAll()
        }
    }
}
