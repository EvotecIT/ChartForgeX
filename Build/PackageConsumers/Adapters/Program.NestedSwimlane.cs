using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void NestedSwimlaneContracts() {
        const string source = "swimlane-beta LR\nsubgraph Product[Product]\nP[Plan]\nsubgraph API[API]\nA[Accept] --> B[Build]\nend\nsubgraph Ops[Operations]\nC[Check]\nend\nend\nP --> A\nB --> C";
        var direct = RenderFixture(source); var fenced = CreateFixtureParser().Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!direct.HasErrors && direct.Diagnostics.Count == 0 && direct.Artifact != null && !fenced.HasErrors && fenced.Artifacts.Count == 1,
            "Packed nested Swimlane source was rejected.");
        var artifact = direct.Artifact ?? throw new InvalidOperationException("Packed nested Swimlane artifact was omitted.");
        var saved = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); var parent = saved.Groups.Single(group => group.Id == "Product");
        foreach (var child in saved.Groups.Where(group => group.Topology!.ParentGroupId == "Product"))
            PackageAssertions.Require(child.X!.Value >= parent.X!.Value && child.Y!.Value >= parent.Y!.Value
                && child.X!.Value + child.Width!.Value <= parent.X!.Value + parent.Width!.Value
                && child.Y!.Value + child.Height!.Value <= parent.Y!.Value + parent.Height!.Value, "Packed nested responsibility shell escaped its actual parent.");
        var restored = saved.ToTopologyChart(); PackageAssertions.Require(restored.Groups.Single(group => group.Id == "API").ParentGroupId == "Product",
            "Packed nested Swimlane portable restoration lost parentage.");
        PackageAssertions.Require(artifact.ToSvg() == fenced.Artifacts[0].ToSvg(), "Packed nested Swimlane direct and fence scenes diverged."); PackageAssertions.Png(artifact.ToPng());
    }
}
