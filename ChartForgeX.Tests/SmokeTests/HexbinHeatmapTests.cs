using ChartForgeX;
using ChartForgeX.Core;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HexbinHeatmapRowsRenderHoneycombCells() {
        var chart = Chart.Create()
            .WithSize(560, 360)
            .WithXAxis("Day")
            .WithYAxis("Time")
            .WithXLabels("Mon", "Tue", "Wed")
            .WithHeatmapScale(ChartHeatmapScale.Semantic)
            .WithValueFormatter(value => value.ToString("0", System.Globalization.CultureInfo.InvariantCulture) + "%")
            .AddHexbinHeatmapRow("09:00", Points(68, 82, 91))
            .AddHexbinHeatmapRow("11:00", Points(74, 88, 96));

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-role=\"hexbin-heatmap\"", System.StringComparison.Ordinal), "Hexbin heatmaps should expose a role marker.");
        Assert(svg.Contains("data-cfx-row-count=\"2\"", System.StringComparison.Ordinal), "Hexbin heatmaps should expose row-count metadata.");
        Assert(svg.Contains("data-cfx-column-count=\"3\"", System.StringComparison.Ordinal), "Hexbin heatmaps should expose column-count metadata.");
        Assert(svg.Contains("data-cfx-min=\"68\"", System.StringComparison.Ordinal) && svg.Contains("data-cfx-max=\"96\"", System.StringComparison.Ordinal), "Hexbin heatmaps should expose value-range metadata.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"hexbin-cell\"") == 6, "Hexbin heatmaps should render one hexagon per row/column value.");
        Assert(svg.Contains("data-cfx-role=\"hexbin-heatmap-row-label\"", System.StringComparison.Ordinal), "Hexbin heatmaps should render row labels.");
        Assert(svg.Contains("data-cfx-role=\"hexbin-heatmap-column-label\"", System.StringComparison.Ordinal), "Hexbin heatmaps should render column labels.");
        Assert(svg.Contains("<title>11:00, Wed: 96%, positive</title>", System.StringComparison.Ordinal), "Hexbin cells should expose native SVG hover titles.");
        Assert(chart.ToPng().Length > 64, "Hexbin heatmaps should render PNG output.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRow("Empty", System.Array.Empty<ChartForgeX.Primitives.ChartPoint>()), "Hexbin heatmaps should reject empty row inputs.");
    }

    private static void HexbinHeatmapRowsAcceptOneBasedValues() {
        var chart = Chart.Create()
            .WithSize(360, 240)
            .WithXLabels("Mon", "Tue", "Wed")
            .AddHexbinHeatmapRow("09:00", new[] { 68d, 82, 91 })
            .AddHexbinHeatmapRow("11:00", new[] { 74d, 88, 96 });

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"hexbin-cell\"") == 6, "Value overloads should map each value to a one-based hexbin column.");
        Assert(svg.Contains("<title>09:00, Tue: 82</title>", System.StringComparison.Ordinal), "Value overloads should preserve x-axis label lookup.");
        Assert(chart.ToPng().Length > 64, "Value overloads should render PNG output.");
        var labeled = Chart.Create()
            .WithSize(560, 360)
            .WithDataLabels()
            .AddHexbinHeatmapRow("09:00", new[] { 68d, 82, 91 });
        labeled.Series[0].WithPointLabel(1, "Peak");
        Assert(labeled.ToSvg().Contains(">Peak</text>", System.StringComparison.Ordinal), "Hexbin SVG data labels should honor point-level overrides.");
        Assert(labeled.ToPng().Length > 64, "Hexbin PNG data labels should honor point-level overrides without failing render.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRow("Empty", System.Array.Empty<double>()), "Hexbin value overloads should reject empty rows.");
        AssertThrows<System.ArgumentOutOfRangeException>(() => Chart.Create().AddHexbinHeatmapRow("Bad", new[] { 1d, double.NaN }), "Hexbin value overloads should reject non-finite values.");
    }

    private static void HexbinHeatmapRowsAcceptMatrixValues() {
        var chart = Chart.Create()
            .WithSize(360, 240)
            .WithXLabels("Mon", "Tue", "Wed")
            .AddHexbinHeatmapRows(new[] {
                ChartHeatmapRow.Create("09:00", 68, 82, 91),
                ChartHeatmapRow.Create("11:00", 74, 88, 96)
            });

        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"hexbin-cell\"") == 6, "Matrix overloads should add every row and value.");
        Assert(svg.Contains("data-cfx-row-count=\"2\"", System.StringComparison.Ordinal), "Matrix overloads should preserve row metadata.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRows(System.Array.Empty<ChartHeatmapRow>()), "Hexbin row overloads should require at least one row.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRows(new ChartHeatmapRow?[] { null! }!), "Hexbin row overloads should reject null rows.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRows(new[] { "Only" }, new[] { new[] { 1d }, new[] { 2d } }), "Matrix overloads should require matching row name and value counts.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRows(System.Array.Empty<string>(), System.Array.Empty<double[]>()), "Matrix overloads should require at least one row.");
    }

    private static void HexbinHeatmapRowsCanMaskHoneycombCells() {
        var chart = Chart.Create()
            .WithSize(360, 260)
            .WithXLabels("Mon", "Tue", "Wed", "Thu", "Fri")
            .AddHexbinHeatmapRows(new[] {
                ChartHeatmapRow.CreateMasked("Morning", null, 62, 74, 68, null),
                ChartHeatmapRow.CreateMasked("Noon", null, 83, 96, 88, null),
                ChartHeatmapRow.CreateMasked("Evening", null, null, 71, 64, null)
            });

        var svg = chart.ToSvg();
        Assert(svg.Contains("data-cfx-column-count=\"5\"", System.StringComparison.Ordinal), "Masked cells should preserve the full column span for honeycomb layout.");
        Assert(CountOccurrences(svg, "data-cfx-role=\"hexbin-cell\"") == 8, "Masked cells should not render as low-value hexagons.");
        Assert(!svg.Contains("<title>Morning, Mon:", System.StringComparison.Ordinal), "Masked leading cells should be absent from SVG titles.");
        Assert(svg.Contains("<title>Noon, Wed: 96</title>", System.StringComparison.Ordinal), "Visible masked-row cells should preserve values and labels.");
        Assert(chart.ToPng().Length > 64, "Masked hexbin heatmaps should render PNG output.");
        AssertThrows<System.ArgumentException>(() => Chart.Create().AddHexbinHeatmapRows(new[] { ChartHeatmapRow.CreateMasked("Empty", null, null) }), "Masked rows should require at least one visible cell.");
    }
}
