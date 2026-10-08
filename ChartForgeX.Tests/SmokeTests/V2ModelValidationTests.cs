using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Protects canonical mutable-model validation at native preparation and public export boundaries.</summary>
public sealed class V2ModelValidationTests {
    [Fact]
    public void EveryExportRejectsSpecializedCardinalityAndMixedKinds() {
        Reject(Chart.Create().AddGauge("First", 70).AddGauge("Second", 80));
        Reject(Chart.Create().AddGauge("Score", 70).AddLine("Trend", new[] { new ChartPoint(1, 2) }));
        Reject(Chart.Create().AddPie("First", new[] { new ChartPoint(1, 2) }).AddDonut("Second", new[] { new ChartPoint(1, 2) }));
    }

    [Fact]
    public void SharedMutableCollectionsAreValidatedForSpecializedFamilies() {
        var gauge = Chart.Create().AddGauge("Score", 70);
        gauge.Annotations.Add(null!);
        Reject(gauge);
        var radial = Raw(ChartSeriesKind.LayeredRadial);
        radial.Series[0].RadialLayers.Add(null!);
        Reject(radial);
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie)]
    [InlineData(ChartSeriesKind.Donut)]
    [InlineData(ChartSeriesKind.Funnel)]
    [InlineData(ChartSeriesKind.Treemap)]
    [InlineData(ChartSeriesKind.Polar)]
    [InlineData(ChartSeriesKind.PolarArea)]
    [InlineData(ChartSeriesKind.RadialBar)]
    public void NonNegativeFamiliesRejectNegativeSourceData(ChartSeriesKind kind) {
        Reject(Raw(kind, new ChartPoint(1, 4), new ChartPoint(2, -1)));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Bubble)]
    [InlineData(ChartSeriesKind.ErrorBar)]
    [InlineData(ChartSeriesKind.Candlestick)]
    [InlineData(ChartSeriesKind.RangeArea)]
    [InlineData(ChartSeriesKind.BoxPlot)]
    [InlineData(ChartSeriesKind.Sankey)]
    [InlineData(ChartSeriesKind.Tree)]
    [InlineData(ChartSeriesKind.Gantt)]
    [InlineData(ChartSeriesKind.Gauge)]
    public void PreparationDoesNotTreatIncompleteTuplesAsEmptyData(ChartSeriesKind kind) {
        Reject(Raw(kind, new ChartPoint(0, 1)));
    }

    [Theory]
    [InlineData(ChartSeriesKind.Pie, "radial.no-data")]
    [InlineData(ChartSeriesKind.Donut, "radial.no-data")]
    [InlineData(ChartSeriesKind.Waterfall, "cartesian.no-data")]
    [InlineData(ChartSeriesKind.Bubble, "cartesian.no-data")]
    [InlineData(ChartSeriesKind.Radar, "polar.no-data")]
    [InlineData(ChartSeriesKind.RadialBar, "radial.no-data")]
    [InlineData(ChartSeriesKind.LayeredRadial, "radial.no-data")]
    [InlineData(ChartSeriesKind.Tree, "hierarchy.no-data")]
    [InlineData(ChartSeriesKind.Sankey, "sankey.no-data")]
    [InlineData(ChartSeriesKind.WordCloud, "specialty.no-data")]
    public void EmptyNativeFamiliesRetainTheirNoDataContract(ChartSeriesKind kind, string diagnostic) {
        var chart = Raw(kind);
        var prepared = chart.Prepare(new VisualRenderContext());
        Assert.Contains(prepared.Diagnostics, item => item.Code == diagnostic);
        Assert.Empty(chart.Series[0].Points);
        Assert.NotEmpty(prepared.ToSvg());
    }

    [Fact]
    public void ZeroWeightsRenderWithoutInventingPositiveObservations() {
        foreach (var kind in new[] { ChartSeriesKind.Pie, ChartSeriesKind.Donut, ChartSeriesKind.Treemap, ChartSeriesKind.Pictorial, ChartSeriesKind.WordCloud, ChartSeriesKind.PolarArea }) {
            var chart = Raw(kind, new ChartPoint(1, 0), new ChartPoint(2, 0));
            var prepared = chart.Prepare(new VisualRenderContext());
            Assert.All(chart.Series[0].Points, point => Assert.Equal(0, point.Y));
            Assert.NotEmpty(prepared.ToSvg());
            Assert.NotEmpty(chart.ToPng());
        }
    }

    [Fact]
    public void GridSharedAxisPreparationAcceptsZeroWeightSpecializedSiblings() {
        var grid = ChartGrid.Create().Add(Raw(ChartSeriesKind.Pie, new ChartPoint(1, 0)))
            .Add(Chart.Create().AddLine("Trend", new[] { new ChartPoint(1, 2), new ChartPoint(2, 4) }));
        grid.WithSharedXAxis().WithSharedYAxis();
        Assert.NotEmpty(grid.ToSvg());
    }

    [Fact]
    public void ExplicitHostFrameControlsLegendIndependentlyOfConvenienceAutoPolicy() {
        var chart = Chart.Create().AddBar("Only source", new[] { new ChartPoint(1, 2) });
        Assert.DoesNotContain(chart.Prepare(VisualExportRequest.ForChart(chart).Context).Scene.Nodes, node => node.Role == "legend-entry");
        Assert.Contains(chart.Prepare(new VisualRenderContext(frame: new VisualFrame(showLegend: true))).Scene.Nodes, node => node.Role == "legend-entry");
        Assert.DoesNotContain(chart.Prepare(new VisualRenderContext(frame: new VisualFrame(showLegend: false))).Scene.Nodes, node => node.Role == "legend-entry");
    }

    private static Chart Raw(ChartSeriesKind kind, params ChartPoint[] points) {
        var chart = Chart.Create();
        chart.Series.Add(new ChartSeries("Source", kind, points));
        return chart;
    }

    private static void Reject(Chart chart) {
        Assert.Throws<InvalidOperationException>(() => chart.Prepare(new VisualRenderContext()));
        Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        Assert.Throws<InvalidOperationException>(() => chart.ToPng());
    }
}
