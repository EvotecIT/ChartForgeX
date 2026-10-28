using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidArchitectureAlignmentTests {
    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-ordered-alignments.mmd"));

    [Fact]
    public void OrderedOccurrencesRetainForwardMembersRawStatementsSpansAndNativeIdentity() {
        var source = Fixture(); var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(source, document.SourceText);
        Assert.Equal(20, document.Statements.Count); Assert.Equal(5, document.Alignments.Count);
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-ordered-alignments.expected.json")));
        var chart = document.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Layered, chart.LayoutMode);
        for (var i = 0; i < document.Alignments.Count; i++) {
            var row = expected.RootElement.GetProperty("alignments")[i]; var sourceAlignment = document.Alignments[i]; var native = chart.Alignments[i];
            Assert.Equal(row[0].GetString(), sourceAlignment.Kind.ToString()); Assert.Equal(row[1].EnumerateArray().Select(value => value.GetString()), sourceAlignment.Members);
            Assert.Equal(row[2].GetInt32(), sourceAlignment.Span.Line); Assert.Equal(sourceAlignment.Members, native.NodeIds);
            Assert.Equal("mermaid-architecture-alignment-" + i, native.Id); Assert.Equal(sourceAlignment.Span.Line.ToString(), native.Metadata["mermaid.line"]);
        }
        Assert.Equal(expected.RootElement.GetProperty("nodes").EnumerateArray().Select(value => value.GetString()), chart.Nodes.Select(node => node.Id));
        Assert.Equal("data", chart.Edges[3].SourceNodeId); Assert.Equal("bus", chart.Edges[3].Metadata["mermaid.source.id"]);
    }

    [Theory]
    [InlineData(false, 390)] [InlineData(false, 1100)] [InlineData(true, 390)] [InlineData(true, 1100)]
    public void CompleteDirectAndFenceConsumersKeepAlignmentContainmentPreparedCoordinatesAndDetachedFacts(bool dark, int width) {
        var source = Fixture(); var options = MermaidArchitectureNativeDepthTests.Options(dark, width);
        var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics);
        var artifact = result.Artifact!; Capture(artifact, "architecture-alignments-direct-" + width + (dark ? "-dark" : "-light"));
        var saved = artifact.ToInterchangeEnvelope(); AssertGeometry(saved); Assert.Equal(5, saved.Topology!.Alignments.Count);
        var json = artifact.ToInterchangeJson(); var loaded = VisualArtifactInterchangeEnvelope.FromJson(json); Assert.Equal(json, loaded.ToJson());
        var restored = loaded.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Manual, restored.LayoutMode); Assert.Equal(5, restored.Alignments.Count);
        var after = restored.ToVisualArtifact().ToInterchangeEnvelope(); AssertGeometry(after);
        Assert.Equal(saved.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)), after.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height)));
        Assert.Equal(saved.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)), after.Groups.Select(group => (group.Id, group.X, group.Y, group.Width, group.Height)));
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts); Capture(fenced, "architecture-alignments-fence-" + width + (dark ? "-dark" : "-light")); AssertGeometry(fenced.ToInterchangeEnvelope());
        var chart = Assert.IsType<TopologyChart>(artifact.Model); var prepared = chart.Prepare(); var frozen = prepared.ToInterchangeEnvelope().ToJson();
        chart.Alignments[0].NodeIds.Clear(); chart.Alignments[0].Metadata["mermaid.line"] = "changed";
        Assert.Equal(frozen, prepared.ToInterchangeEnvelope().ToJson()); Assert.Equal(3, loaded.Topology!.Alignments[0].NodeIds.Count);
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("align row a")] [InlineData("align diagonal a b")] [InlineData("align row a a")]
    [InlineData("align column a missing")] [InlineData("align row scope a")]
    [InlineData("align row a b;")] [InlineData("align row a- b")] [InlineData("align row a b extra")]
    public void InvalidMembersDirectionsAndGrammarRemainLocatedAndDoNotProduceDirectOrFencedArtifacts(string line) {
        var source = "architecture-beta\ngroup scope[Scope]\nservice a[A]\nservice b[B]\n" + line;
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 5);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        Assert.Contains(fence.Diagnostics, diagnostic => diagnostic.Line == 6);
    }

    [Fact]
    public void ContradictoryAuthoredOrderIsAGenuineLocatedConversionConflict() {
        const string source = "architecture-beta\nservice a[A]\nservice b[B]\nalign row a b\nalign row b a";
        var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line is 4 or 5 && diagnostic.Message.Contains("alignment-conflict"));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded);
    }

    [Fact]
    public void SourceMemberAllocationAndRequiredPortableIdentityBoundsApplyBeforeConversion() {
        var source = Fixture(); var limits = new MermaidResourceLimits { MaximumElements = 30 };
        var exact = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = limits }); Assert.Empty(exact.Diagnostics);
        limits.MaximumElements = 29; var over = new MermaidParser().Parse(source, new MermaidParseOptions { ResourceLimits = limits });
        Assert.Contains(over.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line == 21);
        var member = new string('a', 513); var longSource = "architecture-beta\nalign row " + member + " b\nservice " + member + "[A]\nservice b[B]";
        var longResult = MermaidRenderer.Render(longSource); Assert.Contains(longResult.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line == 2);
        var mutable = Assert.IsType<MermaidArchitectureDocument>(exact.Document); mutable.Alignments[0].Members[0] = member;
        var error = Assert.Throws<MermaidResourceLimitException>(() => mutable.ToTopologyChart()); Assert.Equal("Interchange.MaximumIdCharacters", error.LimitName); Assert.Equal(2, error.Span.Line);
    }

    [Fact]
    public void ReachableLargeAlignedSourceReportsLocatedOperationalResourceExhaustionWithoutAFalseConflict() {
        var source = "architecture-beta\n" + "align row n0 n1\n" + string.Join("\n", Enumerable.Range(0, 4200).Select(index => "service n" + index + "[Node]"));
        var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && diagnostic.Span.Line == 2 && diagnostic.Message.Contains("Topology.AlignmentWork"));
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Message.Contains("alignment-conflict"));
    }

    internal static void AssertGeometry(VisualArtifactInterchangeEnvelope saved) {
        Assert.Equal(7, saved.Nodes.Count); Assert.Equal(4, saved.Groups.Count); Assert.Equal(4, saved.Edges.Count);
        foreach (var declaration in saved.Topology!.Alignments) {
            var nodes = declaration.NodeIds.Select(id => saved.Nodes.Single(node => node.Id == id)).ToArray();
            for (var i = 1; i < nodes.Length; i++) {
                var a = nodes[i - 1]; var b = nodes[i];
                if (declaration.Kind == TopologyAlignmentKind.Row) {
                    Assert.Equal(a.Y!.Value + a.Height!.Value / 2, b.Y!.Value + b.Height!.Value / 2, 6); Assert.True(a.X!.Value + a.Width!.Value < b.X!.Value);
                } else {
                    Assert.Equal(a.X!.Value + a.Width!.Value / 2, b.X!.Value + b.Width!.Value / 2, 6); Assert.True(a.Y!.Value + a.Height!.Value < b.Y!.Value);
                }
            }
        }
        foreach (var group in saved.Groups) {
            foreach (var node in saved.Nodes.Where(node => node.GroupId == group.Id)) Contains(group, node.X!.Value, node.Y!.Value, node.Width!.Value, node.Height!.Value);
            foreach (var child in saved.Groups.Where(child => child.Topology!.ParentGroupId == group.Id)) Contains(group, child.X!.Value, child.Y!.Value, child.Width!.Value, child.Height!.Value);
        }
        static void Contains(VisualArtifactInterchangeGroup parent, double x, double y, double width, double height) {
            Assert.True(x >= parent.X!.Value && y >= parent.Y!.Value && x + width <= parent.X!.Value + parent.Width!.Value + .000001 && y + height <= parent.Y!.Value + parent.Height!.Value + .000001);
        }
    }

    internal static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name); File.WriteAllText(path + ".svg", artifact.ToSvg());
        File.WriteAllBytes(path + ".native.png", artifact.ToPng()); File.WriteAllText(path + ".native.json", artifact.ToInterchangeJson());
    }
}
