using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class CartesianDataLabelPlacementTests {
    [Fact]
    public void OrdinaryOneBasedBarsKeepAllThreeFullLabels() {
        var chart = Chart.Create().WithSize(640, 360).WithDataLabels()
            .AddBar("Values", new[] { new ChartPoint(1, 42), new ChartPoint(2, 84), new ChartPoint(3, 126) });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "data-label").ToArray();
        Assert.Equal(new[] { "42", "84", "126" }, labels.Select(node => node.Text.Lines.Single().Text));
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        Assert.Equal(prepared.ToSvg(), chart.ToSvg());
        Assert.True(chart.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AutomaticContainedFallbackUsesContrastAndPreservesExplicitInk(bool explicitInk) {
        var fill = ChartColor.FromHex("#172554");
        var chart = Chart.Create().WithSize(180, 180).WithHeader(false).WithLegend(false).WithAxes(false).WithGrid(false)
            .WithYAxisBounds(0, 100).WithDataLabels().AddBar("Value", new[] { new ChartPoint(1, 100) }, fill);
        if (explicitInk) chart.Series[0].WithDataLabelStyle(style => style.WithColor("#FFFF00"));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        Assert.Equal("100", label.Text.Lines.Single().Text);
        var mark = Assert.Single(prepared.Regions, region => region.Role == "point").Bounds;
        Assert.InRange(label.X, mark.Left, mark.Right);
        Assert.InRange(label.Baseline, mark.Top, mark.Bottom);
        Assert.Equal(explicitInk ? ChartColor.FromHex("#FFFF00") : ChartColorMath.AccessibleTextOnBackground(fill), label.Color);
        Assert.True(prepared.ToPng().Length > 64);
    }
}
