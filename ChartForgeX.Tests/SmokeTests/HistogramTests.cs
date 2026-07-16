using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Svg;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void HistogramValuesRenderAsBinnedBars() {
        var chart = Chart.Create()
            .WithSize(640, 360)
            .WithDataLabels()
            .AddHistogram("Latency samples", new[] { 1d, 2d, 2d, 3d, 5d }, 2, ChartColor.FromRgb(37, 99, 235));
        var svg = chart.ToSvg();
        Assert(CountOccurrences(svg, "data-cfx-role=\"bar\"") == 2, "Histogram values should render one bar per requested bin.");
        Assert(svg.Contains(">1-3</text>", StringComparison.Ordinal), "Histogram bins should render range labels.");
        Assert(svg.Contains(">3-5</text>", StringComparison.Ordinal), "Histogram bins should render range labels.");
        Assert(svg.Contains(">3</text>", StringComparison.Ordinal), "Histogram data labels should render bin counts.");
        Assert(chart.ToPng().Length > 64, "Histogram charts should render PNG output.");
    }

    private static void HistogramBinWidthPreservesRequestedIntervals() {
        var layout = ChartHistogramBinLayout.FromWidth(0, 10, 3);
        var chart = Chart.Create()
            .WithSize(640, 360)
            .AddHistogram("Requested width", new[] { 0d, 1d, 3d, 5d, 6d, 9d, 10d }, layout);

        Assert(layout.Count == 4 && Math.Abs(layout.Width - 3) < 0.000001, "Histogram layouts should preserve the requested bin width.");
        Assert(chart.Series[0].Points.Select(point => point.Y).SequenceEqual(new[] { 2d, 2d, 1d, 2d }), "Histogram values should use the requested width when assigning bins.");
        Assert(chart.Options.XAxisLabels.Select(label => label.Text).SequenceEqual(new[] { "0-3", "3-6", "6-9", "9-10" }), "Histogram labels should retain full-width bins and a final remainder bin.");
        var bars = SvgDocument.Parse(chart.ToSvg()).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        var firstWidth = double.Parse(bars[0].GetAttribute("width")!, CultureInfo.InvariantCulture);
        var finalWidth = double.Parse(bars[3].GetAttribute("width")!, CultureInfo.InvariantCulture);
        Assert(Math.Abs(finalWidth / firstWidth - 1.0 / 3.0) < 0.01, "Histogram geometry should render a shorter remainder bin in proportion to its numeric width.");
        var decimalLayout = ChartHistogramBinLayout.FromWidth(0, 0.07, 0.005);
        var remainderLayout = ChartHistogramBinLayout.FromWidth(0, 0.071, 0.005);
        var tinyRemainderLayout = ChartHistogramBinLayout.FromWidth(0, 1.0000000000005, 1);
        Assert(decimalLayout.Count == 14, "Histogram layouts should not create a phantom bin when floating-point division is negligibly above an integer.");
        Assert(remainderLayout.Count == 15, "Histogram layouts should retain a final bin when the range has a real remainder.");
        Assert(tinyRemainderLayout.Count == 2, "Histogram layouts should retain representable remainder bins beyond floating-point rounding noise.");
        var boundaryLayout = ChartHistogramBinLayout.FromWidth(0.1, 0.5, 0.1);
        var boundaryChart = Chart.Create().AddHistogram("Decimal boundaries", new[] { 0.1, 0.2, 0.3, 0.4, 0.5 }, boundaryLayout);
        Assert(boundaryChart.Series[0].Points.Select(point => point.Y).SequenceEqual(new[] { 1d, 1d, 1d, 2d }), "Decimal values on exact bin boundaries should be assigned to the following bin.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartHistogramBinLayout.FromWidth(0, 10, 0), "Histogram layouts should reject zero bin widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Outside", new[] { 11d }, layout), "Shared histogram layouts should reject values outside their bounds.");
    }

    private static void SeriesKindCapabilitiesExposeExclusiveRenderingOwnership() {
        Assert(ChartSeriesKindCapabilities.IsExclusive(ChartSeriesKind.Heatmap), "Shared kind capabilities should classify exclusive rendering surfaces.");
        Assert(!ChartSeriesKindCapabilities.IsExclusive(ChartSeriesKind.Line), "Shared kind capabilities should keep cartesian line series non-exclusive.");
    }
}
