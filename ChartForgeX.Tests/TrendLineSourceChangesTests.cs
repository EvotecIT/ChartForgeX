using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TrendLineSourceChangesTests {
    [Fact]
    public void CompleteReplacementRefitsSameSeriesCapturesSourceAndKeepsPreparedExportsDetached() {
        var series = Series().WithPointLabel(1, "Derived end");
        var chart = Chart.Create().AddSeries(series); var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var replacement = new[] { new ChartPoint(0, 10), new ChartPoint(1, 12), new ChartPoint(2, 18), new ChartPoint(3, 28) };
        Assert.Same(series, series.ReplaceTrendLineSourcePoints(replacement));
        Assert.Equal(new ChartSeriesId("retained"), series.Id); Assert.Empty(series.ObservationIds); Assert.Equal(4, series.SourcePointCount);
        Assert.Equal(replacement, series.TrendLineSourcePoints); Assert.Equal("Derived end", series.PointLabels[1]); Assert.Equal(2, series.Points.Count);
        Assert.Equal(10, series.Points[0].Y, 10); Assert.Equal(28, series.Points[1].Y, 10);
        replacement[0] = new ChartPoint(99, 99); Assert.Equal(0, series.TrendLineSourcePoints[0].X);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng()); Assert.NotEqual(svg, chart.ToSvg());
        Assert.Throws<NotSupportedException>(() => series.Points.Clear());
    }

    [Fact]
    public void KeyedChangesAndRollingRetentionPreserveRawIdsWithoutRemappingDerivedEndpointPresentation() {
        var series = Series().WithPointLabel(0, "Start handle").WithPointLabel(1, "End handle");
        series.ApplyTrendLineSourceChanges(new[] {
            ChartObservationChange<ChartPoint>.Remove(Id("a")),
            ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(1, 1).WithColorValue(-7)),
            ChartObservationChange<ChartPoint>.Append(Id("d"), new ChartPoint(3, 9))
        });
        Assert.Equal(new[] { "b", "c", "d" }, series.ObservationIds.OrderBy(pair => pair.Key).Select(pair => pair.Value.Value));
        Assert.Equal(-7, series.TrendLineSourcePoints[0].ColorValue);
        Assert.Equal(new[] { "Start handle", "End handle" }, series.PointLabels);
        Assert.All(series.TrendLineSamples, sample => Assert.Equal(sample.X * sample.X, sample.Value, 10));
        series.ApplyTrendLineSourceChanges(new[] { ChartObservationChange<ChartPoint>.Append(Id("e"), new ChartPoint(4, 16)) }, maximumObservations: 3);
        Assert.Equal(new[] { "c", "d", "e" }, series.ObservationIds.OrderBy(pair => pair.Key).Select(pair => pair.Value.Value));
        Assert.Equal(new[] { 2d, 3d, 4d }, series.TrendLineSourcePoints.Select(point => point.X));
        Assert.Equal(new[] { "Start handle", "End handle" }, series.PointLabels); Assert.Equal(2, series.Points.Count);
    }

    [Fact]
    public void RankInvalidThrowingOversizedAndCanceledChangesRollBackSourceFitConfigurationAndIds() {
        var series = Series(); var chart = Chart.Create().AddSeries(series); var before = chart.ToSvg();
        var source = series.TrendLineSourcePoints.ToArray(); var samples = series.TrendLineSamples.ToArray();
        var ids = series.ObservationIds.OrderBy(pair => pair.Key).ToArray();
        Assert.Throws<ArgumentException>(() => series.ApplyTrendLineSourceChanges(new[] { ChartObservationChange<ChartPoint>.Remove(Id("a")) }));
        Assert.Throws<ArgumentException>(() => series.ApplyTrendLineSourceChanges(new[] { ChartObservationChange<ChartPoint>.Append(Id("a"), new ChartPoint(4, 16)) }));
        Assert.Throws<ArgumentException>(() => series.ApplyTrendLineSourceChanges(new[] {
            ChartObservationChange<ChartPoint>.Replace(Id("a"), new ChartPoint(0, 0)),
            ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(1, 1)) }, maximumChanges: 1));
        Assert.Throws<InvalidOperationException>(() => series.ReplaceTrendLineSourcePoints(ThrowingSource()));
        Assert.Throws<OperationCanceledException>(() => series.ReplaceTrendLineSourcePoints(source, new CancellationToken(true)));
        Assert.Throws<OperationCanceledException>(() => series.ApplyTrendLineSourceChanges(Array.Empty<ChartObservationChange<ChartPoint>>(), cancellationToken: new CancellationToken(true)));
        Assert.Throws<OperationCanceledException>(() => series.ConfigureTrendLine(options => options.Model = ChartTrendLineModel.Power, new CancellationToken(true)));
        Assert.Equal(source, series.TrendLineSourcePoints); Assert.Equal(samples, series.TrendLineSamples);
        Assert.Equal(ids, series.ObservationIds.OrderBy(pair => pair.Key)); Assert.Equal(before, chart.ToSvg());
        Assert.Equal(ChartTrendLineModel.Polynomial, series.TrendLineConfiguration!.Model);
    }

    [Fact]
    public void ChartConfigurationOverloadValidatesTheCompleteCandidateBeforeAppendingIt() {
        var chart = Chart.Create().AddLine("Retained", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        Assert.Throws<ArgumentException>(() => chart.AddTrendLine("Invalid positive domain", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) },
            null, options => options.Model = ChartTrendLineModel.Logarithmic));
        Assert.Single(chart.Series);
        chart.AddTrendLine("Log model", new[] { new ChartPoint(1, 2), new ChartPoint(2, 3), new ChartPoint(4, 4) },
            null, options => options.Model = ChartTrendLineModel.Logarithmic);
        Assert.Equal(2, chart.Series.Count); Assert.Equal(ChartTrendLineModel.Logarithmic, chart.Series[1].TrendLineConfiguration!.Model);
    }

    private static ChartSeries Series() => ChartSeries.CreateTrendLine(new ChartSeriesId("retained"), "Retained quadratic",
        new[] { new ChartPoint(0, 0), new ChartPoint(1, 1), new ChartPoint(2, 4) })
        .ConfigureTrendLine(options => options.Model = ChartTrendLineModel.Polynomial)
        .WithObservationIds(new[] { Id("a"), Id("b"), Id("c") });

    private static IEnumerable<ChartPoint> ThrowingSource() {
        yield return new ChartPoint(1, 2); yield return new ChartPoint(2, 4);
        throw new InvalidOperationException("Source acquisition failed.");
    }

    private static ChartObservationId Id(string value) => new(value);
}
