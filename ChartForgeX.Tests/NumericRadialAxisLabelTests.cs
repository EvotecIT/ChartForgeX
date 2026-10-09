using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class NumericRadialAxisLabelTests {
    [Theory]
    [InlineData(360, false, 0)]
    [InlineData(800, true, 0)]
    [InlineData(360, true, -50)]
    [InlineData(800, false, 20)]
    public void ProportionalAxesRetainBothFullTickScalesInSeparateLanes(int width, bool reversed, int minimum) {
        var chart = DualAxes(width, reversed, minimum);
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var captions = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "radial-value-label").ToArray();
        Assert.Equal(scene.Regions.Count(region => region.Role == "radial-value-label"), captions.Length);
        foreach (var prefix in new[] { "P", "S" }) {
            var expected = Enumerable.Range(0, 5).Select(index => prefix + (minimum + index * 25d).ToString("0", CultureInfo.InvariantCulture));
            var displayed = captions.Where(text => text.Id!.Contains(prefix == "P" ? "Primary" : "Secondary", StringComparison.Ordinal))
                .Select(text => text.Text.Lines.Single().Text).ToArray();
            foreach (var text in expected) Assert.Contains(text, displayed);
        }
        for (var first = 0; first < captions.Length; first++) for (var second = first + 1; second < captions.Length; second++) {
            var a = CartesianReversalLabelTests.Bounds(captions[first]); var b = CartesianReversalLabelTests.Bounds(captions[second]);
            Assert.False(a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom);
        }
    }

    [Theory]
    [InlineData(0, 360, false)]
    [InlineData(90, 800, true)]
    [InlineData(180, 360, true)]
    [InlineData(270, 800, false)]
    [InlineData(45, 800, false)]
    [InlineData(225, 800, true)]
    public void CategoryCaptionsStayBeforeTheStartRayWithoutDependingOnDataLabels(int start, int width, bool reversed) {
        var chart = Categories(start, width, reversed);
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var captions = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "radial-category-label").ToArray();
        Assert.Equal(3, captions.Length);
        foreach (var name in new[] { "North", "South", "East" }) Assert.Contains(scene.Regions, region => region.Role == "radial-category-label" && region.Label == name);
        var marks = NumericRadialSeriesTests.Marks(scene);
        foreach (var caption in captions) foreach (var mark in marks) {
            var shape = new LabelMarkShape(VisualSceneGeometry.Flatten(mark, 8), true, 0);
            Assert.False(shape.Intersects(CartesianReversalLabelTests.Bounds(caption)), caption.Id + " overlaps a radial band.");
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-90)]
    public void FullTurnCategoryCaptionsNeverCrossPaintedRingsWhenDataCaptionsAreOff(int start) {
        var chart = Categories(start, 800, false).WithRadialGeometry(new(start, start + 360, .1, .25, .1));
        for (var point = 0; point < chart.Series[0].Points.Count; point++) chart.Series[0].Points[point] = new ChartPoint(point + 1, 100);
        var scene = chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene;
        var shapes = NumericRadialSeriesTests.Marks(scene).Select(mark => new LabelMarkShape(VisualSceneGeometry.Flatten(mark, 8), true, 0)).ToArray();
        foreach (var caption in scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "radial-category-label"))
            Assert.All(shapes, shape => Assert.False(shape.Intersects(CartesianReversalLabelTests.Bounds(caption))));
        Assert.Equal(3, scene.Regions.Count(region => region.Role == "radial-category-label"));
        Assert.Contains(scene.Diagnostics, diagnostic => diagnostic.Code == "numeric-radial.label-overflow");
    }

    internal static Chart DualAxes(int width, bool reversed, int minimum = 0) {
        var chart = Chart.Create().WithSize(width, width == 360 ? 360 : 440).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithXLabels("North", "South", "East").WithRadialGeometry(new(-75, 195, .2, .25, .1))
            .AddRadialBar("Count", new[] { new ChartPoint(1, minimum + 60), new ChartPoint(2, minimum + 70), new ChartPoint(3, minimum + 80) })
            .AddRadialBar("Rate", new[] { new ChartPoint(1, (minimum + 50) / 10d), new ChartPoint(2, (minimum + 65) / 10d), new ChartPoint(3, (minimum + 75) / 10d) });
        chart.Series[1].UseSecondaryYAxis();
        chart.Options.YAxis.WithBounds(minimum, minimum + 100).WithReversal(reversed);
        chart.Options.SecondaryYAxis.WithBounds(minimum / 10d, (minimum + 100) / 10d).WithReversal(reversed);
        chart.Options.YAxis.TickCount = 5; chart.Options.SecondaryYAxis.TickCount = 5;
        foreach (var index in Enumerable.Range(0, 5)) {
            chart.Options.YAxis.Labels.Add(new ChartAxisLabel(minimum + index * 25, "P" + (minimum + index * 25).ToString(CultureInfo.InvariantCulture)));
            chart.Options.SecondaryYAxis.Labels.Add(new ChartAxisLabel((minimum + index * 25) / 10d, "S" + (minimum + index * 25).ToString(CultureInfo.InvariantCulture)));
        }
        return chart;
    }

    internal static Chart Categories(int start, int width, bool reversed) {
        var chart = Chart.Create().WithSize(width, width == 360 ? 360 : 440).WithHeader(false).WithLegend(false).WithDataLabels(false).WithGrid(false)
            .WithXLabels("North", "South", "East").WithYAxisBounds(0, 100).WithRadialGeometry(new(start, start + 270, .1, .25, .1))
            .AddRadialBar("Observed", new[] { new ChartPoint(1, 80), new ChartPoint(2, 70), new ChartPoint(3, 60) });
        chart.Options.YAxis.Visible = false; chart.Options.XAxis.WithReversal(reversed);
        return chart;
    }
}
