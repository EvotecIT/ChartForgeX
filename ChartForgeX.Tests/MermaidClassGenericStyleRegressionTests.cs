using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidClassGenericStyleRegressionTests {
    [Theory]
    [InlineData("+List~Item~ items", false, "+List<Item> items")]
    [InlineData("+List~Item~ find()*", true, "+List<Item> find()")]
    [InlineData("+Map~K,List~V~~ values$", false, "+Map<K,List<V>> values")]
    [InlineData("~List~ Item ~ values", false, "~List< Item > values")]
    [InlineData("+Map~K, List~ V~~ values", false, "+Map<K, List< V>> values")]
    [InlineData("+get(List~T~ input) List~Result~", true, "+get(List<T> input) List<Result>")]
    [InlineData("+string literal = \"List~T~\"", false, "+string literal = \"List~T~\"")]
    [InlineData("+string literal = `literal~T~`", false, "+string literal = `literal~T~`")]
    public void GenericTypeBoundariesExcludeMemberNamesAndPreserveQuotedLiterals(string raw, bool method, string expected) {
        var member = new MermaidClassMember(raw, method, new MermaidSourceSpan(3, 3, raw.Length));
        Assert.Equal(raw, member.Text); Assert.Equal(expected, member.DisplayText);
        var document = new MermaidClassDocument { Kind = MermaidDiagramKind.Class, Header = "classDiagram" };
        var owner = new MermaidClassNode("Owner~literal", member.Span); owner.Members.Add(member); document.Classes.Add(owner);
        var node = Assert.Single(document.ToTopologyChart().Nodes); var detail = Assert.Single(node.Details);
        Assert.Equal(expected, detail.Text); Assert.Equal(raw, detail.Metadata["mermaid.member.source"]); Assert.Equal("Owner~literal", node.Id);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LegacyQuotedSelectorListsAndQuotedLiteralIdentityReachDirectFenceSvgPngAndSemanticFacts(bool dark) {
        var source = (dark ? "%%{init: {'theme':'dark'}}%%\n" : "") + """
            classDiagram
            class Box~Item~ {
              +List~Item~ items
              +find() Item
            }
            class Other
            class `Literal,Name~T~`
            classDef marked fill:#fef9c3,color:#7c2d12
            cssClass "Box,Other" marked
            style `Literal,Name~T~` fill:#dcfce7,stroke:#15803d
            Box --> Other
            """;
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        var document = Assert.IsType<MermaidClassDocument>(result.Document); Assert.Empty(document.RawStatements);
        Assert.Equal(new[] { "Box", "Other", "Literal,Name~T~" }, document.Classes.Select(node => node.Id));
        Assert.Equal("+List~Item~ items", document.Classes[0].Members[0].Text);
        Assert.Equal("+List<Item> items", document.Classes[0].Members[0].DisplayText);
        var artifact = result.Artifact!; var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.All(chart.Nodes.Take(2), node => { Assert.Equal("#fef9c3", node.BackgroundColor); Assert.Equal("marked", node.Metadata["mermaid.classes"]); });
        Assert.Equal("#dcfce7", chart.Nodes[2].BackgroundColor); Assert.Equal("#15803d", chart.Nodes[2].Color);
        Assert.Equal("Literal,Name~T~", chart.Nodes[2].Label);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); Assert.NotEmpty(png);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Diagnostics);
        var fenced = Assert.Single(fence.Artifacts); Assert.Equal(svg, fenced.ToSvg()); Assert.Equal(png, fenced.ToPng());
        Assert.Contains(XDocument.Parse(svg).Descendants(), element => element.Name.LocalName == "text" && element.Value == "+List<Item> items");
        var facts = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal("+List<Item> items", facts.Nodes[0].Details[0].Text);
        Assert.Equal("+List~Item~ items", facts.Nodes[0].Details[0].Extensions["mermaid.member.source"]);
        Assert.Equal(chart.Nodes.Select(node => node.BackgroundColor), facts.Nodes.Select(node => node.BackgroundColor));
    }

    [Fact]
    public void ExactQuotedCommaIdentityWinsOverLegacyAggregateListAndGenericSelectorsShareCanonicalIdentity() {
        const string source = """
            classDiagram
            class Box
            class Other
            class `Box,Other`
            class Pair~K,V~
            classDef marked fill:#dbeafe
            classDef aggregate color:#172554
            cssClass "Box,Other" marked
            cssClass Pair~K,V~ marked
            style Pair~K,V~ stroke:#1e3a8a
            cssClass "Pair~K,V~,Box" aggregate
            """;
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Equal(new[] { 5, 9, 10, 11 }, result.Diagnostics.Select(item => item.Span.Line));
        Assert.All(result.Diagnostics, item => Assert.Equal(MermaidDiagnosticCodes.ApproximateMapping, item.Code));
        var document = Assert.IsType<MermaidClassDocument>(result.Document); Assert.Empty(document.RawStatements);
        Assert.Equal(new[] { "Box", "Other", "Box,Other", "Pair" }, document.Classes.Select(node => node.Id));
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Null(chart.Nodes[0].BackgroundColor); Assert.Equal("#172554", chart.Nodes[0].LabelColor);
        Assert.Null(chart.Nodes[1].BackgroundColor); Assert.Null(chart.Nodes[1].LabelColor);
        Assert.Equal("#dbeafe", chart.Nodes[2].BackgroundColor); Assert.Null(chart.Nodes[2].LabelColor);
        Assert.Equal(("Pair<K,V>", "#dbeafe", "#1e3a8a", "#172554"), (chart.Nodes[3].Label, chart.Nodes[3].BackgroundColor, chart.Nodes[3].Color, chart.Nodes[3].LabelColor));
        Assert.NotEmpty(result.Artifact.ToSvg()); Assert.NotEmpty(result.Artifact.ToPng());
    }
    [Fact]
    public void StyleValuesDoNotConsumeGenericDepthAndMalformedTypesKeepLocatedRejection() {
        const string source = "classDiagram\nclass Box~Item~\nclassDef marked fill:#dbeafe\ncssClass Box~Item~ marked\nstyle Box~Item~ stroke:#1e3a8a";
        var options = new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumNestingDepth = 1 } } };
        var accepted = MermaidRenderer.Render(source, options); Assert.Empty(accepted.Diagnostics);
        var node = Assert.Single(Assert.IsType<TopologyChart>(accepted.Artifact!.Model).Nodes);
        Assert.Equal(("Box<Item>", "#dbeafe", "#1e3a8a"), (node.Label, node.BackgroundColor, node.Color));
        var rejected = MermaidRenderer.Render("classDiagram\nclass Box~List~Item~~", options); Assert.Null(rejected.Artifact);
        Assert.Contains(rejected.Diagnostics, item => item.Code == MermaidDiagnosticCodes.ResourceLimitExceeded && item.Span.Line == 2);
        foreach (var malformed in new[] { "Pair~K,,V~", "Pair~K,V~~", "Pair~K+V~", "Pair~?~" }) {
            var invalid = MermaidRenderer.Render("classDiagram\nclass " + malformed); Assert.Null(invalid.Artifact);
            Assert.Contains(invalid.Diagnostics, item => item.Code == MermaidDiagnosticCodes.InvalidStatement && item.Span.Line == 2);
        }
        var missing = MermaidRenderer.Render("classDiagram\nclass Box\ncssClass `Unknown, literal~T~` marked");
        Assert.False(missing.HasErrors); Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, Assert.Single(missing.Diagnostics).Code);
        Assert.Equal("Box", Assert.Single(Assert.IsType<TopologyChart>(missing.Artifact!.Model).Nodes).Id);
    }

}
