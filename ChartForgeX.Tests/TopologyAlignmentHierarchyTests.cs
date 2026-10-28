using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyAlignmentHierarchyTests {
    [Fact]
    public void RawHierarchyIdentityRestorationKeepsCollidingAlignmentIdsAndRemapsMembersExactlyOnce() {
        var chart = TopologyChart.Create().WithTitle("").WithViewport(800, 600).AddHierarchy(new[] {
            new TopologyHierarchyItem("root", "Root"), new TopologyHierarchyItem("branch", "Branch", "root"),
            new TopologyHierarchyItem("leaf", "Leaf", "branch"), new TopologyHierarchyItem("peer", "Peer", "root") });
        // This caller deliberately replaces authored hierarchy ranks with global alignment preferences.
        foreach (var node in chart.Nodes) node.Metadata.Remove("layer");
        var declaration = new TopologyAlignment("root", TopologyAlignmentKind.Row, new[] { "branch", "leaf", "peer" });
        declaration.Metadata["source"] = "raw"; chart.Alignments.Add(declaration);
        var prepared = chart.Prepare(new TopologyRenderOptions { HierarchyView = new TopologyHierarchyView(new[] { "branch" }), IncludeLegend = false });
        var saved = prepared.ToInterchangeEnvelope(); var visible = Assert.Single(saved.Topology!.Alignments);
        Assert.Equal(new[] { "branch", "peer" }, visible.NodeIds); Assert.NotEqual("root", visible.Id);
        var hierarchy = saved.Topology!.Hierarchy!; Assert.Single(hierarchy.SourceAlignments); Assert.Equal("root", hierarchy.SourceAlignments[0].SourceId);
        Assert.Equal(visible.Id, hierarchy.SourceAlignments[0].Id); Assert.Equal(3, hierarchy.Source.Topology!.Alignments[0].NodeIds.Count);
        var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()); var raw = loaded.ToTopologyHierarchySource();
        var restored = Assert.Single(raw.Alignments); Assert.Equal("root", restored.Id); Assert.Equal(new[] { "branch", "leaf", "peer" }, restored.NodeIds); Assert.Equal("raw", restored.Metadata["source"]);
        Assert.All(raw.Nodes, node => { Assert.Equal(0, node.X); Assert.Equal(0, node.Y); }); Assert.Equal(TopologyLayoutMode.Layered, raw.LayoutMode);
        var ordinary = loaded.ToTopologyChart(); Assert.Equal(visible.Id, ordinary.Alignments[0].Id); Assert.Equal(2, ordinary.Alignments[0].NodeIds.Count);
        var retained = prepared.ToInterchangeEnvelope().ToJson(); raw.Alignments[0].NodeIds.Clear(); loaded.Topology!.Hierarchy!.SourceAlignments.Clear();
        Assert.Equal(retained, prepared.ToInterchangeEnvelope().ToJson());
    }
}
