using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects physical axis orientation rather than source order or export-backend assumptions.</summary>
public sealed class NumericRadialAxisPolicyTests {
    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    public void RelativeLabelsFollowProjectedAxesWithIndependentOffsetsAndReversal(bool bars, bool reversed, bool rounded) {
        var chart = RelativeChart(bars, reversed);
        if (rounded) chart.WithRadialGeometry(new(0, 240, .2, .25, .1, cornerRadius: 6));
        var scene = Prepare(chart).Scene;
        var captions = NumericRadialAxisTextTests.Footprints(scene).Where(item => item.Text.Role is "radial-category-label" or "radial-value-label").ToArray();
        foreach (var prefix in new[] { "radial-category-label-", "radial-value-label-Primary-", "radial-value-label-Secondary-" }) {
            var selected = captions.Where(item => item.Text.Id!.StartsWith(prefix, StringComparison.Ordinal)
                && (prefix == "radial-category-label-" || item.Text.Text.Lines.Single().Text.StartsWith(prefix.Contains("Primary", StringComparison.Ordinal) ? "P" : "S", StringComparison.Ordinal))).ToArray();
            Assert.NotEmpty(selected);
            foreach (var item in selected) {
                var index = int.Parse(prefix == "radial-category-label-" ? item.Text.Id!.Substring(prefix.Length)
                    : item.Text.Text.Lines.Single().Text.Substring(1), System.Globalization.CultureInfo.InvariantCulture);
                // These authored ticks project to 0, 120 and 240 degrees. Category
                // centers are 40, 120 and 200 degrees, with their source IDs retained.
                var expected = prefix == "radial-category-label-" ? bars ? 15 : new[] { -35d, 45, -55 }[reversed ? 2 - index : index]
                    : prefix == "radial-value-label-Primary-" ? bars ? new[] { 80d, 20, -40 }[reversed ? 2 - index : index] : -10
                    : bars ? new[] { 5d, -55, 65 }[reversed ? index : 2 - index] : -25;
                Assert.Equal(expected, item.Angle, 6);
                Assert.InRange(item.Angle, -90, 90);
            }
        }
        Assert.All(captions, caption => Assert.True(LabelPlacementService.Contains(new ChartRect(0, 0, 800, 440), caption.Bounds)));
        Assert.Equal("1200", Point(scene, 0, 0)["data-cfx-y"]);
        Assert.Equal("85", Point(scene, 1, 0)["data-cfx-y"]);
        var marks = scene.Nodes.Where(node => node.Role is "radial-bar" or "radial-column"
            && node is VisualSceneSlice or VisualScenePath).ToArray();
        Assert.NotEmpty(marks);
        if (rounded) Assert.All(marks, mark => Assert.IsType<VisualScenePath>(mark));
        Assert.All(captions, caption => Assert.All(marks, mark =>
            Assert.False(new LabelMarkShape(mark is VisualScenePath path ? VisualSceneGeometry.Flatten(path, 8)
                : VisualSceneGeometry.Flatten((VisualSceneSlice)mark, 8), true, 0).Intersects(caption.Bounds))));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RelativeOffsetsKeepEquivalentFullTurnsAndDoNotRotateTitlesOrDataLabels(bool bars) {
        var chart = RelativeChart(bars).WithDataLabels().WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var pixels = prepared.ToRgba().Pixels;
        chart.Options.XAxis.LabelAngle += 720;
        chart.Options.YAxis.LabelAngle -= 360;
        chart.Options.SecondaryYAxis.LabelAngle += 1080;
        Assert.Equal(svg, Prepare(chart).ToSvg()); Assert.Equal(pixels, Prepare(chart).ToRgba().Pixels);
        Assert.All(NumericRadialAxisTextTests.Footprints(prepared.Scene).Where(item => item.Text.Role is "radial-category-axis-title" or "radial-value-axis-title" or "radial-data-label"),
            item => Assert.Equal(0, item.Angle));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Timeline)]
    [InlineData(ChartSeriesKind.PolarArea)]
    public void UnsupportedFamiliesRejectRelativeOrientationEvenOnAHiddenAxis(ChartSeriesKind kind) {
        var chart = V2GalleryModels.Create(kind);
        chart.Options.SecondaryYAxis.Visible = false;
        chart.Options.SecondaryYAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
        Assert.Throws<NotSupportedException>(() => chart.ToSvg());
    }

    [Fact]
    public void RadarAcceptsTheQualifiedRelativePolicyWithoutPaintingAHiddenSecondaryAxis() {
        var chart = V2GalleryModels.Create(ChartSeriesKind.Radar);
        chart.Options.SecondaryYAxis.Visible = false;
        var ordinary = Prepare(chart).ToSvg();
        chart.Options.SecondaryYAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        Assert.Equal(ordinary, Prepare(chart).ToSvg());
    }

    [Fact]
    public void OrientationRejectsUnknownValuesAndAnEmptyUnspecifiedCoordinateFamily() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartAxis().LabelOrientation = (ChartAxisLabelOrientation)99);
        var chart = Chart.Create(); chart.Options.XAxis.LabelOrientation = ChartAxisLabelOrientation.Perpendicular;
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
    }

    internal static Chart RelativeChart(bool bars, bool reversed = false) {
        var chart = NumericRadialAxisTextTests.Configured(bars, 800).WithRadialGeometry(new(0, 240, .2, .25, .1));
        chart.Options.XAxis.LabelOrientation = chart.Options.YAxis.LabelOrientation = ChartAxisLabelOrientation.Parallel;
        chart.Options.SecondaryYAxis.LabelOrientation = ChartAxisLabelOrientation.Perpendicular;
        chart.Options.XAxis.LabelAngle = 15; chart.Options.YAxis.LabelAngle = -10; chart.Options.SecondaryYAxis.LabelAngle = 5;
        chart.Options.XAxis.Reversed = chart.Options.YAxis.Reversed = reversed;
        chart.Options.SecondaryYAxis.Reversed = !reversed;
        foreach (var axis in new[] { chart.Options.XAxis, chart.Options.YAxis, chart.Options.SecondaryYAxis }) axis.LabelDensity = ChartLabelDensity.All;
        chart.Options.YAxis.Labels.Clear(); chart.Options.SecondaryYAxis.Labels.Clear();
        for (var index = 0; index < 3; index++) {
            chart.Options.YAxis.Labels.Add(new ChartAxisLabel(index * 750, "P" + index));
            chart.Options.SecondaryYAxis.Labels.Add(new ChartAxisLabel(index * 50, "S" + index));
        }
        return chart;
    }

    internal static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
    private static IReadOnlyDictionary<string, string> Point(VisualScene scene, int series, int point) =>
        scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == $"series-{series}-point-{point}").Metadata;
}
