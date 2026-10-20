using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects numeric callers of the shared value transaction and its capture-before-commit boundary.</summary>
public sealed class NumericObservationChangeApiTests {
    [Fact]
    public void GenericPointPayloadCapturesCompleteValuesAndKeepsPreparedOutputDetached() {
        var series = Series().WithPointLabel(1, "Retained observation");
        var chart = Chart.Create().WithSize(640, 360).AddSeries(series);
        var prepared = Prepare(chart);
        var originalSvg = prepared.ToSvg();
        var originalPng = prepared.ToPng();
        var replacement = new ChartPoint(20, -25, true).WithColorValue(-7);
        var replace = ChartObservationChange<ChartPoint>.Replace(Id("b"), replacement);
        replacement = new ChartPoint(99, 100).WithColorValue(42);

        series.ApplyPointChanges(new[] {
            replace,
            ChartObservationChange<ChartPoint>.Append(Id("zero"), default)
        }, maximumObservations: 2);

        Assert.Equal(new[] { "b", "zero" }, Keys(series));
        Assert.Equal((20d, -25d, true, (double?)-7), Facts(series.Points[0]));
        Assert.Equal((0d, 0d, false, (double?)null), Facts(series.Points[1]));
        Assert.Equal((99d, 100d, false, (double?)42), Facts(replacement));
        Assert.Equal("Retained observation", series.PointLabels[0]);
        Assert.Equal((20d, -25d, true, (double?)-7), Facts(replace.Value!.Value));
        var updated = Prepare(chart);
        Assert.Contains("data-cfx-observation-id=\"zero\"", updated.ToSvg());
        Assert.NotEmpty(updated.ToPng());
        Assert.Equal(originalSvg, prepared.ToSvg());
        Assert.Equal(originalPng, prepared.ToPng());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SinglePassGenericBatchFinishesDisposalBeforeInstallingAnyChanges(bool cancelOnDisposal) {
        var series = Series().WithPointLabel(1, "Stable");
        var chart = Chart.Create().AddSeries(series);
        var originalSvg = Prepare(chart).ToSvg();
        using var cancellation = new CancellationTokenSource();
        var enumerations = 0;
        var disposals = 0;
        IEnumerable<ChartObservationChange<ChartPoint>> Capture() {
            if (++enumerations != 1) throw new InvalidOperationException("The source only supports one capture.");
            try {
                yield return ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 50));
                yield return ChartObservationChange<ChartPoint>.Append(Id("c"), new ChartPoint(3, 30));
            } finally {
                disposals++;
                Assert.Equal(new[] { "a", "b" }, Keys(series));
                Assert.Equal(new[] { 10d, 20d }, series.Points.Select(point => point.Y));
                Assert.Equal("Stable", series.PointLabels[1]);
                if (cancelOnDisposal) cancellation.Cancel();
            }
        }

        if (cancelOnDisposal) {
            Assert.Throws<OperationCanceledException>(() => series.ApplyPointChanges(Capture(), cancellationToken: cancellation.Token));
            Assert.Equal(originalSvg, Prepare(chart).ToSvg());
        } else {
            Assert.Same(series, series.ApplyPointChanges(Capture(), maximumObservations: 2, cancellationToken: cancellation.Token));
            Assert.Equal(new[] { "b", "c" }, Keys(series));
            Assert.Equal(new[] { 50d, 30d }, series.Points.Select(point => point.Y));
            Assert.Equal("Stable", series.PointLabels[0]);
        }
        Assert.Equal(1, enumerations);
        Assert.Equal(1, disposals);
    }

    private static ChartSeries Series() => ChartSeries.CreateScatter(new ChartSeriesId("live"), "Live",
        new[] { new ChartPoint(1, 10), new ChartPoint(2, 20) }).WithObservationIds(new[] { Id("a"), Id("b") });
    private static ChartObservationId Id(string value) => new(value);
    private static string[] Keys(ChartSeries series) => series.ObservationIds.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value).ToArray();
    private static (double X, double Y, bool BreakBefore, double? ColorValue) Facts(ChartPoint point) =>
        (point.X, point.Y, point.BreakBefore, point.ColorValue);
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
