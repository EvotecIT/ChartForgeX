using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidClassSemanticNotationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void GenericMembersAndClassifiersUseNativeTypographyInPairedDirectFenceArtifacts(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Fixture("class-native-members-namespaces");
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var document = Assert.IsType<MermaidClassDocument>(direct.Document);
        var owner = document.Classes.Single(item => item.Id == "Store");
        Assert.Equal("Store<Item>", owner.Label);
        Assert.Equal("Domain.Core", owner.Namespace);
        Assert.Equal(new[] { "+List<List<int>> values", "+lookup(int id, string key) Item", "+create() Item", "+int count", "+reset()" }, owner.Members.Select(item => item.DisplayText));
        Assert.True(owner.Members[1].IsMethod);
        Assert.True(owner.Members[1].IsAbstract);
        Assert.Equal('+', owner.Members[1].Visibility);
        Assert.True(owner.Members[2].IsAbstract);
        Assert.True(owner.Members[2].IsMethod);
        Assert.True(owner.Members[3].IsStatic);
        Assert.False(owner.Members[3].IsMethod);
        Assert.True(owner.Members[4].IsStatic);
        Assert.Equal("Customer, account", document.Classes.Single(item => item.Id == "Customer").Label);
        var chart = Assert.IsType<TopologyChart>(direct.Artifact!.Model);
        var details = chart.Nodes.Single(node => node.Id == "Store").Details;
        Assert.Equal(owner.Members.Select(item => item.DisplayText), details.Select(detail => detail.Text));
        Assert.True(details[1].TextStyle!.Italic);
        Assert.True(details[2].TextStyle!.Italic);
        Assert.True(details[3].TextStyle!.Underline);
        Assert.True(details[4].TextStyle!.Underline);
        var prepared = chart.Prepare();
        var texts = prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().Where(item => item.Role == "topology-node-detail").ToArray();
        Assert.Contains(texts, item => item.Text.Style.Font.Italic && item.Text.Lines.Any(line => line.Text == "+lookup(int id, string key) Item"));
        Assert.Equal(2, prepared.Visual.Scene.Nodes.Count(item => item.Role == "text-decoration"));
        var svg = XDocument.Parse(direct.Artifact.ToSvg());
        Assert.Contains(svg.Descendants(), item => item.Name.LocalName == "text" && item.Value == "+lookup(int id, string key) Item" && (string?)item.Attribute("font-style") == "italic");
        Assert.DoesNotContain(svg.Descendants().Where(item => item.Name.LocalName == "text"), item => item.Value.EndsWith('*') || item.Value.EndsWith('$'));
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts);
        var svgText = direct.Artifact.ToSvg();
        var png = direct.Artifact.ToPng();
        Assert.Equal(svgText, fenced.ToSvg());
        Assert.Equal(png, fenced.ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(direct.Artifact.ToInterchangeJson());
        var rows = envelope.Nodes.Single(node => node.Id == "Store").Details;
        Assert.True(rows[1].TextStyle!.Italic);
        Assert.True(rows[2].TextStyle!.Italic);
        Assert.True(rows[3].TextStyle!.Underline);
        rows[1].TextStyle!.Italic = false;
        Assert.True(VisualArtifactInterchangeEnvelope.FromJson(direct.Artifact.ToInterchangeJson()).Nodes.Single(node => node.Id == "Store").Details[1].TextStyle!.Italic);
        Capture("class-members" + (dark ? "-dark" : "-light"), svgText, png);
    }

    [Fact]
    public void NamespaceLabelsAndEmptyDottedNamespacesPreserveNativeGroupIdentity() {
        var parsed = new MermaidParser().ParseClass(Fixture("class-native-members-namespaces"));
        Assert.Empty(parsed.Diagnostics);
        Assert.Equal(new[] { "Domain", "Domain.Core", "Empty" }, parsed.Document!.Namespaces.Select(item => item.Id));
        var chart = parsed.Document.ToTopologyChart();
        Assert.Equal("Domain core", chart.Groups.Single(group => group.Id == "Domain.Core").Label);
        Assert.Equal("Domain", chart.Groups.Single(group => group.Id == "Domain.Core").ParentGroupId);
        Assert.Equal("Empty namespace", chart.Groups.Single(group => group.Id == "Empty").Label);
        Assert.Equal("Domain.Core", chart.Nodes.Single(node => node.Id == "Customer").GroupId);
        Assert.DoesNotContain(chart.Nodes, node => node.Id == "Domain" || node.Id == "Empty");
    }

    [Fact]
    public void CommasInDisplayLabelsAndMethodParametersAreNotStyleAssignments() {
        var parsed = new MermaidParser().ParseClass("classDiagram\nclass A[\"Comma, label\"]\nclass B {\n+lookup(int id, string key)\n}\nA --> B");
        Assert.Empty(parsed.Diagnostics);
        Assert.Empty(parsed.Document!.StyleStatements);
        Assert.Equal(new[] { "A", "B" }, parsed.Document.Classes.Select(item => item.Id));
        Assert.Equal("Comma, label", parsed.Document.Classes[0].Label);
        Assert.Equal("+lookup(int id, string key)", Assert.Single(parsed.Document.Classes[1].Members).DisplayText);
        Assert.Single(parsed.Document.Relationships);
    }

    [Fact]
    public void UpstreamUnsupportedCommaGenericsRemainOneLocatedDiagnosticWithoutFakeClasses() {
        const string statement = "class Pair~K,V~";
        var result = MermaidRenderer.Render("classDiagram\nclass A\n  " + statement);
        Assert.False(result.HasErrors);
        var warning = Assert.Single(result.Diagnostics);
        Assert.Equal((MermaidDiagnosticCodes.UnsupportedStatement, 3, 3, statement.Length),
            (warning.Code, warning.Span.Line, warning.Span.Column, warning.Span.Length));
        Assert.Equal("A", Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes).Id);
        Assert.Equal(statement, Assert.Single(result.Document!.RawStatements).Text);
    }

    [Theory]
    [InlineData("https://example.invalid/open", true)]
    [InlineData("javascript:alert(1)", false)]
    public void ClassLinksReuseNativeSafeUriPolicyAndKeepTooltipsAsInertText(string href, bool safe) {
        var result = MermaidRenderer.Render("classDiagram\nclass A\nclick A href \"" + href + "\" \"Open <script>ignored</script>\"");
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(href, Assert.Single(chart.Nodes).Href);
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        var links = svg.Descendants().Where(item => item.Name.LocalName == "a").ToArray();
        Assert.Equal(safe ? 1 : 0, links.Length);
        Assert.DoesNotContain(svg.Descendants(), item => item.Name.LocalName == "script");
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        Assert.Equal(safe ? href : null, Assert.Single(envelope.Nodes).Href);
        Assert.DoesNotContain(chart.Nodes, node => node.Id.Contains("script", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EveryLollipopOccurrenceIsASeparateVisibleNativeInterface(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Fixture("class-native-lollipops");
        var rendered = MermaidRenderer.Render(source);
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(new[] { "A", "B" }, Assert.IsType<MermaidClassDocument>(rendered.Document).Classes.Select(item => item.Id));
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        var interfaces = chart.Nodes.Where(node => node.Metadata["mermaid.kind"] == "interface").ToArray();
        Assert.Equal(3, interfaces.Length);
        Assert.Equal(3, interfaces.Select(node => node.Id).Distinct().Count());
        Assert.Equal(new[] { "Port", "Port", "Clock" }, interfaces.Select(node => node.Metadata["mermaid.id"]));
        Assert.All(interfaces, node => {
            Assert.Equal(TopologyNodeShape.Circle, node.Shape);
            Assert.Equal("Services", node.GroupId);
            Assert.False(node.ShowLabel);
        });
        var svg = XDocument.Parse(rendered.Artifact.ToSvg());
        Assert.Equal(3, svg.Descendants().Count(item => (string?)item.Attribute("data-cfx-role") == "topology-edge-endpoint-label"));
        foreach (var name in new[] { "Port", "Clock" }) Assert.Contains(svg.Descendants(), item => item.Name.LocalName == "text" && item.Value == name);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(rendered.Artifact.ToInterchangeJson());
        Assert.All(chart.Edges, edge => {
            var saved = envelope.Edges.Single(item => item.Id == edge.Id);
            Assert.Equal((edge.SourceNodeId, edge.TargetNodeId), (saved.SourceId, saved.TargetId));
        });
        var fenced = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors);
        var png = rendered.Artifact.ToPng();
        Assert.Equal(png, Assert.Single(fenced.Artifacts).ToPng());
        Capture("class-lollipops" + (dark ? "-dark" : "-light"), rendered.Artifact.ToSvg(), png);
    }

    private static string Fixture(string name) => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name + ".mmd"));
    private static void Capture(string name, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, name + ".native.svg"), svg);
        File.WriteAllBytes(Path.Combine(root, name + ".native.png"), png);
    }
}
