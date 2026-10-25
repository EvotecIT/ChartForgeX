$assemblyPath = Get-BenchmarkInput AssemblyPath -Required
$storiesPath = Get-BenchmarkInput StoriesAssemblyPath -Required
Add-Type -Path $assemblyPath
Add-Type -Path $storiesPath

New-BenchmarkSuite 'chartforgex-animation' {
    Set-BenchmarkPolicy -Warmup 1 -Iterations 5 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    Set-BenchmarkProfile Current -Cleanup KeepOnFailure
    Add-BenchmarkCases { Add-BenchmarkCase Gradient1080 @{} }
    Set-BenchmarkSetup {
        param($case, $run)
        # Each pixel has a distinct RGB colour; the cache fills within the first 35 rows.
        $pixels = [byte[]]::new(1920 * 1080 * 4)
        for ($i = 0; $i -lt 1920 * 1080; $i++) {
            $at = $i * 4
            $pixels[$at] = ($i -shr 16) -band 255
            $pixels[$at + 1] = ($i -shr 8) -band 255
            $pixels[$at + 2] = $i -band 255
            $pixels[$at + 3] = 255
        }
        $image = [ChartForgeX.Raster.RgbaImage]::new(1920, 1080, $pixels)
        $run.Frames = [ChartForgeX.Raster.RasterAnimationFrame[]]@(
            [ChartForgeX.Raster.RasterAnimationFrame]::new($image, [TimeSpan]::FromSeconds(1)))
    }
    Add-BenchmarkEngine ChartForgeX {
        Add-BenchmarkOperation Encode {
            param($case, $run)
            $run.Bytes = [ChartForgeX.Raster.RasterAnimationEncoder]::Encode(
                $run.Frames, [ChartForgeX.Raster.RasterAnimationFormat]::Gif)
        }
    }
    Add-BenchmarkValidation {
        param($case, $run)
        $decoded = [ChartForgeX.Raster.RasterImageDecoder]::Decode($run.Bytes)
        if ($decoded.Width -ne 1920 -or $decoded.Height -ne 1080) { throw 'Unexpected GIF dimensions.' }
        $run.Hash = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($run.Bytes))
        $expected = Get-BenchmarkInput ExpectedSha256
        if ($expected -and $run.Hash -ne $expected) { throw 'GIF output changed.' }
        $output = Get-BenchmarkInput OutputPath
        if ($output) { [IO.File]::WriteAllBytes($output, $run.Bytes) }
    }
    Add-BenchmarkMetric OutputBytes { param($case, $run) $run.Bytes.Length }
    Add-BenchmarkMetadata AssemblySha256 (Get-FileHash $storiesPath -Algorithm SHA256).Hash
    Set-BenchmarkArtifacts Json, Csv, Markdown
}
