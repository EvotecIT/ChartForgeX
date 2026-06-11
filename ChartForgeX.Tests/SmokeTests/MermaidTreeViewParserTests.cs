using System;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;

namespace ChartForgeX.Tests;

internal static partial class SmokeTests {
    private static void MermaidParserParsesTreeViewHierarchy() {
        const string source = @"treeView-beta
    ""src""
        ""ChartForgeX.Mermaid""
            ""MermaidParser.cs""
            ""MermaidTreeViewParser.cs""
        ""ChartForgeX.Tests""
            ""SmokeTests""
    ""README.md""";

        var result = new MermaidParser().ParseTreeView(source);

        Assert(!result.HasErrors, "Mermaid TreeView parser should parse indentation hierarchy: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid TreeView parser should produce a document.");
        Assert(document.Kind == MermaidDiagramKind.TreeView && document.Header == "treeView-beta", "Mermaid TreeView parser should preserve the header.");
        Assert(document.Roots.Count == 2 && document.Nodes.Count == 7, "Mermaid TreeView parser should preserve roots and node count.");
        Assert(document.Nodes[1].Parent == document.Nodes[0] && document.Nodes[2].Parent == document.Nodes[1], "Mermaid TreeView parser should preserve parent-child relationships.");
        Assert(document.Nodes[2].Label == "MermaidParser.cs" && document.Nodes[2].Level == 2, "Mermaid TreeView parser should preserve quoted labels and levels.");
    }

    private static void MermaidTreeViewConvertsToTopologyArtifactAndRenders() {
        const string source = @"treeView-beta
    ""src""
        ""ChartForgeX.Mermaid""
            ""MermaidParser.cs""
        ""ChartForgeX.Tests""";

        var result = new MermaidParser().ParseTreeView(source);
        Assert(!result.HasErrors, "Mermaid TreeView parser should parse renderable source: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid TreeView parser should produce a document.");
        var topology = document.ToTopologyChart(new MermaidTopologyRenderOptions { Id = "source-tree", Title = "Source Tree", Width = 840, Height = 520 });
        Assert(topology.Id == "source-tree" && topology.Nodes.Count == 4 && topology.Edges.Count == 3, "Mermaid TreeView conversion should map hierarchy to topology nodes and edges.");

        var artifact = document.ToVisualArtifact(new MermaidTopologyRenderOptions { Id = "source-tree" });
        Assert(artifact.Kind == VisualArtifactKind.Mermaid, "Mermaid TreeView visual artifact should report Mermaid artifact kind.");
        Assert(artifact.Model is TopologyChart, "Mermaid TreeView visual artifact should carry a topology model.");
        Assert(artifact.Metadata["mermaid.nodes"] == "4" && artifact.Metadata["mermaid.roots"] == "1", "Mermaid TreeView artifacts should expose model counts.");
        Assert(artifact.Metadata["render.model"] == nameof(TopologyChart), "Mermaid TreeView artifacts should expose the topology render model.");

        var svg = document.ToSvg(new MermaidTopologyRenderOptions { Id = "source-tree" });
        var png = document.ToPng(new MermaidTopologyRenderOptions { Id = "source-tree" });
        Assert(svg.Contains("Source Tree", StringComparison.Ordinal) || svg.Contains("src", StringComparison.Ordinal), "Mermaid TreeView SVG rendering should include tree labels.");
        Assert(png.Length > 64 && png[0] == 0x89 && png[1] == 0x50 && png[2] == 0x4E && png[3] == 0x47, "Mermaid TreeView PNG rendering should emit a valid PNG.");
    }

    private static void MermaidTreeViewPreservesEscapedQuotesBeforePercentText() {
        const string source = "treeView-beta\n    \"Root \\\"%% marker\\\"\"\n        \"Child\"";

        var result = new MermaidParser().ParseTreeView(source);

        Assert(!result.HasErrors, "Mermaid TreeView parser should not strip percent text after escaped quotes: " + MermaidDiagnostics(result));
        var document = result.Document ?? throw new InvalidOperationException("Mermaid TreeView parser should produce a document.");
        Assert(document.Nodes.Count == 2 && document.Nodes[0].Label == "Root \"%% marker\"", "Mermaid TreeView parser should preserve escaped quotes and literal percent text inside labels.");
    }
}
