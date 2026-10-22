using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NativeTinyViewportResourceTests {
    [Theory]
    [InlineData(ChartSeriesKind.RangeArea, "x", false, false)]
    [InlineData(ChartSeriesKind.RangeArea, "y", true, false)]
    [InlineData(ChartSeriesKind.RangeArea, "xy", true, true)]
    [InlineData(ChartSeriesKind.RangeBand, "x", true, true)]
    [InlineData(ChartSeriesKind.RangeBand, "y", false, false)]
    [InlineData(ChartSeriesKind.RangeBand, "xy", false, false)]
    [InlineData(ChartSeriesKind.Area, "x", true, false)]
    [InlineData(ChartSeriesKind.Area, "y", false, true)]
    [InlineData(ChartSeriesKind.Area, "xy", true, false)]
    [InlineData(ChartSeriesKind.Line, "x", false, false)]
    public void OneUlpSourceWindowsPrepareAndExportWithoutExpandingTheirBounds(ChartSeriesKind kind, string axes, bool reversed, bool adaptive) {
        ChartPathFlatteningBoundsTests.RequireBoundedCurvePlan();
        var id = new ChartSeriesId("source");
        var series = kind is ChartSeriesKind.Area or ChartSeriesKind.Line
            ? new ChartSeries(id, "Source", kind, new[] { new ChartPoint(0, 2), new ChartPoint(1, 6), new ChartPoint(2, 3) })
            : kind == ChartSeriesKind.RangeBand
                ? ChartSeries.CreateRangeBand(id, "Source", new[] { new ChartRangeBand(0, 2, 4), new ChartRangeBand(1, 5, 7), new ChartRangeBand(2, 3, 5) })
                : ChartSeries.CreateRangeArea(id, "Source", new[] { new ChartRangeBand(0, 2, 4), new ChartRangeBand(1, 5, 7), new ChartRangeBand(2, 3, 5) });
        series.WithSmooth();
        for (var index = 0; index < 3; index++) series.WithObservationId(index, new ChartObservationId("reading-" + index));
        var chart = Chart.Create().WithSize(390, 280).WithLegend(false).WithXAxisBounds(0, 2).WithYAxisBounds(0, 10).AddSeries(series);
        if (kind == ChartSeriesKind.Line) chart.WithDataLabels();
        chart.Options.XAxis.Reversed = reversed; chart.Options.YAxis.Reversed = reversed;
        var x = axes.Contains('x') ? new ChartAxisWindow(1, Math.BitIncrement(1)) : null;
        var y = axes.Contains('y') ? new ChartAxisWindow(6, Math.BitIncrement(6)) : null;
        var viewport = new ChartCartesianViewport(x, y);
        PreparedVisual[] drawings;
        if (adaptive) {
            var section = new HtmlInteractiveChartRenderer().PrepareSection(chart, viewport,
                options => options.ResponsiveLayout = HtmlChartResponsiveLayout.Adaptive);
            drawings = new[] { section.PreparedVisual, section.NarrowPreparedVisual! };
        } else drawings = new[] { chart.Prepare(VisualExportRequest.ForChart(chart).Context, viewport) };
        foreach (var drawing in drawings) {
            Assert.Equal(x?.Minimum ?? 0, drawing.XProjection!.Minimum);
            Assert.Equal(x?.Maximum ?? 2, drawing.XProjection.Maximum);
            Assert.Equal(y?.Minimum ?? 0, drawing.YProjection!.Minimum);
            Assert.Equal(y?.Maximum ?? 10, drawing.YProjection.Maximum);
            var svg = drawing.ToSvg();
            Assert.DoesNotContain("NaN", svg); Assert.DoesNotContain("Infinity", svg);
            var observations = XDocument.Parse(svg).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
            Assert.Equal(new[] { "reading-0", "reading-1", "reading-2" }, observations.Select(node => (string?)node.Attribute("data-cfx-observation-id")));
            Assert.Equal(new[] { "0", "1", "2" }, observations.Select(node => (string?)node.Attribute("data-cfx-x")));
            var png = drawing.ToPng();
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        }
        Assert.Equal(0, chart.Options.XAxis.Minimum); Assert.Equal(2, chart.Options.XAxis.Maximum);
        Assert.Equal(0, chart.Options.YAxis.Minimum); Assert.Equal(10, chart.Options.YAxis.Maximum);
        Assert.Equal(3, series.SourcePointCount);
    }
}
