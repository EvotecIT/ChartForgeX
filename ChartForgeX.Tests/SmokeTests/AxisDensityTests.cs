using System.Globalization;
using ChartForgeX;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void FormattedNumericXAxisTicksThinWhenCrowded() {
        string Formatter(double value) => "Checkpoint " + value.ToString("00", CultureInfo.InvariantCulture);

        var auto = Chart.Create()
            .WithSize(360, 260)
            .WithXAxisBounds(0, 12)
            .WithTickCount(13)
            .WithXAxisValueFormatter(Formatter)
            .AddLine("Values", new[] {
                new ChartPoint(0, 10),
                new ChartPoint(6, 20),
                new ChartPoint(12, 15)
            })
            .ToSvg();
        Assert(auto.Contains(">Checkpoint 00</text>", System.StringComparison.Ordinal), "Generated x-axis tick thinning should preserve the first formatted tick.");
        Assert(auto.Contains(">Checkpoint 12</text>", System.StringComparison.Ordinal), "Generated x-axis tick thinning should preserve the last formatted tick.");
        Assert(!auto.Contains(">Checkpoint 01</text>", System.StringComparison.Ordinal), "Generated x-axis tick thinning should omit crowded intermediate formatted ticks.");
        Assert(CountOccurrences(auto, ">Checkpoint ") < 13, "Generated x-axis tick thinning should reduce crowded formatted tick count.");

        var all = Chart.Create()
            .WithSize(360, 260)
            .WithXAxisBounds(0, 12)
            .WithTickCount(13)
            .WithXAxisLabelDensity(ChartLabelDensity.All)
            .WithXAxisValueFormatter(Formatter)
            .AddLine("Values", new[] {
                new ChartPoint(0, 10),
                new ChartPoint(6, 20),
                new ChartPoint(12, 15)
            })
            .ToSvg();
        Assert(all.Contains(">Checkpoint 01</text>", System.StringComparison.Ordinal), "All label density should preserve every generated formatted x-axis tick.");
        Assert(Chart.Create().WithSize(360, 260).WithXAxisBounds(0, 12).WithTickCount(13).WithXAxisValueFormatter(Formatter).AddLine("Values", new[] { new ChartPoint(0, 10), new ChartPoint(6, 20), new ChartPoint(12, 15) }).ToPng().Length > 64, "PNG should render formatted generated ticks with density-aware layout.");
    }

    private static void HorizontalCategoryLabelsWrapInSvg() {
        var svg = Chart.Create()
            .WithSize(360, 260)
            .WithXLabels("Mail auth enforcement", "DNSSEC")
            .AddHorizontalBar("Coverage", Points(82, 74))
            .ToSvg();
        Assert(svg.Contains("data-cfx-role=\"horizontal-category-label\" data-cfx-line=\"0\"", System.StringComparison.Ordinal), "Horizontal category labels should expose stable SVG role markers.");
        Assert(svg.Contains(">Mail auth</text>", System.StringComparison.Ordinal), "Long horizontal category labels should wrap onto a first readable SVG line.");
        Assert(svg.Contains(">enforcement</text>", System.StringComparison.Ordinal), "Long horizontal category labels should wrap onto a second readable SVG line.");
        Assert(svg.Contains(">DNSSEC</text>", System.StringComparison.Ordinal), "Short horizontal category labels should remain a single SVG line.");
    }

    private static void NumericYAxisTicksThinWhenCrowded() {
        var automatic = Chart.Create()
            .WithSize(360, 150)
            .WithYAxisBounds(0, 100)
            .WithTickCount(11)
            .AddLine("Values", Points(10, 50, 90));
        var automaticSvg = automatic.ToSvg();
        var automaticLabels = CountOccurrences(automaticSvg, "data-cfx-role=\"y-axis-label\"");
        Assert(automaticLabels >= 2 && automaticLabels < 11, "Automatic y-axis density should retain the endpoints while removing vertically crowded labels.");
        Assert(automatic.ToPng().Length > 64, "PNG should use the same y-axis density policy.");

        var allSvg = Chart.Create()
            .WithSize(360, 150)
            .WithYAxisBounds(0, 100)
            .WithTickCount(11)
            .WithYAxisLabelDensity(ChartLabelDensity.All)
            .AddLine("Values", Points(10, 50, 90))
            .ToSvg();
        Assert(CountOccurrences(allSvg, "data-cfx-role=\"y-axis-label\"") == 11, "All y-axis density should preserve every generated label.");
    }
}
