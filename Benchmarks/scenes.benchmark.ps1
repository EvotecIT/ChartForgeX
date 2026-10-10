$candidate = Get-BenchmarkInput AssemblyPath -Required
$baseline = Get-BenchmarkInput BaselineAssemblyPath -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
$baselineRendering = Get-BenchmarkInput BaselineRendering -Default 'Public'
if ($baselineRendering -notin 'Public', 'Direct') { throw 'BaselineRendering must be Public or Direct.' }
$baselineApiProfile = Get-BenchmarkInput BaselineApiProfile -Default 'Current'
if ($baselineApiProfile -notin 'Current', 'Legacy') { throw 'BaselineApiProfile must be Current or Legacy.' }
if ($baselineRendering -eq 'Direct' -and $baselineApiProfile -eq 'Legacy') { throw 'Direct baseline rendering requires the Current API profile.' }
$baselineFixtures = Get-BenchmarkInput BaselineFixtureAssemblyPath -Required
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
        $adapter = if ($lane -eq 'Baseline') { $baselineFixtures } else { $fixtures }
        $direct = $lane -eq 'Candidate' -or $baselineRendering -eq 'Direct'
        $lanes["$lane-$fixture"] = [SceneBenchmarkLane]::new($path, $adapter, $tokens, $font, $boldFont, $fixture, $direct)
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
            Add-BenchmarkCase $fixture @{ Fixture = $fixture }
        }
    }
    Set-BenchmarkSetup { param($case, $run) $run.Lane = $lanes["$($case.Engine)-$($case.Fixture)"]; $run.Lane.Reset($case.Operation) }
    foreach ($lane in 'Baseline', 'Candidate') {
        $formats = @('Svg', 'Rgba', 'Png')
        if ($lane -eq 'Candidate') { $formats += 'Compile', 'PreparedSvg', 'PreparedRgba', 'PreparedPng' }
        Add-BenchmarkEngine $lane {
            foreach ($format in $formats) {
                Add-BenchmarkOperation $format {
                    param($case, $run)
                    $before = [GC]::GetAllocatedBytesForCurrentThread()
                    $run.Lane.Execute()
                    $run.Allocated = [GC]::GetAllocatedBytesForCurrentThread() - $before
                }
            }
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric OutputBytesOrRegions { param($case, $run) $run.Lane.OutputLength }
    Add-BenchmarkMetric ThreadAllocatedBytes { param($case, $run) $run.Allocated }
    Add-BenchmarkMetadata BaselineSha256 (Get-FileHash -LiteralPath $baseline).Hash
    Add-BenchmarkMetadata CandidateSha256 (Get-FileHash -LiteralPath $candidate).Hash
    Add-BenchmarkMetadata FixtureSha256 (Get-FileHash -LiteralPath $fixtures).Hash
    Add-BenchmarkMetadata BaselineFixtureSha256 (Get-FileHash -LiteralPath $baselineFixtures).Hash
    Add-BenchmarkMetadata BaselineRendering $baselineRendering
    Add-BenchmarkMetadata BaselineApiProfile $baselineApiProfile
    Add-BenchmarkMetadata PairedOrdering 'Shared fixture scenarios and format operations pair Baseline/Candidate engines; Rotated alternates each paired group. Lane and format are runtime fields, not differing case variables.'
    Add-BenchmarkMetadata ThemeSha256 (Get-FileHash -LiteralPath $tokens).Hash
    Add-BenchmarkMetadata FontSha256 (Get-FileHash -LiteralPath $font).Hash
    Add-BenchmarkMetadata BoldFontSha256 (Get-FileHash -LiteralPath $boldFont).Hash
    Add-BenchmarkMetadata SceneGroup $sceneGroup
    Add-BenchmarkMetadata FixtureNames ($fixtureNames -join ',')
    foreach ($fixture in $fixtureNames) { Add-BenchmarkMetadata ("SourceSha256-" + $fixture) $lanes["Candidate-$fixture"].SourceDigest }
    Add-BenchmarkMetadata Scope 'Baseline and candidate Svg/Rgba/Png include complete layout and export. Candidate Compile measures preparation only; Prepared exports reuse a scene prepared outside timing. Models, canonical tokens, logical 800x440 size and scale 1 match. Frame and renderer changes intentionally differ; validation requires dimensions/content and within-lane determinism, not old/new byte equality.'
    Add-BenchmarkMetadata Phase3Scope 'Matrix 8x12, calendar120days with missing/zero days, six progress rows, two24point polar series, six weighted map locations, twelve treemap tiles, eight Sankey links and twelve-node/eleven-edge topology. Chart padding24, typography22/13/11/12/11, Carlito and supersampling2 are explicit. Each adapter is compiled against its measured product binary; Legacy selects historical relationship APIs and public export metadata. Source digests compare actual common node labels, directed endpoints and weights rather than candidate-only identities. Direct baseline mode matches requested frame, theme and raster contracts including topology. Public baseline mode retains its convenience-export mapping, whose topology surface/typography can differ. Flow/sequence are excluded because historical public exporters cannot accept equivalent token/font inputs. These bounded fixtures do not establish dense-scale or all-family performance.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata ProcessorPlacement 'Both lanes run in one process with rotated ordering; record power policy and processor placement with qualification.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
