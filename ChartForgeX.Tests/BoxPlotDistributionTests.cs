using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BoxPlotDistributionTests {
    [Fact]
    public void ExistingRawSampleOverloadAcceptsFiniteOppositeSignExtrema() {
        var series = ChartSeries.CreateBoxPlot(new ChartSeriesId("legacy-raw"), "Raw", 1, new[] { -double.MaxValue, double.MaxValue });
        Assert.Equal(-double.MaxValue, series.BoxPlots[0].Minimum); Assert.Equal(double.MaxValue, series.BoxPlots[0].Maximum);
        Assert.Equal(0, series.BoxPlots[0].Median);
        Assert.True(double.IsFinite(series.BoxPlots[0].Q1)); Assert.True(double.IsFinite(series.BoxPlots[0].Q3));
    }
    [Fact]
    public void GroupedInputCopiesSourceOrderAndCalculatesTheExistingMinMaxEstimator() {
        var samples = new[] { 30d, 0, 20, 10 };
        var group = new ChartBoxPlotSampleGroup(2, samples); samples[0] = 999;
        var series = Raw(group, new ChartBoxPlotSampleGroup(2, new[] { 4d }));
        Assert.Equal(new[] { 30d, 0, 20, 10, 4 }, series.BoxPlotSourceSamples);
        Assert.Equal(2, series.ObservationCount); Assert.Equal(5, series.SourcePointCount);
        var first = series.BoxPlotDistributions[0]; var summary = first.Summary;
        Assert.Equal(new[] { 0d, 7.5, 15, 22.5, 30 }, Values(summary));
        Assert.Equal(new[] { 0, 1, 2, 3 }, first.SourceIndices);
        Assert.Equal(new[] { 4 }, series.BoxPlotDistributions[1].SourceIndices);
        Assert.All(Values(series.BoxPlots[1]), value => Assert.Equal(4, value));
        Assert.All(series.BoxPlotDistributions, item => { Assert.Empty(item.OutlierSourceIndices); Assert.Equal(ChartBoxPlotWhiskerPolicy.MinMax, item.WhiskerPolicy); });
        Assert.Equal(ChartBoxPlotQuartileEstimator.LinearInterpolated, series.BoxPlotQuartileEstimator);
        Assert.Throws<NotSupportedException>(() => ((IList<double>)group.Samples)[0] = 100);
    }

    [Fact]
    public void IqrUsesFullSampleQuartilesAndOriginalOutlierIndices() {
        var series = Raw(new ChartBoxPlotSampleGroup(1, new[] { 100d, 0, 1, 2, 3, 4, 5, 6, 7, 8 }),
            new ChartBoxPlotSampleGroup(1, new[] { 1d, 1, 1, 1, 2 }));
        series.ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        Assert.Equal(new[] { 0d, 2.25, 4.5, 6.75, 8 }, Values(series.BoxPlots[0]));
        Assert.Equal(new[] { 0 }, series.BoxPlotDistributions[0].OutlierSourceIndices);
        Assert.Equal(new[] { 14 }, series.BoxPlotDistributions[1].OutlierSourceIndices);
        Assert.Equal(100, series.BoxPlotDistributions[0].SourceMaximum);
        Assert.Equal(8, series.BoxPlotDistributions[0].InlierMaximum);
    }

    [Fact]
    public void InclusiveFencesKeepTiesAndSeparateSparseInliersFromDrawableWhiskers() {
        var ties = Raw(new ChartBoxPlotSampleGroup(1, new[] { -3d, 0, 1, 2, 5 }));
        ties.ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        Assert.Empty(ties.BoxPlotDistributions[0].OutlierSourceIndices);
        ties.UpdateBoxPlotSampleGroup(0, new ChartBoxPlotSampleGroup(1, new[] { Math.BitDecrement(-3d), 0, 1, 2, Math.BitIncrement(5d) }));
        Assert.Equal(new[] { 0, 4 }, ties.BoxPlotDistributions[0].OutlierSourceIndices);
        var sparse = Raw(new ChartBoxPlotSampleGroup(1, new[] { 0d, 100, 101, 102 }));
        sparse.ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        var result = sparse.BoxPlotDistributions[0];
        Assert.Equal(0, result.SourceMinimum); Assert.Equal(100, result.InlierMinimum); Assert.Equal(75, result.Summary.Minimum);
        Assert.Equal(75, result.Summary.Q1); Assert.Equal(new[] { 0 }, result.OutlierSourceIndices);
        var none = Raw(new ChartBoxPlotSampleGroup(1, new[] { 0d, 10 }));
        none.ConfigureBoxPlotStatistics(options => { options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr; options.IqrMultiplier = double.Epsilon; });
        Assert.Null(none.BoxPlotDistributions[0].InlierMinimum); Assert.Null(none.BoxPlotDistributions[0].InlierMaximum);
        Assert.Equal(new[] { 2.5, 2.5, 5, 7.5, 7.5 }, Values(none.BoxPlots[0]));
        Assert.Equal(new[] { 0, 1 }, none.BoxPlotDistributions[0].OutlierSourceIndices);
    }

    [Fact]
    public void ExtremeFiniteSamplesInterpolateAndClassifyWithoutOverflowOrNeutralFallback() {
        var series = Raw(new ChartBoxPlotSampleGroup(1, new[] { -double.MaxValue, double.MaxValue }));
        Assert.Equal(0, series.BoxPlots[0].Median);
        Assert.True(double.IsFinite(series.BoxPlots[0].Q1)); Assert.True(double.IsFinite(series.BoxPlots[0].Q3));
        series.ConfigureBoxPlotStatistics(options => { options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr; options.IqrMultiplier = double.MaxValue; });
        Assert.Empty(series.BoxPlotDistributions[0].OutlierSourceIndices);
        var remote = Raw(new ChartBoxPlotSampleGroup(1, new[] { 0d, double.Epsilon, double.Epsilon, double.Epsilon, double.MaxValue }));
        remote.ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        Assert.Equal(new[] { 0, 4 }, remote.BoxPlotDistributions[0].OutlierSourceIndices);
    }

    [Fact]
    public void StatisticsCallbacksAndInvalidInputsRollbackAllInstalledState() {
        var series = Keyed(); var groups = series.BoxPlotSourceGroups; var distributions = series.BoxPlotDistributions;
        Assert.Throws<InvalidOperationException>(() => series.ConfigureBoxPlotStatistics(options => { options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr; throw new InvalidOperationException(); }));
        foreach (var value in new[] { 0d, -1, double.NaN, double.PositiveInfinity })
            Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigureBoxPlotStatistics(options => options.IqrMultiplier = value));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = (ChartBoxPlotWhiskerPolicy)99));
        Assert.Throws<InvalidOperationException>(() => series.ReplaceBoxPlotSampleGroups(BrokenGroups()));
        Assert.Throws<ArgumentException>(() => series.ReplaceBoxPlotSampleGroups(new ChartBoxPlotSampleGroup[] { null! }));
        Assert.Same(groups, series.BoxPlotSourceGroups); Assert.Same(distributions, series.BoxPlotDistributions);
        Assert.Equal("group-a", series.BoxPlotGroupIds[0].Value); Assert.Equal("sample-4", series.ObservationIds[4].Value);
        ChartBoxPlotStatisticsOptions? escaped = null;
        series.ConfigureBoxPlotStatistics(options => { escaped = options; options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr; });
        escaped!.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.MinMax;
        Assert.Equal(ChartBoxPlotWhiskerPolicy.Iqr, series.BoxPlotWhiskerPolicy);
        Assert.Equal("group-a", series.BoxPlotGroupIds[0].Value); Assert.Equal("sample-4", series.ObservationIds[4].Value);
        Assert.Throws<ArgumentException>(() => new ChartBoxPlotSampleGroup(1, Array.Empty<double>()));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartBoxPlotSampleGroup(double.NaN, new[] { 1d }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChartBoxPlotSampleGroup(1, new[] { double.NaN }));
    }

    [Fact]
    public void UpdatingOneGroupPreservesGroupPresentationAndShiftsOnlyUnaffectedSampleIds() {
        var series = Keyed(); series.PointLabels.Add("A"); series.PointColors.Add(ChartColor.FromRgb(220, 30, 30));
        series.UpdateBoxPlotSampleGroup(0, new ChartBoxPlotSampleGroup(1, new[] { 8d, 9, 10, 11 }));
        Assert.Equal(7, series.SourcePointCount); Assert.Equal(2, series.ObservationCount);
        Assert.Equal(new[] { 4, 5, 6 }, series.ObservationIds.Keys);
        Assert.Equal(new[] { "sample-2", "sample-3", "sample-4" }, series.ObservationIds.Values.Select(value => value.Value));
        Assert.Equal("group-a", series.BoxPlotGroupIds[0].Value); Assert.Equal("group-b", series.BoxPlotGroupIds[1].Value);
        Assert.Equal("A", series.PointLabels[0]); Assert.Equal(ChartColor.FromRgb(220, 30, 30), series.PointColors[0]);
        series.WithObservationId(0, new ChartObservationId("group-a")); // Separate namespace permits identical strings.
        Assert.Throws<ArgumentException>(() => series.WithBoxPlotGroupId(1, new ChartObservationId("group-a")));
        Assert.Throws<ArgumentException>(() => series.WithBoxPlotGroupIds(new[] { new ChartObservationId("same"), new ChartObservationId("same") }));
        Assert.Throws<ArgumentException>(() => series.WithBoxPlotGroupIds(new[] { new ChartObservationId("short") }));
        Assert.Equal("group-b", series.BoxPlotGroupIds[1].Value);
        series.UpdateBoxPlotSampleGroup(0, new ChartBoxPlotSampleGroup(1, new[] { 12d }));
        Assert.Equal(new[] { 1, 2, 3 }, series.ObservationIds.Keys);
        Assert.Equal(new[] { "sample-2", "sample-3", "sample-4" }, series.ObservationIds.Values.Select(value => value.Value));
        var installed = series.BoxPlotSourceGroups;
        Assert.Throws<ArgumentOutOfRangeException>(() => series.UpdateBoxPlotSampleGroup(-1, new ChartBoxPlotSampleGroup(1, new[] { 1d })));
        Assert.Throws<ArgumentOutOfRangeException>(() => series.UpdateBoxPlotSampleGroup(2, new ChartBoxPlotSampleGroup(1, new[] { 1d })));
        Assert.Throws<ArgumentNullException>(() => series.UpdateBoxPlotSampleGroup(0, null!));
        Assert.Same(installed, series.BoxPlotSourceGroups);
    }

    [Fact]
    public void ReplacementAndSummaryConversionKeepPolicyWithoutReinterpretingSourceIdentities() {
        var series = Keyed().ConfigureBoxPlotStatistics(options => options.WhiskerPolicy = ChartBoxPlotWhiskerPolicy.Iqr);
        series.PointLabels.Add("Retained edit caption");
        series.UpdateBoxPlot(0, new ChartBoxPlot(1, 0, 1, 2, 3, 4));
        Assert.Empty(series.BoxPlotSourceGroups); Assert.Empty(series.BoxPlotSourceSamples); Assert.Empty(series.BoxPlotDistributions);
        Assert.Empty(series.ObservationIds); Assert.Empty(series.BoxPlotGroupIds); Assert.Null(series.BoxPlotWhiskerPolicy);
        Assert.Equal("Retained edit caption", series.PointLabels[0]); Assert.Equal(2, series.SourcePointCount);
        Assert.Throws<InvalidOperationException>(() => series.ConfigureBoxPlotStatistics(_ => { }));
        Assert.Throws<InvalidOperationException>(() => series.WithBoxPlotGroupId(0, new ChartObservationId("no")));
        series.ReplaceBoxPlotSampleGroups(new[] { new ChartBoxPlotSampleGroup(1, new[] { 1d, 2, 3 }) });
        Assert.Equal(ChartBoxPlotWhiskerPolicy.Iqr, series.BoxPlotWhiskerPolicy); Assert.Empty(series.PointLabels);
        series.WithBoxPlotGroupId(0, new ChartObservationId("new")).WithObservationIds(new[] { new ChartObservationId("a"), new ChartObservationId("b"), new ChartObservationId("c") });
        series.ReplaceBoxPlots(new[] { new ChartBoxPlot(1, 0, 1, 2, 3, 4) });
        Assert.Empty(series.ObservationIds); Assert.Empty(series.BoxPlotGroupIds); Assert.Null(series.BoxPlotQuartileEstimator);
        series.ReplaceBoxPlotSampleGroups(Array.Empty<ChartBoxPlotSampleGroup>()).ConfigureBoxPlotStatistics(options => options.IqrMultiplier = 2);
        Assert.Equal(0, series.ObservationCount); Assert.Equal(0, series.SourcePointCount); Assert.Equal(2, series.BoxPlotIqrMultiplier);
        var empty = Chart.Create().AddSeries(series).Prepare(new VisualRenderContext());
        Assert.DoesNotContain(empty.Scene.Nodes, node => node.Role is "boxplot-body" or "boxplot-outlier");
        Assert.NotEmpty(empty.ToPng());
        Assert.Throws<ArgumentException>(() => Chart.Create().AddBoxPlot("Empty", Array.Empty<ChartBoxPlotSampleGroup>()));
    }

    private static IEnumerable<ChartBoxPlotSampleGroup> BrokenGroups() { yield return new ChartBoxPlotSampleGroup(1, new[] { 1d }); throw new InvalidOperationException(); }
    private static ChartSeries Raw(params ChartBoxPlotSampleGroup[] groups) => ChartSeries.CreateBoxPlot(new ChartSeriesId("raw"), "Raw", groups);
    private static ChartSeries Keyed() => Raw(new ChartBoxPlotSampleGroup(1, new[] { 0d, 1 }), new ChartBoxPlotSampleGroup(1, new[] { 2d, 3, 4 }))
        .WithBoxPlotGroupIds(new[] { new ChartObservationId("group-a"), new ChartObservationId("group-b") })
        .WithObservationIds(Enumerable.Range(0, 5).Select(index => new ChartObservationId("sample-" + index)));
    private static double[] Values(ChartBoxPlot value) => new[] { value.Minimum, value.Q1, value.Median, value.Q3, value.Maximum };
}
