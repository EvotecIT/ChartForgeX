using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void LegendsAndDataLabelsKeepGuardGaps() {
        var topLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithLegendPosition(ChartLegendPosition.Top)
            .AddLine("First long legend item", Points(12, 18, 26))
            .AddLine("Second long legend item", Points(8, 14, 22))
            .AddLine("Third long legend item", Points(4, 9, 18));
        var topPrepared = PreparedFamily(topLegend);
        var topEntries = topPrepared.Regions.Where(region => region.Role == "legend").ToArray();
        var topPlot = topPrepared.Scene.Nodes.OfType<VisualSceneLine>().Single(line => line.Role == "axis-y");
        Assert(topEntries.Length == 3 && topPlot.Start.Y - topEntries.Max(entry => entry.Bounds.Bottom) >= 12,
            "Top legends should retain all series and reserve a visible gap before the actual plot guard.");
        Assert(topPrepared.ToPng().Length > 64, "Top legend guard gaps should render PNG output.");

        var bottomLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithLegendPosition(ChartLegendPosition.Bottom)
            .AddLine("First long legend item", Points(12, 18, 26))
            .AddLine("Second long legend item", Points(8, 14, 22))
            .AddLine("Third long legend item", Points(4, 9, 18));
        var bottomPrepared = PreparedFamily(bottomLegend);
        var bottomEntries = bottomPrepared.Regions.Where(region => region.Role == "legend").ToArray();
        var bottomPlot = bottomPrepared.Scene.Nodes.OfType<VisualSceneLine>().Single(line => line.Role == "axis-x");
        Assert(bottomEntries.Length == 3 && bottomEntries.All(entry => entry.Bounds.Bottom <= 320 - bottomLegend.Options.Padding.Bottom)
            && bottomEntries.Min(entry => entry.Bounds.Top) - bottomPlot.Start.Y >= 12,
            "Bottom legends should retain all series inside the authored padding and clear the actual plot guard.");
        Assert(bottomPrepared.ToPng().Length > 64, "Bottom legend guard gaps should render PNG output.");

        var radialLegend = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(520, 320)
            .WithLegendPosition(ChartLegendPosition.Bottom)
            .WithRadialBarCenterLabel(false)
            .AddRadialBar("Readiness", Points(82, 61, 44));
        var radialPrepared = PreparedFamily(radialLegend);
        var radialEntries = radialPrepared.Regions.Where(region => region.Role == "legend").ToArray();
        Assert(radialEntries.Length > 0 && radialEntries.All(entry => entry.Bounds.Bottom <= 320 - radialLegend.Options.Padding.Bottom),
            "Bottom radial-bar legends should stay inside the authored padding instead of sitting on the canvas edge.");
        Assert(radialPrepared.ToPng().Length > 64, "Bottom radial-bar legend guard gaps should render PNG output.");

        var edgeLabels = Chart.Create().WithTheme(ChartForgeX.Themes.ChartTheme.Light())
            .WithSize(420, 280)
            .WithDataLabels()
            .WithYAxisBounds(0, 100)
            .AddBar("Edge", Points(100));
        var edgePrepared = PreparedFamily(edgeLabels);
        var plot = edgePrepared.Scene.Nodes.OfType<VisualSceneLine>().Single(line => line.Role == "axis-y");
        var labels = FamilyLabels(edgePrepared, "data-label");
        Assert(labels.Length == 1 && labels.All(label => label.Baseline - label.Text.Ascent - plot.Start.Y >= 12
            && plot.End.Y - (label.Baseline - label.Text.Ascent + label.Text.Metrics.Height) >= 12),
            "Complete data-label extents should stay away from the actual plot guard lines.");
        Assert(edgePrepared.ToPng().Length > 64, "Data-label guard gaps should render PNG output.");
    }
}
