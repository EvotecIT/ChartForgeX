using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PolarGuideThemeTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void PolarGuidesUseConfiguredGridAndAxisColors(bool radar, bool dark) {
        var grid = ChartColor.FromHex("#DC2626");
        var axis = ChartColor.FromHex("#2563EB");
        var chart = Chart.Create().WithThemeMode(dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .WithSize(520, 380).WithHeader(false).WithLegend(false)
            .WithDataLabels(false).WithYAxisBounds(0, 50);
        var points = new[] { new ChartPoint(1, 15), new ChartPoint(2, 35), new ChartPoint(3, 25) };
        if (radar) chart.AddRadarLine("Signal", points);
        else chart.AddPolar("Signal", points);
        var original = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        chart.WithTheme(VisualTheme.Graphite().Configure(theme => {
            var colors = theme.Colors(dark ? VisualThemeMode.Dark : VisualThemeMode.Light);
            colors.Grid = grid; colors.Axis = axis;
        }));
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var prefix = radar ? "radar-" : "polar-";
        var rings = prepared.Scene.Nodes.OfType<VisualSceneMark>().Where(node => node.Role == prefix + "ring").ToArray();
        var spokes = prepared.Scene.Nodes.OfType<VisualSceneLine>().Where(node => node.Role == prefix + "spoke").ToArray();
        Assert.NotEmpty(rings);
        Assert.NotEmpty(spokes);
        Assert.All(rings, node => Assert.Equal(grid, node.Stroke));
        Assert.All(spokes, node => Assert.Equal(axis, node.Stroke));
        Assert.False(original.ToPng().SequenceEqual(prepared.ToPng()));
        Assert.Equal(prepared.ToPng(), prepared.ToPng());
    }
}
