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
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Manual)
            .AddNode("default", "Default", 0, 0).WithNodeDisplay("default", display)
            .AddNode("white", "White", 160, 0, backgroundColor: "#ffffff").WithNodeDisplay("white", display)
            .AddNode("purple", "Purple", 320, 0, backgroundColor: "#7c3aed").WithNodeDisplay("purple", display);

        var scene = chart.ToGraphScene();
        Assert.Null(scene.Nodes.Single(node => node.Id == "default").Style.BackgroundColor);
        Assert.Equal("#ffffff", scene.Nodes.Single(node => node.Id == "white").Style.BackgroundColor);
        Assert.Equal("#7c3aed", scene.Nodes.Single(node => node.Id == "purple").Style.BackgroundColor);
        if (display is TopologyNodeDisplayMode.Card or TopologyNodeDisplayMode.CompactCard or TopologyNodeDisplayMode.Pill)
            Assert.All(scene.Nodes, node => Assert.Equal("true", node.Metadata["topology.card"]));
    }

    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#111827")]
    [InlineData("#fed7aa")]
    public void StaticCardPaintPreservesTheProjectedThemeWithoutAuthoringIt(string surface) {
        var theme = VisualTheme.Graphite();
        theme = theme.Configure(builder => builder.Light.Surface = ChartForgeX.Primitives.ChartColor.FromHex(surface));
        var chart = TopologyChart.Create().WithTheme(theme).AddNode("card", "Card", 0, 0)
            .WithNodeDisplay("card", TopologyNodeDisplayMode.Card);
        var implicitScene = chart.ToGraphScene();
        Assert.Null(implicitScene.Nodes[0].Style.BackgroundColor);
        Assert.Equal(ChartForgeX.Primitives.ChartColor.FromHex(surface).ToCss(), implicitScene.Nodes[0].Metadata["topology.cardBackgroundColor"]);
        var implicitPng = implicitScene.ToGraphPng(configure: options => { options.Width = 320; options.Height = 180; });
        chart.Nodes[0].BackgroundColor = surface;
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
        var chart = TopologyChart.Create().WithTheme(VisualTheme.Graphite(), dark ? VisualThemeMode.Dark : VisualThemeMode.Light)
            .AddNode("node", "Service", 0, 0).WithNodeDisplay("node", display);
        var scene = chart.ToGraphScene();
        Assert.Null(scene.Nodes[0].Style.BackgroundColor);
        Assert.Equal("theme", scene.Nodes[0].Metadata["topology.backgroundSource"]);
        var implicitPng = scene.ToGraphPng(configure: o => { o.Width = 320; o.Height = 180; });
        chart.Nodes[0].BackgroundColor = chart.Theme.Resolve(chart.ThemeMode).Surface.ToCss();
        var explicitScene = chart.ToGraphScene();
        Assert.Equal(explicitScene.ToGraphPng(configure: o => { o.Width = 320; o.Height = 180; }), implicitPng);
        Assert.Contains("--cfx-node-fill:" + chart.Nodes[0].BackgroundColor, scene.ToGraphSvg());
    }
}
