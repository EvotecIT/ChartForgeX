$candidate = Get-BenchmarkInput AssemblyPath -Required
$baseline = Get-BenchmarkInput BaselineAssemblyPath -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
$tokens = Get-BenchmarkInput TokenPath -Required
$font = Get-BenchmarkInput FontPath -Required
$boldFont = Get-BenchmarkInput BoldFontPath -Required
$sceneGroup = Get-BenchmarkInput SceneGroup -Default 'Phase2'
if ($sceneGroup -notin 'Phase2', 'Phase3', 'All') { throw 'SceneGroup must be Phase2, Phase3 or All.' }
Add-Type -Path (Join-Path $PSScriptRoot 'ChartBenchmarkLane.cs')
$lanes = @{}
$fixtureNames = @('cartesian', 'donut', 'grouped-bars', 'stacked-bars', 'scatter', 'dense-line')
$phase3FixtureNames = @('matrix', 'calendar', 'progress', 'polar', 'map', 'treemap', 'sankey', 'topology')
if ($sceneGroup -eq 'Phase3') { $fixtureNames = $phase3FixtureNames }
elseif ($sceneGroup -eq 'All') { $fixtureNames += $phase3FixtureNames }
foreach ($lane in 'Baseline', 'Candidate') {
    foreach ($fixture in $fixtureNames) {
        $path = if ($lane -eq 'Baseline') { $baseline } else { $candidate }
        $lanes["$lane-$fixture"] = [SceneBenchmarkLane]::new($path, $fixtures, $tokens, $font, $boldFont, $fixture, $lane -eq 'Candidate')
    }
}
foreach ($fixture in $fixtureNames) {
    if ($lanes["Baseline-$fixture"].SourceDigest -ne $lanes["Candidate-$fixture"].SourceDigest) { throw "Paired source data changed for $fixture." }
}
$suiteName = if ($sceneGroup -eq 'Phase2') { 'chartforgex-direct-scenes' } else { 'chartforgex-direct-scenes-' + $sceneGroup.ToLowerInvariant() }
New-BenchmarkSuite $suiteName {
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
    Add-BenchmarkMetadata SceneGroup $sceneGroup
    Add-BenchmarkMetadata FixtureNames ($fixtureNames -join ',')
    foreach ($fixture in $fixtureNames) { Add-BenchmarkMetadata ("SourceSha256-" + $fixture) $lanes["Candidate-$fixture"].SourceDigest }
    Add-BenchmarkMetadata Scope 'Baseline and candidate Svg/Rgba/Png include complete layout and export. Candidate Compile measures preparation only; Prepared exports reuse a scene prepared outside timing. Models, canonical tokens, logical 800x440 size and scale 1 match. Frame and renderer changes intentionally differ; validation requires dimensions/content and within-lane determinism, not old/new byte equality.'
    Add-BenchmarkMetadata Phase3Scope 'Matrix 8x12, calendar 120-day interval with missing/zero days, six scalar progress rows, two 24-point polar series, six weighted world-map locations, twelve positive treemap tiles, eight Sankey links, and a twelve-node/eleven-edge layered topology. Phase3 chart padding24, typography22/13/11/12/11, registered Carlito and raster supersampling2 are explicit in both lanes. Topology shares tokens/font and fixed viewport but retains legacy topology typography versus the common candidate frame. Flow/sequence are excluded: frozen public exporters cannot accept equivalent token/font inputs. No new family is a complete-family or dense-scale performance claim.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata ProcessorPlacement 'Both lanes run in one process with rotated ordering; record power policy and processor placement with qualification.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
