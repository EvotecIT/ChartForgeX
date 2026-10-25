using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class DifferenceAreaTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void IndependentSmoothAndStepFillsFollowPaintedBoundariesAndSourcePolarity(bool reverseX, bool reverseY) {
        var chart = Model(new(0, 8, 3), new(1, 2, 6), new(10, 8, 2), new(11, 2, 7));
        chart.Options.XAxis.WithBounds(0, 11).WithReversal(reverseX); chart.Options.YAxis.WithBounds(0, 10).WithReversal(reverseY);
        chart.Series[0].WithBoundaryInterpolation(new(ChartInterpolation.Smooth), new(ChartInterpolation.Step, ChartStepPosition.Middle))
            .ConfigureMarkers(markers => markers.Enabled = false);
        var prepared = Prepare(chart);
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        Assert.Contains(first.Commands, command => command.Kind == ChartPathCommandKind.CubicTo);
        Assert.DoesNotContain(second.Commands, command => command.Kind == ChartPathCommandKind.CubicTo);
        var a = new ChartPath(first.Commands).Flatten(12); var b = new ChartPath(second.Commands).Flatten(12);
        if (reverseX) { a.Reverse(); b.Reverse(); }
        var captures = Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var path = Path.Combine(captures, "difference-independent-smooth-x" + reverseX + "-y" + reverseY);
            File.WriteAllText(path + ".svg", prepared.ToSvg()); File.WriteAllBytes(path + ".png", prepared.ToPng());
        }
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)); var probes = 0;
        var jumps = b.Zip(b.Skip(1), (left, right) => (left, right))
            .Where(pair => pair.left.X == pair.right.X && pair.left.Y != pair.right.Y).Select(pair => pair.left.X).Distinct().ToArray();
        // A vertical step is painted boundary ink, not an interior fill sample. Check both adjacent interiors instead.
        var positions = Enumerable.Range(1, 31).Select(index => a[0].X + (a[^1].X - a[0].X) * index / 32)
            .Where(x => jumps.All(jump => Math.Abs(x - jump) > 2))
            .Concat(jumps.SelectMany(jump => new[] { jump - 2, jump + 2 }))
            .Where(x => x > a[0].X && x < a[^1].X).Distinct();
        foreach (var x in positions) {
            var ay = At(a, x); var by = At(b, x);
            if (Math.Abs(ay - by) < 12) continue;
            var expected = (ay < by) != reverseY ? Red : Blue;
            AssertPaint(image, new ChartPoint(x, ay / 2 + by / 2), expected, 128); probes++;
        }
        Assert.True(probes > 10);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "point"));
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-positive");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        Assert.DoesNotContain("NaN", prepared.ToSvg()); Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void SeriesSmoothAndExplicitBreaksUseTheSameDetachedNativeCurves() {
        var chart = Model(new(0, 8, 3), new(1, 2, 6), new(10, 8, 2), new(11, 2, 7), new(13, 1, 8, true));
        chart.Series[0].WithInterpolation(ChartInterpolation.Smooth);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal(2, Boundary(prepared, "difference-first").Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(2, Boundary(prepared, "difference-second").Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Contains(Boundary(prepared, "difference-first").Commands, command => command.Kind == ChartPathCommandKind.CubicTo);
        chart.Series[0].WithBoundaryInterpolation(new(ChartInterpolation.Step), null).ReplaceDifferencePoints(Array.Empty<ChartDifferencePoint>());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    private static double At(IReadOnlyList<ChartPoint> points, double x) {
        for (var item = 1; item < points.Count; item++) {
            if (points[item].X <= points[item - 1].X || x > points[item].X) continue;
            var fraction = (x - points[item - 1].X) / (points[item].X - points[item - 1].X);
            return points[item - 1].Y * (1 - fraction) + points[item].Y * fraction;
        }
        throw new InvalidOperationException("No painted edge at probe X.");
    }
}
