using System.Linq;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ExampleRecipeTests {
    [Theory]
    [InlineData("collapsed", VisualThemeMode.Light, 1)]
    [InlineData("horizontal", VisualThemeMode.Dark, 3)]
    [InlineData("reordered", VisualThemeMode.Light, 3)]
    public void OrganizationHierarchyRecipeKeepsCompleteSourceAndNativeView(string variant, VisualThemeMode mode, int visible) {
        var recipe = ExampleRecipes.Named("org-portraits"); var source = Source(recipe, variant);
        Assert.Contains("TopologyHierarchyView", source); Assert.Contains("TopologyIconArtwork.Portrait", source);
        Assert.DoesNotContain("ChartForgeX.Examples", source); Assert.DoesNotContain("{{", source);
        var chart = Assert.IsType<TopologyChart>(recipe.Create(variant, mode)); var prepared = chart.Prepare();
        Assert.Equal(3, chart.Nodes.Count); Assert.Equal(visible, prepared.NodeCount); Assert.Equal(3, prepared.SourceNodeCount);
        var envelope = prepared.ToInterchangeEnvelope(); Assert.Equal("Lead", envelope.Nodes.Single(node => node.Id == "avery").Badge);
        Assert.Equal(3, envelope.Topology!.Hierarchy!.Source.Nodes.Count); Assert.NotEmpty(prepared.ToPng());
        if (variant == "horizontal") Assert.Equal(TopologyLayoutDirection.LeftToRight, envelope.Topology.LayoutDirection);
        var reset = envelope.ToTopologyHierarchySource().Prepare(new TopologyRenderOptions { HierarchyView = new TopologyHierarchyView() });
        Assert.Equal(3, reset.NodeCount);
    }
}
