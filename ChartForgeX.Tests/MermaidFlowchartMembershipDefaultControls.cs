using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidFlowchartMembershipDefaultControls {
    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void OrdinaryNonconflictingMembershipKeepsSourceFactsAndExactNativeExports(string family) {
        var source = family + " LR\nsubgraph G[First]\nA[Accept] --> B[Build]\nend\nsubgraph H[Second]\nC[Check]\nend\nB --> C\nU[Unscoped]";
        var direct = MermaidRenderer.Render(source); Assert.Empty(direct.Diagnostics); var artifact = direct.Artifact!;
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var json = artifact.ToInterchangeJson();
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var path = Path.Combine(output, "flowchart-membership-default-" + family);
            File.WriteAllText(path + ".svg", svg); File.WriteAllBytes(path + ".png", png); File.WriteAllText(path + ".json", json);
        }
        var document = Assert.IsAssignableFrom<MermaidFlowchartDocument>(direct.Document);
        Assert.Equal(new[] { "G", "G", "H", null }, document.Nodes.Select(node => node.SubgraphId));
        Assert.Equal(new[] { "A", "B" }, document.Subgraphs[0].NodeIds); Assert.Equal(new[] { "C" }, document.Subgraphs[1].NodeIds);
        Assert.Equal(source, document.SourceText); Assert.True(png.Length > 64);
    }
}
