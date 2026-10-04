$fixtureAssembly = Get-BenchmarkInput FixtureAssembly -Required
$fontPath = Get-BenchmarkInput FontPath -Required
$productAssembly = Get-BenchmarkInput ProductAssembly -Required
$sourceCommit = Get-BenchmarkInput SourceCommit -Required
$proofDelay = Get-BenchmarkInput ProofDelayMilliseconds -Required
$powerPlan = if ($IsWindows) { (& powercfg /getactivescheme) -join ' ' } else { 'Not reported by this runner' }
Add-Type -Path $productAssembly
Add-Type -Path $fixtureAssembly
[RenderingHistoryFixture]::Initialize($fontPath)

New-BenchmarkSuite 'chartforgex-rendering-history' {
    Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases {
        foreach ($mode in 'Auto', 'Full', 'Svg') { Add-BenchmarkCase $mode @{ Mode = $mode } }
    }
    Add-BenchmarkEngine ChartForgeX {
        Add-BenchmarkOperation Render {
            param($case, $run)
            $run.Image = [RenderingHistoryFixture]::Render($case.Mode, [int]$proofDelay)
        }
    }
    Add-BenchmarkValidation { param($case, $run) [RenderingHistoryFixture]::Validate($run.Image) }
    Add-BenchmarkMetric PixelBytes { param($case, $run) $run.Image.Pixels.Length }
    Add-BenchmarkMetadata SourceCandidateCommit $sourceCommit
    Add-BenchmarkMetadata AssemblySha256 (Get-FileHash $productAssembly).Hash
    Add-BenchmarkMetadata FixtureSha256 (Get-FileHash $fixtureAssembly).Hash
    Add-BenchmarkMetadata FontSha256 (Get-FileHash $fontPath).Hash
    Add-BenchmarkMetadata ProofDelayMilliseconds $proofDelay
    Add-BenchmarkMetadata PowerPlan $powerPlan
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
