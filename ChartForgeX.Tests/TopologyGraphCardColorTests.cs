using ChartForgeX.Interactivity.Html;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyGraphCardColorTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Card)]
    [InlineData(TopologyNodeDisplayMode.CompactCard)]
    [InlineData(TopologyNodeDisplayMode.Pill)]
    public void ProjectionKeepsThemeDefaultsImplicitAndAuthoredFillsExplicit(TopologyNodeDisplayMode display) {
        var chart = TopologyChart.Create().WithLayout(TopologyLayoutMode.Manual)
            .AddNode("default", "Default", 0, 0).WithNodeDisplay("default", display)
            .AddNode("white", "White", 160, 0, backgroundColor: "#ffffff").WithNodeDisplay("white", display)
            .AddNode("purple", "Purple", 320, 0, backgroundColor: "#7c3aed").WithNodeDisplay("purple", display);

        var scene = chart.ToGraphScene();
        Assert.Null(scene.Nodes.Single(node => node.Id == "default").Style.BackgroundColor);
        Assert.Equal("#ffffff", scene.Nodes.Single(node => node.Id == "white").Style.BackgroundColor);
        Assert.Equal("#7c3aed", scene.Nodes.Single(node => node.Id == "purple").Style.BackgroundColor);
        Assert.All(scene.Nodes, node => Assert.Equal("true", node.Metadata["topology.card"]));
    }

    [Theory]
    [InlineData("#FFFFFF")]
    [InlineData("#111827")]
    [InlineData("#fed7aa")]
    public void StaticCardPaintPreservesTheProjectedThemeWithoutAuthoringIt(string surface) {
        var theme = TopologyTheme.Light();
        theme.Card = surface;
        var chart = TopologyChart.Create().WithTheme(theme).AddNode("card", "Card", 0, 0)
            .WithNodeDisplay("card", TopologyNodeDisplayMode.Card);
        var implicitScene = chart.ToGraphScene();
        Assert.Null(implicitScene.Nodes[0].Style.BackgroundColor);
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
}
