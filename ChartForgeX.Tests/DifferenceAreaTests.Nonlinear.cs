using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class DifferenceAreaTests {
    [Theory]
    [InlineData(ChartScaleKind.Logarithmic, false, false, false, false)]
    [InlineData(ChartScaleKind.Logarithmic, false, true, false, false)]
    [InlineData(ChartScaleKind.Logarithmic, false, false, true, false)]
    [InlineData(ChartScaleKind.Logarithmic, false, true, true, true)]
    [InlineData(ChartScaleKind.Logarithmic, true, false, false, false)]
    [InlineData(ChartScaleKind.Logarithmic, true, true, true, true)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false, false, false, false)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic, false, true, true, true)]
    public void NonlinearCrossingsFollowActuallyPaintedNativeBoundariesAndRawPolarity(ChartScaleKind scale, bool negative, bool reverseX, bool reverseY, bool secondary) {
        var pairs = scale == ChartScaleKind.SymmetricLogarithmic ? new[] { new ChartDifferencePoint(0, -100, -10), new ChartDifferencePoint(1, 100, 2) }
            : negative ? new[] { new ChartDifferencePoint(0, -100, -10), new ChartDifferencePoint(1, -1, -2) }
            : new[] { new ChartDifferencePoint(0, 1, 10), new ChartDifferencePoint(1, 100, 2) };
        var chart = Model(pairs); if (secondary) chart.Series[0].UseSecondaryValueAxis();
        var axis = secondary ? chart.Options.SecondaryYAxis : chart.Options.YAxis;
        if (scale == ChartScaleKind.Logarithmic) axis.WithLogarithmicScale(domain: negative ? ChartLogarithmicDomain.Negative : ChartLogarithmicDomain.Positive);
        else { axis.Scale = scale; axis.SymmetricLogarithmThreshold = 1; }
        axis.WithBounds(scale == ChartScaleKind.SymmetricLogarithmic || negative ? -100 : 1, negative ? -1 : 100).WithReversal(reverseY);
        chart.Options.XAxis.WithBounds(0, 1).WithReversal(reverseX);
        var prepared = Prepare(chart); var a = Boundary(prepared, "difference-first"); var b = Boundary(prepared, "difference-second");
        var d0 = a.Commands[0].Y - b.Commands[0].Y; var d1 = a.Commands[^1].Y - b.Commands[^1].Y;
        var fraction = -d0 / (d1 - d0);
        var x = a.Commands[0].X * (1 - fraction) + a.Commands[^1].X * fraction;
        var y = a.Commands[0].Y * (1 - fraction) + a.Commands[^1].Y * fraction;
        Assert.InRange(fraction, .01, .99);
        var rawFraction = Math.Abs(pairs[0].First - pairs[0].Second)
            / (Math.Abs(pairs[0].First - pairs[0].Second) + Math.Abs(pairs[1].First - pairs[1].Second));
        Assert.True(Math.Abs(rawFraction - fraction) > .03, "The fixture must distinguish transformed-path crossing from raw-value interpolation.");
        foreach (var role in new[] { "difference-positive", "difference-negative" })
            Assert.Contains(Boundary(prepared, role).Commands, point => Math.Abs(point.X - x) < 1e-7 && Math.Abs(point.Y - y) < 1e-7);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, Sample(a, b, fraction / 2), pairs[0].First > pairs[0].Second ? Red : Blue, 128);
        AssertPaint(image, Sample(a, b, (1 + fraction) / 2), pairs[1].First > pairs[1].Second ? Red : Blue, 128);
        Assert.Equal(pairs[0].First.ToString("R", System.Globalization.CultureInfo.InvariantCulture), Group(prepared, "series-0-point-0")["data-cfx-first"]);
        ScatterBubbleColorRenderingTests.Capture(prepared, $"difference-nonlinear-{scale}-{negative}-{reverseX}-{reverseY}-{secondary}");
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic)]
    public void IndependentSmoothAndHeldNonlinearBoundariesKeepTheExistingPaintedContourOwner(ChartScaleKind scale) {
        var chart = Model(new(1, 2, 20), new(2, 80, 10), new(4, 4, 50), new(8, 90, 8));
        chart.Options.YAxis.Scale = scale; chart.Options.YAxis.WithBounds(1, 100).WithReversal();
        chart.Series[0].WithBoundaryInterpolation(new(ChartInterpolation.Smooth), new(ChartInterpolation.Step, ChartStepPosition.Middle))
            .ConfigureMarkers(markers => markers.Enabled = false);
        var prepared = Prepare(chart);
        Assert.Contains(Boundary(prepared, "difference-first").Commands, point => point.Kind == ChartPathCommandKind.CubicTo);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-positive"); Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        Assert.DoesNotContain("NaN", prepared.ToSvg());
        var first = new ChartPath(Boundary(prepared, "difference-first").Commands).Flatten(12);
        var second = new ChartPath(Boundary(prepared, "difference-second").Commands).Flatten(12);
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1)); var probes = 0;
        var jumps = second.Zip(second.Skip(1), (a, b) => (a, b)).Where(pair => pair.a.X == pair.b.X).Select(pair => pair.a.X).ToArray();
        for (var index = 1; index < 32; index++) {
            var x = first[0].X + (first[^1].X - first[0].X) * index / 32;
            if (jumps.Any(jump => Math.Abs(jump - x) < 2)) continue;
            var a = At(first, x); var b = At(second, x); if (Math.Abs(a - b) < 14) continue;
            AssertPaint(image, new ChartPoint(x, a / 2 + b / 2), a > b ? Red : Blue, 128); probes++;
        }
        Assert.True(probes > 5); Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NonlinearAuthoredClippingRetainsUnclampedBoundariesAndCompleteOutsideSourceFacts() {
        var chart = Model(new(0, 1, 10), new(1, 100, 2));
        chart.Options.YAxis.WithLogarithmicScale().WithBounds(2, 80);
        var prepared = Prepare(chart); var a = Boundary(prepared, "difference-first"); var b = Boundary(prepared, "difference-second");
        var d0 = a.Commands[0].Y - b.Commands[0].Y; var d1 = a.Commands[^1].Y - b.Commands[^1].Y; var fraction = -d0 / (d1 - d0);
        var x = a.Commands[0].X * (1 - fraction) + a.Commands[^1].X * fraction;
        var y = a.Commands[0].Y * (1 - fraction) + a.Commands[^1].Y * fraction;
        foreach (var role in new[] { "difference-positive", "difference-negative" })
            Assert.Contains(Boundary(prepared, role).Commands, point => Math.Abs(point.X - x) < 1e-7 && Math.Abs(point.Y - y) < 1e-7);
        Assert.Equal("1", Group(prepared, "series-0-point-0")["data-cfx-first"]); Assert.Equal("100", Group(prepared, "series-0-point-1")["data-cfx-first"]);
        Assert.NotEmpty(prepared.ToPng());
        chart.Options.YAxis.Scale = ChartScaleKind.Time;
        Assert.Throws<NotSupportedException>(() => Prepare(chart));
    }

    [Theory]
    [InlineData(ChartScaleKind.Logarithmic)]
    [InlineData(ChartScaleKind.SymmetricLogarithmic)]
    public void NonlinearXStillUsesExactAuthoredCoordinatesAndMappedStraightBoundaries(ChartScaleKind scale) {
        var chart = Model(new(1, 2, 20), new(100, 80, 10)); chart.Options.XAxis.Scale = scale;
        var prepared = Prepare(chart); Assert.Equal("1", Group(prepared, "series-0-point-0")["data-cfx-x"]);
        Assert.Equal("100", Group(prepared, "series-0-point-1")["data-cfx-x"]);
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-positive"); Assert.Contains(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        Assert.NotEmpty(prepared.ToPng());
    }
}
