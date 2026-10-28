using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Independent boundary choices and nullable pairs reach native geometry without changing source tuples.</summary>
public sealed class PairedAreaInterpolationTests {
    [Theory]
    [InlineData(ChartSeriesKind.RangeBand)]
    [InlineData(ChartSeriesKind.RangeArea)]
    public void IndependentBoundariesHonorGapsAndPreserveMidlineAndDetachedFacts(ChartSeriesKind kind) {
        var pairs = ChartRangeBands.FromXY(new[] { 0d, 1, 2, 3, 4, 5 }, new double?[] { 1, null, 2, 3, 1, 2 }, new double?[] { 5, 7, null, 8, 9, 6 });
        var series = kind == ChartSeriesKind.RangeArea ? ChartSeries.CreateRangeArea(new ChartSeriesId("range"), "Range", pairs, smooth: false)
            : ChartSeries.CreateRangeBand(new ChartSeriesId("range"), "Range", pairs);
        series.WithBoundaryInterpolation(new(ChartInterpolation.Linear), new(ChartInterpolation.Step, ChartStepPosition.Start))
            .WithObservationId(1, new ChartObservationId("resumed"));
        var chart = Chart.Create().WithAxes(false).WithLegend(false).AddSeries(series);
        var prepared = Prepare(chart);
        var paths = prepared.Scene.Nodes.OfType<VisualScenePath>().ToArray();
        var lower = paths.Where(path => path.Role == "range-lower").ToArray();
        var upper = paths.Where(path => path.Role == "range-upper").ToArray();
        Assert.Equal(2, lower.Length); Assert.Equal(2, upper.Length);
        Assert.All(lower.Concat(upper), path => Assert.Single(path.Commands, command => command.Kind == ChartPathCommandKind.MoveTo));
        // The isolated original pair and the resumed run are distinct painted paths, never a bridge over absent pairs.
        Assert.Equal(lower[0].Commands[0].X, lower[0].Commands[^1].X);
        Assert.Equal(upper[0].Commands[0].X, upper[0].Commands[^1].X);
        Assert.True(lower[1].Commands[0].X > lower[0].Commands[^1].X);
        Assert.Equal(lower[1].Commands[0].X, upper[1].Commands[0].X);
        Assert.Equal(lower[1].Commands[^1].X, upper[1].Commands[^1].X);
        Assert.True(upper[1].Commands.Count > lower[1].Commands.Count);
        var points = XDocument.Parse(prepared.ToSvg()).Descendants().Where(node => (string?)node.Attribute("data-cfx-role") == "point").ToArray();
        Assert.Equal(4, points.Length);
        Assert.Equal("3", (string?)points[1].Attribute("data-cfx-lower")); Assert.Equal("8", (string?)points[1].Attribute("data-cfx-upper"));
        Assert.Equal("resumed", (string?)points[1].Attribute("data-cfx-observation-id"));
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        series.WithBoundaryInterpolation(new(ChartInterpolation.Smooth), null).ReplaceRanges(Array.Empty<ChartRangeBand>());
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Fact]
    public void NullablePairsValidateAllSuppliedSlotsAndCancelBeforeChangingASeries() {
        var x = new[] { 0d, 1, 2, 3 }; var a = new double?[] { 0, null, 4, 2 }; var b = new double?[] { 2, 3, 1, 5 };
        var gap = ChartDifferencePoints.FromXY(x, a, b); var connect = ChartDifferencePoints.FromXY(x, a, b, ChartMissingDataPolicy.Connect);
        Assert.Equal(new[] { 0d, 2, 3 }, gap.Select(point => point.X)); Assert.True(gap[1].BreakBefore); Assert.False(connect[1].BreakBefore);
        Assert.Equal(0, gap[0].First);
        Assert.Throws<ArgumentException>(() => ChartDifferencePoints.FromXY(x, a.Take(2), b));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartDifferencePoints.FromXY(new[] { double.NaN }, new double?[] { null }, new double?[] { 1 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartDifferencePoints.FromXY(new[] { 0d }, new double?[] { null }, new double?[] { double.PositiveInfinity }));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartRangeBands.FromXY(new[] { 0d }, new double?[] { 3 }, new double?[] { 2 }));
        var series = ChartSeries.CreateDifferenceArea(new ChartSeriesId("pair"), "Pair", gap);
        using var cancellation = new CancellationTokenSource();
        IEnumerable<double?> CancelDuringEnumeration() { yield return 1; cancellation.Cancel(); yield return 2; }
        Assert.Throws<OperationCanceledException>(() => series.ReplaceDifferencePoints(ChartDifferencePoints.FromXY(new[] { 0d, 1 },
            CancelDuringEnumeration(), new double?[] { 2, 3 }, cancellationToken: cancellation.Token)));
        Assert.Equal(3, series.DifferencePoints.Count); Assert.True(series.DifferencePoints[1].BreakBefore);
        Assert.Empty(ChartDifferencePoints.FromXY(Array.Empty<double>(), Array.Empty<double?>(), Array.Empty<double?>()));
    }

