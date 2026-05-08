using System;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LayeredRadialSeriesRenderIndependentArcLayers() {
        var chart = Chart.Create()
            .WithSize(560, 560)
            .WithValueFormatter(value => value.ToString("0", CultureInfo.InvariantCulture) + " kcal")
            .AddLayeredRadial("Calories left", layers => layers
                .Add("Available area", 100, color: ChartColor.FromHex("#F1F2F6"), configure: layer => layer
                    .WithGeometry(1, 0.18)
                    .WithLineCap(ChartRadialLayerCap.Butt))
                .Add("Target ring", 100, color: ChartColor.FromHex("#FFCD62"), configure: layer => layer
                    .WithGeometry(0.93, 0.035)
                    .WithLineCap(ChartRadialLayerCap.Butt))
                .Add("Current", 1240, maximum: 2700, color: ChartColor.FromHex("#FF9F4A"), configure: layer => layer
                    .WithGeometry(0.93, 0.14)
                    .WithSeparators(3, ChartColor.White, 2)));

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"layered-radial-chart\"", StringComparison.Ordinal), "Layered radial charts should expose a chart role marker.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"layered-radial-layer\"") == 3, "Layered radial charts should render one path per layer.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"layered-radial-separator\"") == 3, "Layered radial layers should render configured separators.");
        Assert(svg.Contains("data-cfx-label=\"Current\"", StringComparison.Ordinal), "Layered radial layers should expose labels.");
        Assert(svg.Contains("data-cfx-percent=\"0.459\"", StringComparison.Ordinal), "Layered radial layers should expose computed ratios.");
        Assert(svg.Contains("stroke-linecap=\"butt\"", StringComparison.Ordinal), "Layered radial layers should support butt caps.");
        Assert(svg.Contains(">1240 kcal</text>", StringComparison.Ordinal), "Layered radial charts should render the configured center value.");
        Assert(chart.ToPng().Length > 64, "Layered radial charts should render PNG output.");
    }
}
