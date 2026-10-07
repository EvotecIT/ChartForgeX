[CmdletBinding()]
param(
    [ValidateSet('Rendering', 'Decimation', 'Topology', 'Charts', 'Scenes')]
    [string] $Suite = 'Rendering',

    [ValidateRange(0, 100)]
    [int] $WarmupCount = 1,

    [ValidateRange(1, 1000)]
    [int] $IterationCount = 5,

    [string] $OutputRoot,

    [string] $BaselineAssemblyPath,

    [switch] $Plan,

    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repositoryRoot 'ChartForgeX\ChartForgeX.csproj'
$assemblyPath = Join-Path $repositoryRoot 'ChartForgeX\bin\Release\net8.0\ChartForgeX.dll'
$specPath = Join-Path $PSScriptRoot ($Suite.ToLowerInvariant() + '.benchmark.ps1')
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $PSScriptRoot ('..\Ignore\Benchmarks\' + $Suite)
}

if (-not $SkipBuild.IsPresent) {
    & dotnet build $projectPath -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'The ChartForgeX Release build failed before the rendering benchmark.'
    }
}

if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "ChartForgeX Release assembly was not found at '$assemblyPath'."
}

$variables = @{ AssemblyPath = $assemblyPath }
if ($Suite -in 'Topology', 'Charts', 'Scenes') {
    if ([string]::IsNullOrWhiteSpace($BaselineAssemblyPath)) { $BaselineAssemblyPath = $assemblyPath }
    $variables.BaselineAssemblyPath = (Resolve-Path -LiteralPath $BaselineAssemblyPath).Path
    $fixtureOutput = Join-Path ([IO.Path]::GetFullPath($OutputRoot)) 'fixtures'
    $fixtureName = if ($Suite -eq 'Topology') { 'TopologyBenchmarkFixtures' } else { 'ChartBenchmarkFixtures' }
    $fixtureFolder = if ($Suite -eq 'Scenes') { 'Charts' } else { $Suite }
    & dotnet build (Join-Path $PSScriptRoot "$fixtureFolder/$fixtureName.csproj") -c Release --nologo -o $fixtureOutput "-p:ProductDll=$assemblyPath"
    if ($LASTEXITCODE -ne 0) { throw "The $Suite benchmark fixture build failed." }
    $variables.FixtureAssemblyPath = Join-Path $fixtureOutput "$fixtureName.dll"
    if ($Suite -eq 'Scenes') {
        $variables.TokenPath = Join-Path $repositoryRoot 'ChartForgeX/Themes/Tokens/evotec.chartforgex.tokens.json'
        $variables.FontPath = Join-Path $repositoryRoot 'ChartForgeX.Examples/Fixtures/Fonts/Carlito/Carlito-Regular.ttf'
        $variables.BoldFontPath = Join-Path $repositoryRoot 'ChartForgeX.Examples/Fixtures/Fonts/Carlito/Carlito-Bold.ttf'
    }
} elseif (-not [string]::IsNullOrWhiteSpace($BaselineAssemblyPath)) {
    throw '-BaselineAssemblyPath is supported by the Topology, Charts and Scenes suites.'
}

Import-Module PSPublishModule -MinimumVersion 3.0.72 -Force -ErrorAction Stop

$invoke = @{
    Path = $specPath
    OutputRoot = $OutputRoot
    WarmupCount = $WarmupCount
    IterationCount = $IterationCount
    RunMode = 'local'
    Variable = $variables
}
if ($Plan.IsPresent) {
    $invoke.Plan = $true
}

$result = Invoke-BenchmarkSuite @invoke
if ($Plan.IsPresent) {
    $result
    return
}

$failed = @($result.Summary | Where-Object { $_.FailureCount -gt 0 -or $_.Status -eq 'Failed' })
if ($failed.Count -gt 0) {
    $failureSummary = $failed | ForEach-Object {
        $reasons = if ($_.FailureReasons -and $_.FailureReasons.Keys.Count -gt 0) {
            $_.FailureReasons.Keys -join ' | '
        } else {
            'No failure reason was recorded.'
        }
        "$($_.Scenario): $reasons"
    }
    throw "Rendering benchmark run $($result.RunId) failed: $($failureSummary -join '; ')"
}

$result
