using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Typography;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Numeric-radial radar labels use the same complete marker contours as native SVG and PNG.</summary>
public sealed class RadarCustomMarkerLabelTests {
    [Theory]
    [InlineData(360, false)] [InlineData(360, true)] [InlineData(800, false)] [InlineData(800, true)]
    public void EquivalentBuiltInAndCustomSquaresRetainIdenticalVisibleCaptionPlacement(int width, bool dark) {
        var shape = ChartMarkerPath.Start(-1, -1).LineTo(1, -1).LineTo(1, 1).LineTo(-1, 1);
        var before = Prepare(Create(width, dark, null, 12, "MMMM"));
        var after = Prepare(Create(width, dark, shape, 12, "MMMM"));
        var expectedMarks = before.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "radar-point").ToArray();
        var marks = after.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "radar-point").ToArray();
        Assert.Equal(4, marks.Length);
        for (int index = 0; index < marks.Length; index++) Assert.Equal(expectedMarks[index].Commands, marks[index].Commands);
        var expected = Captions(before); var actual = Captions(after);
        Assert.Equal(4, expected.Length); Assert.Equal(expected.Length, actual.Length);
        for (int index = 0; index < actual.Length; index++) {
            Assert.Equal(expected[index].Text.Lines.Select(line => line.Text), actual[index].Text.Lines.Select(line => line.Text));
            Assert.Equal(CartesianReversalLabelTests.Bounds(expected[index]), CartesianReversalLabelTests.Bounds(actual[index]));
            Assert.True(new LabelMarkShape(VisualSceneGeometry.Flatten(marks[index], 8), true, 0).Contains(CartesianReversalLabelTests.Bounds(actual[index])));
        }
        var pose = width + "-" + (dark ? "dark" : "light");
        ScatterBubbleColorRenderingTests.Capture(before, "radar-built-in-square-labels-" + pose);
        ScatterBubbleColorRenderingTests.Capture(after, "radar-custom-square-labels-" + pose);
    }

    [Fact]
    public void DisconnectedContoursDoNotInventABridgeAcrossAnEmptyLabelGap() {
        // Opposite contour winding makes concatenating MoveTo-separated paths invent a diagonal through the label.
        var shape = ChartMarkerPath.Start(-1, -1).LineTo(-.5, -1).LineTo(-.5, 1).LineTo(-1, 1)
            .MoveTo(1, -1).LineTo(1, 1).LineTo(.5, 1).LineTo(.5, -1);
        var chart = Create(360, false, shape, 20, "1");
        var context = VisualExportRequest.ForChart(chart).Context;
        var measured = new VisualSceneBuilder(context.Layout.Size, context.Font).MeasureText("1", new TextStyle { Font = context.Font, FontSize = 12 });
        Assert.True(measured.Width + 2 * context.Theme.Spacing / 3 < 20,
            "The fixture's empty gap must accommodate the digit and the ordinary label clearance on both sides.");
        var prepared = Prepare(chart);
        ScatterBubbleColorRenderingTests.Capture(prepared, "radar-custom-disconnected-gap-labels");
        var marks = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(node => node.Role == "radar-point").ToArray();
        var captions = Captions(prepared); Assert.Equal(4, captions.Length);
        for (int index = 0; index < marks.Length; index++) {
            var contours = VisualSceneGeometry.Flatten(marks[index], 8); Assert.Equal(2, contours.Count);
            var box = CartesianReversalLabelTests.Bounds(captions[index]);
            Assert.False(new LabelMarkShape(contours, true, 0).Intersects(box));
            Assert.Equal("1", Assert.Single(captions[index].Text.Lines).Text);
        }
    }

    private static Chart Create(int width, bool dark, ChartMarkerPath? shape, double radius, string label) {
        var chart = Chart.Create().WithSize(width, 300).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithYAxisBounds(0, 100).WithDataLabels(false).WithDataLabelPlacement(ChartDataLabelPlacement.Center)
            .WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddRadialColumn("Observed", Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 40)))
            .AddRadarLine("Target", Enumerable.Range(1, 4).Select(index => new ChartPoint(index, 70)));
        var radar = chart.Series[1]; radar.ShowDataLabels = true; radar.Radar.OutlineStroke = new ChartStrokeStyle(width: 0);
        radar.ConfigureDataLabelStyle(style => style.FontSize = 12);
        for (int index = 0; index < 4; index++) radar.WithPointLabel(index, label);
        radar.ConfigureMarkers(options => { options.Radius = radius; options.StrokeWidth = 0;
            if (shape == null) options.Shape = ChartMarkerShape.Square; else options.CustomPath = shape; });
        return chart;
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static VisualSceneText[] Captions(PreparedVisual prepared) => prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "radar-data-label").ToArray();
}
