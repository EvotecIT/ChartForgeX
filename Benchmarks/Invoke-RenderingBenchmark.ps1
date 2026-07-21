[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [int] $WarmupCount = 1,

    [ValidateRange(1, 1000)]
    [int] $IterationCount = 5,

    [string] $OutputRoot = (Join-Path $PSScriptRoot '..\Ignore\Benchmarks\Rendering'),

    [switch] $Plan,

    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$projectPath = Join-Path $repositoryRoot 'ChartForgeX\ChartForgeX.csproj'
$assemblyPath = Join-Path $repositoryRoot 'ChartForgeX\bin\Release\net8.0\ChartForgeX.dll'
$specPath = Join-Path $PSScriptRoot 'rendering.benchmark.ps1'

if (-not $SkipBuild.IsPresent) {
    & dotnet build $projectPath -c Release --nologo
    if ($LASTEXITCODE -ne 0) {
        throw 'The ChartForgeX Release build failed before the rendering benchmark.'
    }
}

if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
    throw "ChartForgeX Release assembly was not found at '$assemblyPath'."
}

Import-Module PSPublishModule -MinimumVersion 3.0.72 -Force -ErrorAction Stop

$invoke = @{
    Path = $specPath
    OutputRoot = $OutputRoot
    WarmupCount = $WarmupCount
    IterationCount = $IterationCount
    RunMode = 'local'
    Variable = @{ AssemblyPath = $assemblyPath }
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
