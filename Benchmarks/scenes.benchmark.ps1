$candidate = Get-BenchmarkInput AssemblyPath -Required
$baseline = Get-BenchmarkInput BaselineAssemblyPath -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
$tokens = Get-BenchmarkInput TokenPath -Required
$font = Get-BenchmarkInput FontPath -Required
$boldFont = Get-BenchmarkInput BoldFontPath -Required
Add-Type -Path (Join-Path $PSScriptRoot 'ChartBenchmarkLane.cs')
$lanes = @{}
$fixtureNames = @('cartesian', 'donut', 'grouped-bars', 'stacked-bars', 'scatter', 'dense-line')
foreach ($lane in 'Baseline', 'Candidate') {
    foreach ($fixture in $fixtureNames) {
        $path = if ($lane -eq 'Baseline') { $baseline } else { $candidate }
        $lanes["$lane-$fixture"] = [SceneBenchmarkLane]::new($path, $fixtures, $tokens, $font, $boldFont, $fixture, $lane -eq 'Candidate')
    }
}
foreach ($fixture in $fixtureNames) {
    if ($lanes["Baseline-$fixture"].SourceDigest -ne $lanes["Candidate-$fixture"].SourceDigest) { throw "Paired source data changed for $fixture." }
}
New-BenchmarkSuite 'chartforgex-direct-scenes' {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($fixture in $fixtureNames) {
            foreach ($format in 'Svg', 'Rgba', 'Png') {
                foreach ($lane in 'Baseline', 'Candidate') {
                    Add-BenchmarkCase "$lane-$fixture-$format" @{ Lane = $lane; Fixture = $fixture; Format = $format }
                }
            }
        }
        foreach ($fixture in $fixtureNames) {
            foreach ($format in 'Compile', 'PreparedSvg', 'PreparedRgba', 'PreparedPng') {
                Add-BenchmarkCase "Candidate-$fixture-$format" @{ Lane = 'Candidate'; Fixture = $fixture; Format = $format }
            }
        }
    }
    Set-BenchmarkSetup { param($case, $run) $run.Lane = $lanes["$($case.Lane)-$($case.Fixture)"]; $run.Lane.Reset($case.Format) }
    Add-BenchmarkEngine ChartForgeX {
        Add-BenchmarkOperation Execute {
            param($case, $run)
            $before = [GC]::GetAllocatedBytesForCurrentThread()
            $run.Lane.Execute()
            $run.Allocated = [GC]::GetAllocatedBytesForCurrentThread() - $before
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric OutputBytesOrRegions { param($case, $run) $run.Lane.OutputLength }
    Add-BenchmarkMetric ThreadAllocatedBytes { param($case, $run) $run.Allocated }
    Add-BenchmarkMetadata BaselineSha256 (Get-FileHash -LiteralPath $baseline).Hash
    Add-BenchmarkMetadata CandidateSha256 (Get-FileHash -LiteralPath $candidate).Hash
    Add-BenchmarkMetadata FixtureSha256 (Get-FileHash -LiteralPath $fixtures).Hash
    Add-BenchmarkMetadata ThemeSha256 (Get-FileHash -LiteralPath $tokens).Hash
    Add-BenchmarkMetadata FontSha256 (Get-FileHash -LiteralPath $font).Hash
    Add-BenchmarkMetadata BoldFontSha256 (Get-FileHash -LiteralPath $boldFont).Hash
    foreach ($fixture in $fixtureNames) { Add-BenchmarkMetadata ("SourceSha256-" + $fixture) $lanes["Candidate-$fixture"].SourceDigest }
    Add-BenchmarkMetadata Scope 'Baseline and candidate Svg/Rgba/Png include complete layout and export. Candidate Compile measures preparation only; Prepared exports reuse a scene prepared outside timing. Models, canonical tokens, logical 800x440 size and scale 1 match. Frame and renderer changes intentionally differ; validation requires dimensions/content and within-lane determinism, not old/new byte equality.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata ProcessorPlacement 'Both lanes run in one process with rotated ordering; record power policy and processor placement with qualification.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
