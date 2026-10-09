using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianReversalLabelTests {
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, -1)]
    [InlineData(false, true, -1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, -1)]
    [InlineData(true, true, -1)]
    public void AutomaticGroupedLabelsUseTheMappedValueEnd(bool horizontal, bool reversed, int sign) {
        var chart = Grouped(horizontal, reversed, sign);
        var prepared = Prepare(chart);
        var marks = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == (horizontal ? "horizontal-bar" : "bar")).ToArray();
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.Equal(2, labels.Length);
        for (var index = 0; index < labels.Length; index++) {
            var label = Bounds(labels[index]); var mark = marks[index].Bounds;
            if (horizontal) Assert.True(sign > 0 != reversed ? label.Left > mark.Right : label.Right < mark.Left);
            else Assert.True(sign > 0 != reversed ? label.Bottom < mark.Top : label.Top > mark.Bottom);
        }
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ContainedFallbackUsesMarkContrastAndPreservesAuthoredInk(bool horizontal, bool reversed) {
        var fill = ChartColor.FromHex("#172554");
        var chart = Chart.Create().WithSize(180, 180).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels().WithBarStyle(ChartBarStyle.Flat).WithTheme(ChartTheme.GraphiteLight());
        if (horizontal) chart.AddHorizontalBar("Value", new[] { new ChartPoint(1, 100) }, fill);
        else chart.AddBar("Value", new[] { new ChartPoint(1, 100) }, fill);
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(0, 100).WithReversal(reversed);
        var prepared = Prepare(chart);
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        var mark = Assert.Single(prepared.Regions, region => region.Role == "point").Bounds;
        Assert.True(LabelPlacementService.Contains(mark, Bounds(label)));
        Assert.Equal(ChartColorMath.AccessibleTextOnBackground(fill), label.Color);
        Assert.True(prepared.ToPng().Length > 64);
        chart.Series[0].WithDataLabelStyle(style => style.WithColor("#FFFF00"));
        Assert.Equal(ChartColor.FromHex("#FFFF00"), Assert.Single(Prepare(chart).Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label").Color);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SignedReversedStacksKeepAutomaticCaptionsInsideTheirOwnSegments(bool horizontal) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels().WithStackedBars().WithBarStyle(ChartBarStyle.Flat);
        foreach (var value in new[] { 40d, 20d }) {
            var points = new[] { new ChartPoint(1, value), new ChartPoint(2, -value) };
            if (horizontal) chart.AddHorizontalBar(value.ToString(), points); else chart.AddBar(value.ToString(), points);
        }
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(-100, 100).WithReversal();
        var prepared = Prepare(chart);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.Equal(4, labels.Length);
        foreach (var label in labels) {
            var mark = prepared.Regions.Single(region => region.Id == label.Id!.Replace("-label", "", StringComparison.Ordinal));
            Assert.True(LabelPlacementService.Contains(mark.Bounds, Bounds(label)));
        }
    }

    internal static ChartRect Bounds(VisualSceneText text) => new(text.X, text.Baseline - text.Text.Ascent, text.Text.Metrics.Width, text.Text.Metrics.Height);
    internal static Chart Grouped(bool horizontal, bool reversed, int sign) {
        var chart = Chart.Create().WithSize(640, 360).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithDataLabels().WithBarStyle(ChartBarStyle.Flat);
        foreach (var value in new[] { 40d, 60d }) {
            if (horizontal) chart.AddHorizontalBar(value.ToString(), new[] { new ChartPoint(1, value * sign) });
            else chart.AddBar(value.ToString(), new[] { new ChartPoint(1, value * sign) });
        }
        (horizontal ? chart.Options.XAxis : chart.Options.YAxis).WithBounds(-100, 100).WithReversal(reversed);
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
