using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Data;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HistogramNormalizationTests {
    [Fact]
    public void AutomaticLayoutCanNormalizeProbabilityWithoutChangingDefaultCounts() {
        var values = new[] { .25, .75, 1.25, 2d };
        var ordinary = Chart.Create().AddHistogram("Count", values, 2);
        var probability = Chart.Create().AddHistogram("Probability", values, 2, ChartHistogramEncoding.Probability);
        Assert.Equal(ordinary.Series[0].Points, probability.Series[0].Points);
        Assert.Equal(ChartHistogramEncoding.Value, ordinary.Series[0].HistogramEncoding);
        Assert.Equal(1, probability.Series[0].HistogramBins.Sum(bin => bin.RenderedValue), 12);
        Assert.All(ordinary.Series[0].HistogramBins, bin => Assert.Equal(bin.Value!.Value, bin.RenderedValue));
    }

    [Theory]
    [InlineData(ChartHistogramEncoding.Probability)]
    [InlineData(ChartHistogramEncoding.ProbabilityDensity)]
    public void NormalizationRetainsRawCountsAndUsesTheActualUnequalBinWidths(ChartHistogramEncoding encoding) {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d, 8d });
        var chart = Chart.Create().WithAxes(false).WithGrid(false)
            .AddHistogram("Samples", new[] { .25, .75, 2d, 4d, 8d }, layout, encoding);
        var bins = chart.Series[0].HistogramBins;
        Assert.Equal(new[] { 2d, 1d, 2d }, bins.Select(bin => bin.Value!.Value));
        Assert.Equal(new[] { 2, 1, 2 }, bins.Select(bin => bin.Count));
        Assert.Equal(1, bins.Sum(bin => bin.RenderedValue * (encoding == ChartHistogramEncoding.ProbabilityDensity ? bin.Width : 1)), 12);
        Assert.Equal(.4, bins[0].RenderedValue, 12);
        Assert.Equal(encoding == ChartHistogramEncoding.ProbabilityDensity ? .2 / 3 : .2, bins[1].RenderedValue, 12);
        Assert.Equal(new[] { 3, 4 }, bins[2].SourceIndices);
        var prepared = Prepare(chart);
        var point = Point(prepared, 0, 0);
        Assert.Equal("2", point.Metadata["data-cfx-bin-value"]);
        Assert.Equal("2", point.Metadata["data-cfx-y"]);
        Assert.Equal("0,1", point.Metadata["data-cfx-source-points"]);
        Assert.Equal(encoding.ToString().ToLowerInvariant(), point.Metadata["data-cfx-histogram-encoding"]);
        Assert.Equal(bins[0].RenderedValue.ToString("G17", System.Globalization.CultureInfo.InvariantCulture), point.Metadata["data-cfx-rendered-y"]);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Fact]
    public void ProbabilityDensityPaintsFullIntervalsWhoseNativeAreasRepresentProbability() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d });
        var red = ChartColor.FromHex("#C52E3C");
        var chart = Chart.Create().WithAxes(false).WithGrid(false).WithBarStyle(ChartBarStyle.SegmentedCapsule)
            .AddHistogram("Equal mass", new[] { .25, .75, 2d, 3d }, layout, ChartHistogramEncoding.ProbabilityDensity, red);
        chart.Options.XAxis.WithBounds(0, 4); chart.Options.YAxis.WithBounds(0, .5);
        var prepared = Prepare(chart);
        var narrow = Bounds(prepared, 0); var wide = Bounds(prepared, 1);
        Assert.Equal(narrow.Right, wide.Left, 8);
        Assert.Equal(3, wide.Width / narrow.Width, 8);
        Assert.Equal(3, narrow.Height / wide.Height, 8);
        Assert.Equal(narrow.Width * narrow.Height, wide.Width * wide.Height, 7);
        Assert.All(prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bar"), rectangle => Assert.Equal(0, rectangle.Radius));
        var raster = prepared.ToRgba(new VisualRenderOptions(supersampling: 1));
        foreach (var bounds in new[] { narrow, wide }) {
            var offset = ((int)(bounds.Top + bounds.Height / 2) * raster.Width + (int)(bounds.Left + bounds.Width * .03)) * 4;
            Assert.Equal(red.R, raster.Pixels[offset]); Assert.Equal(red.G, raster.Pixels[offset + 1]); Assert.Equal(red.B, raster.Pixels[offset + 2]);
        }
    }

    [Fact]
    public void SumProbabilityAvoidsOverflowingTheTotalAndRejectsNegativeMassAtomically() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 2d });
        var chart = Chart.Create().AddHistogram("Large mass", new[] { new ChartPoint(.5, 1e308), new ChartPoint(1.5, 1e308) },
            layout, ChartHistogramAggregation.Sum, ChartHistogramEncoding.Probability);
        Assert.All(chart.Series[0].HistogramBins, bin => { Assert.Equal(1e308, bin.Value); Assert.Equal(.5, bin.RenderedValue); });
        Assert.Throws<ArgumentException>(() => chart.AddHistogram("Negative", new[] { new ChartPoint(.5, 2), new ChartPoint(.5, -1) },
            layout, ChartHistogramAggregation.Sum, ChartHistogramEncoding.Probability));
        Assert.Single(chart.Series);
    }

    [Fact]
    public void FiniteProbabilityDensityDoesNotLoseATinyMassBeforeDividingByItsWidth() {
        var layout = ChartHistogramBinLayout.FromIntervals(new[] { (0d, 1e-308), (1d, 2d) });
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("density"), "Very different masses",
            new[] { new ChartPoint(5e-309, 1e-308), new ChartPoint(1.5, 1e308) }, layout,
            ChartHistogramAggregation.Sum, ChartHistogramEncoding.ProbabilityDensity);
        Assert.True(series.HistogramBins[0].RenderedValue > 0);
        Assert.InRange(series.HistogramBins[0].RenderedValue / 1e-308, .999999999999, 1.000000000001);
        Assert.Equal(1, series.HistogramBins[1].RenderedValue);
    }

    [Fact]
    public void ProbabilityDensityRoundsTheWholeRatioWhenBothIntermediateOrdersLoseTheMass() {
        var intervals = new[] { (0d, double.Epsilon) }.Concat(Enumerable.Range(1, 10).Select(index => ((double)index, index + 1d)));
        var layout = ChartHistogramBinLayout.FromIntervals(intervals);
        var observations = new[] { new ChartPoint(0, 1e-15) }
            .Concat(Enumerable.Range(1, 10).Select(index => new ChartPoint(index + .5, 1e308))).ToArray();
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("density"), "Narrow finite density", observations,
            layout, ChartHistogramAggregation.Sum, ChartHistogramEncoding.ProbabilityDensity);
        Assert.Equal(.2024022533073106, series.HistogramBins[0].RenderedValue, 14);
        Assert.Equal(1e-15, series.HistogramBins[0].Value);
        Assert.Equal(new[] { 0 }, series.HistogramBins[0].SourceIndices);
        Assert.All(series.HistogramBins.Skip(1), bin => {
            Assert.Equal(1e308, bin.Value); Assert.Equal(.1, bin.RenderedValue, 14); Assert.Equal(1, bin.Count);
        });
        var reversed = ChartSeries.CreateHistogram(new ChartSeriesId("reversed"), "Reversed source", observations.AsEnumerable().Reverse(),
            layout, ChartHistogramAggregation.Sum, ChartHistogramEncoding.ProbabilityDensity);
        Assert.Equal(series.HistogramBins.Select(bin => bin.RenderedValue), reversed.HistogramBins.Select(bin => bin.RenderedValue));
    }

    [Fact]
    public void DensityFallbackPreservesFiniteLargeResultsAndStillRejectsAnUnrepresentableFinalDensity() {
        var layout = ChartHistogramBinLayout.FromIntervals(new[] { (0d, 1e-308), (1d, 2d) });
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("finite"), "Large finite density",
            new[] { new ChartPoint(0, 1e308), new ChartPoint(1.5, 1e308) }, layout,
            ChartHistogramAggregation.Sum, ChartHistogramEncoding.ProbabilityDensity);
        Assert.InRange(series.HistogramBins[0].RenderedValue / 5e307, .999999999999, 1.000000000001);
        Assert.Equal(.5, series.HistogramBins[1].RenderedValue);
        Assert.Throws<ArgumentOutOfRangeException>(() => ChartSeries.CreateHistogram(new ChartSeriesId("overflow"),
            "Unrepresentable density", new[] { new ChartPoint(0, 1) },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, double.Epsilon }),
            ChartHistogramAggregation.Sum, ChartHistogramEncoding.ProbabilityDensity));
    }

    [Theory]
    [InlineData(ChartHistogramEncoding.Probability)]
    [InlineData(ChartHistogramEncoding.ProbabilityDensity)]
    public void EmptyAndZeroMassRetainTheirIntervalsWithoutInventingProbability(ChartHistogramEncoding encoding) {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 4d });
        var empty = Chart.Create().AddHistogram("Empty", Array.Empty<double>(), layout, encoding);
        var zero = Chart.Create().AddHistogram("Zero", new[] { new ChartPoint(.5, 0), new ChartPoint(2, 0) }, layout,
            ChartHistogramAggregation.Sum, encoding);
        foreach (var chart in new[] { empty, zero }) {
            Assert.Equal(2, chart.Series[0].HistogramBins.Count);
            Assert.All(chart.Series[0].HistogramBins, bin => { Assert.Equal(0, bin.Value); Assert.Equal(0, bin.RenderedValue); });
            Assert.True(Prepare(chart).ToPng().Length > 64);
        }
        var typed = ChartDataset<double>.From(new[] { .25, 2d }).Bin(value => value, layout);
        Assert.All(Chart.Create().AddHistogram("Typed", typed, encoding).Series[0].HistogramBins,
            bin => Assert.Equal(encoding == ChartHistogramEncoding.ProbabilityDensity ? .5 / bin.Width : .5, bin.RenderedValue, 12));
    }

    [Theory]
    [InlineData(ChartHistogramAggregation.Mean)]
    [InlineData(ChartHistogramAggregation.WeightedMean)]
    public void ProbabilityRequiresAdditiveCountOrNonnegativeSumMass(ChartHistogramAggregation aggregation) {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d });
        foreach (var encoding in new[] { ChartHistogramEncoding.Probability, ChartHistogramEncoding.ProbabilityDensity })
            Assert.Throws<ArgumentException>(() => Chart.Create().AddHistogram("Not a distribution", new[] { new ChartPoint(.5, 2) }, layout, aggregation, encoding));
    }

    [Theory]
    [InlineData(ChartHistogramEncoding.Probability)]
    [InlineData(ChartHistogramEncoding.ProbabilityDensity)]
    public void ProbabilityRejectsASecondStackNormalization(ChartHistogramEncoding encoding) {
        var chart = Chart.Create().WithStackedBars().AddHistogram("Probability", new[] { .25, .75 },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d }), encoding);
        chart.Series[0].WithNormalization(100);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Theory]
    [InlineData(ChartHistogramEncoding.Probability, ChartHistogramEncoding.Value)]
    [InlineData(ChartHistogramEncoding.Value, ChartHistogramEncoding.Probability)]
    [InlineData(ChartHistogramEncoding.ProbabilityDensity, ChartHistogramEncoding.Value)]
    [InlineData(ChartHistogramEncoding.Value, ChartHistogramEncoding.ProbabilityDensity)]
    [InlineData(ChartHistogramEncoding.ProbabilityDensity, ChartHistogramEncoding.Density)]
    [InlineData(ChartHistogramEncoding.Density, ChartHistogramEncoding.ProbabilityDensity)]
    public void StacksRejectIncompatibleHistogramUnitsInEitherSeriesOrder(ChartHistogramEncoding first, ChartHistogramEncoding second) {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 3d });
        var chart = Chart.Create().WithStackedBars().AddHistogram("First", new[] { .25, 2d }, layout, first)
            .AddHistogram("Second", new[] { .25, 2d }, layout, second);
        Assert.Throws<InvalidOperationException>(() => Prepare(chart));
    }

    [Fact]
    public void CompatibleProbabilityStacksKeepEachSeriesOwnNormalizationAndRawCounts() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 3d });
        var chart = Chart.Create().WithStackedBars()
            .AddHistogram("First", new[] { .25, 2d }, layout, ChartHistogramEncoding.Probability)
            .AddHistogram("Second", new[] { .25, .75, 2d }, layout, ChartHistogramEncoding.Probability);
        Assert.All(chart.Series, series => Assert.Equal(1, series.HistogramBins.Sum(bin => bin.RenderedValue), 12));
        var prepared = Prepare(chart);
        Assert.Equal("0.5", Point(prepared, 1, 0).Metadata["data-cfx-base"]);
        Assert.Equal((.5 + 2d / 3).ToString("G17", System.Globalization.CultureInfo.InvariantCulture), Point(prepared, 1, 0).Metadata["data-cfx-stack-end"]);
        Assert.Equal("2", Point(prepared, 1, 0).Metadata["data-cfx-y"]);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(640, 400)), frame: new VisualFrame(showLegend: false)));
    private static ChartRect Bounds(PreparedVisual prepared, int point) =>
        prepared.Regions.Single(region => region.Id == "series-0-point-" + point && region.Role == "point").Bounds;
    private static VisualSceneGroup Point(PreparedVisual prepared, int series, int point) =>
        prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == "series-" + series + "-point-" + point && node.Role == "point");
}
