using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects independent numeric color facts, immutable source copies and logical Bubble tuple ownership.</summary>
public sealed class ScatterBubbleColorObservationTests {
    [Fact]
    public void ColorCopiesPreserveCoordinatesDateInstantsSegmentBoundariesAndBubbleSize() {
        var date = new DateTime(2026, 4, 5, 12, 30, 0, DateTimeKind.Utc);
        var point = new ChartPoint(date, 7, true);
        var coloredPoint = point.WithColorValue(-4);
        Assert.Equal(point.X, coloredPoint.X);
        Assert.Equal(point.Y, coloredPoint.Y);
        Assert.True(coloredPoint.BreakBefore);
        Assert.Equal(-4d, coloredPoint.ColorValue);
        Assert.Null(point.ColorValue);
        var clearedPoint = coloredPoint.WithColorValue(null);
        Assert.Equal(point, clearedPoint);
        Assert.Equal(-4d, coloredPoint.ColorValue);
        Assert.Null(new ChartPoint(1, 2).ColorValue);
        Assert.Null(new ChartPoint(1, 2, false).ColorValue);
        Assert.Equal(new ChartPoint(date, 7), ChartPoint.FromDateTime(date, 7));

        var bubble = new ChartBubble(date, 8, 25);
        var coloredBubble = bubble.WithColorValue(0);
        Assert.Equal(bubble.X, coloredBubble.X);
        Assert.Equal(bubble.Y, coloredBubble.Y);
        Assert.Equal(bubble.Size, coloredBubble.Size);
        Assert.Equal(0d, coloredBubble.ColorValue);
        Assert.Null(bubble.ColorValue);
        Assert.Equal(bubble, coloredBubble.WithColorValue(null));
        Assert.Null(new ChartBubble(1, 2, 3).ColorValue);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    public void NonFiniteColorMeasurementsAreRejectedByTheImmutableObservationApis(double value) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartPoint(1, 2).WithColorValue(value));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartBubble(1, 2, 3).WithColorValue(value));
    }

    [Fact]
    public void ProjectedRowsKeepIndependentColorAndBubbleSizeWhileExistingXYFactoriesStayUncolored() {
        var rows = new[] {
            new { X = 1d, Y = 90d, Size = 4d, Color = (double?)0 },
            new { X = 2d, Y = 10d, Size = 100d, Color = (double?)-20 },
            new { X = 3d, Y = 40d, Size = 25d, Color = (double?)null }
        };
        var chart = Chart.Create()
            .AddScatter("Samples", rows.Select(row => new ChartPoint(row.X, row.Y).WithColorValue(row.Color)))
            .AddBubble("Assets", rows.Select(row => new ChartBubble(row.X, row.Y, row.Size).WithColorValue(row.Color)))
            .AddScatter("Existing mapping", rows, row => row.X, row => row.Y);
        Assert.Equal(rows.Select(row => row.Color), chart.Series[0].Points.Select(point => point.ColorValue));
        Assert.All(chart.Series[2].Points, point => Assert.Null(point.ColorValue));
        Assert.All(ChartBubbles.FromXYSize(rows.Select(row => row.X), rows.Select(row => row.Y), rows.Select(row => row.Size)),
            bubble => Assert.Null(bubble.ColorValue));
        var points = chart.Series[1].Bubbles;
        for (var index = 0; index < rows.Length; index++) {
            Assert.Equal(rows[index].X, points[index].X);
            Assert.Equal(rows[index].Y, points[index].Y);
            Assert.Equal(rows[index].Color, points[index].ColorValue);
            Assert.Equal(rows[index].Size, points[index].Size);
        }
        // Raw color is a supported source fact even before a scale is selected.
        chart.Prepare(Context());
    }

    [Theory]
    [InlineData(ChartSeriesKind.Scatter)]
    [InlineData(ChartSeriesKind.Bubble)]
    public void ScaleOwnershipUsesOneImmutableReferenceAndNullResetRetainsSourceFacts(ChartSeriesKind kind) {
        var series = kind == ChartSeriesKind.Scatter
            ? ChartSeries.CreateScatter(new ChartSeriesId("source"), "Source", new[] { new ChartPoint(1, 9).WithColorValue(3) })
            : ChartSeries.CreateBubble(new ChartSeriesId("source"), "Source", new[] { new ChartBubble(1, 9, 25).WithColorValue(3) });
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White).WithValueRange(0, 10);
        Assert.Null(series.ColorScale);
        Assert.Same(series, series.WithColorScale(scale));
        Assert.Same(scale, series.ColorScale);
        series.ColorScale = null;
        Assert.Null(series.ColorScale);
        Assert.Equal(3d, kind == ChartSeriesKind.Bubble ? series.Bubbles[0].ColorValue : series.Points[0].ColorValue);
        series.ColorScale = scale;
        Assert.Same(series, series.WithColorScale(null));
        Assert.Null(series.ColorScale);
        Assert.Equal(3d, kind == ChartSeriesKind.Bubble ? series.Bubbles[0].ColorValue : series.Points[0].ColorValue);
    }

    [Fact]
    public void UnsupportedScaleAssignmentFailsAtThePublicBoundaryAndNullResetRemainsValid() {
        var series = new ChartSeries(new ChartSeriesId("fixture-series"), "Line", ChartSeriesKind.Line, new[] { new ChartPoint(1, 2) });
        var scale = ChartColorScale.Sequential(ChartColor.Black, ChartColor.White);
        Assert.Throws<InvalidOperationException>(() => series.ColorScale = scale);
        Assert.Throws<InvalidOperationException>(() => series.WithColorScale(scale));
        Assert.Null(series.ColorScale);
        Assert.Same(series, series.WithColorScale(null));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Line)]
    [InlineData(ChartSeriesKind.Bar)]
    public void PreparationRejectsColorFactsOnUnsupportedDisplayedFamilies(ChartSeriesKind kind) {
        var chart = Chart.Create();
        var series = new ChartSeries(new ChartSeriesId("fixture-series"), "Unsupported", kind, new[] { new ChartPoint(1, 2).WithColorValue(0) });
        chart.AddSeries(series);
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(Context()));
        series.Points[0] = series.Points[0].WithColorValue(null);
        chart.Prepare(Context());
    }

    [Fact]
    public void BubbleColorMeasurementRemainsIndependentOfSizeDuringTypedUpdates() {
        var chart = Chart.Create().AddBubble("Bubble", new[] { new ChartBubble(1, 2, 25).WithColorValue(-10) });
        var series = chart.Series[0];
        series.UpdateBubble(0, series.Bubbles[0].WithColorValue(0));
        Assert.Equal(25, series.Bubbles[0].Size);
        Assert.Equal(0, series.Bubbles[0].ColorValue);
        series.UpdateBubble(0, series.Bubbles[0].WithColorValue(null));
        Assert.Equal(25, series.Bubbles[0].Size);
        chart.Prepare(Context());
    }

    [Theory]
    [InlineData(ChartDecimationMode.MinMax, false)]
    [InlineData(ChartDecimationMode.LargestTriangleThreeBuckets, false)]
    [InlineData(ChartDecimationMode.MinMax, true)]
    [InlineData(ChartDecimationMode.LargestTriangleThreeBuckets, true)]
    public void DecimatedAndAdaptiveScatterRetainColorAtEachOriginalSourceIndex(ChartDecimationMode mode, bool segmented) {
        var source = Enumerable.Range(0, 12).Select(index =>
            new ChartPoint(index, Math.Sin(index), segmented && index == 6)
                .WithColorValue(index % 3 == 0 ? null : index * 3.5)).ToArray();
        var budget = segmented && mode == ChartDecimationMode.MinMax ? 8 : 6;
        var policy = new ChartResolutionPolicy {
            MinimumPointCount = budget, MaximumPointCount = budget, PointsPerPixel = 1, DecimationMode = mode
        };
        var chart = Chart.Create()
            .AddDecimatedScatter("Bounded", source, budget, mode)
            .AddAdaptiveScatter("Adaptive", source, 3, policy)
            .AddAdaptiveScatter("Intact", source, 100);
        foreach (var series in chart.Series.Take(2)) {
            Assert.True(series.IsDecimated);
            Assert.Equal(source.Length, series.SourcePointCount);
            Assert.Equal(series.Points.Count, series.SourcePointIndices.Count);
            for (var index = 0; index < series.Points.Count; index++)
                Assert.Equal(source[series.SourcePointIndices[index]], series.Points[index]);
        }
        Assert.Equal(source, chart.Series[2].Points);
    }

    [Fact]
    public void DerivedSeriesKeepDetachedRawColorFactsWithoutColoringTheirComputedObservations() {
        var source = new[] {
            new ChartPoint(1, 1).WithColorValue(0),
            new ChartPoint(2, 4).WithColorValue(null),
            new ChartPoint(3, 2).WithColorValue(-5)
        };
        var expected = source.ToArray();
        var chart = Chart.Create().AddTrendLine("Trend", source)
            .AddHistogram("Histogram", source, ChartHistogramBinLayout.FromCount(0, 4, 2), ChartHistogramAggregation.Sum);
        source[0] = new ChartPoint(1, 100).WithColorValue(100);
        Assert.Equal(expected, chart.Series[0].TrendLineSourcePoints);
        Assert.Equal(expected, chart.Series[1].HistogramSourcePoints);
        Assert.All(chart.Series.SelectMany(series => series.Points), point => Assert.Null(point.ColorValue));
        chart.Prepare(Context());
    }

    private static VisualRenderContext Context() => new(new VisualLayoutOptions(new VisualSize(400, 300)), VisualTheme.Graphite());
}
