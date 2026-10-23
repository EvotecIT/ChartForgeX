$binaries = Get-BenchmarkInput ProductBinaryRoot -Required
$fixture = Get-BenchmarkInput FixtureAssemblyPath -Required
$matrix = Get-BenchmarkInput Matrix -Default 'Current'
$baseline = Get-BenchmarkInput BaselineBinaryRoot -Default ''
$baselineFixture = Get-BenchmarkInput BaselineFixtureAssemblyPath -Default ''
$updates = Get-BenchmarkInput UpdatesPerOperation -Default 10
$tokens = Get-BenchmarkInput TokenPath -Required
$font = Get-BenchmarkInput FontPath -Required
$boldFont = Get-BenchmarkInput BoldFontPath -Required
if ($matrix -notin 'Current', 'Shared') { throw 'Matrix must be Current or Shared.' }
if ($matrix -eq 'Shared' -and (-not $baseline -or -not $baselineFixture)) {
    throw 'Shared sustained updates require a separately compiled identical fixture and compatible baseline binaries.'
}
Add-Type -Path (Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkLane.cs')
$cases = @(
    @{ Name = 'points-32-primary'; Points = 32; Companion = $false }
    @{ Name = 'points-1000-secondary'; Points = 1000; Companion = $true }
)
if ($matrix -eq 'Current') { $cases += @{ Name = 'points-5000-secondary'; Points = 5000; Companion = $true } }
$engines = if ($matrix -eq 'Shared') { @('Baseline', 'Candidate') } else { @('Candidate') }
$lanes = @{}
foreach ($engine in $engines) {
    foreach ($item in $cases) {
        $root = if ($engine -eq 'Baseline') { $baseline } else { $binaries }
        $assembly = if ($engine -eq 'Baseline') { $baselineFixture } else { $fixture }
        $lanes["$engine-$($item.Name)"] = [InteractivityBenchmarkLane]::new(
            $assembly, $root, $item.Points, $updates, $item.Companion, $tokens, $font, $boldFont, $engine)
    }
}
New-BenchmarkSuite "chartforgex-sustained-update-$($matrix.ToLowerInvariant())" {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($item in $cases) {
            Add-BenchmarkCase $item.Name @{ Fixture = $item.Name; Points = $item.Points; Companion = $item.Companion; Updates = $updates }
        }
    }
    Set-BenchmarkSetup {
        param($case, $run)
        $run.Lane = $lanes["$($case.Engine)-$($case.Fixture)"]
        if ($matrix -eq 'Shared') { $run.Lane.ExpectSource($lanes["Baseline-$($case.Fixture)"]) }
        $run.Lane.Reset()
    }
    foreach ($engine in $engines) {
        Add-BenchmarkEngine $engine {
            Add-BenchmarkOperation SourcePrepareBrowserCommitBatch { param($case, $run) $run.Lane.Execute() }
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric AcceptedCommits { param($case, $run) $run.Lane.CommittedUpdates }
    Add-BenchmarkMetric UpdatesPerOperation { param($case, $run) $case.Updates }
    Add-BenchmarkMetric PointsPerSeries { param($case, $run) $case.Points }
    Add-BenchmarkMetric RetainedSourceCount { param($case, $run) $run.Lane.SourceCount }
    Add-BenchmarkMetric SourceIndexSum { param($case, $run) $run.Lane.SourceIndexSum }
    Add-BenchmarkMetric HtmlUtf8Bytes { param($case, $run) $run.Lane.HtmlBytes }
    if ($matrix -eq 'Shared') { Add-BenchmarkComparison Engine -Baseline Baseline -Metric MedianMs }
    Add-BenchmarkMetadata Matrix $matrix
    Add-BenchmarkMetadata BaselineAvailability (Get-BenchmarkInput BaselineAvailability -Required)
    foreach ($engine in $engines) {
        $root = if ($engine -eq 'Baseline') { $baseline } else { $binaries }
        foreach ($name in 'ChartForgeX', 'ChartForgeX.Interactivity', 'ChartForgeX.Interactivity.Html', 'HtmlTinkerX', 'Microsoft.Playwright') {
            Add-BenchmarkMetadata "$engine-$name-Sha256" (Get-FileHash -LiteralPath (Join-Path $root "$name.dll")).Hash
        }
        $assembly = if ($engine -eq 'Baseline') { $baselineFixture } else { $fixture }
        Add-BenchmarkMetadata "$engine-FixtureAssemblySha256" (Get-FileHash -LiteralPath $assembly).Hash
        $browserAssembly = Join-Path $root 'HtmlTinkerX.dll'
        Add-BenchmarkMetadata "$engine-HtmlTinkerXVersion" ([Reflection.AssemblyName]::GetAssemblyName($browserAssembly).Version.ToString())
        Add-BenchmarkMetadata "$engine-HtmlTinkerXProductVersion" ([Diagnostics.FileVersionInfo]::GetVersionInfo($browserAssembly).ProductVersion)
    }
    foreach ($source in 'SustainedUpdateBenchmarkFixture.cs', 'SustainedUpdateBenchmarkFixture.Verification.cs', 'InteractivityBenchmarkLane.cs', 'InteractivityBenchmarkFixtures.csproj') {
        Add-BenchmarkMetadata "$source-Sha256" (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot "Interactivity/$source")).Hash
    }
    Add-BenchmarkMetadata SpecSha256 (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'sustained-update.benchmark.ps1')).Hash
    Add-BenchmarkMetadata WrapperSha256 (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'Invoke-SustainedUpdateBenchmark.ps1')).Hash
    Add-BenchmarkMetadata ThemeSha256 (Get-FileHash -LiteralPath $tokens).Hash
    Add-BenchmarkMetadata FontSha256 (Get-FileHash -LiteralPath $font).Hash
    Add-BenchmarkMetadata BoldFontSha256 (Get-FileHash -LiteralPath $boldFont).Hash
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata HostNoise (Get-BenchmarkInput HostNoise -Default 'Normal development host; record actual activity separately')
    Add-BenchmarkMetadata Scope 'One measured batch contains the declared number of sequential keyed remove/replace/append transactions, actual adaptive PrepareSection calls, public controller.update Playwright round-trips and one requestAnimationFrame callback after each observed DOM commit. Browser/font startup, input generation, reset, complete source/native/browser/export validation, screenshots, PNG encoding and downloads are outside timing. This measures ordinary host-pushed update delivery, not application transport, native-request supersession or collection cadence.'
    Add-BenchmarkMetadata Validation 'Every original surviving/new observation ID and exact X/Y, bounded per-series count and ordered rolling window in both native drawings and actual committed DOM; surviving source paint, selection and keyboard focus; one connected mounted chart; accepted commit count and scene revisions; every downloaded SVG source fact; first/final native and browser PNG dimensions; page/console errors. A source digest must match before baseline/current paired work.'
    Add-BenchmarkMetadata Interpretation 'PowerForge owns all elapsed samples, warmups, rotated order and artifacts. No local timer, speed threshold, asynchronous thread-allocation figure, total-browser-memory bound, frame-p95, comparison surrogate or README/history overwrite is part of this workload. Current-only output has no historical ranking; dense 5000-point-per-series work is current-only. Retain all failures, slower samples and independent-launch variation.'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
