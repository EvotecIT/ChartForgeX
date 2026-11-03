$candidate = Get-BenchmarkInput AssemblyPath -Required
$fixtures = Get-BenchmarkInput FixtureAssemblyPath -Required
$tokens = Get-BenchmarkInput TokenPath -Required
$font = Get-BenchmarkInput FontPath -Required
$boldFont = Get-BenchmarkInput BoldFontPath -Required
$matrix = Get-BenchmarkInput XZoomMatrix -Default 'Full'
if ($matrix -notin 'Smoke', 'Full') { throw 'XZoomMatrix must be Smoke or Full.' }
Add-Type -Path (Join-Path $PSScriptRoot 'XZoomBenchmarkLane.cs')
$lane = [XZoomBenchmarkLane]::new($candidate, $fixtures, $tokens, $font, $boldFont)
$definitions = @()
foreach ($kind in 'line', 'step') {
    foreach ($scale in 'small', 'regular', 'default', 'dense', 'labeled', 'references') {
        $definitions += @{ Name = "$kind-$scale"; Fixture = "$kind-$scale"; Width = 800; Height = 440; Theme = 'Light'; Reversed = $false }
    }
    $definitions += @{ Name = "$kind-references-compact-dark-reversed"; Fixture = "$kind-references"; Width = 420; Height = 280; Theme = 'Dark'; Reversed = $true }
}
if ($matrix -eq 'Smoke') { $definitions = @($definitions | Where-Object { $_.Name -in 'line-small', 'line-default' }) }
$sourceDigests = @{}
foreach ($definition in $definitions) {
    $descriptor = "$($definition.Fixture)|Standard|2|$($definition.Width)|$($definition.Height)|$($definition.Theme)|$($definition.Reversed)|0.173"
    $sourceDigests[$definition.Name] = $lane.SourceDigest($descriptor)
}

