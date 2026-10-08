using System;
using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using System.Linq;
using ChartForgeX.Rendering;

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
        var prepared = PreparedFamily(chart);
        Assert(FamilyGroups(prepared, "layered-radial-point").Length == 3, "Layered radial charts should retain each independent layer source.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"layered-radial-layer\"") == 3, "Layered radial charts should render one path per layer.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"layered-radial-separator\"") == 3, "Layered radial layers should render configured separators.");
        Assert(svg.Contains("data-cfx-label=\"Current\"", StringComparison.Ordinal), "Layered radial layers should expose labels.");
        var layers = prepared.Scene.Nodes.OfType<VisualSceneSlice>().Where(node => node.Role == "layered-radial-layer").ToArray();
        Assert(IsClose(layers[2].Sweep / layers[0].Sweep, 1240d / 2700), "Layered radial geometry should use full computed ratios.");
        Assert(layers[0].Outer > layers[1].Outer && layers[1].Outer - layers[1].Inner < layers[2].Outer - layers[2].Inner, "Layered radial geometry should preserve independently configured radii and widths.");
        Assert(svg.Contains(">1240 kcal</text>", StringComparison.Ordinal), "Layered radial charts should render the configured center value.");
        Assert(chart.ToPng().Length > 64, "Layered radial charts should render PNG output.");
    }
}
