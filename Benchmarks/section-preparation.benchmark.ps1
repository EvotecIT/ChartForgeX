$binaries = Get-BenchmarkInput ProductBinaryRoot -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
$matrix = Get-BenchmarkInput Matrix -Default 'Shared'
if ($matrix -notin 'Shared', 'Cartesian') { throw 'Matrix must be Shared or Cartesian.' }
$baseline = Get-BenchmarkInput BaselineBinaryRoot -Default ''
$baselineFixtures = Get-BenchmarkInput BaselineFixtureAssemblyPath -Default ''
if ($matrix -eq 'Shared' -and (-not $baseline -or -not $baselineFixtures)) { throw 'Shared preparation requires baseline binaries and its separately compiled fixture.' }
$tokens = Get-BenchmarkInput TokenPath -Required
$font = Get-BenchmarkInput FontPath -Required
$boldFont = Get-BenchmarkInput BoldFontPath -Required
Add-Type -Path (Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkLane.cs')
$windows = if ($matrix -eq 'Shared') { @('full', 'x') } else { @('y', 'xy') }
$engines = if ($matrix -eq 'Shared') { @('Baseline', 'Candidate') } else { @('Candidate') }
$caseNames = foreach ($size in 'small', 'large') { foreach ($domain in 'explicit', 'auto') { foreach ($window in $windows) { "$size-$domain-$window" } } }
$lanes = @{}
foreach ($engine in $engines) {
    foreach ($caseName in $caseNames) {
        $current = $engine -eq 'Candidate'
        $binaryRoot = if ($current) { $binaries } else { $baseline }
        $fixturePath = if ($current) { $fixtures } else { $baselineFixtures }
        $lanes["$engine-$caseName"] = [InteractivityBenchmarkLane]::new($fixturePath, $binaryRoot, $caseName, $tokens, $font, $boldFont, $current)
    }
}
New-BenchmarkSuite "chartforgex-section-preparation-$($matrix.ToLowerInvariant())" {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($caseName in $caseNames) { Add-BenchmarkCase $caseName @{ Fixture = $caseName } }
    }
    Set-BenchmarkSetup {
        param($case, $run)
        $run.Lane = $lanes["$($case.Engine)-$($case.Fixture)"]
        if ($matrix -eq 'Shared') { $run.Lane.ExpectSource($lanes["Baseline-$($case.Fixture)"]) }
        $run.Lane.Reset()
    }
    foreach ($engine in $engines) {
        Add-BenchmarkEngine $engine {
            Add-BenchmarkOperation PrepareSection {
                param($case, $run)
                $before = [GC]::GetAllocatedBytesForCurrentThread()
                $run.Lane.Execute()
                $run.Allocated = [GC]::GetAllocatedBytesForCurrentThread() - $before
            }
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric ThreadAllocatedBytes { param($case, $run) $run.Allocated }
    Add-BenchmarkMetric HtmlUtf8Bytes { param($case, $run) $run.Lane.HtmlBytes }
    Add-BenchmarkMetric SourceCount { param($case, $run) $run.Lane.SourceCount }
    Add-BenchmarkMetric WideSvgElements { param($case, $run) $run.Lane.WideSvgElementCount }
    Add-BenchmarkMetric NarrowSvgElements { param($case, $run) $run.Lane.NarrowSvgElementCount }
    if ($matrix -eq 'Shared') { Add-BenchmarkComparison Engine -Baseline Baseline -Metric MedianMs }
    Add-BenchmarkMetadata Matrix $matrix
    Add-BenchmarkMetadata FixtureAssemblySha256 (Get-FileHash -LiteralPath $fixtures).Hash
    if ($matrix -eq 'Shared') { Add-BenchmarkMetadata BaselineFixtureAssemblySha256 (Get-FileHash -LiteralPath $baselineFixtures).Hash }
    foreach ($engine in $engines) {
        $root = if ($engine -eq 'Baseline') { $baseline } else { $binaries }
        foreach ($name in 'ChartForgeX', 'ChartForgeX.Interactivity', 'ChartForgeX.Interactivity.Html') {
            Add-BenchmarkMetadata "$engine-$name-Sha256" (Get-FileHash -LiteralPath (Join-Path $root "$name.dll")).Hash
        }
    }
    foreach ($source in 'SectionPreparationBenchmarkFixture.cs', 'CartesianSectionPreparationBenchmarkFixture.cs', 'InteractivityBenchmarkLane.cs', 'InteractivityBenchmarkFixtures.csproj') {
        Add-BenchmarkMetadata "$source-Sha256" (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot "Interactivity/$source")).Hash
    }
    Add-BenchmarkMetadata SpecSha256 (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'section-preparation.benchmark.ps1')).Hash
    Add-BenchmarkMetadata WrapperSha256 (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Invoke-SectionPreparationBenchmark.ps1')).Hash
    Add-BenchmarkMetadata ThemeSha256 (Get-FileHash -LiteralPath $tokens).Hash
    Add-BenchmarkMetadata FontSha256 (Get-FileHash -LiteralPath $font).Hash
    Add-BenchmarkMetadata BoldFontSha256 (Get-FileHash -LiteralPath $boldFont).Hash
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata HostNoise (Get-BenchmarkInput HostNoise -Default 'Normal development host; record observed before/after activity separately')
    Add-BenchmarkMetadata Scope 'One synchronous public adaptive PrepareSection call, including native preparation and internal wide/narrow SVG/HTML generation. Source construction, viewport objects, font registration, source/output hashing, validation, receipts and PNG export are outside timing. No browser starts. Shared Full/X pairs independently compiled baseline/current public fixtures exposing the canonical Cartesian preparation API; retired X-only baselines are incompatible. Cartesian Y/XY has only the current engine. There is no historical Y/XY surrogate.'
    Add-BenchmarkMetadata Validation 'Every source ID/X/Y in both native drawings, exact requested windows, unchanged authored axes, native dimensions, asset-free HTML and within-lane deterministic HTML. Current additionally checks aligned full X/Y domains and inverse mappings. Historical automatic width-dependent domains are recorded, not held to the new alignment contract. First/final HTML/SVG/PNG and actual source/projection receipts are written after measurement.'
    Add-BenchmarkMetadata Interpretation 'Warm total-call latency/allocation on this observed development host. No scene-pass count, per-event/browser latency, cold startup, concurrency, all-family or cross-machine claim. Retain slower rows, failures and all rotated samples without outlier removal.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
