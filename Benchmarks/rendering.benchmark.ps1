$assemblyPath = input AssemblyPath -Required
Add-Type -Path $assemblyPath

function New-ChartForgeXBenchmarkChart {
    $pointCount = 72
    $observed = [ChartForgeX.Primitives.ChartPoint[]]::new($pointCount)
    $forecast = [ChartForgeX.Primitives.ChartPoint[]]::new($pointCount)
    $capacity = [ChartForgeX.Primitives.ChartPoint[]]::new($pointCount)
    for ($index = 0; $index -lt $pointCount; $index++) {
        $x = $index + 1
        $observed[$index] = [ChartForgeX.Primitives.ChartPoint]::new($x, 72 + [Math]::Sin($index * 0.13) * 19)
        $forecast[$index] = [ChartForgeX.Primitives.ChartPoint]::new($x, 68 + [Math]::Cos($index * 0.11) * 15)
        $capacity[$index] = [ChartForgeX.Primitives.ChartPoint]::new($x, 84 + [Math]::Sin($index * 0.07) * 8)
    }

    $chart = [ChartForgeX.Core.Chart]::Create()
    $chart.WithTitle('Rendering benchmark') | Out-Null
    $chart.WithSubtitle('Three 72-point series at report size') | Out-Null
    $chart.WithSize(1200, 675) | Out-Null
    $chart.WithLegend() | Out-Null
    $chart.AddLine('Observed', $observed) | Out-Null
    $chart.AddSmoothArea('Forecast', $forecast) | Out-Null
    $chart.AddBar('Capacity', $capacity) | Out-Null
    $chart
}

function New-ChartForgeXBenchmarkTopology {
    $topology = [ChartForgeX.Topology.TopologyChart]::Create()
    $topology.Id = 'rendering-benchmark-topology'
    $topology.Title = 'Rendering benchmark topology'
    $topology.LayoutMode = [ChartForgeX.Topology.TopologyLayoutMode]::ForceDirected
    $topology.Viewport.Width = 1400
    $topology.Viewport.Height = 900

    for ($index = 0; $index -lt 128; $index++) {
        $topology.Nodes.Add([ChartForgeX.Topology.TopologyNode]@{
            Id = 'node-{0:d3}' -f $index
            Label = 'Service {0:d3}' -f $index
            Kind = [ChartForgeX.Topology.TopologyNodeKind]::Service
            Status = [ChartForgeX.Topology.TopologyHealthStatus]($index % 4)
        })
    }

    for ($index = 0; $index -lt 230; $index++) {
        $source = $index % 128
        $target = ($source + 1 + (($index * 17) % 31)) % 128
        $topology.Edges.Add([ChartForgeX.Topology.TopologyEdge]@{
            Id = 'edge-{0:d3}' -f $index
            SourceNodeId = 'node-{0:d3}' -f $source
            TargetNodeId = 'node-{0:d3}' -f $target
            Kind = [ChartForgeX.Topology.TopologyEdgeKind]::Dependency
            Status = [ChartForgeX.Topology.TopologyHealthStatus]::Healthy
        })
    }

    $topology
}

$assemblyHash = (Get-FileHash -LiteralPath $assemblyPath -Algorithm SHA256).Hash

benchmark 'chartforgex-rendering' {
    policy -Warmup 1 -Iterations 5 -Order Rotated -MemoryCleanup BeforeIteration -OutlierMode None
    profile Current -Cleanup KeepOnFailure

    cases {
        case ChartSvg @{ Fixture = 'Chart'; Format = 'Svg' }
        case ChartRgba @{ Fixture = 'Chart'; Format = 'Rgba' }
        case ChartPng @{ Fixture = 'Chart'; Format = 'Png' }
        case TopologySvg @{ Fixture = 'Topology'; Format = 'Svg' }
        case TopologyRgba @{ Fixture = 'Topology'; Format = 'Rgba' }
        case TopologyPng @{ Fixture = 'Topology'; Format = 'Png' }
    }

    setup {
        param($case, $run)

        if ($case.Fixture -eq 'Chart') {
            $run.Model = New-ChartForgeXBenchmarkChart
            $run.SvgRenderer = [ChartForgeX.Svg.SvgChartRenderer]::new()
            $run.PngRenderer = [ChartForgeX.Raster.PngChartRenderer]::new()
        } else {
            $run.Model = New-ChartForgeXBenchmarkTopology
            $run.Options = [ChartForgeX.Topology.TopologyRenderOptions]::new()
            $run.Options.IncludeLegend = $false
            $run.SvgRenderer = [ChartForgeX.Topology.TopologySvgRenderer]::new()
            $run.PngRenderer = [ChartForgeX.Topology.TopologyPngRenderer]::new()
        }
    }

    engine ChartForgeX {
        operation Render {
            param($case, $run)

            if ($case.Fixture -eq 'Chart' -and $case.Format -eq 'Svg') {
                $run.OutputLength = [Text.Encoding]::UTF8.GetByteCount($run.SvgRenderer.Render($run.Model))
            } elseif ($case.Fixture -eq 'Chart' -and $case.Format -eq 'Rgba') {
                $run.OutputLength = [ChartForgeX.ChartExtensions]::ToRgbaImage($run.Model).Pixels.Length
            } elseif ($case.Fixture -eq 'Chart') {
                $run.OutputLength = $run.PngRenderer.Render($run.Model).Length
            } elseif ($case.Format -eq 'Svg') {
                $run.OutputLength = [Text.Encoding]::UTF8.GetByteCount($run.SvgRenderer.Render($run.Model, $run.Options))
            } elseif ($case.Format -eq 'Rgba') {
                $run.OutputLength = [ChartForgeX.Topology.TopologyChartExtensions]::ToRgbaImage($run.Model, $run.Options).Pixels.Length
            } else {
                $run.OutputLength = $run.PngRenderer.Render($run.Model, $run.Options).Length
            }
        }
    }

    validate {
        param($case, $run)

        if ($run.OutputLength -lt 1000) {
            throw "$($case.Fixture) $($case.Format) produced only $($run.OutputLength) bytes."
        }
    }

    metric OutputBytes {
        param($case, $run)
        $run.OutputLength
    }

    metadata AssemblySha256 $assemblyHash
    metadata ComparisonMode 'ChartForgeX release rendering only; no browser-library comparison'
    artifacts Json, Csv, Markdown
}
