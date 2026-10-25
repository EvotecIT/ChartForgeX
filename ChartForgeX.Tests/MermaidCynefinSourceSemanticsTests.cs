using ChartForgeX.Topology;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidCynefinSourceSemanticsTests {
    [Fact]
    public void PinnedStringsRepeatedBlocksAndSelfLoopsRetainExactSourceButUseReferenceFacts() {
        const string source = "cynefin-beta:\r\ncomplex 'old'\r\nclear \"Ready\"\r\ncomplex 'Don\\'t' \"a\\nb\\t\\u1234\"\r\ncomplex --> complex : 'ignored loop'\r\ncomplex --> clear : 'Choose \\\"one\\\"'\r\ntitle \"Quoted  title\" %% tail";
        var parsed = new MermaidParser().ParseCynefin(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(source, document.SourceText);
        Assert.Equal(new[] { "complex", "clear" }, document.Domains.Keys);
        Assert.Equal(new[] { "Don't", "a\nb\tu1234" }, document.Domains["complex"]);
        Assert.Equal("\"Quoted title\"", document.Title);
        var edge = Assert.Single(document.Transitions); Assert.Equal("Choose \"one\"", edge.Label);
        Assert.Equal((6, 1), (edge.Span.Line, edge.Span.Column));
        var raw = Assert.Single(document.RawStatements, item => item.Text.Contains("ignored loop", StringComparison.Ordinal));
        Assert.Equal((5, 1, "complex --> complex : 'ignored loop'".Length), (raw.Span.Line, raw.Span.Column, raw.Span.Length));
        foreach (var statement in document.RawStatements) {
            var starts = new List<int> { 0 }; for (var i = 0; i < source.Length; i++) if (source[i] == '\n') starts.Add(i + 1);
            Assert.Equal(statement.Text, source.Substring(starts[statement.Span.Line - 1] + statement.Span.Column - 1, statement.Span.Length));
        }
        // A semantic roundtrip owns a finite host font; arbitrary installed roots retain their independent wire cap.
        var chart = document.ToTopologyChart(new MermaidTopologyRenderOptions {
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
        }); Assert.Equal(2, chart.Nodes.Single(node => node.Id == "complex").Details.Count);
        Assert.Single(chart.Edges); Assert.NotNull(chart.Edges[0].PathGeometry);
        Assert.All(chart.Nodes.Where(node => node.Subtitle != null), node => Assert.True(node.SubtitleTextStyle!.Italic));
        Assert.All(chart.Nodes.SelectMany(node => node.Details), detail => Assert.NotNull(detail.BadgeStyle));
        var prepared = chart.Prepare(); TopologyPortableFontObservation.Capture("cynefin-source-semantics", prepared.Visual);
        var portable = VisualArtifactInterchangeEnvelope.FromJson(prepared.Visual.ToArtifact().ToInterchangeJson());
        Assert.Equal(2, portable.Version); var restored = portable.ToTopologyChart();
        Assert.All(restored.Nodes.Where(node => node.Subtitle != null), node => Assert.True(node.SubtitleTextStyle!.Italic));
        Assert.Equal(2, restored.Nodes.Single(node => node.Id == "complex").Details.Count);
    }

    [Fact]
    public void PhysicalQuotedContinuationCannotCreateConfigurationOrAccessibilityDeclarations() {
        const string source = "cynefin-beta\ncomplex \"First\naccTitle: literal\n%%{init: { 'theme': 'dark' }}%%\nlast\"\naccTitle: Real name\ncomplex --> clear : 'Line\naccDescr: literal\nend'";
        var parsed = new MermaidParser().ParseCynefin(source); Assert.Empty(parsed.Diagnostics);
        Assert.Equal(source, parsed.Document!.SourceText); Assert.Equal("Real name", parsed.Document.Accessibility.Name);
        Assert.Null(parsed.Document.Accessibility.Description);
        Assert.Contains("accTitle: literal", parsed.Document.Domains["complex"][0], StringComparison.Ordinal);
        Assert.Contains("%%{init:", parsed.Document.Domains["complex"][0], StringComparison.Ordinal);
        Assert.Equal("Line\naccDescr: literal\nend", Assert.Single(parsed.Document.Transitions).Label);
        var item = Assert.Single(parsed.Document.RawStatements, statement => statement.Text.StartsWith("\"First", StringComparison.Ordinal));
        Assert.Equal((2, 9), (item.Span.Line, item.Span.Column));
    }

    [Fact]
    public void SharedPinnedFixtureMatchesNativeFactsAndKeepsAllConfusionItemsVisible() {
        var folder = Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid");
        var source = File.ReadAllText(Path.Combine(folder, "cynefin-source-semantics.mmd"));
        var parsed = new MermaidParser().ParseCynefin(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!;
        using var expected = System.Text.Json.JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "cynefin-source-semantics.expected.json")));
        var facts = expected.RootElement.GetProperty("cynefinFacts"); Assert.Equal(facts.GetProperty("title").GetString(), document.Title);
        foreach (var domain in facts.GetProperty("domains").EnumerateArray())
            Assert.Equal(domain[1].EnumerateArray().Select(item => item.GetString()), document.Domains[domain[0].GetString()!]);
        Assert.Equal(4, document.Domains["confusion"].Count);
        var svg = XDocument.Parse(document.ToTopologyChart().ToSvg());
        var confusion = Assert.Single(svg.Descendants(), item => (string?)item.Attribute("data-node-id") == "confusion" && (string?)item.Attribute("data-cfx-role") == "topology-node");
        Assert.Equal(4, confusion.Descendants().Count(item => (string?)item.Attribute("data-cfx-role") == "topology-detail-badge"));
        Assert.Contains("Four", confusion.Value, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("complex 'unterminated", 2)]
    [InlineData("complex --> clear : unquoted", 2)]
    [InlineData("complex --> clear complicated", 2)]
    [InlineData("complex \"bad\\0native\"", 2)]
    public void UnsupportedOrMalformedStringsAreLocatedBeforeNativePaint(string body, int line) {
        var result = new MermaidParser().ParseCynefin("cynefin-beta\n" + body);
        Assert.True(result.HasErrors); Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line == line && diagnostic.Span.Column > 0);
    }

    [Fact]
    public void EmptyQuotedItemRemainsAnEmptyBadgeFact() {
        var parsed = new MermaidParser().ParseCynefin("cynefin-beta\ncomplex ''"); Assert.Empty(parsed.Diagnostics);
        var chart = parsed.Document!.ToTopologyChart(); Assert.Equal("", Assert.Single(chart.Nodes.Single(node => node.Id == "complex").Details).Text);
        Assert.Contains(XDocument.Parse(chart.ToSvg()).Descendants(), item => (string?)item.Attribute("data-cfx-role") == "topology-detail-badge");
    }
}
