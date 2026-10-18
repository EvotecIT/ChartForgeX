$binaries = Get-BenchmarkInput ProductBinaryRoot -Required
$fixture = Get-BenchmarkInput FixtureAssemblyPath -Required
$source = Get-BenchmarkInput AcquisitionSourcePath -Required
$baselineBinaries = Get-BenchmarkInput BaselineBinaryRoot -Default ''
$baselineSource = Get-BenchmarkInput BaselineAcquisitionSourcePath -Default ''
$queries = Get-BenchmarkInput Queries -Default 16
$specHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'interactivity.benchmark.ps1')).Hash
Add-Type -Path (Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkLane.cs')
$lanes = @{}
$engines = if ($baselineBinaries) { @('Baseline', 'Candidate') } else { @('Candidate') }
foreach ($engine in $engines) {
    $binaryRoot = if ($engine -eq 'Baseline') { $baselineBinaries } else { $binaries }
    $acquisition = if ($engine -eq 'Baseline') { $baselineSource } else { $source }
    foreach ($points in 1000, 5000) {
        $lanes["$engine-$points"] = [InteractivityBenchmarkLane]::new($fixture, $binaryRoot, $acquisition, $points, $queries)
    }
}
New-BenchmarkSuite 'chartforgex-interactivity' {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 5 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($points in 1000, 5000) { Add-BenchmarkCase "points-$points" @{ Points = $points; Queries = $queries } }
    }
    Set-BenchmarkSetup { param($case, $run) $run.Lane = $lanes["$($case.Engine)-$($case.Points)"]; $run.Lane.Reset() }
    foreach ($engine in $engines) {
        Add-BenchmarkEngine $engine {
            Add-BenchmarkOperation Acquire { param($case, $run) $run.Lane.Execute() }
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric VerifiedPoints { param($case, $run) $case.Points }
    Add-BenchmarkMetric QueriesPerOperation { param($case, $run) $case.Queries }
    Add-BenchmarkMetric SourceIndexSum { param($case, $run) $run.Lane.SourceIndexSum }
    Add-BenchmarkMetadata AcquisitionSourceSha256 (Get-FileHash -LiteralPath $source).Hash
    Add-BenchmarkMetadata AdapterAssemblySha256 (Get-FileHash -LiteralPath (Join-Path $binaries 'ChartForgeX.Interactivity.Html.dll')).Hash
    if ($baselineBinaries) {
        Add-BenchmarkMetadata BaselineAcquisitionSourceSha256 (Get-FileHash -LiteralPath $baselineSource).Hash
        Add-BenchmarkMetadata BaselineAdapterAssemblySha256 (Get-FileHash -LiteralPath (Join-Path $baselineBinaries 'ChartForgeX.Interactivity.Html.dll')).Hash
        Add-BenchmarkComparison Engine -Baseline Baseline -Metric MedianMs
    }
    Add-BenchmarkMetadata FixtureAssemblySha256 (Get-FileHash -LiteralPath $fixture).Hash
    Add-BenchmarkMetadata BenchmarkSpecSha256 $specHash
    Add-BenchmarkMetadata HtmlTinkerXVersion '3.0.5'
    Add-BenchmarkMetadata Scope 'One persistent Chromium session per point count. PowerForge times a batch of in-page nearest-observation queries plus Playwright round-trip/serialization; chart preparation, browser startup, geometry oracle, public pointer hover, and complete result validation are outside timing. No performance threshold is part of correctness CI.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata HostNoise (Get-BenchmarkInput HostNoise -Default 'Normal development host; placement unchanged')
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
