using System;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using System.Linq;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LegendCasingDrivesAllocationBeforeSerialization() {
        var font = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "Fonts", "Carlito", "Carlito-Regular.ttf");
        Assert(System.IO.File.Exists(font), "The existing Carlito fixture must be available.");
        var chart = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light()).WithPngFont(font).WithSize(800, 280)
            .AddLine("wwwwwwww", Points(1, 2, 3))
            .AddLine("wwwwwwww", Points(2, 3, 4))
            .AddLine("wwwwwwww", Points(3, 4, 5));
        var preservedSpan = LegendSpan(PreparedFamily(chart));
        chart.WithLegendStyle(style => style.WithTextCase(TextCaseTransform.Uppercase));
        var expandedSpan = LegendSpan(PreparedFamily(chart));
        Assert(expandedSpan > preservedSpan + 2, "The fixture must expose measurable case expansion.");
        var padding = VisualExportRequest.ForChart(chart).Context.Layout.PaddingEdges;
        chart.WithSize((int)Math.Ceiling((preservedSpan + expandedSpan) / 2 + padding.Left + padding.Right), 280)
            .WithLegendStyle(style => style.WithTextCase(TextCaseTransform.None));
        var preservedRows = PreparedFamily(chart).Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => text.Baseline).Distinct().Count();

        chart.WithLegendStyle(style => style.WithTextCase(TextCaseTransform.Uppercase));
        var expandedSvg = chart.ToSvg();

        Assert(expandedSvg.Contains(">WWWWWWWW</text>", StringComparison.Ordinal), "SVG legends should materialize casing before serialization.");
        var expandedRows = PreparedFamily(chart).Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "legend-label")
            .Select(text => text.Baseline).Distinct().Count();
        Assert(expandedRows > preservedRows, "SVG legend allocation should measure case-expanded labels before deciding row wraps.");

        static double LegendSpan(PreparedVisual prepared) {
            var entries = prepared.Regions.Where(region => region.Role == "legend").ToArray();
            Assert(entries.Length == 3 && entries.All(entry => entry.Bounds.Width > 0), "All three measured entries must fit the roomy fixture.");
            return entries.Max(entry => entry.Bounds.Right) - entries.Min(entry => entry.Bounds.Left);
        }
    }
}
