[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $ModulePath,
    [Parameter(Mandatory)][string] $RunnerIdentity,
    [ValidateSet('Verify', 'AcceptReference', 'CalibrationProof')][string] $Mode = 'Verify',
    [string] $OutputRoot = (Join-Path $PSScriptRoot '../Ignore/Benchmarks/History'),
    [string] $HistoryPath,
    [switch] $SkipBuild
)
$ErrorActionPreference = 'Stop'
Import-Module $ModulePath -Force
$root = Split-Path $PSScriptRoot
$product = Join-Path $root 'ChartForgeX/bin/Release/net8.0/ChartForgeX.dll'
$fixture = Join-Path $PSScriptRoot 'Rendering/bin/Release/net8.0/RenderingHistoryFixtures.dll'
$font = Join-Path $root 'ChartForgeX.Tests/Fixtures/OpenType/stem-true-type.ttf'
if (!$SkipBuild) {
    & dotnet build (Join-Path $root 'ChartForgeX/ChartForgeX.csproj') -c Release -f net8.0 --nologo
    if ($LASTEXITCODE) { throw 'Product build failed.' }
    & dotnet build (Join-Path $PSScriptRoot 'Rendering/RenderingHistoryFixtures.csproj') -c Release --nologo "-p:ProductDll=$product"
    if ($LASTEXITCODE) { throw 'Benchmark fixture build failed.' }
}
if (!$HistoryPath) { $HistoryPath = Join-Path $OutputRoot 'history.json' }
if ($Mode -eq 'CalibrationProof' -and (Test-Path $HistoryPath)) { throw 'Calibration proof requires a separate new history file.' }
$commit = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE) { throw 'Source revision could not be read.' }
$inputs = @{ ProductAssembly=$product; FixtureAssembly=$fixture; FontPath=$font; SourceCommit=$commit; ProofDelayMilliseconds=0 }
$benchmark = @{ Path=(Join-Path $PSScriptRoot 'rendering-history.benchmark.ps1'); OutputRoot=$OutputRoot; ProcessPriority='Normal'; Variable=$inputs }
$gate = @{ HistoryPath=$HistoryPath; WorkloadId='chartforgex-portable-labels-v1'; RunnerIdentity=$RunnerIdentity; MinimumRuns=5; MinimumSamples=9; RelativeTolerance=0.10; AbsoluteToleranceMs=3 }
$events = [Collections.Generic.List[object]]::new()
foreach ($iteration in 1..$(if ($Mode -eq 'CalibrationProof') { 5 } else { 1 })) {
    $run = Invoke-BenchmarkSuite @benchmark
    $report = $run.Artifacts['run-report.json']
    $accept = $Mode -ne 'Verify'
    $state = Test-BenchmarkHistory @gate -ResultPath $report -Update:$accept
    $events.Add(@{ RunId=$run.RunId; Phase=$(if ($accept) {'Accepted'} else {'Verified'}); State=$state; Report=$report })
}
if ($Mode -eq 'CalibrationProof') {
    $before = (Get-FileHash $HistoryPath).Hash
    $run = Invoke-BenchmarkSuite @benchmark
    $state = Test-BenchmarkHistory @gate -ResultPath $run.Artifacts['run-report.json']
    $events.Add(@{ RunId=$run.RunId; Phase='Healthy'; State=$state; Report=$run.Artifacts['run-report.json'] })
    $inputs.ProofDelayMilliseconds = 50
    $run = Invoke-BenchmarkSuite @benchmark
    try {
        Test-BenchmarkHistory @gate -ResultPath $run.Artifacts['run-report.json']
        throw 'The deliberate slowdown was not rejected.'
    } catch { if ($_.FullyQualifiedErrorId -notmatch '^BenchmarkHistoryFailed') { throw } }
    if ((Get-FileHash $HistoryPath).Hash -ne $before) { throw 'Read-only verification changed accepted history.' }
    $events.Add(@{ RunId=$run.RunId; Phase='DeliberateSlowdownRejected'; Report=$run.Artifacts['run-report.json']; HistoryUnchanged=$true })
}
$events | ConvertTo-Json -Depth 15 | Set-Content (Join-Path $OutputRoot 'history-proof.json')
$events