    [Fact]
    public void InheritedDefaultsAndInvalidIndependentPoliciesHaveClearBoundaries() {
        Assert.Equal(ChartStepPosition.End, default(ChartBoundaryInterpolation).StepPosition);
        Assert.Throws<ArgumentException>(() => new ChartBoundaryInterpolation(ChartInterpolation.Smooth, ChartStepPosition.Middle));
        var line = Chart.Create().AddLine("Line", new[] { new ChartPoint(0, 1) });
        line.Series[0].FirstBoundaryInterpolation = new(ChartInterpolation.Smooth);
        Assert.Throws<InvalidOperationException>(() => Prepare(line));
        var range = Chart.Create().AddRangeArea("Range", new[] { new ChartRangeBand(0, 1, 5), new ChartRangeBand(1, 2, 6), new ChartRangeBand(3, 0, 8) });
        Assert.Equal(ChartInterpolation.Smooth, range.Series[0].Interpolation);
        Assert.Null(range.Series[0].FirstBoundaryInterpolation); Assert.Null(range.Series[0].SecondBoundaryInterpolation);
        Assert.Contains(Prepare(range).Scene.Nodes.OfType<VisualScenePath>().Single(path => path.Role == "range-upper").Commands,
            command => command.Kind == ChartPathCommandKind.CubicTo);
    }
    [Theory]
    [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.DifferenceArea)]
    public void VisibleCategoryFitUsesIndependentBoundaryOnItsOwnPhysicalValueAxis(ChartSeriesKind kind) {
        var series = kind == ChartSeriesKind.RangeArea
            ? ChartSeries.CreateRangeArea(new ChartSeriesId("pair"), "Pair", new[] { new ChartRangeBand(0, 10, 30), new ChartRangeBand(10, 110, 130) }, smooth: false)
            : ChartSeries.CreateDifferenceArea(new ChartSeriesId("pair"), "Pair", new[] { new ChartDifferencePoint(0, 10, 30), new ChartDifferencePoint(10, 110, 130) });
        series.UseSecondaryValueAxis().WithBoundaryInterpolation(null, new(ChartInterpolation.Step, ChartStepPosition.End));
        var chart = Chart.Create().WithLegend(false).AddSeries(series)
            .AddLine("Primary reference", new[] { new ChartPoint(0, 1000), new ChartPoint(10, 2000) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context,
            new ChartCartesianViewport(x: new ChartAxisWindow(4, 6), valueDomain: ChartValueDomain.VisibleCategories));
        Assert.InRange(prepared.SecondaryYProjection!.Minimum, 25, 30);
        Assert.InRange(prepared.SecondaryYProjection.Maximum, 70, 76);
        Assert.True(prepared.YProjection!.Minimum > 1000);
        Assert.Equal(4, prepared.Regions.Count(region => region.Role == "point"));
        Assert.NotEmpty(prepared.ToPng());
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
