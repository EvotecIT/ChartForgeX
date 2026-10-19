using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Native windows retain full source semantics while repainting supported Cartesian families.</summary>
public sealed class ChartXViewportFamilyTests {
    [Theory]
    [InlineData(ChartSeriesKind.Line, false)]
    [InlineData(ChartSeriesKind.Line, true)]
    [InlineData(ChartSeriesKind.StepLine, false)]
    [InlineData(ChartSeriesKind.Area, false)]
    [InlineData(ChartSeriesKind.StepArea, false)]
    [InlineData(ChartSeriesKind.StackedArea, false)]
    [InlineData(ChartSeriesKind.Scatter, false)]
    [InlineData(ChartSeriesKind.Bubble, false)]
    public void TimeWindowsKeepSourceFactsAndFullValueDomains(ChartSeriesKind kind, bool smooth) {
        var first = new DateTime(2026, 1, 28, 0, 0, 0, DateTimeKind.Utc).ToOADate();
        var points = new[] { new ChartPoint(first, -100), new ChartPoint(first + 5, 17), new ChartPoint(first + 10, 100) };
        var series = kind == ChartSeriesKind.Bubble
            ? ChartSeries.CreateBubble(new ChartSeriesId("observations"), "Observations", points.Select(point => new ChartBubble(point.X, point.Y, point.Y + 101)))
            : new ChartSeries(new ChartSeriesId("observations"), "Observations", kind, points);
        if (smooth) series.WithSmooth();
        for (var index = 0; index < 3; index++) series.WithObservationId(index, new ChartObservationId("reading-" + index));
        var chart = Chart.Create().WithLegend(false).WithXAxisScale(ChartScaleKind.Time)
            .WithXAxisBounds(first, first + 10).AddSeries(series);
        chart.Options.XAxis.Reversed = true;
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(640, 380)), VisualTheme.Graphite());
        var full = chart.Prepare(context);
        var window = chart.Prepare(context, new ChartXViewport(first + 4, first + 6));
        var projection = Assert.IsType<ChartXProjection>(window.XProjection);
        Assert.Equal(ChartScaleKind.Time, projection.Scale);
        Assert.True(projection.Reversed);
        Assert.Equal(first, projection.FullMinimum); Assert.Equal(first + 10, projection.FullMaximum);
        Assert.Equal(first + 4, projection.Minimum); Assert.Equal(first + 6, projection.Maximum);
        Assert.Equal(projection.PlotBounds.Right, projection.Project(first + 4), 8);
        Assert.Equal(first + 5, projection.Unproject(projection.Project(first + 5)), 8);
        Assert.Equal(AxisText(full, "axis-y-label"), AxisText(window, "axis-y-label"));
        Assert.NotEqual(AxisText(full, "axis-x-label"), AxisText(window, "axis-x-label"));
        var groups = XDocument.Parse(window.ToSvg()).Descendants()
            .Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(3, groups.Length);
        Assert.Equal(new[] { "reading-0", "reading-1", "reading-2" }, groups.Select(node => (string?)node.Attribute("data-cfx-observation-id")));
        Assert.Equal(new[] { "false", null, "false" }, groups.Select(node => (string?)node.Attribute("data-cfx-visible")));
        Assert.Equal("17", (string?)groups[1].Attribute("data-cfx-y"));
        if (kind == ChartSeriesKind.Bubble) {
            Assert.Single(window.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "bubble");
            Assert.Equal("118", (string?)groups[1].Attribute("data-cfx-size"));
        }
        Assert.NotEmpty(window.ToPng());
        Assert.Equal(first, chart.Options.XAxis.Minimum); Assert.Equal(first + 10, chart.Options.XAxis.Maximum);
        Assert.Equal(3, series.SourcePointCount);
    }

    [Fact]
    public void MixedSmoothAreaAndScatterKeepCrossingPaintWithoutInventingObservations() {
        var chart = Chart.Create().WithLegend(false).WithAxes(false).WithGrid(false).WithXAxisBounds(0, 10).WithYAxisBounds(0, 10)
            .AddArea("Area", new[] { new ChartPoint(0, 2), new ChartPoint(10, 8) })
            .AddScatter("Readings", new[] { new ChartPoint(0, 3), new ChartPoint(10, 7) });
        chart.Series[0].WithSmooth();
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(420, 280)), VisualTheme.Graphite());
        var prepared = chart.Prepare(context, new ChartXViewport(4, 6));
        Assert.NotNull(prepared.XProjection);
        Assert.Contains(prepared.Scene.Nodes.OfType<VisualScenePath>(), node => node.Role == "area" && node.Commands.Count > 3);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "point"));
        Assert.All(XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point"),
            node => Assert.Equal("false", (string?)node.Attribute("data-cfx-visible")));
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneEllipse>(), node => node.Role == "marker");
        Assert.NotEmpty(prepared.ToRgba().Pixels);
    }

    private static string?[] AxisText(PreparedVisual prepared, string role) => prepared.Regions
        .Where(region => region.Role == role).Select(region => region.Label).ToArray();
}
