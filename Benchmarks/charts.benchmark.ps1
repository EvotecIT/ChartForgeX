$candidate = Get-BenchmarkInput AssemblyPath -Required
$baseline = Get-BenchmarkInput BaselineAssemblyPath -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
Add-Type -Path (Join-Path $PSScriptRoot 'ChartBenchmarkLane.cs')
$fixtureNames = 'timeline-60', 'timeline-8', 'latency', 'calendar'
$lanes = @{}
foreach ($lane in 'Baseline', 'Candidate') {
    foreach ($fixture in $fixtureNames) {
        $path = if ($lane -eq 'Baseline') { $baseline } else { $candidate }
        $lanes["$lane-$fixture"] = [ChartBenchmarkLane]::new($path, $fixtures, $fixture)
    }
}
foreach ($fixture in $fixtureNames) {
    $lanes["Candidate-$fixture"].Expect($lanes["Baseline-$fixture"])
}

New-BenchmarkSuite 'chartforgex-charts' {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($lane in 'Baseline', 'Candidate') {
            foreach ($fixture in $fixtureNames) {
                foreach ($format in 'Svg', 'Png') {
                    Add-BenchmarkCase "$lane-$fixture-$format" @{ Lane = $lane; Fixture = $fixture; Format = $format }
                }
            }
        }
    }
    Set-BenchmarkSetup { param($case, $run) $run.Lane = $lanes["$($case.Lane)-$($case.Fixture)"]; $run.Lane.Reset() }
    Add-BenchmarkEngine ChartForgeX {
        Add-BenchmarkOperation Execute {
            param($case, $run)
            $before = [GC]::GetAllocatedBytesForCurrentThread()
            $run.Result = if ($case.Format -eq 'Svg') { $run.Lane.Svg() } else { $run.Lane.Png() }
            $run.Allocated = [GC]::GetAllocatedBytesForCurrentThread() - $before
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric OutputLength { param($case, $run) $run.Result }
    Add-BenchmarkMetric ThreadAllocatedBytes { param($case, $run) $run.Allocated }
    Add-BenchmarkMetadata BaselineSha256 (Get-FileHash -LiteralPath $baseline).Hash
    Add-BenchmarkMetadata CandidateSha256 (Get-FileHash -LiteralPath $candidate).Hash
    Add-BenchmarkMetadata Scope 'Svg renders a fresh report chart with host colour variables and an id scope; Png renders the same chart natively. Validation outside timing requires byte-identical SVG and PNG against the baseline binary. ThreadAllocatedBytes is the managed allocation of the rendering thread during the operation.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata ProcessorPlacement 'Baseline and candidate share the same process and processor placement. Record host placement and power policy when qualifying a comparison.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
