using System.Threading;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects atomic typed streaming changes, source identity and complete observation presentation.</summary>
public sealed class KeyedTypedObservationChangesTests {
    [Fact]
    public void BubbleRollingChangesKeepIndependentFactsAndSurvivingPresentationWithDetachedExports() {
        var series = ChartSeries.CreateBubble(new ChartSeriesId("bubbles"), "Requests", new[] {
            new ChartBubble(1, 10, 2).WithColorValue(-1), new ChartBubble(2, 20, 3).WithColorValue(4)
        }).WithObservationIds(new[] { Id("a"), Id("b") }).WithPointLabel(1, "Retained request")
            .WithPointColor(1, "#123456").WithPointFillPattern(1, ChartFillPattern.Crosshatch)
            .ConfigurePointDataLabelStyle(1, options => options.FontSize = 13);
        series.PointSliceOffsets.AddRange(new[] { 0d, .2 });
        var chart = Chart.Create().WithSize(640, 360).AddSeries(series);
        var frozen = Prepare(chart); var svg = frozen.ToSvg(); var png = frozen.ToPng();
        var oldValues = series.Bubbles;
        series.ApplyBubbleChanges(new[] {
            ChartObservationChange<ChartBubble>.Remove(Id("a")),
            ChartObservationChange<ChartBubble>.Replace(Id("b"), new ChartBubble(2, 25, 7).WithColorValue(-3)),
            ChartObservationChange<ChartBubble>.Append(Id("c"), new ChartBubble(3, 30, 8).WithColorValue(6))
        }, maximumObservations: 2);
        Assert.Equal(new[] { "b", "c" }, Keys(series));
        Assert.Equal((25d, 7d, -3d), (series.Bubbles[0].Y, series.Bubbles[0].Size, series.Bubbles[0].ColorValue!.Value));
        Assert.Equal(2, series.SourcePointCount); Assert.Equal(2, series.ObservationCount);
        Assert.Equal("Retained request", series.PointLabels[0]); Assert.Equal(ChartColor.FromHex("#123456"), series.PointColors[0]);
        Assert.Equal(ChartFillPattern.Crosshatch, series.PointFillPatterns[0]);
        Assert.Equal(13, series.PointDataLabelStyles[0]!.FontSize); Assert.Equal(.2, series.PointSliceOffsets[0]);
        Assert.Equal(10, oldValues[0].Y); Assert.Equal(4, oldValues[1].ColorValue);
        Assert.Equal(svg, frozen.ToSvg()); Assert.Equal(png, frozen.ToPng());
        Assert.NotEmpty(Prepare(chart).ToPng());
        series.ApplyBubbleChanges(Array.Empty<ChartObservationChange<ChartBubble>>(), maximumObservations: 1);
        Assert.Equal(new[] { "c" }, Keys(series)); Assert.Empty(series.PointLabels); Assert.Empty(series.PointColors);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.Ohlc)]
    public void FinancialChangesRetainDatumStylesByIdentityAndGiveNewObservationsSeriesFallback(ChartSeriesKind kind) {
        var chart = FinancialMarkOptionsTests.Prices(kind);
        var series = chart.Series[0].WithObservationIds(new[] { Id("a"), Id("b") }).WithPointLabel(1, "Retained quote");
        var blue = ChartColor.FromHex("#3377CC"); var green = ChartColor.FromHex("#22AA66");
        series.ConfigureFinancial(options => { options.Rising.Stroke = green; options.Falling.Stroke = green; });
        series.ConfigureFinancialValue(1, options => { options.Stroke = blue; options.StrokeWidth = 7; });
        var frozen = Prepare(chart); var before = frozen.ToSvg();
        series.ApplyFinancialChanges(new[] {
            ChartObservationChange<ChartCandlestick>.Remove(Id("a")),
            ChartObservationChange<ChartCandlestick>.Replace(Id("b"), new ChartCandlestick(2, 10, 16, 6, 13)),
            ChartObservationChange<ChartCandlestick>.Append(Id("c"), new ChartCandlestick(3, 12, 17, 8, 14))
        });
        Assert.Equal(new[] { "b", "c" }, Keys(series)); Assert.Equal("Retained quote", series.PointLabels[0]);
        var role = kind == ChartSeriesKind.Candlestick ? "candlestick-body" : "ohlc-stem";
        var marks = Prepare(chart).Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == role).ToArray();
        Assert.Equal(2, marks.Length); Assert.Equal(blue, marks[0].Stroke); Assert.Equal(7, marks[0].StrokeWidth);
        Assert.Equal(green, marks[1].Stroke); Assert.Equal(before, frozen.ToSvg());
        Assert.Equal(2, series.SourcePointCount); Assert.Equal(16, series.FinancialValues[0].High);
    }

    [Fact]
    public void RangeAndDifferenceChangesKeepBothTupleMembersAndExplicitBreaks() {
        var ranges = ChartSeries.CreateRangeArea(new ChartSeriesId("ranges"), "Envelope", new[] { new ChartRangeBand(1, 2, 7), new ChartRangeBand(2, 3, 8) })
            .WithObservationIds(new[] { Id("a"), Id("b") });
        ranges.ApplyRangeChanges(new[] {
            ChartObservationChange<ChartRangeBand>.Remove(Id("a")),
            ChartObservationChange<ChartRangeBand>.Replace(Id("b"), new ChartRangeBand(2, -4, 20, true)),
            ChartObservationChange<ChartRangeBand>.Append(Id("c"), new ChartRangeBand(3, 6, 21))
        });
        Assert.Equal((-4d, 20d, true), (ranges.Ranges[0].Lower, ranges.Ranges[0].Upper, ranges.Ranges[0].BreakBefore));
        var differences = ChartSeries.CreateDifferenceArea(new ChartSeriesId("differences"), "Compared", new[] { new ChartDifferencePoint(1, 7, 2) })
            .WithObservationIds(new[] { Id("a") });
        differences.ApplyDifferenceChanges(new[] {
            ChartObservationChange<ChartDifferencePoint>.Replace(Id("a"), new ChartDifferencePoint(2, -9, 12, true)),
            ChartObservationChange<ChartDifferencePoint>.Append(Id("b"), new ChartDifferencePoint(3, 8, -3))
        });
        Assert.Equal((-9d, 12d, true), (differences.DifferencePoints[0].First, differences.DifferencePoints[0].Second, differences.DifferencePoints[0].BreakBefore));
        Assert.Equal(new[] { "b", "c" }, Keys(ranges)); Assert.Equal(new[] { "a", "b" }, Keys(differences));
        Assert.NotEmpty(Prepare(Chart.Create().AddSeries(ranges).AddSeries(differences)).ToPng());
    }

    [Fact]
    public void InvalidThrowingOverlongAndCanceledTypedBatchesDoNotInstallPartialData() {
        var series = ChartSeries.CreateBubble(new ChartSeriesId("live"), "Live", new[] { new ChartBubble(1, 2, 3) })
            .WithObservationIds(new[] { Id("a") }).WithPointLabel(0, "Stable");
        var chart = Chart.Create().AddSeries(series); var before = Prepare(chart).ToSvg(); var values = series.Bubbles;
        Assert.Throws<ArgumentException>(() => series.ApplyBubbleChanges(new[] {
            ChartObservationChange<ChartBubble>.Replace(Id("a"), new ChartBubble(2, 9, 4)),
            ChartObservationChange<ChartBubble>.Remove(Id("a"))
        }));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ApplyBubbleChanges(new[] { ChartObservationChange<ChartBubble>.Replace(Id("a"), default) }));
        IEnumerable<ChartObservationChange<ChartBubble>> Throwing() {
            yield return ChartObservationChange<ChartBubble>.Replace(Id("a"), new ChartBubble(2, 9, 4));
            throw new InvalidOperationException("Interrupted source.");
        }
        Assert.Throws<InvalidOperationException>(() => series.ApplyBubbleChanges(Throwing()));
        var visited = 0;
        IEnumerable<ChartObservationChange<ChartBubble>> Endless() {
            for (var index = 0; ; index++) { visited++; yield return ChartObservationChange<ChartBubble>.Append(Id("new-" + index), new ChartBubble(index, 1, 1)); }
        }
        Assert.Throws<ArgumentException>(() => series.ApplyBubbleChanges(Endless(), maximumChanges: 2)); Assert.Equal(3, visited);
        using var cancellation = new CancellationTokenSource();
        IEnumerable<ChartObservationChange<ChartBubble>> Canceled() {
            yield return ChartObservationChange<ChartBubble>.Replace(Id("a"), new ChartBubble(2, 9, 4)); cancellation.Cancel();
        }
        Assert.Throws<OperationCanceledException>(() => series.ApplyBubbleChanges(Canceled(), cancellationToken: cancellation.Token));
        Assert.Same(values, series.Bubbles); Assert.Equal(before, Prepare(chart).ToSvg()); Assert.Equal("Stable", series.PointLabels[0]);
    }

    [Fact]
    public void EmptyTypedSeriesCanRefillAndAnonymousOrWrongFamilyTransactionsReject() {
        var empty = ChartSeries.CreateBubble(new ChartSeriesId("empty"), "Empty", Array.Empty<ChartBubble>());
        empty.ApplyBubbleChanges(new[] { ChartObservationChange<ChartBubble>.Append(Id("first"), new ChartBubble(1, 0, 2)) });
        empty.ApplyBubbleChanges(new[] { ChartObservationChange<ChartBubble>.Remove(Id("first")) });
        Assert.Empty(empty.Bubbles); Assert.Equal(0, empty.SourcePointCount); Assert.Empty(empty.ObservationIds);
        empty.ApplyBubbleChanges(new[] { ChartObservationChange<ChartBubble>.Append(Id("refill"), new ChartBubble(2, 5, 3)) });
        Assert.NotEmpty(Prepare(Chart.Create().AddSeries(empty)).ToPng());
        var anonymous = ChartSeries.CreateBubble(new ChartSeriesId("anonymous"), "Anonymous", new[] { new ChartBubble(1, 2, 3) });
        Assert.Throws<InvalidOperationException>(() => anonymous.ApplyBubbleChanges(Array.Empty<ChartObservationChange<ChartBubble>>()));
        Assert.Throws<InvalidOperationException>(() => empty.ApplyFinancialChanges(Array.Empty<ChartObservationChange<ChartCandlestick>>()));
        Assert.Throws<InvalidOperationException>(() => empty.ApplyBubbleChanges(new[] { default(ChartObservationChange<ChartBubble>) }));
    }

    private static ChartObservationId Id(string value) => new(value);
    private static string[] Keys(ChartSeries series) => series.ObservationIds.OrderBy(item => item.Key).Select(item => item.Value.Value).ToArray();
    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(VisualExportRequest.ForChart(chart).Context);
}
