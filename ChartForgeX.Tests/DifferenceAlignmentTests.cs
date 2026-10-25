using System.Collections;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class DifferenceAlignmentTests {
    [Theory]
    [InlineData(ChartMissingDataPolicy.Gap, false, true)]
    [InlineData(ChartMissingDataPolicy.Connect, false, false)]
    [InlineData(ChartMissingDataPolicy.Gap, true, true)]
    [InlineData(ChartMissingDataPolicy.Connect, true, true)]
    public void ExactMergeRetainsUnmatchedRawSourcesIdsAndExplicitBreaks(ChartMissingDataPolicy policy, bool sourceBreak, bool expectedSplit) {
        var first = Source("actual", new[] { new ChartPoint(1, 12), new ChartPoint(2, 15, sourceBreak), new ChartPoint(4, 8), new ChartPoint(6, 18) });
        var second = Source("forecast", new[] { new ChartPoint(1, 10), new ChartPoint(3, 14).WithColorValue(-7), new ChartPoint(4, 11), new ChartPoint(6, 16) });
        var alignment = ChartDifferencePoints.Align(first, second, policy);
        Assert.Equal(new[] { 1d, 4, 6 }, alignment.Points.Select(point => point.X));
        Assert.Equal(new[] { 12d, 8, 18 }, alignment.Points.Select(point => point.First));
        Assert.Equal(new[] { 10d, 11, 16 }, alignment.Points.Select(point => point.Second));
        Assert.Equal(expectedSplit, alignment.Points[1].BreakBefore); Assert.False(alignment.Points[2].BreakBefore);
        Assert.Equal(new[] { 0, 2, 3 }, alignment.FirstSourceIndices); Assert.Equal(new[] { 0, 2, 3 }, alignment.SecondSourceIndices);
        Assert.Equal(4, alignment.FirstPoints.Count); Assert.Equal(4, alignment.SecondPoints.Count);
        Assert.Equal(-7, alignment.SecondPoints[1].ColorValue); Assert.Equal(sourceBreak, alignment.FirstPoints[1].BreakBefore);
        Assert.Equal("actual-2", alignment.FirstObservationIds[2].Value); Assert.Equal("forecast-2", alignment.SecondObservationIds[2].Value);
        Assert.Throws<NotSupportedException>(() => ((IList)alignment.FirstPoints).Clear());
        Assert.Throws<NotSupportedException>(() => ((IDictionary<int, ChartObservationId>)alignment.FirstObservationIds).Clear());
        var series = ChartSeries.CreateAlignedDifferenceArea(new ChartSeriesId("comparison"), "Comparison", alignment);
        Assert.Empty(series.ObservationIds);
        var prepared = Prepare(Chart.Create().WithSize(640, 420).AddSeries(series));
        var facts = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Id == "series-0-point-1").Metadata;
        Assert.Equal("actual", facts["data-cfx-first-source-series-id"]); Assert.Equal("2", facts["data-cfx-first-source-index"]);
        Assert.Equal("forecast-2", facts["data-cfx-second-source-observation-id"]);
        Assert.False(facts.ContainsKey("data-cfx-observation-id"));
        first.Points.Clear(); second.Points[0] = new ChartPoint(1, 999); second.WithName("Changed");
        Assert.Equal("forecast", alignment.SecondName); Assert.Equal(10, alignment.SecondPoints[0].Y); Assert.Equal(4, alignment.FirstPoints.Count);
    }

    [Fact]
    public void ReAlignmentIsTheUpdateRouteAndOrdinaryReplacementExplicitlyLeavesDerivedMode() {
        var first = Source("actual", new[] { new ChartPoint(1, 12), new ChartPoint(2, 8) });
        var second = Source("forecast", new[] { new ChartPoint(1, 10), new ChartPoint(2, 11) });
        var alignment = ChartDifferencePoints.Align(first, second);
        var series = ChartSeries.CreateAlignedDifferenceArea(new ChartSeriesId("comparison"), "Comparison", alignment)
            .WithObservationIds(new[] { new ChartObservationId("pair-one"), new ChartObservationId("pair-two") });
        var chart = Chart.Create().WithSize(640, 420).AddSeries(series); var before = Prepare(chart); var svg = before.ToSvg(); var png = before.ToPng();
        Assert.Throws<NotSupportedException>(() => series.UpdateDifferencePoint(0, new ChartDifferencePoint(1, 100, 1)));
        Assert.Throws<NotSupportedException>(() => series.ApplyDifferenceChanges(new[] { ChartObservationChange<ChartDifferencePoint>.Remove(new ChartObservationId("pair-one")) }));
        Assert.Same(alignment, series.DifferenceAlignment); Assert.Equal(2, series.DifferencePoints.Count);
        IEnumerable<ChartDifferencePoint> Broken() { yield return new ChartDifferencePoint(1, 100, 2); throw new InvalidOperationException("intake failed"); }
        Assert.Throws<InvalidOperationException>(() => series.ReplaceDifferencePoints(Broken()));
        Assert.Same(alignment, series.DifferenceAlignment); Assert.Equal(svg, Prepare(chart).ToSvg());
        first.Points[0] = new ChartPoint(1, 25); first.WithName("Observed");
        var current = ChartDifferencePoints.Align(first, second); series.ReplaceDifferenceAlignment(current);
        Assert.Empty(series.ObservationIds); Assert.Equal("Observed", series.DifferenceArea.FirstName); Assert.Equal(25, series.DifferencePoints[0].First);
        Assert.Equal(12, alignment.Points[0].First); Assert.Equal(svg, before.ToSvg()); Assert.Equal(png, before.ToPng());
        series.ReplaceDifferencePoints(new[] { new ChartDifferencePoint(1, 3, 2) }); Assert.Null(series.DifferenceAlignment);
        series.UpdateDifferencePoint(0, new ChartDifferencePoint(1, 4, 2)); Assert.Equal(4, series.DifferencePoints[0].First);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyOrDisjointInputsKeepCompleteCapturesWithoutInventingPairs(bool empty) {
        var first = Source("actual", empty ? Array.Empty<ChartPoint>() : new[] { new ChartPoint(1, 2) });
        var second = Source("forecast", new[] { new ChartPoint(2, 3) });
        var alignment = ChartDifferencePoints.Align(first, second);
        Assert.Empty(alignment.Points); Assert.Empty(alignment.FirstSourceIndices); Assert.Empty(alignment.SecondSourceIndices);
        Assert.Equal(empty ? 0 : 1, alignment.FirstPoints.Count); Assert.Single(alignment.SecondPoints);
        var chart = Chart.Create().AddAlignedDifferenceArea("Comparison", alignment);
        Assert.NotEmpty(Prepare(chart).ToPng()); Assert.Same(alignment, chart.Series[0].DifferenceAlignment);
    }

    [Fact]
    public void AmbiguousIntakeAndCancellationDoNotMutateSourceOrDestinationAndTupleRestartsRemainValid() {
        var first = Source("actual", new[] { new ChartPoint(1, 3), new ChartPoint(2, 4) });
        var second = Source("forecast", new[] { new ChartPoint(1, 5), new ChartPoint(2, 2) });
        var existing = Chart.Create().AddLine("Reference", new[] { new ChartPoint(1, 1) }); var original = existing.Series[0];
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => ChartDifferencePoints.Align(first, second, cancellationToken: cancellation.Token));
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartDifferencePoints.Align(first, second, (ChartMissingDataPolicy)99));
        Assert.Throws<ArgumentNullException>(() => existing.AddAlignedDifferenceArea("Invalid", null!));
        first.Points[1] = new ChartPoint(1, 4, true);
        Assert.Throws<ArgumentException>(() => ChartDifferencePoints.Align(first, second));
        first.Points[1] = new ChartPoint(0, 4, true);
        Assert.Throws<ArgumentException>(() => ChartDifferencePoints.Align(first, second));
        Assert.Same(original, Assert.Single(existing.Series)); Assert.Equal(5, second.Points[0].Y);
        var tuples = Chart.Create().AddDifferenceArea("Explicit restarts", new[] { new ChartDifferencePoint(1, 3, 5), new ChartDifferencePoint(1, 4, 2, true) });
        Assert.Equal(2, Prepare(tuples).Regions.Count(region => region.Role == "point"));
        var scatter = ChartSeries.CreateScatter(new ChartSeriesId("scatter"), "Scatter", new[] { new ChartPoint(1, 1) });
        Assert.Throws<NotSupportedException>(() => ChartDifferencePoints.Align(scatter, second));
    }

    [Fact]
    public void CapturedInterpolationAndMissingIdentityNeverBecomeFabricatedPairsOrIDs() {
        var first = ChartSeries.CreateLine(new ChartSeriesId("a"), "", new[] { new ChartPoint(1, 3), new ChartPoint(2, 7) }).WithInterpolation(ChartInterpolation.Smooth);
        var second = new ChartSeries(new ChartSeriesId("b"), "Forecast", ChartSeriesKind.StepLine, new[] { new ChartPoint(1, 5), new ChartPoint(2, 2) });
        var aligned = ChartDifferencePoints.Align(first, second);
        var series = ChartSeries.CreateAlignedDifferenceArea(new ChartSeriesId("result"), "Result", aligned);
        Assert.Equal(ChartInterpolation.Smooth, series.FirstBoundaryInterpolation!.Value.Interpolation);
        Assert.Equal(ChartInterpolation.Step, series.SecondBoundaryInterpolation!.Value.Interpolation);
        Assert.Empty(aligned.FirstObservationIds); Assert.Empty(aligned.SecondObservationIds); Assert.Equal("", aligned.FirstName);
        Assert.Equal("First", series.DifferenceArea.FirstName);
        var svg = Prepare(Chart.Create().AddSeries(series)).ToSvg();
        Assert.DoesNotContain("data-cfx-first-source-observation-id", svg); Assert.DoesNotContain("data-cfx-second-source-observation-id", svg);
    }
    [Fact]
    public void AdjacentFloatingCoordinatesAreNotRoundedOrJoinedByCategoryText() {
        var next = Math.BitIncrement(1d);
        var first = Source("a", new[] { new ChartPoint(1, 3), new ChartPoint(2, 4) });
        var second = Source("b", new[] { new ChartPoint(next, 5), new ChartPoint(2, 2) });
        var aligned = ChartDifferencePoints.Align(first, second);
        Assert.Single(aligned.Points); Assert.Equal(2, aligned.Points[0].X);
        Assert.Equal(1, aligned.FirstSourceIndices[0]); Assert.Equal(1, aligned.SecondSourceIndices[0]);
        Assert.Equal(1, aligned.FirstPoints[0].X); Assert.Equal(next, aligned.SecondPoints[0].X);
    }

    internal static ChartSeries Source(string id, IEnumerable<ChartPoint> points) {
        var series = ChartSeries.CreateLine(new ChartSeriesId(id), id, points);
        return series.WithObservationIds(Enumerable.Range(0, series.Points.Count).Select(index => new ChartObservationId(id + "-" + index)));
    }
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