# Separate operation suites avoid duplicate PrepareBank samples for the level-only At/export dimension.
# Rotation is within each operation's input matrix. There is one candidate engine and no old/new comparison claim.
foreach ($operation in 'PrepareBank', 'PreparedAt', 'PreparedSvg') {
    $suiteName = 'chartforgex-xzoom-' + $operation.ToLowerInvariant()
    New-BenchmarkSuite $suiteName {
        Set-BenchmarkPolicy -Warmup 2 -Iterations 9 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
        Set-BenchmarkProfile Current -Cleanup KeepOnFailure
        Add-BenchmarkCases {
            foreach ($definition in $definitions) {
                $banks = if ($matrix -eq 'Smoke') { @('Standard') } else { @('Standard', 'High') }
                foreach ($bank in $banks) {
                    $last = if ($bank -eq 'Standard') { 2 } else { 4 }
                    $levelIndices = if ($operation -eq 'PrepareBank' -or $matrix -eq 'Smoke') { @($last) } else { @(0..$last) }
                    foreach ($level in $levelIndices) {
                        $descriptor = "$($definition.Fixture)|$bank|$level|$($definition.Width)|$($definition.Height)|$($definition.Theme)|$($definition.Reversed)|0.173"
                        Add-BenchmarkCase "$($definition.Name)-$bank-L$level" @{
                            Fixture = $definition.Fixture; Bank = $bank; LevelIndex = $level
                            Width = $definition.Width; Height = $definition.Height; Theme = $definition.Theme
                            Reversed = $definition.Reversed; PanFraction = 0.173; Descriptor = $descriptor
                            SourceSha256 = $sourceDigests[$definition.Name]
                        }
                    }
                }
            }
        }
        Set-BenchmarkSetup { param($case, $run) $run.Lane = $lane; $run.Lane.Reset($case.Descriptor, $case.Operation) }
        Add-BenchmarkEngine Candidate {
            Add-BenchmarkOperation $operation { param($case, $run) $run.Lane.Execute() }
        }
        Add-BenchmarkValidation { param($case, $run) $run.Lane.Validate() }
        foreach ($name in 'BankLevels', 'BankNodes', 'BankPathCommands', 'BankGlyphs', 'BankTicks', 'BankVisibilityIntervals', 'BankNativeSvgUtf8Bytes',
            'ActiveNodes', 'ActivePathCommands', 'ActiveGlyphs', 'ActiveTicks', 'ActiveVisibilityIntervals', 'ActiveNativeSvgUtf8Bytes',
            'BankCaptions', 'BankHiddenClearanceIntervals', 'AtSvgUtf8Bytes', 'SourcePointFacts', 'XFormatterCalls', 'ValueFormatterCalls') {
            Add-BenchmarkMetric $name { param($case, $run) $run.Lane.Metrics[$name] }
        }
        Add-BenchmarkMetadata CandidateSha256 (Get-FileHash -LiteralPath $candidate).Hash
        Add-BenchmarkMetadata FixtureSha256 (Get-FileHash -LiteralPath $fixtures).Hash
        Add-BenchmarkMetadata ThemeSha256 (Get-FileHash -LiteralPath $tokens).Hash
        Add-BenchmarkMetadata FontSha256 (Get-FileHash -LiteralPath $font).Hash
        Add-BenchmarkMetadata BoldFontSha256 (Get-FileHash -LiteralPath $boldFont).Hash
        Add-BenchmarkMetadata XZoomMatrix $matrix
        Add-BenchmarkMetadata LogicalProcessors ([Environment]::ProcessorCount)
        Add-BenchmarkMetadata NativeTypography 'Canonical tokens; Carlito regular400/bold700; typography22/13/11/12/11; padding24; native PNG scale1/supersampling2 validation.'
        Add-BenchmarkMetadata SourceMatrix 'Line and Step(Middle): 8,256,2048 points; default-format256 without a custom value callback; labeled128; references64+64 secondary-logY with fixed/moving line/band/region captions. Bounded gaps (small1,others3) and authored X0..N-1/Y0..100. Extra references rows:420x280 Dark reversed. Other rows:800x440 Light normal.'
        Add-BenchmarkMetadata BankMatrix 'Standard=1,2,4; High=1,2,4,8,16. PrepareBank once per source/bank; PreparedAt/PreparedSvg every level. Nonpreset source-order pan0.173; validation checks0/0.173/1 and shared fixed plot/Y.'
        Add-BenchmarkMetadata TimingScope 'One candidate/public operation per sample; PowerForge owns timing/warmups/iterations/rotation/artifacts. Models/fonts/setup/expectations/native counts/reflection/validation are outside timing. PreparedSvg exports an At view prepared outside timing with fixed host namespace xzoom-benchmark. Includes runner/delegate dispatch; no amortization loop.'
        Add-BenchmarkMetadata NativeCountScope 'Bank/Active exact retained layer scene nodes,pathcommands,glyphs,ticks,caption visibility intervals. NativeSvgUtf8Bytes sums each retained layer exported as a separate complete native SVG document; AtSvgUtf8Bytes is actual current-view native SVG UTF8. HiddenClearanceIntervals excludes source/rectangle admission ranges. These are native fixture payload units, not HTML payload bytes.'
        Add-BenchmarkMetadata ValidationScope 'Every measured result: full source identities, numeric domains/projection, point-anchor geometry, fixed plot/fullY, gaps, static native SVG determinism, callback constancy through At/export/metrics. Native PNG byte determinism/dimensions and blue native raster ink qualified once per exact descriptor, outside timing; no baseline byte equivalence.'
        Add-BenchmarkMetadata Limits 'Production level/geometry/output/interval limits HELD on admitted measured results and host scope. Existing correctness guards and these finite matrix inputs are not measured limits.'
        Add-BenchmarkMetadata ProcessorPlacement 'One current process and one isolated product context. Root records actual host/CPU/OS/power/domain context at admission; no placement or portable performance claim.'
        Set-BenchmarkArtifacts Json, Csv, Markdown
    }
}
