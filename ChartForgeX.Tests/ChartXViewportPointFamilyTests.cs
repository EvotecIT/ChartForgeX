using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartAxisWindowPointFamilyTests {
    [Theory]
    [InlineData(ChartSeriesKind.ErrorBar)]
    [InlineData(ChartSeriesKind.Lollipop)]
    [InlineData(ChartSeriesKind.Dumbbell)]
    [InlineData(ChartSeriesKind.Slope)]
    [InlineData(ChartSeriesKind.TrendLine)]
    public void NativeWindowsRetainPointAndDerivedFactsWhileOnlyInWindowObservationsPaint(ChartSeriesKind kind) {
        var points = new[] { new ChartPoint(0, 20), new ChartPoint(5, 50), new ChartPoint(10, 30) };
        var id = new ChartSeriesId("source");
        var series = kind switch {
            ChartSeriesKind.ErrorBar => ChartSeries.CreateErrorBar(id, "Source", points.Select(point => new ChartErrorBar(point.X, point.Y, yBounds: new ChartErrorBounds(point.Y - 4, point.Y + 8)))),
            ChartSeriesKind.Dumbbell => ChartSeries.CreateDumbbell(id, "Source", points.Select(point => new ChartDumbbell(point.X, point.Y, point.Y + 8))),
            ChartSeriesKind.TrendLine => ChartSeries.CreateTrendLine(id, "Source", points),
            ChartSeriesKind.Slope => new ChartSeries(id, "Source", kind, new[] { points[0], points[2] }),
            _ => new ChartSeries(id, "Source", kind, points)
        };
        var chart = Chart.Create().WithLegend(false).WithXAxisBounds(0, 10).AddSeries(series);
        var context = VisualExportRequest.ForChart(chart).Context;
        var full = chart.Prepare(context); var window = chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(4, 6)));
        Assert.NotNull(window.XProjection);
        Assert.Equal(full.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label), window.Regions.Where(region => region.Role == "axis-y-label").Select(region => region.Label));
        var groups = XDocument.Parse(window.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        if (kind is ChartSeriesKind.Slope or ChartSeriesKind.TrendLine) {
            Assert.Equal(2, groups.Length); Assert.All(groups, group => Assert.Equal("false", (string?)group.Attribute("data-cfx-visible")));
            Assert.Contains(window.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == (kind == ChartSeriesKind.Slope ? "slope-line" : "trend-line"));
            if (kind == ChartSeriesKind.TrendLine) {
                Assert.Equal(3, series.SourcePointCount);
                Assert.All(groups, group => Assert.Equal("regression-endpoint", (string?)group.Attribute("data-cfx-derived")));
                var regression = XDocument.Parse(window.ToSvg()).Descendants().Single(node => (string?)node.Attribute("data-cfx-role") == "regression");
                Assert.Equal("3", (string?)regression.Attribute("data-cfx-source-count"));
            }
        } else {
            Assert.Equal(3, groups.Length);
            Assert.Equal(new[] { "false", null, "false" }, groups.Select(group => (string?)group.Attribute("data-cfx-visible")));
            Assert.NotEmpty(groups[1].Elements()); Assert.Empty(groups[0].Elements()); Assert.Empty(groups[2].Elements());
            Assert.Equal(3, series.SourcePointCount);
        }
        Assert.NotEmpty(window.ToPng());
    }
}
