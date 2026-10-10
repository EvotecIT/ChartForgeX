# Fixed synthetic samples exercise the consumer's PowerForge command boundary. They are not rendering measurements.
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $OutputRoot,
    [Parameter(Mandatory)][string] $RunnerIdentity
)
$ErrorActionPreference = 'Stop'
$history = Join-Path $OutputRoot 'synthetic-history.json'
if (Test-Path $history) { throw 'History contract proof requires a new synthetic history file.' }
New-Item -ItemType Directory -Path $OutputRoot -Force | Out-Null
$gate = @{
    HistoryPath=$history; WorkloadId='chartforgex-history-contract-synthetic-v1'; RunnerIdentity="synthetic-$RunnerIdentity"
    MinimumRuns=5; MinimumSamples=9; RelativeTolerance=0.10; AbsoluteToleranceMs=3
}
$events = [Collections.Generic.List[object]]::new()
$acceptedHash = $null
# Five accepted fixtures have medians 8..12 ms. The fixed healthy boundary is 13 ms; 14 ms must fail.
$durations = @(8, 9, 10, 11, 12, 13, 14)
foreach ($serial in 0..6) {
    $id = "synthetic-history-$serial"
    $timestamp = ([DateTimeOffset]'2026-01-01T00:00:00Z').AddMinutes($serial)
    $samples = foreach ($case in 'Auto', 'Full', 'Svg') {
        foreach ($iteration in 0..8) {
            [ordered]@{
                RunId=$id; Suite='chartforgex-history-contract-synthetic'; Scenario=$case; Operation='Render'; Engine='Fixture'
                Host='Synthetic'; Os='Synthetic'; RunMode='SyntheticContract'; Iteration=$iteration
                Status='Succeeded'; DurationMs=$durations[$serial]
            }
        }
    }
    $run = [ordered]@{
        RunId=$id; Suite='chartforgex-history-contract-synthetic'; StartedUtc=$timestamp; FinishedUtc=$timestamp.AddSeconds(1)
        Samples=$samples
        Environment=[ordered]@{
            OsFamily='Synthetic'; OsArchitecture='Synthetic'; ProcessArchitecture='Synthetic'; ProcessorName='Synthetic CPU'
            RuntimeVersion='Synthetic runtime'; Runner='Synthetic contract fixture'
        }
        Metadata=@{ iterationCount='9'; outlierMode='None'; operationTimingBoundary='SyntheticContractV1'; 'benchmark.EvidenceKind'='SyntheticContract' }
    }
    $report = Join-Path $OutputRoot "$id.json"
    $run | ConvertTo-Json -Depth 10 | Set-Content $report
    if ($serial -lt 5) {
        $state = Test-BenchmarkHistory @gate -ResultPath $report -Update
        if (!$state.Updated -or $state.Passed) { throw 'Explicit synthetic acceptance reported the wrong state.' }
        $events.Add(@{ Phase='AcceptedSynthetic'; RunId=$id; State=$state; Report=$report })
        if ($serial -eq 4) { $acceptedHash = (Get-FileHash $history).Hash }
    } elseif ($serial -eq 5) {
        $state = Test-BenchmarkHistory @gate -ResultPath $report
        if (!$state.Passed -or $state.Calibrating -or $state.Updated -or $state.Metrics.Count -ne 3) { throw 'The healthy synthetic run did not pass all three lanes.' }
        if ((Get-FileHash $history).Hash -ne $acceptedHash) { throw 'Successful verification changed synthetic history.' }
        $events.Add(@{ Phase='HealthySynthetic'; RunId=$id; State=$state; Report=$report; HistoryUnchanged=$true })
    } else {
        try {
            Test-BenchmarkHistory @gate -ResultPath $report | Out-Null
            throw 'The synthetic slowdown was not rejected.'
        } catch {
            if ($_.FullyQualifiedErrorId -notmatch '^BenchmarkHistoryFailed') { throw }
            $state = $_.TargetObject
            if ($state.Passed -or $state.Calibrating -or @($state.Metrics | Where-Object Regressed).Count -ne 3) { throw 'The synthetic slowdown failed for an unexpected reason.' }
        }
        if ((Get-FileHash $history).Hash -ne $acceptedHash) { throw 'Failed verification changed synthetic history.' }
        $events.Add(@{ Phase='SlowSyntheticRejected'; RunId=$id; State=$state; Report=$report; HistoryUnchanged=$true })
    }
}
foreach ($invalidKind in 'Incomplete', 'NonpositiveDuration') {
    $invalid = $run | ConvertTo-Json -Depth 10 | ConvertFrom-Json -AsHashtable
    $invalid.RunId = "synthetic-history-$invalidKind"
    foreach ($sample in $invalid.Samples) { $sample.RunId = $invalid.RunId; $sample.DurationMs = 13 }
    if ($invalidKind -eq 'Incomplete') { $invalid.Samples = @($invalid.Samples | Select-Object -Skip 1) }
    else { $invalid.Samples[0].DurationMs = 0 }
    $report = Join-Path $OutputRoot "$($invalid.RunId).json"
    $invalid | ConvertTo-Json -Depth 10 | Set-Content $report
    foreach ($update in $false, $true) {
        try {
            Test-BenchmarkHistory @gate -ResultPath $report -Update:$update | Out-Null
            throw "The $invalidKind synthetic input was not rejected."
        } catch {
            if ($_.FullyQualifiedErrorId -notmatch '^System.InvalidOperationException,' -or $_.Exception.GetBaseException() -isnot [InvalidOperationException]) { throw }
            $errorId = $_.FullyQualifiedErrorId
        }
        if ((Get-FileHash $history).Hash -ne $acceptedHash) { throw 'Invalid input changed synthetic history.' }
        $events.Add(@{ Phase="${invalidKind}SyntheticRejected"; RunId=$invalid.RunId; Report=$report; Update=$update; ErrorId=$errorId; HistoryUnchanged=$true })
    }
}
@{ EvidenceKind='SyntheticContract'; HistoryPath=$history; AcceptedHistorySha256=$acceptedHash; Events=@($events) }
