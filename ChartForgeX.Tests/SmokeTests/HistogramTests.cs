using System;
using System.Globalization;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
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
        var belowHalf = BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(0.5) - 1);
        var adjacentBoundaryChart = Chart.Create().AddHistogram("Adjacent boundary values", new[] { belowHalf, 0.5 }, ChartHistogramBinLayout.FromWidth(0, 1, 0.1));
        Assert(adjacentBoundaryChart.Series[0].Points[4].Y == 1 && adjacentBoundaryChart.Series[0].Points[5].Y == 1, "Histogram binning should preserve a representable value immediately below a boundary without moving it into the following bin.");
        var subDecimalLayout = ChartHistogramBinLayout.FromCount(0, 1e-28, 2);
        var subDecimalChart = Chart.Create().AddHistogram("Sub-decimal widths", new[] { 2e-29, 7e-29 }, subDecimalLayout);
        Assert(subDecimalChart.Series[0].Points.Select(point => point.Y).SequenceEqual(new[] { 1d, 1d }), "Histogram widths below decimal precision should fall back to binary bin assignment without division by zero.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartHistogramBinLayout.FromWidth(0, 10, 0), "Histogram layouts should reject zero bin widths.");
        AssertThrows<ArgumentOutOfRangeException>(() => Chart.Create().AddHistogram("Outside", new[] { 11d }, layout), "Shared histogram layouts should reject values outside their bounds.");
    }

    private static void HistogramBarsGroupWithRegularBars() {
        var layout = ChartHistogramBinLayout.FromWidth(0, 2, 1);
        var grouped = Chart.Create()
            .WithSize(640, 360)
            .AddHistogram("Observed", new[] { 0.25, 0.75, 1.25, 1.75 }, layout)
            .AddBar("Target", new[] { new ChartPoint(0.5, 3), new ChartPoint(1.5, 2) });
        var bars = SvgDocument.Parse(grouped.ToSvg()).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        var histogramLeft = double.Parse(bars[0].GetAttribute("x")!, CultureInfo.InvariantCulture);
        var histogramWidth = double.Parse(bars[0].GetAttribute("width")!, CultureInfo.InvariantCulture);
        var regularLeft = double.Parse(bars[2].GetAttribute("x")!, CultureInfo.InvariantCulture);
        Assert(histogramLeft + histogramWidth <= regularLeft + 0.001, "Grouped histogram and regular bars at the same numeric coordinate should receive distinct slots.");
        Assert(grouped.ToPng().Length > 64, "Mixed histogram and regular bars should preserve PNG rendering parity.");

        var decimalLayout = ChartHistogramBinLayout.FromWidth(0.1, 0.3, 0.1);
        var stacked = Chart.Create()
            .WithSize(640, 360)
            .WithStackedBars()
            .WithStackTotals()
            .AddHistogram("Observed", new[] { 0.11, 0.12, 0.25 }, decimalLayout)
            .AddBar("Target", new[] { new ChartPoint(0.15, 3), new ChartPoint(0.25, 2) });
        var stackedSvg = stacked.ToSvg();
        var stackedBars = SvgDocument.Parse(stackedSvg).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        Assert(stackedBars[2].GetAttribute("data-cfx-base") == "2", "Stacked regular bars should start at the matching histogram count.");
        Assert(ChartRange.FromChart(stacked).MaxY >= 5, "Stacked range calculation should include decimal-equivalent histogram and regular bar coordinates together.");
        Assert(CountOccurrences(stackedSvg, "data-cfx-role=\"stack-total-label\"") == 2, "Stack totals should emit one label per decimal-equivalent histogram and regular bar coordinate.");
        Assert(stacked.ToPng().Length > 64, "Decimal-equivalent mixed stack totals should preserve PNG rendering parity.");

        var narrowLayout = ChartHistogramBinLayout.FromWidth(0, 0.00000002, 0.00000001);
        var narrow = Chart.Create()
            .WithStackedBars()
            .AddHistogram("Narrow bins", new[] { 0.000000001, 0.000000002, 0.000000015 }, narrowLayout)
            .AddBar("Narrow target", new[] { new ChartPoint(0.000000015, 2) });
        var narrowBars = SvgDocument.Parse(narrow.ToSvg()).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        Assert(narrowBars[2].GetAttribute("data-cfx-base") == "1", "Stacked bars should not match an earlier adjacent histogram bin merely because the bin width is below one millionth.");

        var center = 1.5;
        var centerBits = BitConverter.DoubleToInt64Bits(center);
        var leftOfCenter = BitConverter.Int64BitsToDouble(centerBits - 3);
        var rightOfCenter = BitConverter.Int64BitsToDouble(centerBits + 3);
        var transitive = Chart.Create()
            .WithStackedBars()
            .AddHistogram("Center", new[] { center }, ChartHistogramBinLayout.FromWidth(1, 2, 1))
            .AddBar("Left", new[] { new ChartPoint(leftOfCenter, 2) })
            .AddBar("Right", new[] { new ChartPoint(rightOfCenter, 3) });
        var transitiveBars = SvgDocument.Parse(transitive.ToSvg()).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        Assert(transitiveBars[2].GetAttribute("data-cfx-base") == "3", "Stacked bars canonicalized to one histogram center should include every earlier equivalent coordinate.");
        Assert(transitive.ToPng().Length > 64, "Canonical mixed histogram stack coordinates should preserve PNG rendering parity.");

        var countLayout = ChartHistogramBinLayout.FromCount(0, 0.3, 3);
        var widthLayout = ChartHistogramBinLayout.FromWidth(0, 0.3, 0.1);
        var equivalentLayouts = Chart.Create()
            .WithStackedBars()
            .WithStackTotals()
            .AddHistogram("Count layout", new[] { 0.01 }, countLayout)
            .AddHistogram("Width layout", new[] { 0.01 }, widthLayout);
        var equivalentSvg = equivalentLayouts.ToSvg();
        var equivalentBars = SvgDocument.Parse(equivalentSvg).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        Assert(equivalentBars[3].GetAttribute("data-cfx-base") == "1", "Equivalent independently-created histogram layouts should stack their matching bins.");
        Assert(ChartRange.FromChart(equivalentLayouts).MaxY >= 2, "Equivalent histogram centers should share one range stack key.");
        Assert(CountOccurrences(equivalentSvg, "data-cfx-role=\"stack-total-label\"") == 1, "Equivalent histogram centers should emit one combined stack-total label.");
        Assert(equivalentLayouts.ToPng().Length > 64, "Equivalent histogram layout aggregation should preserve PNG rendering parity.");

        var chainCenter = 1.5;
        var chainBits = BitConverter.DoubleToInt64Bits(chainCenter);
        var chainMiddle = BitConverter.Int64BitsToDouble(chainBits + 3);
        var chainEnd = BitConverter.Int64BitsToDouble(chainBits + 6);
        var chainedLayouts = Chart.Create()
            .WithStackedBars()
            .WithStackTotals()
            .AddHistogram("Chain start", new[] { chainCenter }, ChartHistogramBinLayout.FromCount(0, chainCenter * 2, 1))
            .AddHistogram("Chain middle", new[] { chainMiddle }, ChartHistogramBinLayout.FromCount(0, chainMiddle * 2, 1))
            .AddHistogram("Chain end", new[] { chainEnd }, ChartHistogramBinLayout.FromCount(0, chainEnd * 2, 1));
        var chainedSvg = chainedLayouts.ToSvg();
        var chainedBars = SvgDocument.Parse(chainedSvg).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        Assert(chainedBars[2].GetAttribute("data-cfx-base") == "2", "Transitive coordinate chains should share one stable stacked base.");
        Assert(ChartRange.FromChart(chainedLayouts).MaxY >= 3, "Transitive coordinate chains should share one range stack key.");
        Assert(CountOccurrences(chainedSvg, "data-cfx-role=\"stack-total-label\"") == 1, "Transitive coordinate chains should emit one combined stack-total label.");
        Assert(chainedLayouts.ToPng().Length > 64, "Transitive coordinate chain aggregation should preserve PNG rendering parity.");

        var constantLayout = ChartHistogramBinLayout.FromCount(5, 5, 1);
        var constant = Chart.Create()
            .WithSize(640, 360)
            .AddHistogram("First constant", new[] { 5d, 5d }, constantLayout)
            .AddHistogram("Second constant", new[] { 5d }, constantLayout)
            .AddBar("Constant target", new[] { new ChartPoint(5, 3) });
        var constantBars = SvgDocument.Parse(constant.ToSvg()).Root.FindByTag("rect")
            .Where(element => element.GetAttribute("data-cfx-role") == "bar")
            .ToArray();
        var firstConstantLeft = double.Parse(constantBars[0].GetAttribute("x")!, CultureInfo.InvariantCulture);
        var firstConstantWidth = double.Parse(constantBars[0].GetAttribute("width")!, CultureInfo.InvariantCulture);
        var secondConstantLeft = double.Parse(constantBars[1].GetAttribute("x")!, CultureInfo.InvariantCulture);
        var secondConstantWidth = double.Parse(constantBars[1].GetAttribute("width")!, CultureInfo.InvariantCulture);
        var regularConstantLeft = double.Parse(constantBars[2].GetAttribute("x")!, CultureInfo.InvariantCulture);
        Assert(firstConstantLeft + firstConstantWidth <= secondConstantLeft + 0.001 && secondConstantLeft + secondConstantWidth <= regularConstantLeft + 0.001,
            "Grouped constant-value histograms and regular bars should receive distinct slots.");
        Assert(constant.ToPng().Length > 64, "Grouped constant-value histogram slots should preserve PNG rendering parity.");
    }

    private static void SeriesKindCapabilitiesExposeExclusiveRenderingOwnership() {
        Assert(ChartSeriesKindCapabilities.IsExclusive(ChartSeriesKind.Heatmap), "Shared kind capabilities should classify exclusive rendering surfaces.");
        Assert(!ChartSeriesKindCapabilities.IsExclusive(ChartSeriesKind.Line), "Shared kind capabilities should keep cartesian line series non-exclusive.");
        AssertThrows<ArgumentOutOfRangeException>(() => ChartSeriesKindCapabilities.IsExclusive((ChartSeriesKind)999), "Shared kind capabilities should reject undefined series kinds.");
    }
}
