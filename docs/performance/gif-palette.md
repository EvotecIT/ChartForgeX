# Exact GIF palette lookup

The goal is to bound nearest-colour lookup after the 65,536-entry RGB cache fills,
without changing the palette, nearest-colour result, tie order or encoded bytes.

## Reproduction

Build `ChartForgeX.Stories` in Release for net8.0 at each revision and retain both
assemblies (`ChartForgeX.dll` and `ChartForgeX.Stories.dll`) in separate directories.
Use PowerForge's benchmark runner from PSPublishModule 3.0.168:

```powershell
Import-Module PSPublishModule
Invoke-BenchmarkSuite -Path ./Benchmarks/animation.benchmark.ps1 `
    -OutputRoot ./Ignore/Benchmarks/Animation -WarmupCount 1 -IterationCount 3 `
    -Variable @{ AssemblyPath = '/path/to/ChartForgeX.dll'
                 StoriesAssemblyPath = '/path/to/ChartForgeX.Stories.dll'
                 OutputPath = '/path/to/result.gif'
                 ExpectedSha256 = 'A6407E20839736953266EC3D451238E2E11C12765B81049A40D43E62BCDA3346' }
```

Setup creates a 1920×1080 frame with 2,073,600 distinct RGB colours. Only the
public GIF encode call is timed; decoding, dimension checks, SHA-256 verification
and saving the result happen outside measurement. One warmup precedes three
samples. The benchmark is optional and has no correctness-CI timing threshold.

## Measurements and attempts

2026-10-25, Apple M4, macOS 27.0.1 (26A434), PowerShell 7.5.4, normal process
priority and OS scheduling.
Other development work was running (load averages 24.59/69.45/223.08 at the first
checkpoint); these are local comparisons, not portable throughput guarantees.

| Attempt | Median encode | Range | GIF bytes | Disposition |
| --- | ---: | ---: | ---: | --- |
| Baseline `ec78271ee`: full palette scan on cache miss | 885.0 ms | 809.5–3096.2 ms | 322,084 | Replaced |
| Conservative 16³ cell candidate index | 156.3 ms | 78.8–161.0 ms | 322,084 | Kept; 5.7× lower median |

Both outputs have the SHA-256 above. No samples or outliers were removed.
Allocated bytes per encode rose from about 18.25 MB to 19.38 MB, including the
bounded per-palette candidate index. Its candidate payload is at most 1 MiB
(4,096 cells × 256 indices), within the existing 16 MiB GIF working-buffer reserve.

Each cell keeps every colour whose minimum distance to that cell can beat an
upper bound valid for every pixel in it. A query compares actual RGB distances
among those candidates in palette order. Equal-distance entries retain the first
index. Exact palette colours still bypass the index, which is built lazily with
cancellation checks. This does not use a cell representative as the pixel result.

The mapping tests compare all pixels in opaque and transparent 131,841-colour
frames with independent brute-force scans, plus duplicate/equidistant palette
entries. The public benchmark also proves byte-identical complete GIF output.

Stop rule: stop when exact mapping, complete-output identity, bounded memory and
a clear local encode-time improvement are proven. No k-d tree or approximate
fallback was tried. There are no remaining optimization attempts in this batch;
revisit only if a measured workload exposes a different bottleneck.
