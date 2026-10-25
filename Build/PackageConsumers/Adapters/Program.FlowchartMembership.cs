using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void FlowchartMembershipContracts() {
        foreach (var family in new[] { "flowchart", "swimlane-beta" }) {
            var source = family + " LR\nsubgraph Outer[Outer]\nT[Plan]\nX[Work] --> Q\nsubgraph Inner[Inner]\nN[Original]\nQ[Persist] --> N\nend\nX --> N[Updated]\nend\nsubgraph Sibling[Sibling]\nY[Check] --> N\nend\nT --> Y";
            var direct = MermaidRenderer.Render(source); var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
            PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null && !fence.HasErrors && fence.Artifacts.Count == 1,
                "Packed source membership or its ordinary fence was rejected.");
            var document = direct.Document as MermaidFlowchartDocument ?? throw new InvalidOperationException("Packed source membership AST was lost.");
            PackageAssertions.Require(document.Nodes.Single(node => node.Id == "N").SubgraphId == "Inner" && document.Nodes.Single(node => node.Id == "Q").SubgraphId == "Inner"
                && document.Nodes.Single(node => node.Id == "N").Text == "Updated" && document.Subgraphs.Single(group => group.Id == "Sibling").NodeIds.Contains("N"),
                "Packed semantic membership lost first-close ownership, label updates or raw source references.");
            var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed source membership artifact was lost.");
            var restored = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()).ToTopologyChart();
            PackageAssertions.Require(restored.Nodes.Single(node => node.Id == "N").GroupId == "Inner" && restored.Nodes.Single(node => node.Id == "Q").GroupId == "Inner",
                "Packed portable semantics reassigned a nested node on a later edge reference.");
            PackageAssertions.Require(artifact.ToSvg() == fence.Artifacts[0].ToSvg(), "Packed membership direct/fence scenes differ."); PackageAssertions.Png(artifact.ToPng());
        }
    }
}
