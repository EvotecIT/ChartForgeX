using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartCartesianViewportTests {
    [Theory]
    [InlineData(ChartScaleKind.Linear, false, 0, 100, 20, 80)]
    [InlineData(ChartScaleKind.Linear, true, 0, 100, 20, 80)]
    [InlineData(ChartScaleKind.Logarithmic, false, 1, 1000, 10, 100)]
    [InlineData(ChartScaleKind.Logarithmic, true, 1, 1000, 10, 100)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false, -100, 100, -20, 40)]
    public void ExplicitPrimaryYUsesTheNativeScaleAndDetachedInverse(ChartScaleKind scale, bool reversed, double fullMinimum, double fullMaximum, double minimum, double maximum) {
        var chart = Chart.Create().WithXAxisBounds(0, 10).WithYAxisBounds(fullMinimum, fullMaximum).WithYAxisScale(scale)
            .AddLine("Source", new[] { new ChartPoint(0, fullMinimum), new ChartPoint(5, minimum), new ChartPoint(10, fullMaximum) });
        chart.Options.YAxis.Reversed = reversed;
        var context = VisualExportRequest.ForChart(chart).Context;
        var full = chart.Prepare(context);
        var prepared = chart.Prepare(context, new ChartCartesianViewport(y: new ChartAxisWindow(minimum, maximum)));
        var y = Assert.IsType<ChartAxisProjection>(prepared.YProjection); var plot = y.PlotBounds;
        Assert.Equal(fullMinimum, y.FullMinimum); Assert.Equal(fullMaximum, y.FullMaximum);
        Assert.Equal(reversed ? plot.Top : plot.Bottom, y.Project(minimum), 8);
        Assert.Equal(reversed ? plot.Bottom : plot.Top, y.Project(maximum), 8);
        foreach (var value in new[] { minimum, maximum }) Assert.Equal(value, y.Unproject(y.Project(value)), 8);
        Assert.Equal(full.XProjection!.Minimum, prepared.XProjection!.Minimum); Assert.Equal(full.XProjection.Maximum, prepared.XProjection.Maximum);
        var groups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(new[] { "false", null, "false" }, groups.Select(group => (string?)group.Attribute("data-cfx-visible")));
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "line");
        Assert.NotEmpty(prepared.ToPng());
        var svg = prepared.ToSvg(); chart.Options.YAxis.Reversed = !reversed;
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(minimum, y.Unproject(y.Project(minimum)), 8);
    }

    [Fact]
    public void CartesianWindowsRetainTupleCrossingsSourceFactsAndSingleAxisPolicy() {
        var chart = Chart.Create().WithXAxisBounds(0, 10).WithYAxisBounds(0, 100).AddSeries(ChartSeries.CreateCandlestick(new ChartSeriesId("prices"), "Prices",
            new[] { new ChartCandlestick(0, 80, 95, 70, 90), new ChartCandlestick(5, 80, 95, 20, 90), new ChartCandlestick(10, 10, 20, 5, 15) }));
        chart.AddNote(new ChartPoint(5, 50), "Review", id: new ChartAnnotationId("review"))
            .AddLineSegment(new ChartPoint(0, 0), new ChartPoint(10, 100), "Target");
        var context = VisualExportRequest.ForChart(chart).Context;
        var prepared = chart.Prepare(context, new ChartCartesianViewport(new ChartAxisWindow(2, 8), new ChartAxisWindow(30, 60)));
        var anchor = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "annotation-anchor");
        Assert.Equal(prepared.YProjection!.Project(50), anchor.Cy, 8);
        Assert.Equal(prepared.XProjection!.Project(5), anchor.Cx, 8);
        Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneLine>(), node => node.Role == "annotation-source-line");
        var groups = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(3, groups.Length); Assert.Equal(new[] { "false", null, "false" }, groups.Select(group => (string?)group.Attribute("data-cfx-visible")));
        Assert.NotEmpty(groups[1].Elements()); Assert.Equal("80", (string?)groups[1].Attribute("data-cfx-open"));
        Assert.Equal(0, chart.Options.XAxis.Minimum); Assert.Equal(100, chart.Options.YAxis.Maximum); Assert.NotEmpty(prepared.ToPng());
        chart.ClearAnnotations(); chart.Series[0].UseSecondaryValueAxis();
        Assert.NotNull(chart.Prepare(context, new ChartCartesianViewport(x: new ChartAxisWindow(2, 8))).XProjection);
        Assert.NotNull(chart.Prepare(context).YProjection);
        var secondary = chart.Prepare(context, new ChartCartesianViewport(secondaryY: new ChartAxisWindow(30, 60)));
        Assert.Equal(30, secondary.SecondaryYProjection!.Minimum); Assert.Equal(60, secondary.SecondaryYProjection.Maximum);
        Assert.Equal(0, secondary.YProjection!.Minimum); Assert.Equal(100, secondary.YProjection.Maximum);
        Assert.Throws<ArgumentException>(() => new ChartCartesianViewport());
    }

    [Fact]
    public void LogarithmicConfinementPreservesTransformedSpanAndRejectsInvalidSourceWindows() {
        var chart = Chart.Create().WithXAxisBounds(0, 10).WithYAxisBounds(1, 1000).WithYAxisScale(ChartScaleKind.Logarithmic)
            .AddLine("Positive", new[] { new ChartPoint(0, 1), new ChartPoint(10, 1000) });
        var projection = chart.Prepare(VisualExportRequest.ForChart(chart).Context).YProjection!;
        var confined = projection.ConstrainViewport(new ChartAxisWindow(100, 10000))!;
        Assert.Equal(10, confined.Minimum, 8); Assert.Equal(1000, confined.Maximum, 8);
        Assert.Null(projection.ConstrainViewport(new ChartAxisWindow(1, 1000)));
        Assert.Throws<ArgumentOutOfRangeException>(() => projection.ConstrainViewport(new ChartAxisWindow(-1, 10)));
    }
}
