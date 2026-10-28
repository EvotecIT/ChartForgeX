using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidArchitectureNativeDepthTests {
    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-native-boundaries.mmd"));
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family },
        Architecture = new MermaidTopologyRenderOptions { Width = width, Height = 700 }
    };

    [Fact]
    public void CompleteSourceKeepsOccurrenceIdentityWhileRealNativeGroupsContainTheirDescendants() {
        var source = Fixture(); var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(source, document.SourceText);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-native-boundaries.expected.json")));
        Assert.Equal(expected.RootElement.GetProperty("sourceStatementLines").EnumerateArray().Select(value => value.GetInt32()), document.Statements.Select(statement => statement.Span.Line));
        Assert.Equal("<--", document.Edges[1].Operator); Assert.Equal("worker", document.Edges[1].Left.Id); Assert.Equal("split", document.Edges[1].Right.Id);
        Assert.Equal(14, document.Edges[3].Span.Line); Assert.Equal("store", document.Edges[3].Left.Id); Assert.True(document.Edges[3].Left.GroupBoundary);
        var chart = document.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Layered, chart.LayoutMode);
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(value => value.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.DoesNotContain(chart.Nodes, node => node.DisplayMode == TopologyNodeDisplayMode.Hidden);
        foreach (var row in expected.RootElement.GetProperty("groups").EnumerateArray())
            Assert.Equal(row[1].GetString(), chart.Groups.Single(group => group.Id == row[0].GetString()).ParentGroupId);
        var edge = chart.Edges[3]; Assert.Equal("data", edge.SourceNodeId); Assert.Equal("partner", edge.TargetNodeId);
        Assert.Equal("store", edge.Metadata["mermaid.source.id"]); Assert.Equal("remote", edge.Metadata["mermaid.target.id"]);
        var prepared = chart.Prepare(); CapturePrepared(prepared, "architecture-complete-native"); var json = prepared.ToInterchangeEnvelope().ToJson();
        var portable = VisualArtifactInterchangeEnvelope.FromJson(json);
        foreach (var child in portable.Groups.Where(group => group.Topology!.ParentGroupId != null)) {
            var parent = portable.Groups.Single(group => group.Id == child.Topology!.ParentGroupId);
            Assert.True(parent.X!.Value <= child.X!.Value && parent.Y!.Value <= child.Y!.Value);
            Assert.True(parent.X!.Value + parent.Width!.Value >= child.X!.Value + child.Width!.Value);
            Assert.True(parent.Y!.Value + parent.Height!.Value >= child.Y!.Value + child.Height!.Value);
        }
        foreach (var node in portable.Nodes) {
            var group = portable.Groups.Single(group => group.Id == node.GroupId);
            Assert.True(group.X!.Value <= node.X!.Value && group.Y!.Value <= node.Y!.Value);
            Assert.True(group.X!.Value + group.Width!.Value >= node.X!.Value + node.Width!.Value);
            Assert.True(group.Y!.Value + group.Height!.Value >= node.Y!.Value + node.Height!.Value);
        }
        Assert.Equal("private", portable.Groups.Single(group => group.Id == "data").Topology!.ParentGroupId);
        Assert.Equal("data", portable.Edges[3].SourceId); Assert.Equal("store", portable.Edges[3].Extensions["mermaid.source.id"]);
        var restored = portable.ToTopologyChart(); Assert.Equal("platform", restored.Groups.Single(group => group.Id == "private").ParentGroupId);
        Assert.Equal(TopologyEdgePort.Right, restored.Edges[3].SourcePort); Assert.Equal("data", restored.Edges[3].SourceNodeId);
        var svg = prepared.ToSvg(); chart.Groups.Single(group => group.Id == "data").ParentGroupId = null;
        chart.Nodes[0].Label = "Changed"; Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(json, prepared.ToInterchangeEnvelope().ToJson());
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("-->", "one", "two", TopologyEdgePort.Right, TopologyEdgePort.Left, VisualLinkDirection.Forward)]
    [InlineData("<--", "two", "one", TopologyEdgePort.Left, TopologyEdgePort.Right, VisualLinkDirection.Forward)]
    [InlineData("<-->", "one", "two", TopologyEdgePort.Right, TopologyEdgePort.Left, VisualLinkDirection.Bidirectional)]
    [InlineData("--", "one", "two", TopologyEdgePort.Right, TopologyEdgePort.Left, VisualLinkDirection.None)]
    public void BoundarySidesFollowSemanticDirectionWithoutChangingRawLeftRightOccurrences(string op, string source, string target,
        TopologyEdgePort sourcePort, TopologyEdgePort targetPort, VisualLinkDirection direction) {
        var text = "architecture-beta\ngroup one[One]\ngroup two[Two]\nservice a[A] in one\nservice b[B] in two\na{group}:R " + op + " L:b{group}";
        var parsed = new MermaidParser().ParseArchitecture(text); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal("a", document.Edges[0].Left.Id); Assert.Equal("b", document.Edges[0].Right.Id);
        var chart = document.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.DenseGrouped, chart.LayoutMode);
        var edge = Assert.Single(chart.Edges); Assert.Equal(source, edge.SourceNodeId); Assert.Equal(target, edge.TargetNodeId);
        Assert.Equal(sourcePort, edge.SourcePort); Assert.Equal(targetPort, edge.TargetPort); Assert.Equal(direction, edge.Direction);
        var saved = chart.Prepare().ToInterchangeEnvelope(); Assert.Equal(2, saved.Nodes.Count);
        Assert.Equal(source, saved.Edges[0].SourceId); Assert.Equal(sourcePort, saved.Edges[0].Topology!.SourcePort);
        Assert.Equal(targetPort, saved.Edges[0].Topology!.TargetPort); Assert.Equal(op, saved.Edges[0].Extensions["mermaid.operator"]);
    }

    [Theory]
    [InlineData("L", "R", TopologyEdgePort.Left, TopologyEdgePort.Right)]
    [InlineData("R", "L", TopologyEdgePort.Right, TopologyEdgePort.Left)]
    [InlineData("T", "B", TopologyEdgePort.Top, TopologyEdgePort.Bottom)]
    [InlineData("B", "T", TopologyEdgePort.Bottom, TopologyEdgePort.Top)]
    public void AllAuthoredNodeSidesUseTheExistingNativeAndPortablePortOwner(string left, string right, TopologyEdgePort source, TopologyEdgePort target) {
        var result = MermaidRenderer.Render("architecture-beta\nservice a[A]\nservice b[B]\na:" + left + " --> " + right + ":b"); Assert.Empty(result.Diagnostics);
        var edge = Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Edges);
        Assert.Equal(source, edge.SourcePort); Assert.Equal(target, edge.TargetPort);
        var saved = result.Artifact!.ToInterchangeEnvelope().Edges[0]; Assert.Equal(source, saved.Topology!.SourcePort); Assert.Equal(target, saved.Topology!.TargetPort);
        Assert.True(result.Artifact!.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("service a[A]\nservice b[B]", "a{group}:R --> L:b{group}", "a", "b")]
    [InlineData("group one[One]\nservice a[A] in one\nservice b[B]", "a{group}:R --> L:b", "one", "b")]
    [InlineData("group one[One]\nservice a[A]\nservice b[B] in one", "a:R --> L:b{group}", "a", "one")]
    [InlineData("group one[One]\ngroup two[Two] in one\nservice a[A] in one\nservice b[B] in two", "a{group}:R --> L:b{group}", "one", "two")]
    [InlineData("group one[One]\ngroup two[Two]\njunction a in one\nservice b[B] in two", "a{group}:R --> L:b{group}", "one", "two")]
    public void AcceptedBoundaryCasesKeepTheirImmediateGroupOrUngroupedNode(string declarations, string link, string source, string target) {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\n" + declarations + "\n" + link); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart(); var edge = Assert.Single(chart.Edges);
        Assert.Equal(source, edge.SourceNodeId); Assert.Equal(target, edge.TargetNodeId);
        Assert.Equal(parsed.Document!.Services.Count + parsed.Document!.Junctions.Count, chart.Nodes.Count);
    }

    [Fact]
    public void RepeatedBoundaryOccurrencesKeepTheirSourceOrderAndDistinctNativeEdgeIdentity() {
        const string source = "architecture-beta\ngroup one[One]\ngroup two[Two]\nservice a[A] in one\nservice b[B] in two\na{group}:R --> L:b{group}\na{group}:R --> L:b{group}";
        var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { 6, 7 }, parsed.Document!.Edges.Select(edge => edge.Span.Line));
        var chart = parsed.Document!.ToTopologyChart(); Assert.Equal(2, chart.Edges.Count);
        Assert.Equal(2, chart.Edges.Select(edge => edge.Id).Distinct().Count());
        Assert.All(chart.Edges, edge => { Assert.Equal("one", edge.SourceNodeId); Assert.Equal("two", edge.TargetNodeId); });
        var portable = chart.Prepare().ToInterchangeEnvelope(); Assert.Equal(2, portable.Edges.Count);
        Assert.Equal(new[] { 0, 1 }, portable.Edges.Select(edge => edge.Order));
    }

    [Theory]
    [InlineData("group shared[G]\nservice shared[S]", 3)]
    [InlineData("service shared[S]\ngroup shared[G]", 3)]
    [InlineData("group shared[G]\njunction shared", 3)]
    [InlineData("service a[A]\nservice b[B]\na:X --> L:b", 4)]
    [InlineData("service a[A]\nservice b[B]\na:R --> Q:b", 4)]
    [InlineData("group one[One]\nservice a[A] in one\nservice b[B] in one\na{group}:R --> L:b", 5)]
    [InlineData("group one[One]\nservice a[A] in one\nservice b[B] in one\na{group}:R --> L:b{group}", 5)]
    public void GenuinelyInvalidIdentitySidesAndSameGroupBoundaryStatementsStayLocated(string body, int line) {
        var source = "architecture-beta\n" + body; var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == line);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)] [InlineData(false, 1100)] [InlineData(true, 1100)]
    public void CompleteArchitectureUsesOneNativeSceneForDirectFencedSvgPngAndPortableConsumers(bool dark, int width) {
        var source = Fixture(); var options = Options(dark, width); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fenced.HasErrors);
        var artifact = direct.Artifact!; var other = Assert.Single(fenced.Artifacts);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); Capture(artifact, "architecture-native-" + width + (dark ? "-dark" : "-light"));
        Assert.Equal(svg, other.ToSvg()); Assert.Equal(png, other.ToPng());
        var xml = XDocument.Parse(svg); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "foreignObject" or "image");
        Assert.Equal(5, xml.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-node"));
        Assert.Equal(4, xml.Descendants().Count(element => (string?)element.Attribute("data-cfx-role") == "topology-group"));
        Assert.Equal(4, artifact.ToInterchangeEnvelope().Edges.Count); Assert.True(png.Length > 64);
    }

    [Fact]
    public void ShallowDefaultControlsRetainDenseGroupedAndOptionalAutomaticPorts() {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\ngroup scope[Scope]\nservice a[A] in scope\nservice b[B]\na -- b"); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.DenseGrouped, chart.LayoutMode);
        Assert.All(chart.Groups, group => Assert.Null(group.ParentGroupId));
        Assert.Equal(TopologyEdgePort.Auto, chart.Edges[0].SourcePort); Assert.Equal(TopologyEdgePort.Auto, chart.Edges[0].TargetPort);
    }

    private static void CapturePrepared(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng());
        File.WriteAllText(Path.Combine(output, name + ".native.json"), prepared.ToInterchangeEnvelope().ToJson());
    }

    internal static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), artifact.ToPng()); File.WriteAllText(Path.Combine(output, name + ".native.json"), artifact.ToInterchangeJson());
    }
}
