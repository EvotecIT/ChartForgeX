using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianReversalTests {
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, -1)]
    [InlineData(false, true, -1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, -1)]
    [InlineData(true, true, -1)]
    public void SegmentedCapsAndMinimumInkFollowTheMappedValueEnd(bool horizontal, bool reversed, int sign) {
        var chart = Chart.Create().WithSize(300, 220).WithAxes(false).WithGrid(false).WithHeader(false)
            .WithLegend(false).WithDataLabels(false).WithBarStyle(ChartBarStyle.SegmentedCapsule);
        var points = new[] { new ChartPoint(1, 10 * sign), new ChartPoint(2, .0001 * sign), new ChartPoint(3, 100 * sign) };
        if (horizontal) chart.AddHorizontalBar("Values", points); else chart.AddBar("Values", points);
        var axis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        axis.WithBounds(sign > 0 ? 0 : -100, sign > 0 ? 100 : 0).WithReversal(reversed);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var role = horizontal ? "horizontal-bar" : "bar";
        var bodies = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == role).ToArray();
        var caps = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == role + "-cap").ToArray();
        Assert.Equal(3, bodies.Length); Assert.Equal(3, caps.Length);
        var positiveDirection = sign > 0 != reversed;
        for (var index = 0; index < bodies.Length; index++) {
            var bounds = bodies[index].Bounds; var cap = caps[index];
            if (horizontal) Assert.Equal(positiveDirection ? bounds.Right - cap.StrokeWidth / 2 : bounds.Left + cap.StrokeWidth / 2, cap.Start.X, 9);
            else Assert.Equal(positiveDirection ? bounds.Top + cap.StrokeWidth / 2 : bounds.Bottom - cap.StrokeWidth / 2, cap.Start.Y, 9);
        }
        var tiny = bodies[1].Bounds; var full = bodies[2].Bounds;
        if (horizontal) {
            Assert.Equal(1, tiny.Width);
            Assert.Equal(positiveDirection ? full.Left : full.Right, positiveDirection ? tiny.Left : tiny.Right, 9);
        } else {
            Assert.Equal(1, tiny.Height);
            Assert.Equal(positiveDirection ? full.Bottom : full.Top, positiveDirection ? tiny.Bottom : tiny.Top, 9);
        }
        Assert.Equal(.0001 * sign, chart.Series[0].Points[1].Y);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WaterfallConnectorsJoinFacingEdgesInMappedCategoryOrder(bool reversed) {
        var chart = Chart.Create().WithSize(600, 360).WithLegend(false).WithDataLabels(false).WithBarStyle(ChartBarStyle.Flat)
            .AddWaterfall("Changes", new[] { new ChartPoint(1, 10), new ChartPoint(2, 5), new ChartPoint(3, -3) });
        chart.Options.XAxis.WithReversal(reversed);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var bodies = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "waterfall-bar").ToArray();
        var connectors = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "waterfall-connector").ToArray();
        Assert.Equal(4, bodies.Length); Assert.Equal(2, connectors.Length);
        for (var index = 0; index < connectors.Length; index++) {
            Assert.Equal(reversed ? bodies[index].Bounds.Left : bodies[index].Bounds.Right, connectors[index].Start.X, 9);
            Assert.Equal(reversed ? bodies[index + 1].Bounds.Right : bodies[index + 1].Bounds.Left, connectors[index].End.X, 9);
            Assert.Equal(connectors[index].Start.Y, connectors[index].End.Y);
            Assert.True(reversed ? connectors[index].Start.X > connectors[index].End.X : connectors[index].Start.X < connectors[index].End.X);
        }
        Assert.True(prepared.ToPng().Length > 64);
    }
}
