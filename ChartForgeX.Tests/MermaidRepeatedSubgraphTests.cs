using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidRepeatedSubgraphTests {
    internal static string Fixture(string family) => File.ReadAllText(Path.Combine(TestRepository.Root,
        "ChartForgeX.Tests", "Fixtures", "Mermaid", "flowchart-repeated-subgraphs.mmd")).Replace("flowchart LR", family + " LR", StringComparison.Ordinal);
    internal static MermaidRenderOptions Options(bool dark, int width) => MermaidFlowchartMembershipTests.Options(dark, width);

    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void CompleteSourceHasOneCanonicalGroupPerIdWhileAllDeclarationsAndSpansRemainAvailable(string family) {
        var source = Fixture(family); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics); var document = parsed.Document!;
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "flowchart-repeated-subgraphs.expected.json")));
        var e = expected.RootElement; Assert.Equal(source, document.SourceText);
        Assert.Equal(e.GetProperty("sourceLines").EnumerateArray().Select(value => value.GetInt32()), document.Statements.Select(statement => statement.Span.Line));
        Assert.Equal(e.GetProperty("groupIds").EnumerateArray().Select(value => value.GetString()), document.Subgraphs.Select(group => group.Id));
        Assert.Equal(e.GetProperty("groupTitles").EnumerateArray().Select(value => value.GetString()), document.Subgraphs.Select(group => group.Title));
        Assert.Equal(e.GetProperty("groupLines").EnumerateArray().Select(value => value.GetInt32()), document.Subgraphs.Select(group => group.Span.Line));
        Assert.Equal(e.GetProperty("groupParents").EnumerateArray().Select(value => value.GetString()), document.Subgraphs.Select(group => group.ParentSubgraphId));
        Assert.Equal(e.GetProperty("groupDirections").EnumerateArray().Select(value => value.GetString()), document.Subgraphs.Select(group => group.Direction.ToString()));
        Assert.Equal(e.GetProperty("nodeIds").EnumerateArray().Select(value => value.GetString()), document.Nodes.Select(node => node.Id));
        Assert.Equal(e.GetProperty("nodeParents").EnumerateArray().Select(value => value.GetString()), document.Nodes.Select(node => node.SubgraphId));
        for (var index = 0; index < document.Subgraphs.Count; index++) Assert.Equal(e.GetProperty("rawGroupNodeReferences")[index].EnumerateArray().Select(value => value.GetString()), document.Subgraphs[index].NodeIds);
        foreach (var ignored in new[] { "Ignored later API", "Ignored later service", "Ignored outer operations" }) Assert.Contains(document.Statements, statement => statement.Text.Contains(ignored, StringComparison.Ordinal));
        Assert.Equal(8, document.Statements.Count(statement => statement.Text.StartsWith("subgraph ", StringComparison.Ordinal)));
        Assert.Equal("Accepted", document.Nodes.Single(node => node.Id == "A").Text); Assert.Equal(6, document.Nodes.Single(node => node.Id == "A").Span.Line);
        var artifact = document.ToVisualArtifact(); Capture(artifact, "repeated-subgraphs-native-" + family); var saved = artifact.ToInterchangeEnvelope(); AssertParents(saved);
        Assert.Equal(document.Subgraphs.Select(group => group.Title), saved.Groups.Select(group => group.Extensions["mermaid.title"]));
        Assert.Equal("5", artifact.Metadata["mermaid.subgraphs"]); Assert.Equal("23", saved.Groups.Single(group => group.Id == "Ops").Extensions["mermaid.source.line"]);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart().Prepare().ToInterchangeEnvelope(); AssertParents(restored);
        Assert.Equal(saved.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)), restored.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)));
    }

    [Theory]
    [InlineData("flowchart", false, 390)] [InlineData("flowchart", true, 1100)]
    [InlineData("swimlane-beta", false, 390)] [InlineData("swimlane-beta", true, 1100)]
    public void OrdinaryDirectAndFencedConsumersShareTheCanonicalNativeGroupsAndSourceCaptions(string family, bool dark, int width) {
        var source = Fixture(family); var options = Options(dark, width); var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors); var other = Assert.Single(fence.Artifacts);
        var artifact = direct.Artifact!; Capture(artifact, "repeated-subgraphs-host-" + family + "-" + width); AssertParents(artifact.ToInterchangeEnvelope());
        Assert.Equal(artifact.ToSvg(), other.ToSvg()); Assert.Equal(artifact.ToPng(), other.ToPng());
        var xml = XDocument.Parse(artifact.ToSvg()); var text = xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var caption in new[] { "Product delivery", "API", "Service", "Review", "Operations", "Accepted", "Build", "Serve", "Track", "Review work", "Observe", "Deliver", "Qualify" }) Assert.Contains(caption, text);
        Assert.DoesNotContain(text, caption => caption.Contains("Ignored", StringComparison.Ordinal));
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject");
    }

    [Theory]
    [InlineData("flowchart")] [InlineData("swimlane-beta")]
    public void RepeatedGroupParentReferencesCanFormALocatedInvalidCycleWithoutBecomingAResourceFailure(string family) {
        var source = family + " LR\nsubgraph A[First]\nB\nend\nsubgraph B[Second]\nA\nend";
        var parsed = new MermaidParser().ParseFlowchart(source); Assert.True(parsed.HasErrors); Assert.NotNull(parsed.Document);
        var diagnostic = Assert.Single(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement); Assert.Contains("cycle", diagnostic.Message); Assert.Equal(2, diagnostic.Span.Line);
        Assert.DoesNotContain(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
        var direct = MermaidRenderer.Render(source); Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        Assert.Contains(fence.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Line == 3);
    }

    internal static void AssertParents(VisualArtifactInterchangeEnvelope saved) {
        Assert.Equal(5, saved.Groups.Count); Assert.Equal(9, saved.Nodes.Count);
        foreach (var pair in new[] { ("P", "Product"), ("A", "API"), ("B", "API"), ("S", "Service"), ("T", "Service"), ("R", "Wrapper"), ("O", "Ops"), ("D", "Ops"), ("Q", "Ops") }) Assert.Equal(pair.Item2, saved.Nodes.Single(node => node.Id == pair.Item1).GroupId);
        Assert.Equal("Product", saved.Groups.Single(group => group.Id == "API").Topology!.ParentGroupId); Assert.Equal("Wrapper", saved.Groups.Single(group => group.Id == "Service").Topology!.ParentGroupId);
        Assert.Null(saved.Groups.Single(group => group.Id == "Ops").Topology!.ParentGroupId); MermaidNestedSwimlaneTests.AssertContainment(saved);
    }
    internal static void Capture(VisualArtifact artifact, string name) => MermaidFlowchartMembershipTests.Capture(artifact, name);
}
