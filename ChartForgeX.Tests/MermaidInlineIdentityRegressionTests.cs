using ChartForgeX.VisualArtifacts;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Inline style delimiters never replace source identity or relationship-label boundaries.</summary>
public sealed class MermaidInlineIdentityRegressionTests {
    [Theory]
    [InlineData("classDiagram", "\"\" --> B")]
    [InlineData("classDiagram", "A --> \"\"")]
    [InlineData("classDiagram", "\"\" --() B")]
    [InlineData("classDiagram", "A ()-- \"\"")]
    [InlineData("classDiagram", "class :::marked")]
    [InlineData("classDiagram", "class :::marked {\n+id int\n}")]
    [InlineData("classDiagram", "class [\"Display\"]:::marked")]
    [InlineData("classDiagram", "class \"\":::marked")]
    [InlineData("classDiagram", "class ``:::marked")]
    [InlineData("classDiagram", "class :::marked { +id int }")]
    [InlineData("erDiagram", ":::marked")]
    [InlineData("erDiagram", ":::marked {\nstring id\n}")]
    [InlineData("erDiagram", "[\"Display\"]:::marked")]
    [InlineData("erDiagram", "\"\":::marked")]
    [InlineData("erDiagram", "``:::marked")]
    [InlineData("erDiagram", ":::marked ||--o{ B : owns")]
    [InlineData("erDiagram", "A ||--o{ :::marked : owns")]
    [InlineData("erDiagram", "A ||--o{ \"\":::marked : owns")]
    public void MissingInlineIdentitiesReturnLocatedDiagnosticsInsteadOfConstructorExceptions(string header, string statement) {
        var source = header + "\n  " + statement;
        var parsed = new MermaidParser().Parse(source);
        Assert.True(parsed.HasErrors);
        Assert.Contains(parsed.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 2 && item.Span.Column == 3);
        var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```");
        Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        Assert.Contains(fence.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Line == 3 && item.Column == 3);
    }

    [Theory]
    [InlineData("class List~T~:::marked")]
    [InlineData("class List~T~:::marked {\n+item T\n}")]
    public void NonemptyGenericClassIdentityAndStyleSurviveDeclarationAndBodyForms(string declaration) {
        var result = new MermaidParser().ParseClass("classDiagram\nclassDef marked fill:#ff0000\n" + declaration);
        Assert.Empty(result.Diagnostics);
        var node = Assert.Single(result.Document!.Classes);
        Assert.Equal(("List", "List<T>"), (node.Id, node.Label));
        Assert.Equal("marked", Assert.Single(node.Classes));
        Assert.Equal("#ff0000", Assert.Single(result.Document.ToTopologyChart().Nodes).BackgroundColor);
    }

    [Theory]
    [InlineData("A ||--o{ B:::marked : owns", "A", "B", "owns", false, true)]
    [InlineData("A:::marked ||--o{ B : owns", "A", "B", "owns", true, false)]
    [InlineData("A:::marked ||--o{ B:::marked : owns: event", "A", "B", "owns: event", true, true)]
    [InlineData("\"A:owner\":::marked ||--o{ \"B:child\":::marked : \"owns: safely\"", "A:owner", "B:child", "owns: safely", true, true)]
    [InlineData("\"A:::literal\":::marked ||--o{ \"B:::literal\":::marked : owns:::retained", "A:::literal", "B:::literal", "owns:::retained", true, true)]
    [InlineData("\"A||--o{literal\":::marked ||--o{ B:::marked : owns", "A||--o{literal", "B", "owns", true, true)]
    [InlineData("A ||--o{ B:::marked : owns ||--|| terms", "A", "B", "owns ||--|| terms", false, true)]
    [InlineData("A ||--o{ B:::marked", "A", "B", "", false, true)]
    public void EntityInlineClassesAndQuotedColonsDoNotBecomeTheRelationshipLabel(string relationship, string sourceId,
        string targetId, string label, bool sourceStyled, bool targetStyled) {
        var result = new MermaidParser().ParseEntityRelationship("erDiagram\nclassDef marked fill:#ff0000\n" + relationship);
        Assert.Empty(result.Diagnostics);
        var document = result.Document!;
        Assert.Equal(new[] { sourceId, targetId }, document.Entities.Select(entity => entity.Id));
        var edge = Assert.Single(document.Relationships);
        Assert.Equal((sourceId, targetId, "||--o{", label), (edge.SourceId, edge.TargetId, edge.Connector, edge.Label));
        Assert.Equal(sourceStyled, document.Entities[0].Classes.Contains("marked"));
        Assert.Equal(targetStyled, document.Entities[1].Classes.Contains("marked"));
        var native = document.ToTopologyChart();
        Assert.Equal(sourceStyled ? "#ff0000" : null, native.Nodes[0].BackgroundColor);
        Assert.Equal(targetStyled ? "#ff0000" : null, native.Nodes[1].BackgroundColor);
        Assert.Equal((sourceId, targetId, label), (native.Edges[0].SourceNodeId, native.Edges[0].TargetNodeId, native.Edges[0].Label ?? string.Empty));
    }

    [Fact]
    public void QuotedEntityIdentityDelimitersRemainLiteralOnDeclarationsAndRelationshipReferences() {
        const string source = "erDiagram\n\"A:::literal\"\n\"B:owner\"\n\"A:::literal\" ||--o{ \"B:owner\" : retained";
        var result = new MermaidParser().ParseEntityRelationship(source);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(new[] { "A:::literal", "B:owner" }, result.Document!.Entities.Select(entity => entity.Id));
        Assert.All(result.Document.Entities, entity => Assert.Empty(entity.Classes));
        var edge = Assert.Single(result.Document.Relationships);
        Assert.Equal(("A:::literal", "B:owner", "retained"), (edge.SourceId, edge.TargetId, edge.Label));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void StyledEntityEndpointsReachTheSameActualDirectAndFencedSvgPng(bool dark) {
        const string source = "erDiagram\nclassDef marked fill:#ff0000\n\"A:owner\":::marked ||--o{ \"B:child\":::marked : \"owns: safely\"";
        var options = new MermaidRenderOptions { Presentation = new MermaidPresentationOptions {
            ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, FontFamily = BrowserProofFonts.Family } };
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var svg = artifact.ToSvg(); var png = artifact.ToPng();
        var xml = XDocument.Parse(svg);
        Assert.Contains(xml.Descendants(), item => (string?)item.Attribute("fill") == "#FF0000" || (string?)item.Attribute("fill") == "#ff0000");
        Assert.Contains(xml.Descendants(), item => item.Name.LocalName == "text" && item.Value == "owns: safely");
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        Assert.Empty(fenced.Diagnostics);
        var fence = Assert.Single(fenced.Artifacts); Assert.Equal(svg, fence.ToSvg()); Assert.Equal(png, fence.ToPng());
    }
}
