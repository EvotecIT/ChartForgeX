using System.Globalization;
using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class MermaidMindMapColorModeTests {
    private const string Uneven = "mindmap\n    root((Root))\n          ownerA{{Owner A}}\n            repoA[Repo A]\n                leafA[Leaf A]\n              leafB[Leaf B]\n          ownerB{{Owner B}}\n              repoB[Repo B]\n                leafC[Leaf C]";
    private static readonly int[] BranchSections = { -1, 0, 0, 0, 0, 1, 1, 1 };
    private static readonly int[] DepthSections = { -1, 0, 1, 2, 2, 0, 1, 2 };

    [Theory]
    [InlineData(MermaidMindMapColorMode.Branch, false)]
    [InlineData(MermaidMindMapColorMode.Branch, true)]
    [InlineData(MermaidMindMapColorMode.Depth, false)]
    [InlineData(MermaidMindMapColorMode.Depth, true)]
    public void SourceConfigurationColorsDirectAndFencedMindmaps(MermaidMindMapColorMode mode, bool directive) {
        var path = Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures",
            "mindmap-" + mode.ToString().ToLowerInvariant() + "-uneven.mmd");
        var source = directive ? Configuration(mode, true) + string.Join("\n", File.ReadAllLines(path).SkipWhile(line => line.Trim() != "mindmap"))
            : File.ReadAllText(path);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(path, ".expected.json")));
        var facts = expected.RootElement.GetProperty("mindmap");
        var sections = facts.GetProperty("sections").EnumerateArray().Select(section => section.ValueKind == JsonValueKind.Null ? -1 : section.GetInt32()).ToArray();
        var parsed = new MermaidParser().ParseMindMap(source);
        Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidMindMapDocument>(parsed.Document);
        Assert.Equal(mode, document.ColorBy);
        Assert.Equal(new[] { 0, 1, 2, 3, 3, 1, 2, 3 }, document.Nodes.Select(node => node.Level));
        var indexes = document.Nodes.Select((node, index) => (node.Id, index)).ToDictionary(item => item.Id, item => item.index);
        Assert.Equal(facts.GetProperty("nodes").EnumerateArray().Select(node =>
                (node[0].GetString(), node[1].ValueKind == JsonValueKind.Null ? (int?)null : node[1].GetInt32())),
            document.Nodes.Select(node => ((string?)node.Text, node.ParentId == null ? (int?)null : indexes[node.ParentId])));
        var topology = document.ToTopologyChart();
        AssertSections(topology, sections);

        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.Single(fenced.Artifacts);
        var fencedTopology = Assert.IsType<TopologyChart>(artifact.Model);
        AssertSections(fencedTopology, sections);
        Assert.Equal(topology.Nodes.Select(node => (node.Label, node.PaletteIndex)), fencedTopology.Nodes.Select(node => (node.Label, node.PaletteIndex)));
    }

    [Fact]
    public void BranchIsTheDefaultAndThePublicModeCanChangeWithoutReparsing() {
        Assert.Equal(MermaidMindMapColorMode.Branch, new MermaidMindMapDocument().ColorBy);
        var parsed = new MermaidParser().ParseMindMap(Uneven);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(MermaidMindMapColorMode.Branch, document.ColorBy);
        AssertSections(document.ToTopologyChart(), BranchSections);
        document.ColorBy = MermaidMindMapColorMode.Depth;
        AssertSections(document.ToTopologyChart(), DepthSections);
        Assert.Throws<ArgumentOutOfRangeException>(() => document.ColorBy = (MermaidMindMapColorMode)42);
        Assert.Equal(MermaidMindMapColorMode.Depth, document.ColorBy);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("1")]
    [InlineData("null")]
    [InlineData("''")]
    [InlineData("'levels'")]
    [InlineData("[]")]
    [InlineData("{'nested':'depth'}")]
    public void InvalidColorModesRemainLocatedErrorsWithoutPartialArtifacts(string value) {
        foreach (var directive in new[] { false, true }) {
            var prefix = directive ? "%%{init: {'mindmap': {'colorBy': " + value + "}}}%%\n"
                : "---\nconfig:\n  mindmap:\n    colorBy: " + value + "\n---\n";
            var rendered = MermaidRenderer.Render(prefix + Uneven);
            Assert.True(rendered.HasErrors);
            Assert.Null(rendered.Artifact);
            var error = Assert.Single(rendered.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration);
            Assert.Equal(directive ? 1 : 4, error.Span.Line);
            var fenced = new MermaidVisualMarkupParser().Parse("Intro\n\n```mermaid\n" + prefix + Uneven + "\n```");
            Assert.True(fenced.HasErrors);
            Assert.Empty(fenced.Artifacts);
            Assert.Equal((directive ? 1 : 4) + 3, Assert.Single(fenced.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidConfiguration).Line);
        }
    }

    [Fact]
    public void LaterDeclarationsReplaceTheColorModeAndInactiveScopesStayOpaque() {
        var parsed = new MermaidParser().ParseMindMap(Configuration(MermaidMindMapColorMode.Depth, false)
            + Configuration(MermaidMindMapColorMode.Branch, true) + Uneven);
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(MermaidMindMapColorMode.Branch, parsed.Document!.ColorBy);
        AssertSections(parsed.Document.ToTopologyChart(), BranchSections);

        var inactive = MermaidRenderer.Render("%%{init: {'mindmap': {'colorBy': false}}}%%\nflowchart LR\nA --> B");
        Assert.False(inactive.HasErrors);
        Assert.NotNull(inactive.Artifact);
        Assert.Contains(inactive.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Message.Contains("mindmap.colorBy", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(MermaidMindMapColorMode.Branch)]
    [InlineData(MermaidMindMapColorMode.Depth)]
    public void InsertingAPrecedingBranchChangesOnlyBranchColorAssignments(MermaidMindMapColorMode mode) {
        const string original = "mindmap\n Root\n   Target\n      Child\n         Grandchild";
        const string inserted = "mindmap\n Root\n   Inserted\n      Other\n   Target\n      Child\n         Grandchild";
        var first = new MermaidParser().ParseMindMap(Configuration(mode, false) + original).Document!.ToTopologyChart();
        var second = new MermaidParser().ParseMindMap(Configuration(mode, false) + inserted).Document!.ToTopologyChart();
        Assert.Equal(1, second.Nodes.Single(node => node.Label == "Inserted").PaletteIndex);
        foreach (var label in new[] { "Target", "Child", "Grandchild" }) {
            var before = first.Nodes.Single(node => node.Label == label).PaletteIndex;
            var after = second.Nodes.Single(node => node.Label == label).PaletteIndex;
            Assert.Equal(mode == MermaidMindMapColorMode.Branch ? before + 1 : before, after);
        }
        AssertIncomingEdgesUseChildColors(second);
    }

    [Fact]
    public void DeepMindmapsCycleElevenNonrootSectionsWithoutReusingTheRootSection() {
        var source = Configuration(MermaidMindMapColorMode.Depth, false) + "mindmap\n Root\n"
            + string.Join("\n", Enumerable.Range(1, 13).Select(depth => new string(' ', depth * 3 + 1) + "Depth " + depth));
        var parsed = new MermaidParser().ParseMindMap(source);
        Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        Assert.Equal(Enumerable.Range(0, 14), document.Nodes.Select(node => node.Level));
        var expected = new[] { -1 }.Concat(Enumerable.Range(0, 13).Select(depth => depth % 11)).ToArray();
        var topology = document.ToTopologyChart();
        AssertSections(topology, expected);
        Assert.Equal(1, topology.Nodes[12].PaletteIndex);
        Assert.Equal(2, topology.Nodes[13].PaletteIndex);
        Assert.Equal(0, topology.Nodes[0].PaletteIndex);
    }

    private static string Configuration(MermaidMindMapColorMode mode, bool directive) {
        var value = mode == MermaidMindMapColorMode.Branch ? "branch" : "depth";
        return directive ? "%%{init: {'mindmap': {'colorBy': '" + value + "'}}}%%\n"
            : "---\nconfig:\n  mindmap:\n    colorBy: " + value + "\n---\n";
    }

    private static void AssertSections(TopologyChart topology, int[] expected) {
        Assert.Equal(expected.Select(value => value.ToString(CultureInfo.InvariantCulture)), topology.Nodes.Select(node => node.Metadata["mermaid.section"]));
        Assert.Equal(expected.Select(section => (int?)(section + 1)), topology.Nodes.Select(node => node.PaletteIndex));
        AssertIncomingEdgesUseChildColors(topology);
    }

    private static void AssertIncomingEdgesUseChildColors(TopologyChart topology) {
        Assert.Equal(topology.Nodes.Count - 1, topology.Edges.Count);
        Assert.All(topology.Edges, edge => Assert.Equal(topology.Nodes.Single(node => node.Id == edge.TargetNodeId).PaletteIndex, edge.PaletteIndex));
    }
}
