using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidClassEntityStyleTests {
    [Theory]
    [InlineData("class-native-styles", false)]
    [InlineData("class-native-styles", true)]
    [InlineData("er-native-styles", false)]
    [InlineData("er-native-styles", true)]
    public void NamedAndDirectPaintsReachSemanticModelAndPairedDirectFenceArtifacts(string fixture, bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + File.ReadAllText(Fixture(fixture + ".mmd"));
        using var expected = JsonDocument.Parse(File.ReadAllText(Fixture(fixture + ".expected.json")));
        var direct = MermaidRenderer.Render(source);
        Assert.Empty(direct.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact);
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var facts = expected.RootElement.GetProperty("nodeStyleFacts");
        Assert.Equal(facts.GetProperty("nodeClasses").EnumerateObject().Select(item => item.Name), chart.Nodes.Select(node => node.Id));
        foreach (var node in chart.Nodes) {
            var paint = facts.GetProperty("nodeStyles").GetProperty(node.Id);
            Assert.Equal(paint.GetProperty("fill").GetString(), node.BackgroundColor);
            Assert.Equal(paint.GetProperty("stroke").GetString(), node.Color);
            Assert.Equal(paint.GetProperty("color").GetString(), node.LabelColor);
            Assert.All(node.Details, detail => Assert.Equal(node.LabelColor, detail.Color));
            Assert.Equal(string.Join(",", facts.GetProperty("nodeClasses").GetProperty(node.Id).EnumerateArray().Select(item => item.GetString())), node.Metadata["mermaid.classes"]);
        }
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.False(fence.HasErrors);
        var fenced = Assert.Single(fence.Artifacts);
        var svgText = artifact.ToSvg();
        var pngBytes = artifact.ToPng();
        Assert.Equal(svgText, fenced.ToSvg());
        Assert.Equal(pngBytes, fenced.ToPng());
        var svg = XDocument.Parse(svgText);
        var png = RasterImageDecoder.Decode(pngBytes);
        foreach (var color in new[] { "#fef9c3", "#fed7aa" }) {
            Assert.Contains(svg.Descendants(), item => string.Equals((string?)item.Attribute("fill"), color, StringComparison.OrdinalIgnoreCase));
            Assert.True(Count(png.Pixels, ChartColor.Parse(color)) > 20, "Both authored node surfaces must be visible in native PNG.");
        }
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(chart.Nodes.Select(node => node.BackgroundColor), envelope.Nodes.Select(node => node.BackgroundColor));
        Assert.Equal(chart.Nodes.Select(node => node.LabelColor), envelope.Nodes.Select(node => node.LabelColor));
        Assert.Equal(chart.Nodes.SelectMany(node => node.Details).Select(detail => detail.Color), envelope.Nodes.SelectMany(node => node.Details).Select(detail => detail.Color));
        var captures = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(captures)) {
            Directory.CreateDirectory(captures);
            var path = Path.Combine(captures, fixture + (dark ? "-dark" : "-light"));
            File.WriteAllText(path + ".native.svg", svgText);
            File.WriteAllBytes(path + ".native.png", pngBytes);
        }
    }

    [Fact]
    public void StyledGenericClassesAndNullableEntityAttributesKeepTheirOriginalFacts() {
        var classes = new MermaidParser().ParseClass(File.ReadAllText(Fixture("class-native-styles.mmd")));
        Assert.Empty(classes.Diagnostics);
        Assert.Equal(new[] { "Box", "Other" }, classes.Document!.Classes.Select(node => node.Id));
        Assert.Equal("Box<Item>", classes.Document.Classes[0].Label);
        var classEdge = Assert.Single(classes.Document.ToTopologyChart().Edges);
        Assert.Equal(("Box", "Other", TopologyMarkerKind.OpenTriangle, "1", "many"),
            (classEdge.SourceNodeId, classEdge.TargetNodeId, classEdge.SourceMarker, classEdge.SourceLabel, classEdge.TargetLabel));

        var entities = new MermaidParser().ParseEntityRelationship(File.ReadAllText(Fixture("er-native-styles.mmd")));
        Assert.Empty(entities.Diagnostics);
        Assert.Equal(new[] { "C", "O" }, entities.Document!.Entities.Select(node => node.Id));
        Assert.Equal("Customer account", entities.Document.Entities[0].Label);
        var attribute = entities.Document.Entities[0].Attributes[1];
        Assert.Equal(("string?", "display_name"), (attribute.Type, attribute.Name));
        Assert.Equal(("PK, FK", "External key"), (entities.Document.Entities[0].Attributes[0].Key, entities.Document.Entities[0].Attributes[0].Comment));
        var erEdge = Assert.Single(entities.Document.ToTopologyChart().Edges);
        Assert.Equal((TopologyMarkerKind.ExactlyOne, TopologyMarkerKind.ZeroOrMany, TopologyEdgeLineStyle.Solid),
            (erEdge.SourceMarker, erEdge.TargetMarker, erEdge.LineStyle));
    }

    [Theory]
    [InlineData("classDiagram\nclass A", "style A font-weight:bold", "font-weight")]
    [InlineData("erDiagram\nA", "style A fill:javascript:alert(1)", "fill")]
    public void UnsupportedDeclarationsRemainLocatedAndCannotReachNativePaint(string body, string statement, string property) {
        var rendered = MermaidRenderer.Render(body + "\n  " + statement);
        Assert.False(rendered.HasErrors);
        var diagnostic = Assert.Single(rendered.Diagnostics);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, diagnostic.Code);
        Assert.Equal((3, 3, statement.Length), (diagnostic.Span.Line, diagnostic.Span.Column, diagnostic.Span.Length));
        var chart = Assert.IsType<TopologyChart>(rendered.Artifact!.Model);
        var node = Assert.Single(chart.Nodes);
        Assert.Equal("A", node.Id);
        Assert.Null(node.BackgroundColor);
        Assert.True(node.Metadata.ContainsKey("mermaid.style." + property));
        Assert.NotEmpty(rendered.Artifact.ToPng());
        Assert.DoesNotContain(XDocument.Parse(rendered.Artifact.ToSvg()).Descendants().Attributes("fill"),
            attribute => attribute.Value.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("classDiagram\nclass A", "cssClass \"Missing\" marked")]
    [InlineData("erDiagram\nA", "class Missing marked")]
    public void StylesForUndeclaredTargetsAreDiagnosedWithoutInventingSemanticNodes(string body, string statement) {
        var rendered = MermaidRenderer.Render(body + "\n" + statement);
        Assert.False(rendered.HasErrors);
        Assert.Equal("A", Assert.Single(Assert.IsType<TopologyChart>(rendered.Artifact!.Model).Nodes).Id);
        Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, Assert.Single(rendered.Diagnostics).Code);
        Assert.Equal(statement, Assert.Single(rendered.Document!.RawStatements).Text);
    }

    [Theory]
    [InlineData("classDiagram\nclass A:::")]
    [InlineData("classDiagram\nclass A:::one:::two")]
    [InlineData("erDiagram\nA:::")]
    [InlineData("erDiagram\nA:::one:::two")]
    public void MalformedInlineClassesRejectDirectAndFencedArtifacts(string source) {
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors);
        Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 2);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors);
        Assert.Empty(fence.Artifacts);
    }

    [Fact]
    public void SharedDefinitionsAreUsableThroughThePublicTypedClassAndErApis() {
        var definition = new MermaidStyleDefinition("marked", new MermaidSourceSpan(1, 1, 1));
        definition.Styles["fill"] = "#fef9c3";
        var classes = new MermaidClassDocument();
        classes.ClassDefinitions.Add(definition);
        var item = new MermaidClassNode("A", new MermaidSourceSpan(2, 1, 1));
        item.Classes.Add("marked");
        classes.Classes.Add(item);
        Assert.Equal("#fef9c3", Assert.Single(classes.ToTopologyChart().Nodes).BackgroundColor);
        var entities = new MermaidEntityRelationshipDocument();
        entities.ClassDefinitions.Add(definition);
        var entity = new MermaidEntityNode("A", new MermaidSourceSpan(2, 1, 1));
        entity.Classes.Add("marked");
        entities.Entities.Add(entity);
        Assert.Equal("#fef9c3", Assert.Single(entities.ToTopologyChart().Nodes).BackgroundColor);
    }

    private static int Count(byte[] pixels, ChartColor color) {
        var count = 0;
        for (var index = 0; index < pixels.Length; index += 4)
            if (pixels[index] == color.R && pixels[index + 1] == color.G && pixels[index + 2] == color.B) count++;
        return count;
    }

    private static string Fixture(string name) => Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", name);
}
