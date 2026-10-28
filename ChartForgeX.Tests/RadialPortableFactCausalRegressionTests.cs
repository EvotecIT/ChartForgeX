using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class RadialPortableFactCausalRegressionTests {
    [Fact]
    public void AnExistingMutableChartArtifactExportsTheCurrentExplicitBandAfterANativeEdit() {
        var series = Source(); var chart = Chart.Create().AddSeries(series); var artifact = chart.ToVisualArtifact("radial-facts");
        var captured = artifact.ToInterchangeEnvelope();
        Assert.Equal("0.2", captured.Extensions["chart.series.0.radial.innerRatio"]);
        series.WithRadialBand(new ChartRadialBand(.3, .9));
        Assert.Equal(.3, series.RadialBand!.InnerRatio);
        var edited = artifact.ToInterchangeEnvelope();
        Assert.Equal("0.3", edited.Extensions["chart.series.0.radial.innerRatio"]);
        Assert.Equal("0.9", edited.Extensions["chart.series.0.radial.outerRatio"]);
        Assert.Equal("0.2", captured.Extensions["chart.series.0.radial.innerRatio"]);
    }

    [Fact]
    public void RestoringOrdinaryGeometryRemovesStaleBandFactsFromTheExistingMutableArtifact() {
        var series = Source(); var chart = Chart.Create().AddSeries(series); var artifact = chart.ToVisualArtifact("radial-reset");
        Assert.Equal("0.2", artifact.ToInterchangeEnvelope().Extensions["chart.series.0.radial.innerRatio"]);
        series.WithRadialBand(null); Assert.Null(series.RadialBand);
        var restored = artifact.ToInterchangeEnvelope();
        Assert.DoesNotContain("chart.series.0.radial.innerRatio", restored.Extensions.Keys);
        Assert.DoesNotContain("chart.series.0.radial.outerRatio", restored.Extensions.Keys);
        Assert.Equal("observed", restored.Extensions["chart.series.0.id"]);
    }

    private static ChartSeries Source() => new ChartSeries(new ChartSeriesId("observed"), "Observed", ChartSeriesKind.Donut,
        new[] { new ChartPoint(1, 3), new ChartPoint(2, 1) }).WithRadialBand(new ChartRadialBand(.2, .8));
}
