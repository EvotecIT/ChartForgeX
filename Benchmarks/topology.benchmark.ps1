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
        foreach ($lane in 'Baseline', 'Candidate') {
            foreach ($fixture in 'small', 'mesh', 'overview', 'replication', 'mixed') {
                foreach ($operation in 'Prepare', 'CompletePrepare') {
                    Add-BenchmarkCase "$lane-$fixture-$operation" @{ Lane = $lane; Fixture = $fixture; Mode = $operation; Svg = $false }
                }
            }
            foreach ($fixture in 'small', 'mesh', 'overview', 'replication') {
                foreach ($operation in 'Svg', 'PreparedSvg') {
                    Add-BenchmarkCase "$lane-$fixture-$operation" @{ Lane = $lane; Fixture = $fixture; Mode = $operation; Svg = $true }
                }
            }
        }
    }
    Set-BenchmarkSetup { param($case, $run) $run.Lane = $lanes["$($case.Lane)-$($case.Fixture)"] }
    Add-BenchmarkEngine ChartForgeX {
        Add-BenchmarkOperation Execute {
            param($case, $run)
            $before = [GC]::GetAllocatedBytesForCurrentThread()
            $run.Result = switch ($case.Mode) {
                Prepare { $run.Lane.Prepare() }
                CompletePrepare { $run.Lane.CompletePrepare() }
                Svg { $run.Lane.Svg() }
                PreparedSvg { $run.Lane.PreparedSvg() }
            }
            $run.Allocated = [GC]::GetAllocatedBytesForCurrentThread() - $before
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate($case.Svg) }
    Add-BenchmarkMetric Result { param($case, $run) $run.Result }
    Add-BenchmarkMetric ThreadAllocatedBytes { param($case, $run) $run.Allocated }
    Add-BenchmarkMetadata BaselineSha256 (Get-FileHash -LiteralPath $baseline).Hash
    Add-BenchmarkMetadata CandidateSha256 (Get-FileHash -LiteralPath $candidate).Hash
    Add-BenchmarkMetadata Scope 'Prepare measures layout; CompletePrepare includes Analyze to finish and inspect routes; Svg includes Prepare and ToSvg; PreparedSvg reuses a fully planned snapshot. Validation outside timing requires identical full diagnostics and SVG.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata ProcessorPlacement 'Baseline and candidate share the same process and processor placement. Record host placement and power policy when qualifying a comparison.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
