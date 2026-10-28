using System;
using System.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void KanbanSourceGeometryContracts() {
        const string source = "---\nconfig:\n  kanban:\n    padding: 32\n    sectionWidth: 260\n---\nkanban\nqueue[Queue]\n  review[Review] @{assigned: 'Avery Quinn', priority: 'High', ticket: 'CFX-18'}\nempty[Empty]\n";
        var result = RenderFixture(source, new MermaidRenderOptions { Kanban = new MermaidTopologyRenderOptions { Width = 390, Height = 400 } });
        PackageAssertions.Require(!result.HasErrors && result.Artifact != null, "Packed Kanban source dimensions failed ordinary rendering.");
        var artifact = result.Artifact!;
        var chart = artifact.Model as TopologyChart ?? throw new InvalidOperationException("Packed Kanban omitted its native topology model.");
        PackageAssertions.Require(chart.Viewport.Padding == 32 && chart.Groups.All(group => group.Width == 260), "Packed source dimensions did not reach the canonical native viewport/groups.");
        var portable = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        PackageAssertions.Require(portable.Groups.All(group => group.Width >= 260 && group.Extensions["mermaid.config.kanban.sectionWidth"] == "260"), "Packed native/portable dimensions lost their requested source facts.");
        var restored = portable.ToTopologyChart();
        PackageAssertions.Require(restored.Nodes.Single().Id == "review" && restored.Nodes[0].Details.Any(detail => detail.Text == "Assigned: Avery Quinn") && restored.Groups.Count == 2,
            "Packed portable Kanban lost actual task/empty-column identity or authored details.");
        PackageAssertions.Require(artifact.ToSvg().Contains("Review", StringComparison.Ordinal) && artifact.ToPng().Length > 100 && restored.ToPng().Length > 100,
            "Packed current native SVG/PNG and restored geometry did not render.");
    }
}
