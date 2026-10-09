using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class HierarchyDataLabelVisibilityTests {
    [Theory]
    [InlineData(ChartSeriesKind.Tree, 720, 460, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Tree, 340, 300, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Sunburst, 720, 460, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Sunburst, 340, 300, VisualThemeMode.Dark)]
    [InlineData(ChartSeriesKind.Treemap, 720, 460, VisualThemeMode.Light)]
    [InlineData(ChartSeriesKind.Treemap, 340, 300, VisualThemeMode.Dark)]
    public void ChartLabelsAndSeriesOverridesShareOneRenderedPolicy(ChartSeriesKind kind, int width, int height, VisualThemeMode mode) {
        var chart = Model(kind).WithDataLabels(false);
        var series = chart.Series[0];
        var context = new VisualRenderContext(new VisualLayoutOptions(new VisualSize(width, height)), themeMode: mode,
            frame: new VisualFrame(showLegend: false));
        var role = kind == ChartSeriesKind.Tree ? "tree-node-label" : kind == ChartSeriesKind.Sunburst ? "sunburst-label" : "treemap-label";
        var expected = kind == ChartSeriesKind.Treemap ? 2 : 3;
        var hidden = chart.Prepare(context);
        Assert.Equal(0, Labels(hidden, role));
        var hiddenPixels = Pixels(hidden);

        series.WithDataLabels();
        var overridden = chart.Prepare(context);
        Assert.Equal(expected, Labels(overridden, role));
        var visiblePixels = Pixels(overridden);
        Assert.False(hiddenPixels.SequenceEqual(visiblePixels));
        Assert.Equal(hidden.Regions.Select(region => (region.Id, region.Label, region.Bounds)),
            overridden.Regions.Select(region => (region.Id, region.Label, region.Bounds)));

        chart.WithDataLabels();
        series.WithDataLabels(false);
        var suppressed = chart.Prepare(context);
        Assert.Equal(0, Labels(suppressed, role));
        Assert.Equal(hiddenPixels, Pixels(suppressed));

        series.UseChartDataLabels();
        var inherited = chart.Prepare(context);
        Assert.Equal(expected, Labels(inherited, role));
        Assert.Equal(visiblePixels, Pixels(inherited));
        chart.WithDataLabels(false);
        var reset = chart.Prepare(context);
        Assert.Equal(0, Labels(reset, role));
        Assert.Equal(hiddenPixels, Pixels(reset));
    }

    private static int Labels(PreparedVisual prepared, string role) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Count(element => (string?)element.Attribute("data-cfx-role") == role);
    private static byte[] Pixels(PreparedVisual prepared) => prepared.ToRgba(new VisualRenderOptions(supersampling: 1)).Pixels;
    private static Chart Model(ChartSeriesKind kind) {
        if (kind == ChartSeriesKind.Treemap) return Chart.Create().AddTreemap("Allocation", new[] { new ChartTreemapItem("Alpha", 7), new ChartTreemapItem("Beta", 3) });
        var nodes = new[] { new ChartNode("all", "All"), new ChartNode("alpha", "Alpha"), new ChartNode("beta", "Beta") };
        var links = new[] { new ChartTreeLink("all", "alpha", 7), new ChartTreeLink("all", "beta", 3) };
        return kind == ChartSeriesKind.Tree ? Chart.Create().AddTree("Allocation", nodes, links) : Chart.Create().AddSunburst("Allocation", nodes, links);
    }
}
