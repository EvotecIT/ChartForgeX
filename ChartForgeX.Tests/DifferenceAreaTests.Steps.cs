using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class DifferenceAreaTests {
    [Theory]
    [InlineData(ChartStepPosition.Start)]
    [InlineData(ChartStepPosition.Middle)]
    [InlineData(ChartStepPosition.End)]
    public void StepHoldsUseDeclaredTransitionsAndRawPolarityInBothNativeExports(ChartStepPosition position) {
        var chart = Model(new(0, 8, 2), new(10, 2, 5));
        chart.Options.XAxis.WithBounds(0, 10); chart.Options.YAxis.WithBounds(0, 10);
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, position);
        var prepared = Prepare(chart);
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        var start = first.Commands[0]; var end = first.Commands[^1];
        var transition = position == ChartStepPosition.Start ? start.X
            : position == ChartStepPosition.End ? end.X : start.X / 2 + end.X / 2;
        AssertStepTransition(first, transition); AssertStepTransition(second, transition);
        var fills = prepared.Scene.Nodes.OfType<VisualScenePath>()
            .Where(path => path.Role is "difference-positive" or "difference-negative").ToArray();
        Assert.Equal(position == ChartStepPosition.Middle ? 2 : 1, fills.Length);
        Assert.All(fills, fill => { Assert.True(fill.Close); Assert.Null(fill.Stroke); AssertAxisAligned(fill); });
        if (position != ChartStepPosition.End) {
            var negative = Assert.Single(fills, path => path.Role == "difference-negative");
            Assert.Equal(transition, negative.Commands.Min(point => point.X), 8);
            Assert.Equal(end.X, negative.Commands.Max(point => point.X), 8);
        }
        if (position != ChartStepPosition.Start) {
            var positive = Assert.Single(fills, path => path.Role == "difference-positive");
            Assert.Equal(start.X, positive.Commands.Min(point => point.X), 8);
            Assert.Equal(transition, positive.Commands.Max(point => point.X), 8);
        }
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var fraction in new[] { .25, .75 }) {
            var previous = position == ChartStepPosition.End || position == ChartStepPosition.Middle && fraction < .5;
            var y = previous ? (start.Y + second.Commands[0].Y) / 2 : (end.Y + second.Commands[^1].Y) / 2;
            AssertPaint(image, new ChartPoint(start.X * (1 - fraction) + end.X * fraction, y), previous ? Red : Blue, 128);
        }
        var svg = XDocument.Parse(prepared.ToSvg());
        foreach (var fill in fills)
            Assert.Single(svg.Descendants(), element => (string?)element.Attribute("data-cfx-role") == fill.Role);
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point"));
    }

    [Fact]
    public void SameSignStepHoldsKeepVerticalKnotsAndOneTranslucentEnvelope() {
        var chart = Model(new(0, 4, 1), new(1, 8, 5), new(2, 6, 2));
        chart.Options.XAxis.WithBounds(0, 2); chart.Options.YAxis.WithBounds(0, 10);
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        var prepared = Prepare(chart);
        var fill = Boundary(prepared, "difference-positive");
        AssertAxisAligned(fill);
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        foreach (var role in new[] { "difference-first", "difference-second" }) {
            var boundary = Boundary(prepared, role);
            // Every boundary knot belongs to a positive-width hold in this Middle fixture, including both ends of each jump.
            foreach (var knot in boundary.Commands)
                Assert.Contains(fill.Commands, point => point.X == knot.X && point.Y == knot.Y);
        }
        var middle = prepared.Regions.Single(region => region.Id == "series-0-point-1").Bounds;
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, new ChartPoint(middle.X, middle.Y + middle.Height / 2), Red, 128);
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        AssertPaint(image, new ChartPoint(first.Commands[0].X * .875 + first.Commands[^1].X * .125,
            (first.Commands[0].Y + second.Commands[0].Y) / 2), Red, 128);
    }

    [Theory]
    [InlineData(ChartStepPosition.Start)]
    [InlineData(ChartStepPosition.Middle)]
    [InlineData(ChartStepPosition.End)]
    public void TiedStepHoldsCutFillWithoutInventingTerminalOrSingletonRegions(ChartStepPosition position) {
        var chart = Model(new(0, 8, 2), new(1, 8, 2), new(2, 4, 4), new(3, 4, 4), new(4, 8, 2), new(5, 8, 2));
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, position);
        var prepared = Prepare(chart);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "difference-positive"));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "difference-negative");
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(0, Pixel(image, first.Commands[0].X / 2 + first.Commands[^1].X / 2,
            first.Commands[0].Y / 2 + second.Commands[0].Y / 2).A);
        Assert.Equal(6, prepared.Regions.Count(region => region.Role == "point"));
        Assert.Equal("4", Group(prepared, "series-0-point-2")["data-cfx-first"]);
        foreach (var empty in new[] { Model(new(0, 4, 4), new(1, 4, 4)), Model(new ChartDifferencePoint(0, 8, 2)) }) {
            empty.Series[0].WithInterpolation(ChartInterpolation.Step, position);
            Assert.DoesNotContain(Prepare(empty).Scene.Nodes, node => node.Role is "difference-positive" or "difference-negative");
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EitherTupleMemberCutsStepBoundariesAndNativeFill(bool breakSecond) {
        var chart = Model(new(0, 8, 2), new(1, 8, 2), new(3, 8, 2), new(4, 8, 2));
        var series = chart.Series[0];
        series.WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        series.Points[4] = new ChartPoint(3, 8, !breakSecond);
        series.Points[5] = new ChartPoint(3, 2, breakSecond);
        var prepared = Prepare(chart);
        foreach (var role in new[] { "difference-first", "difference-second" })
            Assert.Equal(2, Boundary(prepared, role).Commands.Count(command => command.Kind == ChartPathCommandKind.MoveTo));
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role == "difference-positive"));
        var left = prepared.Regions.Single(region => region.Id == "series-0-point-1").Bounds;
        var right = prepared.Regions.Single(region => region.Id == "series-0-point-2").Bounds;
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        Assert.Equal(0, Pixel(image, left.X / 2 + right.X / 2, left.Y + left.Height / 2).A);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "point"));
        var breakFacts = Group(prepared, "series-0-point-2");
        Assert.Equal("true", breakFacts["data-cfx-source-" + (breakSecond ? "1" : "0") + "-break"]);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void TimeStepsUseSecondaryScaleAndMappedTransitionThroughClippingAndReversal(bool reverseX, bool reverseY) {
        var date = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var chart = Model(new(date, 600, 100), new(date.AddDays(10), 100, 500));
        chart.Series[0].UseSecondaryYAxis().WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        chart.AddLine("Primary", new[] { new ChartPoint(date.ToOADate(), 1), new ChartPoint(date.AddDays(10).ToOADate(), 2) });
        chart.Options.XAxis.Scale = ChartScaleKind.Time;
        chart.Options.XAxis.WithBounds(date.AddDays(2).ToOADate(), date.AddDays(8).ToOADate()).WithReversal(reverseX);
        chart.Options.YAxis.WithBounds(0, 3);
        chart.Options.SecondaryYAxis.WithBounds(150, 450).WithReversal(reverseY);
        var prepared = Prepare(chart);
        var first = Boundary(prepared, "difference-first"); var second = Boundary(prepared, "difference-second");
        var transition = first.Commands[0].X / 2 + first.Commands[^1].X / 2;
        AssertStepTransition(first, transition); AssertStepTransition(second, transition);
        Assert.Equal(2, prepared.Scene.Nodes.Count(node => node.Role is "difference-positive" or "difference-negative"));
        foreach (var role in new[] { "difference-positive", "difference-negative" }) AssertAxisAligned(Boundary(prepared, role));
        var image = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        AssertPaint(image, new ChartPoint(first.Commands[0].X * .6 + first.Commands[^1].X * .4,
            first.Commands[0].Y / 2 + second.Commands[0].Y / 2), Red, 128);
        AssertPaint(image, new ChartPoint(first.Commands[0].X * .4 + first.Commands[^1].X * .6,
            first.Commands[^1].Y / 2 + second.Commands[^1].Y / 2), Blue, 128);
        Assert.Equal("secondary", Group(prepared, "series-0-point-0")["data-cfx-axis"]);
        Assert.Equal("600", Group(prepared, "series-0-point-0")["data-cfx-first"]);
        Assert.Equal("500", Group(prepared, "series-0-point-1")["data-cfx-second"]);
    }

    [Fact]
    public void ExtremeFinitePairsUseRawStepPolarityWithoutCrossingArithmetic() {
        var chart = Model(new(0, double.MaxValue, -double.MaxValue), new(1, -double.MaxValue, double.MaxValue));
        chart.Series[0].WithInterpolation(ChartInterpolation.Step, ChartStepPosition.Middle);
        // Ordinary mapped coordinates isolate sign/step arithmetic from the separate axis-domain contract.
        var ordinary = Model(new(0, 1, 0), new(1, 0, 1));
        ordinary.Options.XAxis.WithBounds(0, 1); ordinary.Options.YAxis.WithBounds(0, 1);
        var mapped = new ChartPairedBoundaryGeometry(ordinary.Series[0], new ChartMapper(new ChartRect(0, 0, 100, 100),
            ChartRange.FromChart(ordinary), ordinary.Options.XAxis, ordinary.Options.YAxis));
        var regions = ChartDifferenceAreaGeometry.Regions(chart.Series[0], mapped);
        Assert.Equal(2, regions.Count);
        Assert.True(regions[0].Positive); Assert.False(regions[1].Positive);
        Assert.Equal(50, regions[0].Path.Commands.Max(point => point.X));
        Assert.Equal(50, regions[1].Path.Commands.Min(point => point.X));
        Assert.All(regions, region => Assert.All(region.Path.Commands, point => Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y))));
        var boundary = ChartPathBuilder.FromPoints(new[] { new ChartPoint(-double.MaxValue, 1), new ChartPoint(double.MaxValue, 2) },
            ChartInterpolation.Step, ChartStepPosition.Middle);
        Assert.All(boundary.Commands, point => Assert.True(double.IsFinite(point.X) && double.IsFinite(point.Y)));
        Assert.Contains(boundary.Commands, point => point.X == 0 && point.Y == 2);
    }

    private static void AssertStepTransition(VisualScenePath boundary, double expectedX) {
        AssertAxisAligned(boundary);
        var vertical = Enumerable.Range(1, boundary.Commands.Count - 1)
            .Single(index => boundary.Commands[index].Y != boundary.Commands[index - 1].Y);
        Assert.Equal(expectedX, boundary.Commands[vertical - 1].X, 8);
        Assert.Equal(expectedX, boundary.Commands[vertical].X, 8);
    }

    private static void AssertAxisAligned(VisualScenePath path) {
        for (var index = 1; index < path.Commands.Count; index++) {
            var a = path.Commands[index - 1]; var b = path.Commands[index];
            if (b.Kind != ChartPathCommandKind.MoveTo)
                Assert.True(a.X == b.X || a.Y == b.Y, "Stepped geometry must not invent diagonal edges.");
        }
        if (path.Close) Assert.True(path.Commands[0].X == path.Commands[^1].X || path.Commands[0].Y == path.Commands[^1].Y);
    }
}
