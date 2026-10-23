using System;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TrendLineIntervalsTests {
    [Fact]
    public void QuadraticPointwiseIntervalsUseResidualDegreesOfFreedomAndPredictionVariation() {
        // The error vector is orthogonal to 1, X and X². The fitted curve is 1+2X+3X²,
        // SSE=70, df=2, s²=35 and leverage at zero=17/35.
        var errors = new[] { 1d, -4d, 6d, -4d, 1d };
        var source = Enumerable.Range(-2, 5).Select((x, index) => new ChartPoint(x, 1 + 2 * x + 3 * x * x + errors[index])).ToArray();
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("quadratic"), "Quadratic uncertainty", source)
            .ConfigureTrendLine(options => { options.Model = ChartTrendLineModel.Polynomial; options.Interval = ChartTrendLineIntervalKind.Confidence; });
        var confidence = series.TrendLineSamples.Single(sample => sample.X == 0);
        var critical = Math.Sqrt(2) * .95 / Math.Sqrt((1 - .95) * (1 + .95));
        Assert.Equal(1, confidence.Value, 10);
        Assert.Equal(critical * Math.Sqrt(17), confidence.Upper!.Value - confidence.Value, 9);
        Assert.Equal(critical * Math.Sqrt(17), confidence.Value - confidence.Lower!.Value, 9);
        series.ConfigureTrendLine(options => options.Interval = ChartTrendLineIntervalKind.Prediction);
        var prediction = series.TrendLineSamples.Single(sample => sample.X == 0);
        Assert.Equal(critical * Math.Sqrt(52), prediction.Upper!.Value - prediction.Value, 9);
        Assert.True(prediction.Upper > confidence.Upper); Assert.True(prediction.Lower < confidence.Lower);
        var chart = Chart.Create().AddSeries(series); var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.True(prepared.YProjection!.Minimum <= series.TrendLineSamples.Min(sample => sample.Lower!.Value));
        Assert.True(prepared.YProjection.Maximum >= series.TrendLineSamples.Max(sample => sample.Upper!.Value));
        Assert.Single(prepared.Scene.Nodes.OfType<VisualScenePath>(), path => path.Role == "trend-line-interval");
        Assert.Equal(2, prepared.Regions.Count(region => region.Role == "point")); Assert.NotEmpty(prepared.ToPng());
    }

    [Theory]
    [InlineData(3, 12.706204736432095)]
    [InlineData(6, 2.7764451051977987)]
    [InlineData(32, 2.042272456301238)]
    public void LinearIntervalsAgreeWithIndependentSmallAndModerateSampleStudentCriticalValues(int count, double critical) {
        // (1,-2,1,0,...) is orthogonal to intercept and X: SSE=6 and h at the mean X=1/n.
        var source = Enumerable.Range(0, count).Select(index => new ChartPoint(index,
            2 + 3 * index + (index == 0 || index == 2 ? 1 : index == 1 ? -2 : 0))).ToArray();
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("linear"), "Student interval", source)
            .ConfigureTrendLine(options => options.Interval = ChartTrendLineIntervalKind.Confidence);
        var middle = series.TrendLineSamples.Single(sample => sample.X == (count - 1) / 2d);
        var expected = critical * Math.Sqrt(6d / (count - 2) / count);
        Assert.Equal(expected, middle.Upper!.Value - middle.Value, 8);
        Assert.Equal(expected, middle.Value - middle.Lower!.Value, 8);
        Assert.True(expected > 1.96 * Math.Sqrt(6d / (count - 2) / count));
    }

    [Theory]
    [InlineData(ChartTrendLineIntervalKind.Confidence)]
    [InlineData(ChartTrendLineIntervalKind.Prediction)]
    public void LogResponseFitsExposePositiveAsymmetricMedianIntervals(ChartTrendLineIntervalKind interval) {
        var errors = new[] { 1d, -4d, 6d, -4d, 1d };
        var source = Enumerable.Range(1, 5).Select((x, index) => new ChartPoint(x, 2 * Math.Exp(.3 * x + .02 * errors[index]))).ToArray();
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("exponential"), "Log-fitted median", source)
            .ConfigureTrendLine(options => { options.Model = ChartTrendLineModel.Exponential; options.Interval = interval; });
        Assert.All(series.TrendLineSamples, sample => {
            Assert.Equal(2 * Math.Exp(.3 * sample.X), sample.Value, 9);
            Assert.True(sample.Lower > 0); Assert.True(sample.Lower < sample.Value); Assert.True(sample.Upper > sample.Value);
            Assert.Equal(sample.Value * sample.Value, sample.Lower!.Value * sample.Upper!.Value, 8);
        });
        var chart = Chart.Create().AddSeries(series); var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var regression = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Single(group => group.Role == "regression");
        Assert.Equal("median", regression.Metadata["data-cfx-fit-target"]); Assert.Equal("log", regression.Metadata["data-cfx-fit-response-scale"]);
        Assert.Equal(interval.ToString().ToLowerInvariant(), regression.Metadata["data-cfx-pointwise-interval"]);
    }

    [Fact]
    public void IntervalConfigurationRequiresResidualDegreesOfFreedomAndCapturesConfidenceIndependently() {
        var shortSeries = ChartSeries.CreateTrendLine(new ChartSeriesId("two"), "Two exact points", new[] { new ChartPoint(0, 1), new ChartPoint(1, 2) });
        Assert.Throws<ArgumentException>(() => shortSeries.ConfigureTrendLine(options => options.Interval = ChartTrendLineIntervalKind.Confidence));
        Assert.Equal(ChartTrendLineIntervalKind.None, shortSeries.TrendLineConfiguration!.Interval);
        var series = ChartSeries.CreateTrendLine(new ChartSeriesId("three"), "Three points", new[] { new ChartPoint(0, 1), new ChartPoint(1, 3), new ChartPoint(2, 2) });
        var previous = 0d;
        foreach (var confidence in new[] { .5, .9, .95, .99, .999 }) {
            series.ConfigureTrendLine(options => { options.Interval = ChartTrendLineIntervalKind.Confidence; options.ConfidenceLevel = confidence; });
            var middle = series.TrendLineSamples.Single(sample => sample.X == 1); var width = middle.Upper!.Value - middle.Lower!.Value;
            Assert.True(width > previous); previous = width;
        }
        var before = series.TrendLineSamples.ToArray();
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigureTrendLine(options => options.ConfidenceLevel = 1));
        Assert.Equal(before, series.TrendLineSamples);
    }
}
