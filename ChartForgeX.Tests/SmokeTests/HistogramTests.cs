using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

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
        AssertThrows<ArgumentOutOfRangeException>(() => ChartHistogramBinLayout.FromWidth(0, 10, 0), "Histogram layouts should reject zero bin widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Outside", new[] { 11d }, layout), "Shared histogram layouts should reject values outside their bounds.");
    }
}
