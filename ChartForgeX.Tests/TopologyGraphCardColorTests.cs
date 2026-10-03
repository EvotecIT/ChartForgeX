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
}
