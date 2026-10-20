using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects keyed live transactions, rolling source ownership and detached exports.</summary>
public sealed class KeyedPointChangesTests {
    [Fact]
    public void RollingTransactionKeepsSurvivingKeysAndEveryPresentationOverride() {
        var series = Series().WithPointColor(1, "#123456").WithPointLabel(1, "Second")
            .WithPointFillPattern(1, ChartFillPattern.Crosshatch)
            .ConfigurePointDataLabelStyle(1, style => style.FontSize = 13);
        series.PointSliceOffsets.AddRange(new[] { 0d, .2, 0d });
        var chart = Chart.Create().WithSize(640, 360).AddSeries(series);
        var frozen = Prepare(chart); var svg = frozen.ToSvg(); var png = frozen.ToPng();
        var pointsView = series.Points;
        var labelsView = series.PointLabels;
        series.ApplyPointChanges(new[] {
            ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 25)),
            ChartObservationChange<ChartPoint>.Remove(Id("a")),
            ChartObservationChange<ChartPoint>.Append(Id("d"), new ChartPoint(4, 40)),
            ChartObservationChange<ChartPoint>.Append(Id("e"), new ChartPoint(5, 50))
        }, maximumObservations: 4);
        Assert.Same(pointsView, series.Points); Assert.Same(labelsView, series.PointLabels);
        Assert.Equal(new[] { "b", "c", "d", "e" }, series.ObservationIds.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value));
        Assert.Equal(new[] { 25d, 30, 40, 50 }, series.Points.Select(point => point.Y));
        Assert.Equal("Second", series.PointLabels[0]); Assert.Equal(ChartColor.FromHex("#123456"), series.PointColors[0]);
        Assert.Equal(ChartFillPattern.Crosshatch, series.PointFillPatterns[0]);
        Assert.Equal(13, series.PointDataLabelStyles[0]!.FontSize); Assert.Equal(.2, series.PointSliceOffsets[0]);
        var mark = Prepare(chart).Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Role == "point"
            && node.Metadata.TryGetValue("data-cfx-observation-id", out var id) && id == "b");
        Assert.Equal("25", mark.Metadata["data-cfx-y"]);
        Assert.Equal(svg, frozen.ToSvg()); Assert.Equal(png, frozen.ToPng());
        series.ApplyPointChanges(Array.Empty<ChartObservationChange<ChartPoint>>(), maximumObservations: 2);
        Assert.Equal(new[] { "d", "e" }, series.ObservationIds.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value));
        Assert.Empty(series.PointLabels); Assert.Empty(series.PointColors); Assert.Empty(series.PointDataLabelStyles);
        Assert.NotEmpty(Prepare(chart).ToPng());
    }

    [Fact]
    public void InvalidThrowingOversizedAndCanceledBatchesLeaveInstalledFactsAndOverridesUntouched() {
        var series = Series().WithPointColor(1, "#123456").WithPointLabel(1, "Second");
        var chart = Chart.Create().AddSeries(series); var before = Prepare(chart).ToSvg();
        var invalid = new[] {
            new[] { ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 99)), ChartObservationChange<ChartPoint>.Append(Id("a"), new ChartPoint(7, 70)) },
            new[] { ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 99)), ChartObservationChange<ChartPoint>.Remove(Id("missing")) },
            new[] { ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 99)), ChartObservationChange<ChartPoint>.Remove(Id("b")) }
        };
        foreach (var changes in invalid) Assert.Throws<ArgumentException>(() => series.ApplyPointChanges(changes));
        Assert.Throws<InvalidOperationException>(() => series.ApplyPointChanges(Throwing()));
        var visited = 0;
        IEnumerable<ChartObservationChange<ChartPoint>> Endless() {
            for (var index = 0; ; index++) { visited++; yield return ChartObservationChange<ChartPoint>.Append(Id("new-" + index), new ChartPoint(index, 1)); }
        }
        Assert.Throws<ArgumentException>(() => series.ApplyPointChanges(Endless(), maximumChanges: 2));
        Assert.Equal(3, visited);
        using var cancellation = new CancellationTokenSource();
        IEnumerable<ChartObservationChange<ChartPoint>> Canceled() {
            yield return ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 99));
            cancellation.Cancel();
        }
        Assert.Throws<OperationCanceledException>(() => series.ApplyPointChanges(Canceled(), cancellationToken: cancellation.Token));
        Assert.Equal(before, Prepare(chart).ToSvg());
        Assert.Equal("Second", series.PointLabels[1]); Assert.Equal(ChartColor.FromHex("#123456"), series.PointColors[1]);
        Assert.Equal("b", series.ObservationIds[1].Value); Assert.Equal(20, series.Points[1].Y);
    }

    [Fact]
    public void EmptySeriesCanStartFromKeyedChangesAndDerivedTypedOrAnonymousSourcesRejectWithoutChanges() {
        var empty = ChartSeries.CreateLine(new ChartSeriesId("live"), "Live", Array.Empty<ChartPoint>());
        empty.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Append(Id("first"), new ChartPoint(1, 0)) });
        Assert.Equal("first", empty.ObservationIds[0].Value); Assert.Single(empty.Points);
        empty.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Remove(Id("first")) });
        Assert.Empty(empty.Points); Assert.Empty(empty.ObservationIds);
        empty.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Append(Id("refilled"), new ChartPoint(2, 8)) });
        Assert.NotEmpty(Prepare(Chart.Create().AddSeries(empty)).ToPng());
        var anonymous = ChartSeries.CreateLine(new ChartSeriesId("anonymous"), "Anonymous", new[] { new ChartPoint(1, 1) });
        Assert.Throws<InvalidOperationException>(() => anonymous.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Remove(Id("first")) }));
        Assert.Single(anonymous.Points);
        var derived = ChartSeries.CreateTrendLine(new ChartSeriesId("fit"), "Fit", new[] { new ChartPoint(1, 1), new ChartPoint(2, 3) });
        var typed = ChartSeries.CreateBubble(new ChartSeriesId("typed"), "Typed", new[] { new ChartBubble(1, 2, 3) });
        foreach (var series in new[] { derived, typed }) {
            var sourceCount = series.SourcePointCount;
            Assert.Throws<NotSupportedException>(() => series.ApplyPointChanges(Array.Empty<ChartObservationChange<ChartPoint>>()));
            Assert.Equal(sourceCount, series.SourcePointCount);
        }
        Assert.Throws<InvalidOperationException>(() => empty.ApplyPointChanges(new[] { default(ChartObservationChange<ChartPoint>) }));
        Assert.Equal("refilled", empty.ObservationIds[0].Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => empty.ApplyPointChanges(Array.Empty<ChartObservationChange<ChartPoint>>(), maximumObservations: 0));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.StepLine)]
    [InlineData(ChartSeriesKind.Area)]
    [InlineData(ChartSeriesKind.StepArea)]
    [InlineData(ChartSeriesKind.StackedArea)]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bar)]
    [InlineData(ChartSeriesKind.HorizontalBar)]
    [InlineData(ChartSeriesKind.RadialBar)]
    [InlineData(ChartSeriesKind.RadialColumn)]
    [InlineData(ChartSeriesKind.Lollipop)]
    public void SupportedPointMappedFamiliesRetainKeyedSourceFactsThroughBothNativeExports(ChartSeriesKind kind) {
        var series = new ChartSeries(new ChartSeriesId("live"), "Live", kind,
            new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) })
            .WithObservationIds(new[] { Id("a"), Id("b"), Id("c") });
        var chart = Chart.Create().WithSize(640, 360).AddSeries(series);
        var frozen = Prepare(chart); var original = frozen.ToSvg();
        series.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 25)),
            ChartObservationChange<ChartPoint>.Append(Id("d"), new ChartPoint(4, 40)) }, maximumObservations: 3);
        var updated = Prepare(chart);
        Assert.Equal(new[] { "b", "c", "d" }, series.ObservationIds.OrderBy(entry => entry.Key).Select(entry => entry.Value.Value));
        Assert.Contains("data-cfx-observation-id=\"d\"", updated.ToSvg());
        Assert.NotEmpty(updated.ToPng());
        Assert.Equal(original, frozen.ToSvg());
        var updatedSvg = updated.ToSvg();
        series.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Remove(Id("b")), ChartObservationChange<ChartPoint>.Remove(Id("c")), ChartObservationChange<ChartPoint>.Remove(Id("d")) });
        var empty = Prepare(chart);
        Assert.Empty(series.Points); Assert.Empty(series.ObservationIds);
        Assert.Contains(empty.Diagnostics, diagnostic => diagnostic.Code.EndsWith(".no-data", StringComparison.Ordinal));
        Assert.DoesNotContain("data-cfx-observation-id=", empty.ToSvg());
        Assert.NotEmpty(empty.ToPng());
        series.ApplyPointChanges(new[] { ChartObservationChange<ChartPoint>.Append(Id("zero"), new ChartPoint(5, 0)) });
        var singleton = Prepare(chart);
        Assert.Single(series.Points); Assert.Equal(0, series.Points[0].Y);
        Assert.Contains("data-cfx-observation-id=\"zero\"", singleton.ToSvg());
        Assert.NotEmpty(singleton.ToPng());
        Assert.Equal(updatedSvg, updated.ToSvg());
        Assert.Equal(original, frozen.ToSvg());
    }

    private static IEnumerable<ChartObservationChange<ChartPoint>> Throwing() {
        yield return ChartObservationChange<ChartPoint>.Replace(Id("b"), new ChartPoint(2, 99));
        throw new InvalidOperationException("Upstream batch failed.");
    }
    private static ChartObservationId Id(string value) => new(value);
    private static ChartSeries Series() => ChartSeries.CreateScatter(new ChartSeriesId("orders"), "Orders",
        new[] { new ChartPoint(1, 10), new ChartPoint(2, 20), new ChartPoint(3, 30) }).WithObservationIds(new[] { Id("a"), Id("b"), Id("c") });
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
