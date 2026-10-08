using System;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LegendCasingDrivesAllocationBeforeSerialization() {
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithSize(380, 240)
            .AddLine("wwwwwwww", Points(1, 2, 3))
            .AddLine("wwwwwwww", Points(2, 3, 4))
            .AddLine("wwwwwwww", Points(3, 4, 5));
        var preservedRows = PreparedFamily(chart).Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => text.Baseline).Distinct().Count();

        chart.WithLegendStyle(style => style.WithTextCase(TextCaseTransform.Uppercase));
        var expandedSvg = chart.ToSvg();

        Assert(expandedSvg.Contains(">WWWWWWWW</text>", StringComparison.Ordinal), "SVG legends should materialize casing before serialization.");
        var expandedRows = PreparedFamily(chart).Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => text.Baseline).Distinct().Count();
        Assert(expandedRows > preservedRows, "SVG legend allocation should measure case-expanded labels before deciding row wraps.");
    }
}
