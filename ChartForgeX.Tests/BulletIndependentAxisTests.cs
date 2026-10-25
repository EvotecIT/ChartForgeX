using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class BulletIndependentAxisTests {
    [Theory]
    [InlineData(false, 390, false)]
    [InlineData(true, 760, true)]
    public void EachIdentifiedRowUsesItsOwnDomainReversalAndCompleteRawFacts(bool vertical, int width, bool dark) {
        var variant = vertical ? "vertical" : width == 390 ? "compact" : "wide";
        var chart = IndependentBulletRecipe.Create(variant, dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var rows = prepared.Scene.Nodes.OfType<VisualSceneGroup>().Where(node => node.Role == "bullet-row").ToArray();
        Assert.Equal(new[] { "1", "1100" }, rows.Select(row => row.Metadata["data-cfx-scale-max"]));
        Assert.Equal(new[] { "load", "queue" }, rows.Select(row => row.Metadata["data-cfx-series-key"]));
        Assert.All(rows, row => Assert.Equal("current", row.Metadata["data-cfx-observation-id"]));
        Assert.Equal("1250", rows[1].Metadata["data-cfx-value"]); Assert.Equal("true", rows[1].Metadata["data-cfx-reversed"]);
        var values = prepared.Scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bullet-value").ToArray();
        var targets = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == "bullet-target").ToArray();
        if (vertical) Assert.True(targets[1].Start.Y < values[1].Bounds.Bottom);
        else Assert.True(targets[1].Start.X > values[1].Bounds.Left);
        var svg = prepared.ToSvg();
        chart.Series[0].UpdateBulletValue(0, new ChartBulletValue(.1, .2, 0, 1));
        Assert.Equal(svg, prepared.ToSvg());
        ScalarDepthCapture.Write(prepared, "independent-bullet-" + vertical + "-" + width + "-" + dark);
    }

    [Fact]
    public void LocalAxisConfigurationRejectsSharedModeAndRetainsDetachedCapturedOptions() {
        var series = ChartSeries.CreateBullet(new ChartSeriesId("one"), "One", 20, 50);
        ChartAxis? callback = null; series.ConfigureBulletAxis(axis => { callback = axis; axis.Reversed = true; axis.Labels.Add(new ChartAxisLabel(50, "Goal")); });
        callback!.Reversed = false; callback.Labels.Clear(); Assert.True(series.BulletAxis!.Reversed); Assert.Single(series.BulletAxis!.Labels);
        series.BulletAxis!.Labels.Clear(); Assert.Single(series.BulletAxis!.Labels);
        var chart = Chart.Create().AddSeries(series); Assert.Throws<InvalidOperationException>(() => chart.ToSvg());
        chart.ConfigureBullet(options => options.IndependentAxes = true); Assert.Contains("bullet-value", chart.ToSvg(), StringComparison.Ordinal);
        Assert.Throws<NotSupportedException>(() => series.ConfigureBulletAxis(axis => axis.Scale = ChartScaleKind.Time));
        Assert.True(series.BulletAxis!.Reversed);
    }

    [Fact]
    public void NumericRowScaleReusesCanonicalLogarithmInsteadOfLinearFractions() {
        var series = ChartSeries.CreateBullet(new ChartSeriesId("rate"), "Rate", new ChartBulletValue(10, 100, 1, 1000))
            .ConfigureBulletAxis(axis => axis.Scale = ChartScaleKind.Logarithmic);
        var chart = Chart.Create().WithHeader(false).WithLegend(false).AddSeries(series).ConfigureBullet(options => options.IndependentAxes = true);
        var scene = chart.Prepare(new VisualRenderContext()).Scene;
        var ranges = scene.Nodes.OfType<VisualSceneRectangle>().Where(node => node.Role == "bullet-range").ToArray();
        var value = Assert.Single(scene.Nodes.OfType<VisualSceneRectangle>(), node => node.Role == "bullet-value");
        Assert.Equal(ranges.Sum(range => range.Bounds.Width) / 3, value.Bounds.Width, 7);
    }
}
