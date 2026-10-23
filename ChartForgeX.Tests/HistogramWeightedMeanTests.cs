using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HistogramWeightedMeanTests {
    [Fact]
    public void WeightedMeansUseIndependentWeightsAndRetainSourceIdentityAndUndefinedBins() {
        var source = new[] { new ChartHistogramObservation(.25, 10, 1), new ChartHistogramObservation(.75, 20, 3), new ChartHistogramObservation(2, 999, 0) };
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d, 3d, 4d });
        var series = ChartSeries.CreateHistogram(new ChartSeriesId("weighted"), "Weighted quantity", source, layout, ChartHistogramAggregation.WeightedMean);
        var chart = Chart.Create().WithAxes(false).WithGrid(false).AddSeries(series);
        Assert.Equal(17.5, series.HistogramBins[0].Value);
        Assert.Equal(2, series.HistogramBins[0].Count);
        Assert.Equal(new[] { 0, 1 }, series.HistogramBins[0].SourceIndices);
        Assert.Null(series.HistogramBins[1].Value); Assert.Null(series.HistogramBins[2].Value);
        Assert.Equal(new[] { 1d, 3d, 0d }, series.HistogramSourceWeights);
        Assert.Equal(new[] { 10d, 20d, 999d }, series.HistogramSourcePoints.Select(point => point.Y));
        Assert.Throws<NotSupportedException>(() => ((IList<double>)series.HistogramSourceWeights)[0] = 99);
        var prepared = Prepare(chart); var svg = prepared.ToSvg(); var png = prepared.ToPng();
        var empty = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(node => node.Id == "series-0-point-1" && node.Role == "point");
        Assert.Equal("false", empty.Metadata["data-cfx-bin-has-value"]);
        Assert.False(empty.Metadata.ContainsKey("data-cfx-y"));
        Assert.Equal("1", empty.Metadata["data-cfx-bin-count"]);
        source[0] = new ChartHistogramObservation(99, 99, 99); chart.ClearSeries();
        Assert.Equal(1, series.HistogramSourceWeights[0]); Assert.Equal(.25, series.HistogramSourcePoints[0].X);
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
    }

    [Theory]
    [InlineData(ChartHistogramAggregation.Count, 2)]
    [InlineData(ChartHistogramAggregation.Sum, 30)]
    [InlineData(ChartHistogramAggregation.Mean, 15)]
    [InlineData(ChartHistogramAggregation.WeightedMean, 17.5)]
    public void WeightsDoNotRedefineExistingCountSumOrMeanSemantics(ChartHistogramAggregation aggregation, double expected) {
        var source = new[] { new ChartHistogramObservation(.25, 10, 1), new ChartHistogramObservation(.75, 20, 3) };
        var chart = Chart.Create().AddHistogram("Quantity", source, ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d }), aggregation);
        Assert.Equal(expected, Assert.Single(chart.Series[0].HistogramBins).Value);
    }

    [Fact]
    public void OriginalPointInputUsesUnitWeightsAndWeightedDensityPreservesRawMean() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 2d });
        var original = new[] { new ChartPoint(.25, -4), new ChartPoint(.75, 12) };
        var ordinary = Chart.Create().AddHistogram("Mean", original, layout, ChartHistogramAggregation.Mean);
        var unit = Chart.Create().AddHistogram("Unit weighted", original, layout, ChartHistogramAggregation.WeightedMean);
        Assert.Equal(ordinary.Series[0].HistogramBins[0].Value, unit.Series[0].HistogramBins[0].Value);
        Assert.Empty(unit.Series[0].HistogramSourceWeights);
        var weighted = Chart.Create().AddHistogram("Density", new[] { new ChartHistogramObservation(.25, -4, 1), new ChartHistogramObservation(.75, 12, 3) },
            layout, ChartHistogramAggregation.WeightedMean, ChartHistogramEncoding.Density);
        Assert.Equal(8, weighted.Series[0].HistogramBins[0].Value);
        Assert.Equal(4, weighted.Series[0].HistogramBins[0].RenderedValue);
        Assert.True(Prepare(weighted).ToPng().Length > 64);
    }

    [Fact]
    public void FiniteWeightedMeansSurviveLargeProductsCancellationAndSmallContributions() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d });
        double? Mean(params ChartHistogramObservation[] values) =>
            Chart.Create().AddHistogram("Weighted", values, layout, ChartHistogramAggregation.WeightedMean).Series[0].HistogramBins[0].Value;
        Assert.Equal(1e308, Mean(new ChartHistogramObservation(.5, 1e308, 1e308), new ChartHistogramObservation(.5, 1e308, 1e308)));
        Assert.Equal(0, Mean(new ChartHistogramObservation(.5, 1e308, 1e308), new ChartHistogramObservation(.5, -1e308, 1e308)));
        var small = Mean(new ChartHistogramObservation(.5, 0, 1e308), new ChartHistogramObservation(.5, 1e308, 1e-308));
        Assert.True(small.HasValue && small.Value > 0);
        Assert.InRange(small!.Value / 1e-308, .999999999999, 1.000000000001);
        Assert.Null(Mean(new ChartHistogramObservation(.5, 42, 0)));
    }

    [Fact]
    public void EqualPositiveWeightsPreserveSubnormalMeansAndImplicitUnitWeightResults() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d });
        var cases = new[] {
            (Values: new[] { 0d, double.Epsilon, double.Epsilon }, Expected: double.Epsilon),
            (Values: new[] { 0d, -double.Epsilon, -double.Epsilon }, Expected: -double.Epsilon),
            (Values: new[] { 0d, double.Epsilon }, Expected: 0d),
            (Values: new[] { double.Epsilon, 2 * double.Epsilon }, Expected: 2 * double.Epsilon)
        };
        foreach (var item in cases) {
            var implicitUnit = ChartSeries.CreateHistogram(new ChartSeriesId("implicit"), "Implicit unit weights",
                item.Values.Select(value => new ChartPoint(.5, value)), layout, ChartHistogramAggregation.WeightedMean);
            Assert.Equal(item.Expected, implicitUnit.HistogramBins[0].Value);
            foreach (var weight in new[] { 1d, 2d, 1e308, double.Epsilon }) {
                var explicitUnit = ChartSeries.CreateHistogram(new ChartSeriesId("explicit"), "Equal positive weights",
                    item.Values.Select(value => new ChartHistogramObservation(.5, value, weight)), layout, ChartHistogramAggregation.WeightedMean);
                Assert.Equal(implicitUnit.HistogramBins[0].Value, explicitUnit.HistogramBins[0].Value);
                Assert.Equal(item.Values, explicitUnit.HistogramSourcePoints.Select(point => point.Y));
            }
        }
    }

    [Fact]
    public void UnequalWeightsRoundTheCompleteSubnormalMeanAndRetainSmallResidualsAfterCancellation() {
        var layout = ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d });
        double? Mean(params ChartHistogramObservation[] source) => ChartSeries.CreateHistogram(new ChartSeriesId("weighted"),
            "Weighted", source, layout, ChartHistogramAggregation.WeightedMean).HistogramBins[0].Value;
        Assert.Equal(double.Epsilon, Mean(new ChartHistogramObservation(.5, double.Epsilon, 1), new ChartHistogramObservation(.5, double.Epsilon, 2)));
        Assert.Equal(double.Epsilon, Mean(new ChartHistogramObservation(.5, 0, 2), new ChartHistogramObservation(.5, 3 * double.Epsilon, 1)));
        Assert.Equal(double.Epsilon, Mean(new ChartHistogramObservation(.5, 0, 1), new ChartHistogramObservation(.5, double.Epsilon, 3)));
        var cancellation = new[] { new ChartHistogramObservation(.5, double.MaxValue, double.MaxValue),
            new ChartHistogramObservation(.5, -double.MaxValue, double.MaxValue), new ChartHistogramObservation(.5, 1, 1) };
        Assert.Equal(.5 / double.MaxValue, Mean(cancellation));
        Assert.Equal(Mean(cancellation), Mean(cancellation.AsEnumerable().Reverse().ToArray()));
        var raw = new[] { new ChartPoint(.5, 1e308), new ChartPoint(.5, -1e308), new ChartPoint(.5, 3 * double.Epsilon) };
        var implicitUnit = ChartSeries.CreateHistogram(new ChartSeriesId("implicit"), "Unit cancellation", raw, layout, ChartHistogramAggregation.WeightedMean);
        Assert.Equal(double.Epsilon, implicitUnit.HistogramBins[0].Value);
        Assert.Equal(implicitUnit.HistogramBins[0].Value, Mean(raw.Select(point => new ChartHistogramObservation(point.X, point.Y)).ToArray()));
    }

    [Fact]
    public void InvalidWeightsAndOutOfLayoutMeasurementsDoNotAppendAPartialSeries() {
        foreach (var weight in new[] { -1d, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => new ChartHistogramObservation(.5, 1, weight));
        var chart = Chart.Create().AddLine("Retained", new[] { new ChartPoint(0, 1) });
        Assert.Throws<ArgumentOutOfRangeException>(() => chart.AddHistogram("Rejected", new[] { new ChartHistogramObservation(.5, 1), new ChartHistogramObservation(3, 2) },
            ChartHistogramBinLayout.FromBoundaries(new[] { 0d, 1d }), ChartHistogramAggregation.WeightedMean));
        Assert.Single(chart.Series);
    }

    private static PreparedVisual Prepare(Chart chart) => chart.Prepare(new VisualRenderContext(
        new VisualLayoutOptions(new VisualSize(640, 400)), frame: new VisualFrame(showLegend: false)));
}
