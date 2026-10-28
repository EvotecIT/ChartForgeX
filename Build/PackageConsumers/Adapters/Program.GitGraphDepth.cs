using System;
using System.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualBlocks;
using ChartForgeX.VisualArtifacts;

internal static partial class Program {
    private static void GitGraphSourcePolicyContracts() {
        const string source = "%%{init: {\"gitGraph\":{\"mainBranchName\":\"trunk\",\"mainBranchOrder\":2,\"parallelCommits\":true,\"showBranches\":false,\"showCommitLabel\":false}}}%%\ngitGraph BT\ncommit id:\"base\"\nbranch work order:1\ncommit id:\"work\"\ncheckout trunk\ncommit id:\"next\"\nmerge work id:\"joined\"";
        var rendered = RenderFixture(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        var fenced = CreateFixtureParser(new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }).Parse("```mermaid\n" + source + "\n```");
        PackageAssertions.Require(!rendered.HasErrors && rendered.Artifact != null && rendered.Diagnostics.Count == 0 && !fenced.HasErrors && fenced.Artifacts.Count == 1,
            "Packed Git graph source settings became warnings or fallback graphs.");
        var graph = (GitGraphBlock)rendered.Artifact!.Model!;
        PackageAssertions.Require(graph.Direction == GitGraphDirection.BottomToTop && graph.ParallelCommits && !graph.ShowBranchLabels && !graph.ShowCommitLabels
            && graph.Branches[0].Name == "trunk" && graph.Branches[0].Order > graph.Branches[1].Order,
            "Packed Git graph source precedence, main branch or display order was lost.");
        PackageAssertions.Require(rendered.Artifact.ToSvg() == fenced.Artifacts[0].ToSvg() && rendered.Artifact.ToPng().SequenceEqual(fenced.Artifacts[0].ToPng()),
            "Packed direct and fenced Git graph output differs.");
        Console.WriteLine("Packed Mermaid Git graph source configuration and native/fenced exports passed.");
    }
}
