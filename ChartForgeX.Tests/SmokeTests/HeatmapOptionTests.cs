using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HeatmapHonorsAxesVisibility() {
        var compact = HeatmapSample()
            .WithAxes(false)
            .WithLegend(false)
            .ToSvg();

        Assert(compact.Contains("data-cfx-role=\"heatmap-cell\"", System.StringComparison.Ordinal), "Compact heatmaps should still render cells.");
        Assert(!compact.Contains("data-cfx-role=\"heatmap-row-label\"", System.StringComparison.Ordinal), "Heatmaps should hide row labels when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"heatmap-column-label\"", System.StringComparison.Ordinal), "Heatmaps should hide column labels when axes are disabled.");
        Assert(!compact.Contains("data-cfx-role=\"heatmap-x-axis-title\"", System.StringComparison.Ordinal), "Heatmaps should hide x-axis titles when axes are disabled.");
        Assert(compact.Contains("data-cfx-role=\"heatmap-scale-label\"", System.StringComparison.Ordinal), "Heatmaps should keep scale labels as heatmap context.");

        var axesOnly = HeatmapSample().WithLegend(false).ToSvg();
        Assert(axesOnly.Contains("data-cfx-role=\"heatmap-row-label\"", System.StringComparison.Ordinal), "Heatmaps should keep axes when only legends are disabled.");
        Assert(axesOnly.Contains("data-cfx-role=\"heatmap-scale-label\"", System.StringComparison.Ordinal), "Heatmap scales should remain visible when series legends are disabled.");
        var withoutScale = HeatmapSample().WithHeatmapScaleLegend(false).ToSvg();
        Assert(!withoutScale.Contains("data-cfx-role=\"heatmap-scale-label\"", System.StringComparison.Ordinal), "Heatmap scale legends should be optional.");
        Assert(HeatmapSample().WithHeatmapScaleLegend(false).ToPng().Length > 64, "Heatmaps without scale legends should render valid PNG output.");
        var withoutColumns = HeatmapSample().WithHeatmapColumnLabels(false).ToSvg();
        Assert(withoutColumns.Contains("data-cfx-role=\"heatmap-row-label\"", System.StringComparison.Ordinal), "Heatmap row labels should remain visible when column labels are hidden.");
        Assert(!withoutColumns.Contains("data-cfx-role=\"heatmap-column-label\"", System.StringComparison.Ordinal), "Heatmap column labels should be optional.");
        Assert(HeatmapSample().WithHeatmapColumnLabels(false).ToPng().Length > 64, "Heatmaps without column labels should render valid PNG output.");
        Assert(HeatmapSample().WithAxes(false).WithLegend(false).ToPng().Length > 64, "Compact heatmap options should render valid PNG output.");
    }

    private static void HeatmapRowsCanMaskCells() {
        var chart = Chart.Create()
            .WithSize(420, 280)
            .WithXLabels("Mon", "Tue", "Wed", "Thu")
            .AddHeatmapRows(new[] {
                ChartHeatmapRow.CreateMasked("Morning", null, 52, 61, null),
                ChartHeatmapRow.CreateMasked("Afternoon", null, 88, null, 57)
            });

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"heatmap-cell\"") == 4, "Masked heatmap rows should render only visible cells.");
        Assert(svg.Contains("data-cfx-column-count=\"4\"", System.StringComparison.Ordinal), "Masked heatmap rows should preserve the full column count.");
        Assert(!svg.Contains("<title>Morning, Mon:", System.StringComparison.Ordinal), "Masked heatmap rows should skip null cells.");
        Assert(svg.Contains("<title>Morning, Tue: 52</title>", System.StringComparison.Ordinal), "Masked heatmap rows should preserve later column label lookup.");
        Assert(svg.Contains("<title>Afternoon, Thu: 57</title>", System.StringComparison.Ordinal), "Masked heatmap rows should keep trailing values aligned after fully masked columns.");
        Assert(chart.ToPng().Length > 64, "Masked heatmap rows should render PNG output.");
    }

    private static void HeatmapRowsAcceptMatrixValues() {
        var chart = Chart.Create()
            .WithSize(420, 280)
            .WithXLabels("SPF", "DMARC", "DNSSEC")
            .AddHeatmapRows(new[] {
                ChartHeatmapRow.Create("Primary", 80, 70, 64),
                ChartHeatmapRow.Create("Acquired", 45, 60, 74)
            });

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"heatmap-cell\"") == 6, "Heatmap matrix overloads should add every row and value.");
        Assert(svg.Contains("data-cfx-row-count=\"2\"", System.StringComparison.Ordinal), "Heatmaps should expose row-count metadata.");
        Assert(svg.Contains("data-cfx-column-count=\"3\"", System.StringComparison.Ordinal), "Heatmaps should expose column-count metadata.");
        Assert(svg.Contains("data-cfx-min=\"45\"", System.StringComparison.Ordinal) && svg.Contains("data-cfx-max=\"80\"", System.StringComparison.Ordinal), "Heatmaps should expose value-range metadata.");
        Assert(svg.Contains("<title>Primary, SPF: 80</title>", System.StringComparison.Ordinal), "Heatmap matrix overloads should preserve one-based x-axis label lookup.");
        Assert(chart.ToPng().Length > 64, "Heatmap matrix overloads should render PNG output.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHeatmapRows(System.Array.Empty<ChartHeatmapRow>()), "Heatmap row overloads should require at least one row.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHeatmapRows(new ChartHeatmapRow?[] { null! }!), "Heatmap row overloads should reject null rows.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHeatmapRows(new[] { "Only" }, new[] { new[] { 1d }, new[] { 2d } }), "Heatmap matrix overloads should require matching row name and value counts.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHeatmapRows(System.Array.Empty<string>(), System.Array.Empty<double[]>()), "Heatmap matrix overloads should require at least one row.");
    }

    private static Chart HeatmapSample() => Chart.Create()
        .WithSize(520, 320)
        .WithXAxis("Control")
        .WithYAxis("Group")
        .WithXLabels("SPF", "DMARC")
        .AddHeatmapRow("Primary", Points(80, 70))
        .AddHeatmapRow("Acquired", Points(45, 60));
}
