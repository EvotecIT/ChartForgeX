using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Trait("BrowserSuite", "Compatibility")]
public sealed partial class MermaidArchitectureCaptionTests {
    internal static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-optional-captions.mmd"));

    [Fact]
    public void CompleteOptionalSourceRetainsAbsentCaptionsAsFactsAndUsesNativeVisibilityAndCatalogs() {
        var source = Fixture(); var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(source, document.SourceText); Assert.Equal(2, document.Groups[0].Span.Line);
        Assert.Equal("", document.Groups[0].Title); Assert.Equal("", document.Services[0].Title); Assert.Equal("", document.Services[1].Title);
        var chart = document.ToTopologyChart();
        using var expected = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestRepository.Root, "ChartForgeX.Tests", "Fixtures", "Mermaid", "architecture-optional-captions.expected.json")));
        foreach (var row in expected.RootElement.GetProperty("groups").EnumerateArray()) {
            var group = chart.Groups.Single(group => group.Id == row[0].GetString());
            Assert.Equal(row[2].GetBoolean(), group.ShowLabel); Assert.Equal(row[3].GetString(), group.IconId);
            Assert.False(string.IsNullOrWhiteSpace(group.Label));
        }
        foreach (var row in expected.RootElement.GetProperty("services").EnumerateArray()) {
            var node = chart.Nodes.Single(node => node.Id == row[0].GetString());
            Assert.Equal(row[2].GetBoolean(), node.ShowLabel); Assert.Equal(row[3].GetString(), node.IconId);
            Assert.False(string.IsNullOrWhiteSpace(node.Label));
        }
        var artifact = document.ToVisualArtifact(); Capture(artifact, "architecture-optional-complete");
        var xml = XDocument.Parse(artifact.ToSvg()); var text = xml.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value).ToArray();
        foreach (var label in new[] { "Facts", "Orders", "Plain" }) Assert.Contains(label, text);
        foreach (var id in new[] { "root", "inner", "web", "worker" }) Assert.DoesNotContain(id, text);
        var portable = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        var root = portable.Groups.Single(group => group.Id == "root"); Assert.Equal("root", root.Label); Assert.False(root.Topology!.ShowLabel);
        var restored = portable.ToTopologyChart(); Assert.False(restored.Groups.Single(group => group.Id == "root").ShowLabel);
        Assert.Equal("common:server", restored.Groups.Single(group => group.Id == "inner").IconId);
        Assert.False(restored.Nodes.Single(node => node.Id == "web").ShowLabel); Assert.True(artifact.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("group g", false, null)] [InlineData("group g(cloud)", false, "cloud")]
    [InlineData("group g[Caption]", true, null)] [InlineData("group g(server)[Caption]", true, "server")]
    [InlineData("service g", false, null)] [InlineData("service g(database)", false, "database")]
    [InlineData("service g[Caption]", true, null)] [InlineData("service g(server)[Caption]", true, "server")]
    [InlineData("group g[\"\"]", false, null)] [InlineData("service g[\"\"]", false, null)]
    public void OptionalIconAndCaptionFormsKeepSourceIdentityWithoutFabricatingPaintedIds(string declaration, bool showLabel, string? icon) {
        var parsed = new MermaidParser().ParseArchitecture("architecture-beta\n" + declaration); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; var chart = document.ToTopologyChart();
        if (declaration.StartsWith("group", StringComparison.Ordinal)) {
            Assert.Equal(icon, Assert.Single(document.Groups).Icon); Assert.Equal(showLabel, Assert.Single(chart.Groups).ShowLabel);
            Assert.Equal("g", chart.Groups[0].Id); Assert.Equal(showLabel ? "Caption" : "", document.Groups[0].Title);
        } else {
            Assert.Equal(icon, Assert.Single(document.Services).Icon); Assert.Equal(showLabel, Assert.Single(chart.Nodes).ShowLabel);
            Assert.Equal("g", chart.Nodes[0].Id); Assert.Equal(showLabel ? "Caption" : "", document.Services[0].Title);
        }
        var sourceText = XDocument.Parse(document.ToSvg()).Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value);
        if (!showLabel) Assert.DoesNotContain("g", sourceText); Assert.True(document.ToPng().Length > 64);
    }

    [Theory]
    [InlineData("database", "common:database", TopologyNodeKind.Database)]
    [InlineData("server", "common:server", TopologyNodeKind.Server)]
    [InlineData("cloud", "cloud:cloud", TopologyNodeKind.Cloud)]
    public void ExactBuiltinsReachExistingGroupAndNodeGlyphOwnersAndPortableCatalogIdentity(string token, string id, TopologyNodeKind kind) {
        var source = "architecture-beta\ngroup g(" + token + ")[Group]\nservice a(" + token + ")[Node] in g";
        var rendered = MermaidRenderer.Render(source); Assert.Empty(rendered.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model); Assert.Equal(id, chart.Groups[0].IconId);
        Assert.Equal(id, chart.Nodes[0].IconId); Assert.Equal(kind, chart.Nodes[0].Kind);
        var definition = TopologyIconCatalog.Default().Resolve(id)!; Assert.Null(chart.Nodes[0].Symbol);
        var xml = XDocument.Parse(rendered.Artifact!.ToSvg());
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-group-icon-shape") == definition.Shape.ToString());
        Assert.Contains(xml.Descendants(), element => (string?)element.Attribute("data-node-icon-shape") == definition.Shape.ToString());
        Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "image" or "script" or "foreignObject");
        Assert.Equal(id, rendered.Artifact!.ToInterchangeEnvelope().Groups[0].Topology!.IconId);
    }

    [Theory]
    [InlineData("disk")] [InlineData("internet")] [InlineData("logos:custom")]
    public void UnmappedBuiltinsAndPacksRemainRawDiagnosedAndInert(string token) {
        var source = "architecture-beta\ngroup g(" + token + ")[Group]\nservice a(" + token + ")[Node] in g";
        var parsed = new MermaidParser().ParseArchitecture(source); Assert.False(parsed.HasErrors);
        Assert.Equal(new[] { 2, 3 }, parsed.Diagnostics.Select(diagnostic => diagnostic.Span.Line));
        Assert.All(parsed.Diagnostics, diagnostic => Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, diagnostic.Code));
        Assert.Equal(token, parsed.Document!.Groups[0].Icon); Assert.Equal(token, parsed.Document!.Services[0].Icon);
        var chart = parsed.Document!.ToTopologyChart(); Assert.Null(chart.Groups[0].IconId); Assert.Null(chart.Nodes[0].IconId);
        Assert.Equal(token, chart.Nodes[0].Metadata["mermaid.icon"]); Assert.Equal(TopologyNodeKind.Service, chart.Nodes[0].Kind);
        var xml = XDocument.Parse(chart.ToSvg()); Assert.DoesNotContain(xml.Descendants(), element => element.Name.LocalName is "image" or "script" or "foreignObject");
    }

    [Theory]
    [InlineData("group g()")][InlineData("service a(server) trailing")][InlineData("group g[Caption")]
    [InlineData("service a(server))")][InlineData("group g in")][InlineData("service a in g extra")]
    [InlineData("group g[]")][InlineData("service a(\"server\")")]
    public void MalformedOptionalDeclarationsStayLocatedAndProduceNoDirectOrFencedArtifact(string declaration) {
        var source = "architecture-beta\n" + declaration; var rendered = MermaidRenderer.Render(source); Assert.True(rendered.HasErrors); Assert.Null(rendered.Artifact);
        Assert.Contains(rendered.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.InvalidStatement && diagnostic.Span.Line == 2);
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fenced.HasErrors); Assert.Empty(fenced.Artifacts);
    }

    [Fact]
    public void OptionalFormsStillParseTrailingParentWithoutSplittingCaptionTextContainingIn() {
        const string source = "architecture-beta\ngroup parent(cloud)\nservice child(database)[Data in memory] in parent";
        var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        Assert.Equal("Data in memory", parsed.Document!.Services[0].Title); Assert.Equal("parent", parsed.Document!.Services[0].GroupId);
        Assert.Equal("parent", parsed.Document!.ToTopologyChart().Nodes[0].GroupId);
    }

    [Fact]
    public void QuotedCaptionsKeepClosingBracketsAndThePrimaryEscapedQuoteSemantics() {
        const string source = """
            architecture-beta
            group scope["Scope] \"quoted\""]
            service owner['Owner\'s caption'] in scope
            """;
        var parsed = new MermaidParser().ParseArchitecture(source); Assert.Empty(parsed.Diagnostics);
        Assert.Equal("Scope] \"quoted\"", parsed.Document!.Groups[0].Title);
        Assert.Equal("Owner's caption", parsed.Document!.Services[0].Title);
        Assert.Equal(3, parsed.Document!.Services[0].Span.Line);
        var xml = XDocument.Parse(parsed.Document!.ToSvg());
        Assert.Contains(xml.Descendants(), element => element.Name.LocalName == "text" && element.Value == "Owner's caption");
    }

    internal static void Capture(VisualArtifact artifact, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), artifact.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), artifact.ToPng()); File.WriteAllText(Path.Combine(output, name + ".native.json"), artifact.ToInterchangeJson());
    }
}
