$candidate = Get-BenchmarkInput AssemblyPath -Required
$baseline = Get-BenchmarkInput BaselineAssemblyPath -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
Add-Type -Path (Join-Path $PSScriptRoot 'TopologyBenchmarkLane.cs')
$lanes = @{}
foreach ($lane in 'Baseline', 'Candidate') {
    foreach ($fixture in 'small', 'mesh', 'overview', 'replication', 'mixed') {
        $path = if ($lane -eq 'Baseline') { $baseline } else { $candidate }
        $lanes["$lane-$fixture"] = [TopologyBenchmarkLane]::new($path, $fixtures, $fixture)
    }
}
foreach ($fixture in 'small', 'mesh', 'overview', 'replication', 'mixed') {
    $lanes["Candidate-$fixture"].Expect($lanes["Baseline-$fixture"])
}

New-BenchmarkSuite 'chartforgex-topology' {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($fixture in 'small', 'mesh', 'overview', 'replication', 'mixed') {
            Add-BenchmarkCase $fixture @{ Fixture = $fixture }
        }
    }
    Add-BenchmarkSkipRule { param($case) if ($case.Fixture -eq 'mixed' -and $case.Operation -like '*Svg') { 'The mixed fixture qualifies preparation only.' } }
    Set-BenchmarkSetup { param($case, $run) $run.Lane = $lanes["$($case.Engine)-$($case.Fixture)"] }
    foreach ($lane in 'Baseline', 'Candidate') {
        Add-BenchmarkEngine $lane {
            foreach ($operation in 'Prepare', 'CompletePrepare', 'Svg', 'RepeatSvg', 'PreparedSvg') {
                Add-BenchmarkOperation $operation {
                    param($case, $run)
                    $before = [GC]::GetAllocatedBytesForCurrentThread()
                    $run.Result = switch ($case.Operation) {
                        Prepare { $run.Lane.Prepare() }
                        CompletePrepare { $run.Lane.CompletePrepare() }
                        Svg { $run.Lane.Svg() }
                        RepeatSvg { $run.Lane.RepeatSvg() }
                        PreparedSvg { $run.Lane.PreparedSvg() }
                    }
                    $run.Allocated = [GC]::GetAllocatedBytesForCurrentThread() - $before
                }
            }
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate($case.Operation -like '*Svg') }
    Add-BenchmarkMetric Result { param($case, $run) $run.Result }
    Add-BenchmarkMetric ThreadAllocatedBytes { param($case, $run) $run.Allocated }
    Add-BenchmarkMetadata BaselineSha256 (Get-FileHash -LiteralPath $baseline).Hash
    Add-BenchmarkMetadata CandidateSha256 (Get-FileHash -LiteralPath $candidate).Hash
    Add-BenchmarkMetadata PairedOrdering 'Shared fixture scenarios and operation names pair Baseline/Candidate engines; Rotated alternates each paired group. The six mixed-SVG work items are explicitly skipped; all 22 supported pairs remain.'
    Add-BenchmarkMetadata Scope 'Prepare measures layout; CompletePrepare includes Analyze to finish and inspect routes; Prepare, CompletePrepare and Svg clear the shared plan cache first; Svg includes Prepare and ToSvg; RepeatSvg is Svg without clearing, as a host drawing the same chart again; PreparedSvg reuses a fully planned snapshot. Validation outside timing requires identical full diagnostics and SVG.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata ProcessorPlacement 'Baseline and candidate share the same process and processor placement. Record host placement and power policy when qualifying a comparison.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
