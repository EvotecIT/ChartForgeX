using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Preserves standalone bar origins and inside labels when authored bounds exclude zero.</summary>
public sealed class CartesianBaselineTests {
    [Theory]
    [InlineData(false, false, 1)]
    [InlineData(false, true, 1)]
    [InlineData(false, false, -1)]
    [InlineData(false, true, -1)]
    [InlineData(true, false, 1)]
    [InlineData(true, true, 1)]
    [InlineData(true, false, -1)]
    [InlineData(true, true, -1)]
    public void StandaloneBarsClampTheirZeroOriginAndRetainInsideLabels(bool horizontal, bool clipping, int sign) {
        var chart = Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false).WithDataLabels()
            .WithDataLabelPlacement(ChartDataLabelPlacement.Inside);
        var points = new[] { new ChartPoint(0, 9 * sign) };
        if (horizontal) chart.AddHorizontalBar("Observed", points); else chart.AddBar("Observed", points);
        var axis = horizontal ? chart.Options.XAxis : chart.Options.YAxis;
        axis.WithBounds(sign > 0 ? 5 : -15, sign > 0 ? 15 : -5);
        (horizontal ? chart.Options.YAxis : chart.Options.XAxis).WithBounds(-.5, .5);
        chart.Options.BarStyle = ChartBarStyle.Flat;
        chart.Options.ClipMarksToPlot = clipping;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 260)),
            VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false)));
        var mark = Assert.Single(prepared.Regions, region => region.Role == "point").Bounds;
        Assert.InRange(mark.Left, 24, 376); Assert.InRange(mark.Right, 24, 376);
        Assert.InRange(mark.Top, 24, 236); Assert.InRange(mark.Bottom, 24, 236);
        Assert.Equal(horizontal ? 352 * .4 : 212 * .4, horizontal ? mark.Width : mark.Height, 8);
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), node => node.Role == "data-label");
        Assert.Equal((9 * sign).ToString(System.Globalization.CultureInfo.InvariantCulture), label.Text.Lines.Single().Text);
        Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code == "cartesian.data-label-overflow");
        Assert.Contains("data-cfx-base=\"0\"", prepared.ToSvg());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SingleCategoryBarsUseACenteredAutomaticCategoryDomain(bool horizontal) {
        var chart = Chart.Create().WithAxes(false).WithGrid(false).WithLegend(false);
        if (horizontal) chart.AddHorizontalBar("Observed", new[] { new ChartPoint(0, 9) });
        else chart.AddBar("Observed", new[] { new ChartPoint(0, 9) });
        chart.Options.BarStyle = ChartBarStyle.Flat;
        var prepared = chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 260)),
            VisualTheme.Graphite(), frame: new VisualFrame(showLegend: false)));
        var mark = Assert.Single(prepared.Regions, region => region.Role == "point").Bounds;
        Assert.Equal(horizontal ? 130 : 200, horizontal ? mark.Top + mark.Height / 2 : mark.Left + mark.Width / 2, 8);
        Assert.InRange(mark.Left, 24, 376); Assert.InRange(mark.Right, 24, 376);
        Assert.InRange(mark.Top, 24, 236); Assert.InRange(mark.Bottom, 24, 236);
        Assert.True(prepared.ToPng().Length > 64);
    }
}
