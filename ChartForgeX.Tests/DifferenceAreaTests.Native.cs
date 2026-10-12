using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class DifferenceAreaTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ActualCrossingSplitsNativeFillWithoutChangingPolarityOnReversedAxes(bool reverseX, bool reverseY) {
        var chart = Model(new(0, 8, 2), new(1, 2, 5)); chart.Series[0].WithStrokeWidth(1);
        chart.Options.XAxis.WithBounds(0, 1).WithReversal(reverseX);
        chart.Options.YAxis.WithBounds(0, 10).WithReversal(reverseY);
        var prepared = Prepare(chart);
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        var start = first.Commands[0]; var end = first.Commands[^1];
        // Authored differences are +6 and -3: the actual crossing is at two thirds, not the sample midpoint.
        var crossX = start.X + (end.X - start.X) * 2 / 3;
        var crossY = start.Y + (end.Y - start.Y) * 2 / 3;
        var positive = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "difference-positive");
        var negative = Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "difference-negative");
        foreach (var region in new[] { positive, negative }) {
            Assert.True(region.Close); Assert.Null(region.Stroke);
            Assert.Contains(region.Commands, point => Math.Abs(point.X - crossX) < 1e-8 && Math.Abs(point.Y - crossY) < 1e-8);
        }
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, Sample(first, second, .25), Red, 128);
        AssertPaint(image, Sample(first, second, .85), Blue, 128);
        var svg = XDocument.Parse(prepared.ToSvg());
        foreach (var role in new[] { "difference-positive", "difference-negative" })
            Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == role);
        Assert.Equal("8", Group(prepared, "series-0-point-0")["data-cfx-first"]);
        Assert.Equal("5", Group(prepared, "series-0-point-1")["data-cfx-second"]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EitherTupleMemberBreaksBothNativeBoundariesAndFill(bool breakSecond) {
        var chart = Model(new(0, 8, 2), new(1, 8, 2), new(3, 8, 2, true), new(4, 8, 2));
        chart.Series[0].WithStrokeWidth(1);
        chart.Series[0].Points[4] = new ChartPoint(3, 8, !breakSecond);
        chart.Series[0].Points[5] = new ChartPoint(3, 2, breakSecond);
        var prepared = Prepare(chart);
        foreach (var role in new[] { "difference-first", "difference-second" })
            Assert.Equal(2, Boundary(prepared, role).Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "difference-positive"));
        var left = prepared.Regions.Single(region => region.Id == "series-0-point-1").Bounds;
        var right = prepared.Regions.Single(region => region.Id == "series-0-point-2").Bounds;
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(0, Pixel(image, (left.X + right.X) / 2, left.Y + left.Height / 2).A);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Fact]
    public void SameSignRunsHaveNoAlphaSeamAndTiesDoNotInventFilledRegions() {
        var chart = Model(new(0, 8, 2), new(1, 8, 2), new(2, 8, 2));
        var prepared = Prepare(chart);
        Assert.Single(prepared.Scene.Nodes, node => node.Role == "difference-positive");
        var middle = prepared.Regions.Single(region => region.Id == "series-0-point-1").Bounds;
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, new ChartPoint(middle.X, middle.Y + middle.Height / 2), Red, 128);
        var tied = Prepare(Model(new(0, 4, 4), new(1, 4, 4), new(2, 4, 4)));
        Assert.DoesNotContain(tied.Scene.Nodes, node => node.Role is "difference-positive" or "difference-negative");
        Assert.Equal(3, tied.Regions.Count(region => region.Role == "point"));
        var alternating = Prepare(Model(new(0, 8, 2), new(1, 2, 8), new(2, 8, 2), new(3, 5, 5), new(4, 5, 5), new(5, 2, 8)));
        Assert.Equal(2, alternating.Scene.Nodes.Count(node => node.Role == "difference-positive"));
        Assert.Equal(2, alternating.Scene.Nodes.Count(node => node.Role == "difference-negative"));
    }

    [Fact]
    public void SecondaryLinearScaleAndClippingKeepTheUnclampedCrossing() {
        var chart = Model(new(0, 600, 100), new(1, 100, 500));
        chart.AddLine("Primary", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        chart.Series[0].UseSecondaryYAxis().WithStrokeWidth(1);
        chart.Options.XAxis.WithBounds(.2, .8); chart.Options.YAxis.WithBounds(0, 3);
        chart.Options.SecondaryYAxis.WithBounds(150, 450);
        var prepared = Prepare(chart);
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        var x = first.Commands[0].X + (first.Commands[^1].X - first.Commands[0].X) * 5 / 9;
        var y = first.Commands[0].Y + (first.Commands[^1].Y - first.Commands[0].Y) * 5 / 9;
        foreach (var role in new[] { "difference-positive", "difference-negative" })
            Assert.Contains(Boundary(prepared, role).Commands, point => Math.Abs(point.X - x) < 1e-8 && Math.Abs(point.Y - y) < 1e-8);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, Sample(first, second, .4), Red, 128);
        AssertPaint(image, Sample(first, second, .7), Blue, 128);
        Assert.Equal("secondary", Group(prepared, "series-0-point-0")["data-cfx-axis"]);
        var automatic = Model(new(0, 600, 100), new(1, 100, 500)); automatic.Series[0].UseSecondaryYAxis();
        automatic.AddLine("Primary", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        var primary = ChartRange.FromChart(automatic); var secondary = ChartRange.FromSecondaryYAxis(automatic, primary);
        Assert.True(primary.MaxY < 10); Assert.True(secondary.MaxY >= 600 && secondary.MinY <= 100);
    }

    [Fact]
    public void ExtremeFinitePairsHaveAFiniteCrossingWithoutSubtractingRawValues() {
        var chart = Model(new(0, double.MaxValue, -double.MaxValue), new(1, -double.MaxValue, double.MaxValue));
        // This isolates crossing arithmetic from the separate axis-domain arithmetic contract.
        var ordinary = Model(new(0, 1, 0), new(1, 0, 1));
        ordinary.Options.XAxis.WithBounds(0, 1); ordinary.Options.YAxis.WithBounds(0, 1);
        var mapped = new ChartPairedBoundaryGeometry(ordinary.Series[0], new ChartMapper(new ChartRect(0, 0, 100, 100),
            ChartRange.FromChart(ordinary), ordinary.Options.XAxis, ordinary.Options.YAxis));
        var regions = ChartDifferenceAreaGeometry.Regions(chart.Series[0], mapped);
        Assert.Equal(2, regions.Count);
        Assert.All(regions, region => Assert.Contains(region.Path.Commands, point => point.X == 50 && point.Y == 50));
    }

    private static VisualScenePath Boundary(PreparedVisual prepared, string role) =>
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == role);
    private static ChartPoint Sample(VisualScenePath first, VisualScenePath second, double fraction) => new(
        first.Commands[0].X * (1 - fraction) + first.Commands[^1].X * fraction,
        (first.Commands[0].Y + second.Commands[0].Y) / 2 * (1 - fraction)
            + (first.Commands[^1].Y + second.Commands[^1].Y) / 2 * fraction);
    private static ChartColor Pixel(RgbaImage image, double x, double y) {
        var offset = ((int)Math.Round(y) * image.Width + (int)Math.Round(x)) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }
    private static void AssertPaint(RgbaImage image, ChartPoint point, ChartColor color, int alpha) {
        var actual = Pixel(image, point.X, point.Y);
        Assert.Equal(color.R, actual.R); Assert.Equal(color.G, actual.G); Assert.Equal(color.B, actual.B); Assert.Equal(alpha, actual.A);
    }
}
