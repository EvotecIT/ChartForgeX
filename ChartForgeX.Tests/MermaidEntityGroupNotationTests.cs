using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEntityGroupNotationTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedErGroupsRetainAliasesDirectionsShellEdgesAndAuthoredPaintsInDirectFenceExports(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + Fixture();
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidEntityRelationshipDocument>(result.Document);
        Assert.Equal(new[] { "Outside", "C", "O" }, document.Entities.Select(item => item.Id));
        Assert.Equal(new[] { "Domain", "Orders" }, document.Groups.Select(item => item.Id));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(TopologyLayoutDirection.LeftToRight, chart.LayoutDirection);
        Assert.Equal("Domain", chart.Edges[0].TargetNodeId);
        Assert.DoesNotContain(chart.Nodes, node => node.Id == "Domain" || node.Id == "Orders");
        var domain = chart.Groups.Single(group => group.Id == "Domain");
        Assert.Equal(("Data domain", TopologyLayoutDirection.TopToBottom), (domain.Label, domain.LayoutDirection));
        Assert.Equal(("#bae6fd", "#075985", "#082f49"), (domain.BackgroundColor, domain.BorderColor, domain.LabelColor));
        var orders = chart.Groups.Single(group => group.Id == "Orders");
        Assert.Equal(("Domain", TopologyLayoutDirection.BottomToTop), (orders.ParentGroupId, orders.LayoutDirection));
        Assert.Equal(("#dcfce7", "#166534", "#052e16"), (orders.BackgroundColor, orders.BorderColor, orders.LabelColor));
        // The inner group closes first in Mermaid's grammar and owns referenced entities.
        Assert.Equal("Orders", chart.Nodes.Single(node => node.Id == "C").GroupId);
        Assert.Equal("Orders", chart.Nodes.Single(node => node.Id == "O").GroupId);
        Assert.Equal("Customer account", chart.Nodes.Single(node => node.Id == "C").Label);
        Assert.Equal("string?", document.Entities.Single(item => item.Id == "C").Attributes[1].Type);
        var svgText = result.Artifact.ToSvg();
        var svg = XDocument.Parse(svgText);
        Assert.Equal(2, svg.Descendants().Count(item => (string?)item.Attribute("data-cfx-role") == "topology-group-surface"));
        var png = result.Artifact.ToPng();
        var image = RasterImageDecoder.Decode(png);
        foreach (var color in new[] { "#bae6fd", "#dcfce7" }) {
            Assert.Contains(svg.Descendants(), item => string.Equals((string?)item.Attribute("fill"), color, StringComparison.OrdinalIgnoreCase));
            var expected = ChartColor.Parse(color);
            Assert.True(Enumerable.Range(0, image.Pixels.Length / 4).Count(index => image.Pixels[index * 4] == expected.R
                && image.Pixels[index * 4 + 1] == expected.G && image.Pixels[index * 4 + 2] == expected.B) > 20);
        }
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        Assert.Equal(svgText, Assert.Single(fence.Artifacts).ToSvg());
        Assert.Equal(png, Assert.Single(fence.Artifacts).ToPng());
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(result.Artifact.ToInterchangeJson());
        var saved = envelope.Groups.Single(group => group.Id == "Domain");
        Assert.Equal((domain.BackgroundColor, domain.BorderColor, domain.LabelColor), (saved.BackgroundColor, saved.BorderColor, saved.LabelColor));
        var savedOrders = envelope.Groups.Single(group => group.Id == "Orders");
        Assert.Equal("Domain", Assert.IsType<VisualArtifactInterchangeTopologyGroup>(savedOrders.Topology).ParentGroupId);
        Capture("er-groups" + (dark ? "-dark" : "-light"), svgText, png);
    }

    [Fact]
    public void EntityDefaultClassDoesNotImplicitlyStyleGroupsAndLaterSiblingsCannotStealMembers() {
        const string source = "erDiagram\nsubgraph First\nA\nend\nsubgraph Second\nA ||--o{ B : refs\nend\nclassDef default fill:#fef9c3,stroke:#713f12,color:#422006";
        var result = MermaidRenderer.Render(source);
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("First", chart.Nodes.Single(node => node.Id == "A").GroupId);
        Assert.Equal("Second", chart.Nodes.Single(node => node.Id == "B").GroupId);
        Assert.All(chart.Groups, group => {
            Assert.Null(group.BackgroundColor);
            Assert.Null(group.BorderColor);
            Assert.Null(group.LabelColor);
        });
        Assert.All(chart.Nodes, node => Assert.Equal("#fef9c3", node.BackgroundColor));
    }

    [Theory]
    [InlineData("A[Alias]")]
    [InlineData("A {\nint id PK\n}")]
    public void ARealEntityAndGroupCannotSilentlyShareOneIdentity(string declaration) {
        var source = "erDiagram\n" + declaration + "\nsubgraph A\nB\nend";
        var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors);
        Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line > 1);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors);
        Assert.Empty(fence.Artifacts);
    }

    [Fact]
    public void GroupUnsupportedStylesRemainLocatedWithoutAffectingNativePaints() {
        const string statement = "style Domain fill:url(https://example.invalid/pattern),stroke:#123456";
        var result = MermaidRenderer.Render("erDiagram\nsubgraph Domain\nA\nend\n  " + statement);
        Assert.False(result.HasErrors);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal((MermaidDiagnosticCodes.UnsupportedStatement, 5, 3, statement.Length),
            (diagnostic.Code, diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        var group = Assert.Single(Assert.IsType<TopologyChart>(result.Artifact!.Model).Groups);
        Assert.Null(group.BackgroundColor);
        Assert.Equal("#123456", group.BorderColor);
        Assert.Equal("url(https://example.invalid/pattern)", group.Metadata["mermaid.style.fill"]);
        Assert.DoesNotContain(XDocument.Parse(result.Artifact.ToSvg()).Descendants().Attributes("fill"), attribute => attribute.Value.StartsWith("url(", StringComparison.OrdinalIgnoreCase));
    }

    private static string Fixture() => File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "er-native-groups.mmd"));
    private static void Capture(string name, string svg, byte[] png) {
        var root = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (string.IsNullOrWhiteSpace(root)) return;
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, name + ".native.svg"), svg);
        File.WriteAllBytes(Path.Combine(root, name + ".native.png"), png);
    }
}
