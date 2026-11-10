$binaries = Get-BenchmarkInput ProductBinaryRoot -Required
$fixture = Get-BenchmarkInput FixtureAssemblyPath -Required
$baselineBinaries = Get-BenchmarkInput BaselineBinaryRoot -Default ''
$queries = Get-BenchmarkInput Queries -Default 16
$specHash = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot 'interactivity.benchmark.ps1')).Hash
Add-Type -Path (Join-Path $PSScriptRoot 'Interactivity/InteractivityBenchmarkLane.cs')
$lanes = @{}
$engines = if ($baselineBinaries) { @('Baseline', 'Candidate') } else { @('Candidate') }
foreach ($engine in $engines) {
    $binaryRoot = if ($engine -eq 'Baseline') { $baselineBinaries } else { $binaries }
    foreach ($points in 1000, 5000) {
        $lanes["$engine-$points"] = [InteractivityBenchmarkLane]::new($fixture, $binaryRoot, $points, $queries)
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
            Add-BenchmarkOperation PointerHover { param($case, $run) $run.Lane.Execute() }
        }
    }
    Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
    Add-BenchmarkMetric VerifiedPoints { param($case, $run) $case.Points }
    Add-BenchmarkMetric QueriesPerOperation { param($case, $run) $case.Queries }
    Add-BenchmarkMetric SourceIndexSum { param($case, $run) $run.Lane.SourceIndexSum }
    Add-BenchmarkMetadata AdapterAssemblySha256 (Get-FileHash -LiteralPath (Join-Path $binaries 'ChartForgeX.Interactivity.Html.dll')).Hash
    if ($baselineBinaries) {
        Add-BenchmarkMetadata BaselineAdapterAssemblySha256 (Get-FileHash -LiteralPath (Join-Path $baselineBinaries 'ChartForgeX.Interactivity.Html.dll')).Hash
        Add-BenchmarkComparison Engine -Baseline Baseline -Metric MedianMs
    }
    Add-BenchmarkMetadata FixtureAssemblySha256 (Get-FileHash -LiteralPath $fixture).Hash
    Add-BenchmarkMetadata BenchmarkSpecSha256 $specHash
    $browserAssembly = Join-Path $binaries 'HtmlTinkerX.dll'
    Add-BenchmarkMetadata HtmlTinkerXVersion ([Reflection.AssemblyName]::GetAssemblyName($browserAssembly).Version.ToString())
    Add-BenchmarkMetadata HtmlTinkerXProductVersion ([Diagnostics.FileVersionInfo]::GetVersionInfo($browserAssembly).ProductVersion)
    if ($baselineBinaries) {
        $baselineBrowserAssembly = Join-Path $baselineBinaries 'HtmlTinkerX.dll'
        Add-BenchmarkMetadata BaselineHtmlTinkerXVersion ([Reflection.AssemblyName]::GetAssemblyName($baselineBrowserAssembly).Version.ToString())
        Add-BenchmarkMetadata BaselineHtmlTinkerXProductVersion ([Diagnostics.FileVersionInfo]::GetVersionInfo($baselineBrowserAssembly).ProductVersion)
    }
    Add-BenchmarkMetadata Scope 'One persistent Chromium session per point count. PowerForge times a batch of synthetic pointermove events dispatched on the plot stage at each probe, with the animation frames they request run synchronously, plus one Playwright round-trip/serialization. Each query covers nearest-observation acquisition, hover state and tooltip commit. Chart preparation, browser startup, geometry oracle, a real mouse hover proof and complete result validation are outside timing. No performance threshold is part of correctness CI.'
    Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
    Add-BenchmarkMetadata HostNoise (Get-BenchmarkInput HostNoise -Default 'Normal development host; placement unchanged')
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
