using ChartForgeX.Core;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class ChartCategoricalProjectionContractsTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void PartialNumericTickAliasesKeepThePreparedCategoryDomainContinuous(bool horizontal, bool middleAlias) {
        var points = new[] { new ChartPoint(20, 2), new ChartPoint(40, 4), new ChartPoint(60, 6), new ChartPoint(80, 8) };
        var chart = Frame(horizontal, 0, 100).AddLine("Readings", points);
        var axis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        axis.Labels.Add(new ChartAxisLabel(0, "Low")); axis.Labels.Add(new ChartAxisLabel(100, "High"));
        if (middleAlias) axis.Labels.Add(new ChartAxisLabel(40, "Middle"));
        var projection = Projection(chart);
        Assert.Null(projection.CategoryLabels);
        var categoryAxis = horizontal ? projection.Y! : projection.X;
        Assert.Equal(new[] { 0d, 100d }, categoryAxis.DisplayBounds.Select(bound => bound.Value));
        Assert.Equal(new[] { "Low", "High" }, categoryAxis.DisplayBounds.Select(bound => bound.Text));
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CompleteSparseNamesBindOnlyActualCategoryCoordinatesAndIgnoreEmptyLineCompanions(bool horizontal, bool emptyCompanion) {
        var chart = Frame(horizontal, 0, 6).AddLine("Readings", new[] { new ChartPoint(1, 2), new ChartPoint(3, 8) });
        var axis = horizontal ? chart.Options.YAxis : chart.Options.XAxis;
        axis.Labels.AddRange(new[] { new ChartAxisLabel(1, "North"), new ChartAxisLabel(3, "South"), new ChartAxisLabel(5, "Unused alias") });
        if (emptyCompanion) chart.AddLine("Empty", Array.Empty<ChartPoint>());
        var projection = Projection(chart);
        Assert.NotNull(projection.CategoryLabels);
        Assert.Equal(new[] { 1d, 3d }, projection.CategoryLabels!.Select(label => label.Value));
        Assert.Equal(new[] { "North", "South" }, projection.CategoryLabels.Select(label => label.Text));
        Assert.Empty((horizontal ? projection.Y! : projection.X).DisplayBounds);
    }

    [Theory]
    [InlineData(ChartSeriesKind.HorizontalBar)]
    [InlineData(ChartSeriesKind.RangeBar)]
    [InlineData(ChartSeriesKind.BoxPlot)]
    [InlineData(ChartSeriesKind.Waterfall)]
    public void IntrinsicCategoryBindingsCaptureEachActualCaptionOnceWithoutFormattingPaddingBounds(ChartSeriesKind kind) {
        var chart = Chart.Create().WithSize(700, 380).WithHeader(false).WithLegend(false).WithDataLabels(false)
            .WithXAxisBounds(0, 100).WithYAxisBounds(.5, 2.5);
        if (kind == ChartSeriesKind.HorizontalBar) chart.AddHorizontalBar("Readings", new[] { new ChartPoint(1, 20), new ChartPoint(2, 40) });
        else if (kind == ChartSeriesKind.RangeBar) {
            chart.AddRangeBar("Readings", new[] { new ChartInterval(1, 10, 20), new ChartInterval(2, 30, 40) });
            chart.Options.RangeBar.Orientation = ChartOrientation.Horizontal;
        } else if (kind == ChartSeriesKind.BoxPlot) {
            chart.AddBoxPlot("Readings", new[] { new ChartBoxPlot(1, 10, 12, 15, 18, 20), new ChartBoxPlot(2, 30, 32, 35, 38, 40) });
            chart.Options.BoxPlot.Orientation = ChartOrientation.Horizontal;
        } else {
            chart.AddWaterfall("Readings", new[] { ChartWaterfallItem.OpeningBalance(1, 20), ChartWaterfallItem.Delta(2, 20) });
            chart.Options.Waterfall.Orientation = ChartOrientation.Horizontal;
        }
        var calls = new Dictionary<double, int>();
        chart.Options.YAxis.LabelFormatter = value => {
            calls[value] = calls.TryGetValue(value, out var count) ? count + 1 : 1;
            return "Category " + value;
        };
        var projection = Projection(chart);
        Assert.Equal(new[] { 1d, 2d }, calls.Keys.OrderBy(value => value));
        Assert.All(calls.Values, count => Assert.Equal(1, count));
        Assert.NotNull(projection.CategoryLabels);
        Assert.Equal(new[] { "Category 1", "Category 2" }, projection.CategoryLabels!.Select(label => label.Text));
        Assert.Empty(projection.Y!.DisplayBounds);
        Assert.Equal(new[] { 0d, 100d }, projection.X.DisplayBounds.Select(bound => bound.Value));
    }

    [Fact]
    public void HistogramAndTimeBucketBarsKeepContinuousProjectionSemantics() {
        var histogram = Chart.Create().AddSeries(ChartSeries.CreateHistogram(new ChartSeriesId("duration"), "Duration", new[] { 30d, 45 },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 20, 40, 60, 80, 100 })));
        Assert.Null(Projection(histogram).CategoryLabels);
        var start = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
        var buckets = Chart.Create().WithXAxisTimeScale().AddSeries(ChartSeries.CreateBar(new ChartSeriesId("opened"), "Opened",
            new[] { new ChartPoint(start, 2), new ChartPoint(start.AddDays(1), 4) }).WithTimeBuckets(new ChartTimeTickInterval(ChartTimeTickUnit.Day)));
        Assert.Null(Projection(buckets).CategoryLabels);
    }

    private static Chart Frame(bool horizontal, double minimum, double maximum) {
        var chart = Chart.Create().WithSize(700, 380).WithHeader(false).WithLegend(false).WithDataLabels(false);
        if (horizontal) {
            chart.Options.LineOrientation = ChartOrientation.Horizontal;
            chart.WithXAxisBounds(0, 10).WithYAxisBounds(minimum, maximum);
        } else chart.WithXAxisBounds(minimum, maximum).WithYAxisBounds(0, 10);
        return chart;
    }

    private static ChartDataProjection Projection(Chart chart) => Assert.IsType<ChartDataProjection>(chart.Prepare(new VisualRenderContext()).Scene.DataProjection);
}
