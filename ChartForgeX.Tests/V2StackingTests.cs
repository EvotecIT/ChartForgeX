using System;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class V2StackingTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NamedStacks_UseIndependentSlotsAndSignedBaselinesAlongsideStandaloneBars(bool horizontal) {
        var chart = Bars(horizontal, new[] { 4d, -2 }, new[] { 6d, -3 }, new[] { 8d, -4 }, new[] { 2d, -6 }, new[] { 5d, -5 });
        chart.Series[0].WithStackGroup("First"); chart.Series[1].WithStackGroup("First");
        chart.Series[2].WithStackGroup("Second"); chart.Series[3].WithStackGroup("Second");
        chart.Options.ShowStackTotals = true;
        var prepared = Prepare(chart);
        var first = Bounds(prepared, 0, 0); var firstTop = Bounds(prepared, 1, 0);
        var second = Bounds(prepared, 2, 0); var secondTop = Bounds(prepared, 3, 0);
        var standalone = Bounds(prepared, 4, 0);
        if (horizontal) {
            Assert.Equal(first.Top, firstTop.Top, 8); Assert.Equal(second.Top, secondTop.Top, 8);
            Assert.Equal(first.Right, firstTop.Left, 8); Assert.Equal(second.Right, secondTop.Left, 8);
            Assert.True(first.Bottom < second.Top); Assert.True(second.Bottom < standalone.Top);
            Assert.Equal(Bounds(prepared, 0, 1).Left, Bounds(prepared, 1, 1).Right, 8);
        } else {
            Assert.Equal(first.Left, firstTop.Left, 8); Assert.Equal(second.Left, secondTop.Left, 8);
            Assert.Equal(first.Top, firstTop.Bottom, 8); Assert.Equal(second.Top, secondTop.Bottom, 8);
            Assert.True(first.Right < second.Left); Assert.True(second.Right < standalone.Left);
            Assert.Equal(Bounds(prepared, 0, 1).Bottom, Bounds(prepared, 1, 1).Top, 8);
        }
        Assert.Equal("0", Point(prepared, 4, 0).Metadata["data-cfx-base"]);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "stack-total").ToArray();
        Assert.Equal(4, totals.Length);
        Assert.Equal(new[] { "First", "Second" }, totals.Select(node => node.Metadata["data-cfx-stack-group"]).Distinct().OrderBy(value => value));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NormalizedStacks_KeepSourceValuesWhileGeometryAndTotalsUseResolvedUnits(bool horizontal) {
        var chart = Bars(horizontal, new[] { 3d, -2 }, new[] { 9d, -6 });
        var red = ChartColor.FromHex("#C52E3C"); chart.Series[0].Color = red;
        foreach (var series in chart.Series) series.WithStackGroup("Share").WithNormalization(100);
        chart.Options.ShowStackTotals = true;
        var prepared = Prepare(chart);
        var small = Bounds(prepared, 0, 0); var large = Bounds(prepared, 1, 0);
        Assert.Equal(3, horizontal ? large.Width / small.Width : large.Height / small.Height, 8);
        Assert.Equal("3", Point(prepared, 0, 0).Metadata["data-cfx-y"]);
        Assert.Equal("-2", Point(prepared, 0, 1).Metadata["data-cfx-y"]);
        Assert.Equal("25", Point(prepared, 0, 0).Metadata["data-cfx-rendered-y"]);
        Assert.Equal("-25", Point(prepared, 0, 1).Metadata["data-cfx-rendered-y"]);
        Assert.Equal("100", Point(prepared, 1, 0).Metadata["data-cfx-stack-end"]);
        Assert.Equal("-100", Point(prepared, 1, 1).Metadata["data-cfx-stack-end"]);
        var totals = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "stack-total").ToArray();
        Assert.Contains(totals, total => total.Metadata["data-cfx-y"] == "100" && total.Metadata["data-cfx-source-total"] == "12");
        Assert.Contains(totals, total => total.Metadata["data-cfx-y"] == "-100" && total.Metadata["data-cfx-source-total"] == "-8");
        var range = ChartRange.FromChart(chart);
        Assert.InRange(horizontal ? range.MaxX : range.MaxY, 100, 120);
        Assert.Equal(-100, horizontal ? range.MinX : range.MinY);

        // The source geometry consumed by SVG also places the same solid colour in native raster output.
        var svg = prepared.ToSvg();
        Assert.Contains(XDocument.Parse(svg).Descendants(), element => (string?)element.Attribute("data-cfx-rendered-y") == "25");
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        var pixel = (((int)(small.Top + small.Height / 2)) * image.Width + (int)(small.Left + small.Width / 2)) * 4;
        Assert.Equal(red.R, image.Pixels[pixel]); Assert.Equal(red.G, image.Pixels[pixel + 1]); Assert.Equal(red.B, image.Pixels[pixel + 2]);
        chart.Series[0].WithNormalization(1); chart.Series[0].Points.Clear();
        Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void AxisAndGroupOwnership_KeepNormalizationAndRangeIndependent() {
        var chart = Bars(false, new[] { 3d }, new[] { 20d }, new[] { 9d }, new[] { 30d });
        chart.Series[0].WithStackGroup("Shared name").WithNormalization(100);
        chart.Series[2].WithStackGroup("Shared name").WithNormalization(100);
        chart.Series[1].UseSecondaryYAxis().WithStackGroup("Shared name").WithNormalization(1);
        chart.Series[3].UseSecondaryYAxis().WithStackGroup("Shared name").WithNormalization(1);
        var coordinates = ChartBarCoordinateMap.Create(chart); var stacks = ChartStackLayout.Create(chart, coordinates);
        Assert.Equal(25, stacks.Point(2, 0).Base); Assert.Equal(.4, stacks.Point(3, 0).Base, 8);
        var range = ChartRange.FromChart(chart, coordinates, stacks); var secondary = ChartRange.FromSecondaryYAxis(chart, range, stacks);
        Assert.InRange(range.MaxY, 100, 110); Assert.InRange(secondary.MaxY, 1, 1.1);
        var prepared = Prepare(chart);
        Assert.Equal("100", Point(prepared, 2, 0).Metadata["data-cfx-stack-end"]);
        Assert.Equal("1", Point(prepared, 3, 0).Metadata["data-cfx-stack-end"]);
        Assert.True(Bounds(prepared, 0, 0).Right < Bounds(prepared, 1, 0).Left);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ZeroOnlyNormalizedGroup_RemainsAtBaselineWithoutChangingAnotherGroup(bool horizontal) {
        var chart = Bars(horizontal, new[] { 0d }, new[] { 0d }, new[] { 8d });
        chart.Series[0].WithStackGroup("Empty").WithNormalization(100);
        chart.Series[1].WithStackGroup("Empty").WithNormalization(100);
        chart.Series[2].WithStackGroup("Observed").WithNormalization(100);
        chart.Options.ShowStackTotals = true;
        var prepared = Prepare(chart);
        var diagnostic = Assert.Single(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.stack-zero-total");
        Assert.Contains("Empty", diagnostic.Message);
        Assert.Equal(0, horizontal ? Bounds(prepared, 0, 0).Width : Bounds(prepared, 0, 0).Height);
        Assert.Equal("0", Point(prepared, 0, 0).Metadata["data-cfx-y"]);
        Assert.Equal("0", Point(prepared, 1, 0).Metadata["data-cfx-stack-end"]);
        Assert.Equal("100", Point(prepared, 2, 0).Metadata["data-cfx-stack-end"]);
        var total = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "stack-total");
        Assert.Equal("Observed", total.Metadata["data-cfx-stack-group"]);
    }

    [Fact]
    public void NormalizedStackedArea_RetainsSegmentBreaksSourceMetadataAndNamedBases() {
        var points = new[] { new ChartPoint(1, 2), new ChartPoint(2, 3), new ChartPoint(4, 4, true), new ChartPoint(5, 2) };
        var chart = Chart.Create().AddStackedArea("First", points).AddStackedArea("Second", points)
            .AddStackedArea("Independent", points).WithAxes(false);
        chart.Options.ShowGrid = false;
        chart.Series[0].WithStackGroup("Pair").WithNormalization(100);
        chart.Series[1].WithStackGroup("Pair").WithNormalization(100);
        chart.Series[2].WithStackGroup("Separate").WithNormalization(100);
        var prepared = Prepare(chart);
        Assert.Equal(6, prepared.Scene.Nodes.Count(node => node.Role == "area"));
        Assert.Equal("50", Point(prepared, 1, 2).Metadata["data-cfx-base"]);
        Assert.Equal("0", Point(prepared, 2, 2).Metadata["data-cfx-base"]);
        Assert.Equal("4", Point(prepared, 1, 2).Metadata["data-cfx-y"]);
        Assert.Equal("100", Point(prepared, 1, 2).Metadata["data-cfx-stack-end"]);
        Assert.True(chart.Series[1].Points[2].BreakBefore);
        Assert.DoesNotContain(prepared.Scene.Nodes.OfType<VisualSceneGroup>(), node => node.Role == "point" && node.Metadata["data-cfx-x"] == "3");
    }

    [Theory]
    [InlineData(ChartInterpolation.Linear, ChartStepPosition.End)]
    [InlineData(ChartInterpolation.Step, ChartStepPosition.End)]
    public void StackedArea_SharedBoundariesRequireMatchingInterpolationWithinEachGroupAndAxis(ChartInterpolation interpolation, ChartStepPosition position) {
        var points = new[] { new ChartPoint(0, 2), new ChartPoint(2, 8) };
        var chart = Chart.Create().AddStackedArea("Lower", points).AddStackedArea("Upper", points)
            .AddStackedArea("Other group", points).AddStackedArea("Other axis", points).WithAxes(false).WithGrid(false);
        chart.Series[0].WithStackGroup("Pair").WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Start);
        chart.Series[1].WithStackGroup("Pair").WithInterpolation(interpolation, position);
        chart.Series[2].WithStackGroup("Separate").WithInterpolation(ChartInterpolation.Smooth);
        chart.Series[3].UseSecondaryYAxis().WithStackGroup("Pair").WithInterpolation(ChartInterpolation.Step, ChartStepPosition.End);
        var exception = Assert.Throws<InvalidOperationException>(() => Prepare(chart));
        Assert.Contains("Interpolation and StepPosition", exception.Message);

        chart.Series[1].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Start);
        var prepared = Prepare(chart);
        var lowerLine = prepared.Scene.Nodes.OfType<VisualScenePath>().First(path => path.Role == "line");
        var upperArea = prepared.Scene.Nodes.OfType<VisualScenePath>().Where(path => path.Role == "area").ElementAt(1);
        var sharedBoundary = upperArea.Commands.Reverse().Take(lowerLine.Commands.Count).ToArray();
        for (var index = 0; index < lowerLine.Commands.Count; index++) {
            Assert.Equal(lowerLine.Commands[index].X, sharedBoundary[index].X, 8);
            Assert.Equal(lowerLine.Commands[index].Y, sharedBoundary[index].Y, 8);
        }
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "area"));
    }

    [Fact]
    public void HistogramStacks_PreserveIntervalIdentityAndUseGroupSlotsForNormalizedFrequencies() {
        var bins = ChartHistogramBinLayout.FromWidth(0, 2, 1, roundBounds: false);
        var chart = Chart.Create().AddHistogram("First", new[] { .2, .4, 1.2 }, bins)
            .AddHistogram("Second", new[] { .2, 1.2, 1.4 }, bins)
            .AddHistogram("Separate", new[] { .2, 1.2 }, bins).WithAxes(false);
        chart.Options.ShowGrid = false; chart.Options.ShowStackTotals = true;
        chart.Series[0].WithStackGroup("Pair").WithNormalization(100);
        chart.Series[1].WithStackGroup("Pair").WithNormalization(100);
        chart.Series[2].WithStackGroup("Separate").WithNormalization(100);
        var prepared = Prepare(chart);
        Assert.Equal(Bounds(prepared, 0, 0).Left, Bounds(prepared, 1, 0).Left, 8);
        Assert.True(Bounds(prepared, 0, 0).Right < Bounds(prepared, 2, 0).Left);
        Assert.True(Bounds(prepared, 2, 0).Right < Bounds(prepared, 0, 1).Left);
        Assert.Equal("2", Point(prepared, 0, 0).Metadata["data-cfx-y"]);
        Assert.Equal("100", Point(prepared, 1, 0).Metadata["data-cfx-stack-end"]);
        Assert.Equal("100", Point(prepared, 1, 1).Metadata["data-cfx-stack-end"]);
        Assert.Equal(4, prepared.Scene.Nodes.Count(node => node.Role == "stack-total"));
    }

    [Fact]
    public void StackConfiguration_RejectsConflictingTargetsAndUnsupportedArrangements() {
        var conflicting = Bars(false, new[] { 1d }, new[] { 2d });
        conflicting.Series[0].WithStackGroup("Same").WithNormalization(100);
        conflicting.Series[1].WithStackGroup("Same").WithNormalization(1);
        Assert.Throws<InvalidOperationException>(() => Prepare(conflicting));
        conflicting.Series[1].WithNormalization(null);
        Assert.Throws<InvalidOperationException>(() => Prepare(conflicting));
        var grouped = Bars(false, new[] { 1d }); grouped.Series[0].WithNormalization();
        Assert.Throws<InvalidOperationException>(() => Prepare(grouped));
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(1, 2) }).Series[0];
        Assert.Throws<InvalidOperationException>(() => line.WithStackGroup("Unsupported"));
        Assert.Throws<InvalidOperationException>(() => line.WithNormalization());
        Assert.Throws<ArgumentException>(() => grouped.Series[0].WithStackGroup(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => grouped.Series[0].WithNormalization(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => grouped.Series[0].WithNormalization(double.NaN));
    }

    private static Chart Bars(bool horizontal, params double[][] observations) {
        var chart = Chart.Create().WithAxes(false);
        chart.Options.ShowGrid = false; chart.Options.BarStyle = ChartBarStyle.Flat;
        foreach (var values in observations) {
            var points = values.Select((value, index) => new ChartPoint(index + 1, value));
            var name = "Series " + chart.Series.Count.ToString(CultureInfo.InvariantCulture);
            if (horizontal) chart.AddHorizontalBar(name, points); else chart.AddBar(name, points);
        }
        return chart;
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(640, 400)), frame: new VisualFrame(showLegend: false)));
    private static ChartRect Bounds(PreparedVisual prepared, int series, int point) =>
        prepared.Regions.Single(region => region.Id == "series-" + series + "-point-" + point && region.Role == "point").Bounds;
    private static VisualSceneGroup Point(PreparedVisual prepared, int series, int point) =>
        prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == "series-" + series + "-point-" + point && node.Role == "point");
}
