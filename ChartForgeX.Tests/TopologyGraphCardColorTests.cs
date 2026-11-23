using ChartForgeX.Themes;
using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyGraphCardColorTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeDisplayMode.Pill)]
    [InlineData(TopologyNodeDisplayMode.Dot)]
    [InlineData(TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeDisplayMode.Tile)]
    public void ProjectionKeepsThemeDefaultsImplicitAndAuthoredFillsExplicit(TopologyNodeDisplayMode display) {
        var chart = TopologyChart.Create().LayoutMode(TopologyLayoutMode.Manual)
            .AddNode("default", "Default", 0, 0).UpdateNode("default", n => n.DisplayMode(display).Badge(null))
            .AddNode("white", "White", 160, 0, backgroundColor: ChartColor.FromHex("#FFFFFF")).UpdateNode("white", n => n.DisplayMode(display).Badge(null))
            .AddNode("purple", "Purple", 320, 0, backgroundColor: ChartColor.FromHex("#7C3AED")).UpdateNode("purple", n => n.DisplayMode(display).Badge(null));

        var scene = chart.ToGraphScene();
        Assert.Null(scene.Nodes.Single(node => node.Id == "default").Style.BackgroundColor);
        Assert.Equal("#FFFFFF", scene.Nodes.Single(node => node.Id == "white").Style.BackgroundColor);
        Assert.Equal("#7C3AED", scene.Nodes.Single(node => node.Id == "purple").Style.BackgroundColor);
        if (display is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill)
            Assert.All(scene.Nodes, node => Assert.Equal("true", node.Metadata["topology.card"]));
    }

    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#111827")]
    [InlineData("#FED7AA")]
    public void StaticCardPaintPreservesTheProjectedThemeWithoutAuthoringIt(string surface) {
        var theme = VisualTheme.Graphite();
        theme = theme.Configure(builder => builder.Light.Surface = ChartForgeX.Primitives.ChartColor.FromHex(surface));
        var chart = TopologyChart.Create().Theme(theme).AddNode("card", "Card", 0, 0)
            .UpdateNode("card", n => n.DisplayMode(TopologyNodeDisplayMode.Card).Badge(null));
        var implicitScene = chart.ToGraphScene();
        Assert.Null(implicitScene.Nodes[0].Style.BackgroundColor);
        Assert.Equal(ChartForgeX.Primitives.ChartColor.FromHex(surface).ToCss(), implicitScene.Nodes[0].Metadata["topology.cardBackgroundColor"]);
        var implicitPng = implicitScene.ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; });
        chart.Nodes[0].BackgroundColor = ChartColorText.Parse(surface);
        var explicitPng = chart.ToGraphScene().ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; });
        Assert.Equal(explicitPng, implicitPng);
        Assert.Null(implicitScene.Nodes[0].Style.BackgroundColor);
        if (surface == "#FFFFFF") {
            implicitScene.Nodes[0].Metadata.Remove("topology.cardBackgroundColor");
            Assert.Equal(explicitPng, implicitScene.ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; }));
        }
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Dot, false)]
    [InlineData(TopologyNodeDisplayMode.Dot, true)]
    [InlineData(TopologyNodeDisplayMode.Icon, false)]
    [InlineData(TopologyNodeDisplayMode.Icon, true)]
    public void StaticNonCardPaintRetainsTheProjectedSurfaceWithoutFreezingInteractivePaint(TopologyNodeDisplayMode display, bool dark) {
        var chart = TopologyChart.Create().Theme(VisualTheme.Graphite()).ThemeMode(dark ? ThemeMode.Dark : ThemeMode.Light)
            .AddNode("node", "Service", 0, 0).UpdateNode("node", n => n.DisplayMode(display).Badge(null));
        var scene = chart.ToGraphScene();
        Assert.Null(scene.Nodes[0].Style.BackgroundColor);
        Assert.Equal("theme", scene.Nodes[0].Metadata["topology.backgroundSource"]);
        var implicitPng = scene.ToGraphPng(configure: o => { o.Width = 320; o.Height = 180; });
        chart.Nodes[0].BackgroundColor = ChartColorText.Parse(chart.Theme.Resolve(chart.ThemeMode).Surface.ToCss());
        var explicitScene = chart.ToGraphScene();
        Assert.Equal(explicitScene.ToGraphPng(configure: o => { o.Width = 320; o.Height = 180; }), implicitPng);
        Assert.Contains("--cfx-node-fill:" + chart.Nodes[0].BackgroundColor, scene.ToGraphSvg());
    }
}
