using System;
using System.Linq;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void OrganizationHierarchyContracts() {
        var chart = TopologyChart.Create().WithViewport(720, 420).WithLegend(null).AddHierarchy(new[] {
            new TopologyHierarchyItem("lead", "Avery Quinn"), new TopologyHierarchyItem("engineering", "Engineering", "lead"),
            new TopologyHierarchyItem("platform", "Platform", "engineering"), new TopologyHierarchyItem("operations", "Operations", "lead")
        }, new TopologyHierarchyOptions { NodeDisplayMode = TopologyNodeDisplayMode.Card });
        chart.Nodes.Single(node => node.Id == "engineering").Badge = "Owner";
        var prepared = chart.Prepare(new TopologyRenderOptions { IncludeLegend = false,
            HierarchyView = new TopologyHierarchyView(new[] { "engineering" }, new[] { "operations", "engineering" }, TopologyLayoutDirection.LeftToRight) });
        var envelope = prepared.ToInterchangeEnvelope();
        PackageAssertions.Require(chart.Nodes.Count == 4 && prepared.NodeCount == 3 && prepared.SourceNodeCount == 4,
            "Packed hierarchy preparation deleted source contributors or invented visible nodes.");
        PackageAssertions.Require(envelope.Nodes.Single(node => node.Id == "engineering").Badge == "Owner", "Packed collapse overwrote the authored badge.");
        var restored = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyHierarchySource();
        PackageAssertions.Require(restored.LayoutMode == TopologyLayoutMode.Layered && restored.Nodes.Count == 4 && restored.Edges.Count == 3,
            "Packed hierarchy interchange did not restore raw authored source.");
        var reset = restored.Prepare(new TopologyRenderOptions { IncludeLegend = false, HierarchyView = new TopologyHierarchyView() });
        PackageAssertions.Require(reset.NodeCount == 4 && restored.Nodes.All(node => !node.Metadata.ContainsKey("hierarchy.collapsed")),
            "Packed hierarchy reset did not restore its source contributors.");
        PackageAssertions.Png(prepared.ToPng()); PackageAssertions.Png(reset.ToPng());
    }
}
