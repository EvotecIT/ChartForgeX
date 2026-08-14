$assemblyPath = Get-BenchmarkInput AssemblyPath -Required
Add-Type -Path $assemblyPath

function New-ChartForgeXDenseSeries {
    $pointCount = 100000
    $points = [ChartForgeX.Primitives.ChartPoint[]]::new($pointCount)
    for ($index = 0; $index -lt $pointCount; $index++) {
        $signal = 140 + [Math]::Sin($index / 190.0) * 22 + [Math]::Sin($index / 37.0) * 5
        if ($index -ge 51700 -and $index -lt 51900) { $signal += 84 }
        $points[$index] = [ChartForgeX.Primitives.ChartPoint]::new($index, $signal)
    }
    $points
}

$assemblyHash = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash

New-BenchmarkSuite 'chartforgex-decimation' {
    Set-BenchmarkPolicy -Warmup 1 -Iterations 7 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure

    Add-BenchmarkCases {
        Add-BenchmarkCase LargestTriangleThreeBuckets @{ Mode = 'LargestTriangleThreeBuckets'; MaximumPoints = 1200 }
        Add-BenchmarkCase MinMax @{ Mode = 'MinMax'; MaximumPoints = 1200 }
    }

    Set-BenchmarkSetup {
        param($case, $run)
        $run.Points = [ChartForgeX.Primitives.ChartPoint[]](New-ChartForgeXDenseSeries)
        $run.Mode = [ChartForgeX.Core.ChartDecimationMode]::$($case.Mode)
    }

    Add-BenchmarkEngine ChartForgeX {
        Add-BenchmarkOperation Decimate {
            param($case, $run)
            $run.Result = [ChartForgeX.Core.ChartDecimator]::Decimate($run.Points, $case.MaximumPoints, $run.Mode)
            $run.IndexChecksum = ($run.Result.SourceIndices | Measure-Object -Sum).Sum
        }
    }

    Add-BenchmarkValidation {
        param($case, $run)
        if ($run.Result.SourcePointCount -ne $run.Points.Length) { throw 'The decimator did not report the complete source count.' }
        if ($run.Result.Points.Count -gt $case.MaximumPoints) { throw 'The decimator exceeded the requested point budget.' }
        if ($run.Result.SourceIndices[0] -ne 0 -or $run.Result.SourceIndices[$run.Result.SourceIndices.Count - 1] -ne $run.Points.Length - 1) { throw 'The decimator did not preserve both endpoints.' }
        if ($run.IndexChecksum -le 0) { throw 'The decimator source-index result was not consumed.' }
    }

    Add-BenchmarkMetric SourcePoints { param($case, $run) $run.Result.SourcePointCount }
    Add-BenchmarkMetric RetainedPoints { param($case, $run) $run.Result.Points.Count }
    Add-BenchmarkMetric RetentionPercent { param($case, $run) [Math]::Round($run.Result.Points.Count * 100.0 / $run.Result.SourcePointCount, 4) }

    Add-BenchmarkMetadata AssemblySha256 $assemblyHash
    Add-BenchmarkMetadata ComparisonMode 'Explicit ChartForgeX point reduction only; rendering is measured by the separate rendering suite'
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
