using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AxisReversalTests {
    [Theory]
    [InlineData(ChartScaleKind.Linear)]
    [InlineData(ChartScaleKind.Logarithmic)]
    [InlineData(ChartScaleKind.Time)]
    public void CartesianAxisReversalMovesMarksGridAndTickFactsTogether(ChartScaleKind scale) {
        var xs = scale == ChartScaleKind.Time ? new[] { 45000d, 45001, 45002 } : new[] { 1d, 10, 100 };
        var ys = new[] { 1d, 10, 100 };
        var chart = Chart.Create().WithSize(800, 460).WithLegend(false).WithLineMarkers(ChartLineMarkerMode.All).WithGridStyle(style => style.WithVerticalLines())
            .AddLine("Values", xs.Select((x, index) => new ChartPoint(x, ys[index])));
        chart.Options.XAxis.WithScale(scale).WithBounds(xs[0], xs[2]);
        chart.Options.YAxis.WithScale(scale == ChartScaleKind.Logarithmic ? scale : ChartScaleKind.Linear).WithBounds(1, 100);
        foreach (var x in xs) chart.Options.XAxis.Labels.Add(new ChartAxisLabel(x, "X" + x.ToString(CultureInfo.InvariantCulture)));
        foreach (var y in ys) chart.Options.YAxis.Labels.Add(new ChartAxisLabel(y, "Y" + y.ToString(CultureInfo.InvariantCulture)));
        var normal = Prepare(chart);
        var first = Markers(normal)[0]; var last = Markers(normal)[2];
        Assert.True(first.Cx < last.Cx); Assert.True(first.Cy > last.Cy);
        chart.Options.XAxis.WithReversal(); chart.Options.YAxis.WithReversal();
        var reversed = Prepare(chart); var reversedFirst = Markers(reversed)[0]; var reversedLast = Markers(reversed)[2];
        Assert.True(reversedFirst.Cx > reversedLast.Cx); Assert.True(reversedFirst.Cy < reversedLast.Cy);
        foreach (var prepared in new[] { normal, reversed }) {
            for (var index = 0; index < 3; index++) {
                var marker = Markers(prepared)[index];
                var xTick = Assert.Single(prepared.Regions, region => region.Role == "axis-x-label" && region.Label!.EndsWith("(" + xs[index].ToString(CultureInfo.InvariantCulture) + ")", StringComparison.Ordinal));
                var yTick = Assert.Single(prepared.Regions, region => region.Role == "axis-y-label" && region.Label!.EndsWith("(" + ys[index].ToString(CultureInfo.InvariantCulture) + ")", StringComparison.Ordinal));
                Assert.Equal(marker.Cx, xTick.Bounds.Left, 10); Assert.Equal(marker.Cy, yTick.Bounds.Top, 10);
                Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "grid-x" && Math.Abs(line.Start.X - marker.Cx) < 1e-8);
                Assert.Contains(prepared.Scene.Nodes.OfType<VisualSceneLine>(), line => line.Role == "grid-y" && Math.Abs(line.Start.Y - marker.Cy) < 1e-8);
            }
        }
    }

    [Fact]
    public void ReversedLogarithmicBarsRetainPositiveDomainBaselineAndCategoryDirection() {
        var chart = Chart.Create().WithSize(800, 460).WithLegend(false).WithBarStyle(ChartBarStyle.Flat)
            .AddBar("Counts", new[] { new ChartPoint(1, 10), new ChartPoint(2, 100) });
        chart.Options.YAxis.WithScale(ChartScaleKind.Logarithmic).WithBounds(1, 100).WithReversal();
        var prepared = Prepare(chart); var bars = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "bar").ToArray();
        Assert.Equal(bars[0].Bounds.Top, bars[1].Bounds.Top, 10); Assert.True(bars[1].Bounds.Height > bars[0].Bounds.Height);
        var horizontal = Chart.Create().WithSize(800, 460).WithLegend(false).WithBarStyle(ChartBarStyle.Flat)
            .AddHorizontalBar("Counts", new[] { new ChartPoint(1, 10), new ChartPoint(2, 100) });
        horizontal.Options.XAxis.WithScale(ChartScaleKind.Logarithmic).WithBounds(1, 100).WithReversal();
        horizontal.Options.YAxis.WithReversal();
        var h = Prepare(horizontal); var marks = h.Scene.Nodes.OfType<VisualSceneRectangle>().Where(mark => mark.Role == "horizontal-bar").ToArray();
        Assert.Equal(marks[0].Bounds.Right, marks[1].Bounds.Right, 10); Assert.True(marks[0].Bounds.Top < marks[1].Bounds.Top);
        Assert.True(marks[1].Bounds.Width > marks[0].Bounds.Width);
    }

    [Fact]
    public void UnsupportedScheduleAxisReversalRejectsInsteadOfSplittingTicksFromSpans() {
        var chart = Chart.Create().AddTimelineRange("Window", 1, 3).ConfigureXAxis(axis => axis.WithReversal());
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick, "candlestick-wick")]
    [InlineData(ChartSeriesKind.Ohlc, "ohlc-stem")]
    [InlineData(ChartSeriesKind.BoxPlot, "boxplot-whisker")]
    public void ReversedFinancialObservationBoundsContainThePaintedExtrema(ChartSeriesKind kind, string role) {
        var chart = Chart.Create().WithSize(800, 460).WithLegend(false).WithYAxisBounds(0, 100);
        if (kind == ChartSeriesKind.BoxPlot) chart.AddBoxPlot("Values", new[] { new ChartBoxPlot(1, 10, 30, 45, 60, 90) });
        else if (kind == ChartSeriesKind.Candlestick) chart.AddCandlestick("Values", new[] { new ChartCandlestick(1, 30, 90, 10, 60) });
        else chart.AddOhlc("Values", new[] { new ChartCandlestick(1, 30, 90, 10, 60) });
        chart.Options.YAxis.WithReversal();
        var prepared = Prepare(chart); var line = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == role);
        var region = Assert.Single(prepared.Regions, item => item.Id == "series-0-point-0");
        Assert.Equal(Math.Min(line.Start.Y, line.End.Y), region.Bounds.Top, 9);
        Assert.Equal(Math.Max(line.Start.Y, line.End.Y), region.Bounds.Bottom, 9);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneEllipse[] Markers(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneEllipse>().Where(mark => mark.Role == "marker").ToArray();
}
