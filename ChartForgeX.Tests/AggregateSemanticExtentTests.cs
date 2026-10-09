using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class AggregateSemanticExtentTests {
    [Theory]
    [InlineData(ChartSeriesKind.Bar, 10)]
    [InlineData(ChartSeriesKind.TrendLine, 2)]
    [InlineData(ChartSeriesKind.BoxPlot, 1)]
    public void RawAggregatesDescribeTheirRenderedExtentsAndRetainTypedSourceFacts(ChartSeriesKind kind, int extents) {
        var observations = Enumerable.Range(0, 4096).Select(index => new ChartPoint(index % 10 + .5, index % 7 - 3)).ToArray();
        var chart = Chart.Create().WithAxes(false).WithGrid(false);
        switch (kind) {
            case ChartSeriesKind.Bar:
                chart.AddHistogram("Measurements", observations,
                    ChartHistogramBinLayout.FromBoundaries(Enumerable.Range(0, 11).Select(value => (double)value)), ChartHistogramAggregation.Sum);
                Assert.Equal(observations, chart.Series[0].HistogramSourcePoints);
                Assert.Equal(Enumerable.Range(0, observations.Length), chart.Series[0].HistogramBins.SelectMany(bin => bin.SourceIndices).OrderBy(index => index));
                break;
            case ChartSeriesKind.TrendLine:
                chart.AddTrendLine("Regression", observations);
                Assert.Equal(observations, chart.Series[0].TrendSourcePoints);
                break;
            case ChartSeriesKind.BoxPlot:
                chart.AddBoxPlot("Distribution", 1, observations.Select(point => point.Y));
                Assert.Equal(observations.Select(point => point.Y), chart.Series[0].BoxPlotSourceSamples);
                break;
        }
        Assert.Equal(observations.Length, chart.Series[0].SourcePointCount);
        var prepared = chart.Prepare(new VisualRenderContext(frame: new VisualFrame(showLegend: false)));
        Assert.Equal(extents, prepared.Regions.Count);
        Assert.All(prepared.Regions, region => Assert.Equal("point", region.Role));
    }
}
