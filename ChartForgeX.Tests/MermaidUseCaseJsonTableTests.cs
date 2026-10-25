using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidUseCaseJsonTableTests {
    internal static MermaidRenderOptions Options(int width, bool dark) => new() {
        WarningPolicy = MermaidWarningPolicy.Reject,
        UseCase = new MermaidFlowchartRenderOptions { Width = width, Height = width == 390 ? 1000 : 680,
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family,
                ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light, TopologySizing = MermaidTopologySizing.FixedViewport } }
    };
    [Theory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(1000, false)] [InlineData(1000, true)]
    public void LinkedJsonVertexKeepsItsNativeGraphNoteOrderedRawFactsAndDirectFencePortableExports(int width, bool dark) {
        var source = UseCaseJsonTableFixture.Read(width == 390); var options = Options(width, dark);
        var result = MermaidRenderer.Render(source, options); Assert.Empty(result.Diagnostics); var artifact = result.Artifact!;
        var fence = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fence.Diagnostics);
        Assert.Equal(artifact.ToSvg(), Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(artifact.ToPng(), fence.Artifacts[0].ToPng());
        var chart = Assert.IsType<TopologyChart>(artifact.Model); Assert.Equal(new[] { "User", "Publish", "Details" }, chart.Nodes.Select(node => node.Id));
        var node = chart.Nodes[2]; Assert.Empty(node.Details); var table = node.TableContent!; Assert.Equal(9, table.Rows.Count);
        Assert.Equal(new[] { "/version", "/active", "/memo", "/owner/name", "/owner/missing", "/tags/0", "/tags/1", "/path~1~0", "/empty" }, table.Rows.Select(row => row.Key));
        Assert.Equal(new[] { "1.00", "false", "", "Aster", "null", "ready", "signed", "kept", "[]" }, table.Rows.Select(row => row.Cells[1].DisplayText));
        Assert.Equal(new[] { "Number", "Boolean", "String", "String", "Null", "String", "String", "String", "Array" }, table.Rows.Select(row => row.Metadata["mermaid.data.kind"]));
        Assert.Equal("", table.Rows[6].Cells[0].DisplayText); Assert.Equal("tags", table.Rows[6].Cells[0].SearchText);
        Assert.Equal("Details", chart.Annotations.Single().TargetId); Assert.Equal("Details", chart.Edges.Single(edge => edge.Id == "inspect").TargetNodeId);
        var prepared = chart.Prepare(); var text = string.Join(" ", prepared.Visual.Scene.Nodes.OfType<VisualSceneText>().SelectMany(item => item.Text.Lines.Select(line => line.Text)));
        foreach (var word in new[] { "Property", "Value", "Aster", "false", "null", "1.00", "signed", "kept", "Captured", "linked" }) Assert.Contains(word, text);
        Assert.Equal(18, prepared.Visual.Scene.Regions.Count(region => region.Role == "table-cell"));
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson()); Assert.Equal(2, envelope.Version);
        Assert.Equal(source, envelope.Extensions["mermaid.source"]); Assert.Equal(table.Metadata["mermaid.data.source"], envelope.Nodes[2].Topology!.TableContent!.Extensions["mermaid.data.source"]);
        var loaded = envelope.ToTopologyChart(); loaded.Nodes[2].TableContent!.Rows[0].Cells[1].DisplayText = "2.00"; Assert.Contains("2.00", loaded.ToSvg());
        Assert.Equal("1.00", envelope.Nodes[2].Topology!.TableContent!.Rows[0].Cells[1].DisplayText);
        Assert.DoesNotContain(XDocument.Parse(artifact.ToSvg()).Descendants(), element => element.Name.LocalName is "script" or "image" or "foreignObject");
        TopologyInterchangeRestorationTests.Save("usecase-json-" + width + (dark ? "-dark" : "-light"), artifact.ToSvg(), artifact.ToPng());
    }
    [Fact]
    public void TypedSourceOrderSpansOpaqueStringsAndNativeRecipeFactsRemainActual() {
        var source = UseCaseJsonTableFixture.Read(); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidUseCaseDocument>(parsed.Document); Assert.Equal(source, document.SourceText);
        var declaration = document.Nodes.Single(node => node.Id == "Details");
        Assert.Equal("Details", source.Split('\n')[declaration.Span.Line - 1].Substring(declaration.Span.Column - 1, declaration.Span.Length));
        var native = document.ToTopologyChart(); var sourceTable = native.Nodes[2].TableContent!;
        var recipeTable = TopologyTableContentRecipe.CreateTable();
        Assert.Equal(recipeTable.Rows.Select(row => (row.Key, row.Cells[0].DisplayText, row.Cells[1].DisplayText, row.Metadata["mermaid.data.kind"])),
            sourceTable.Rows.Select(row => (row.Key, row.Cells[0].DisplayText, row.Cells[1].DisplayText, row.Metadata["mermaid.data.kind"])));
        var literal = MermaidRenderer.Render("usecase-beta\njson Data @ {\n \"value\": \"%%{init: {theme:'dark'}}%% accTitle: literal\"\n}\nA --> Data");
        Assert.Empty(literal.Diagnostics); var graph = Assert.IsType<TopologyChart>(literal.Artifact!.Model);
        Assert.Contains("accTitle: literal", graph.Nodes.Single(node => node.Id == "Data").TableContent!.Rows[0].Cells[1].DisplayText);
        Assert.Equal("%%{init: {theme:'dark'}}%% accTitle: literal", graph.Nodes.Single(node => node.Id == "Data").TableContent!.Rows[0].Cells[1].DisplayText);
    }
    [Theory]
    [InlineData("json Data @ {\"key\":1,\"key\":2}")]
    [InlineData("json Data @ [1,2]")]
    [InlineData("json Data @ {\"key\":1} trailing")]
    [InlineData("actor Data\njson Data @ {\"key\":1}")]
    [InlineData("Data(Other)\njson Data @ {\"key\":1}")]
    [InlineData("json Data @ {\"key\":1}\nA --|> Data")]
    [InlineData("json Data @ {\"key\":1}\nA ..> : include Data")]
    public void MalformedConflictingOrUnsupportedJsonAssociationsStopAtLocatedSourceBoundary(string body) {
        var source = "usecase-beta\n" + body; var result = MermaidRenderer.Render(source);
        Assert.True(result.HasErrors); Assert.Null(result.Artifact); Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line > 1);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
    }
    [Fact]
    public void JsonInlineClassesAndExistingNamedSelectorsUseTheCanonicalNodeStyleOwner() {
        const string source = "usecase-beta\nclassDef marked fill:#F1F5F9,stroke:#2563EB\njson Data @ {\"zero\":0}:::marked\nstyle Data color:#0F766E\nA --> Data";
        var result = MermaidRenderer.Render(source); Assert.Empty(result.Diagnostics);
        var node = Assert.IsType<TopologyChart>(result.Artifact!.Model).Nodes.Single(item => item.Id == "Data");
        Assert.Equal(("#F1F5F9", "#2563EB", "#0F766E"), (node.BackgroundColor, node.Color, node.LabelColor));
        Assert.Equal("0", node.TableContent!.Rows[0].Cells[1].DisplayText);
    }
    [Theory]
    [InlineData("-->")] [InlineData("<--")] [InlineData("---")] [InlineData("--")]
    public void PointReversedPointAndMarkerlessSolidAssociationsRetainTheOriginalJsonVertex(string token) {
        var result = MermaidRenderer.Render("usecase-beta\nA " + token + " Data\njson Data @ {\"zero\":0}"); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); Assert.Equal(new[] { "A", "Data" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal("0", chart.Nodes[1].TableContent!.Rows[0].Cells[1].DisplayText); Assert.Single(chart.Edges);
    }
    [Fact]
    public void JsonDepthAndFiniteTableRowsCannotBypassExistingCapturedResourcePolicy() {
        var source = "usecase-beta\njson Data @ {\"a\":{\"b\":{\"c\":1}}}";
        var limited = MermaidRenderer.Render(source, new MermaidRenderOptions { ParseOptions = new MermaidParseOptions { ResourceLimits = new MermaidResourceLimits { MaximumNestingDepth = 2 } } });
        Assert.True(limited.HasErrors); Assert.Null(limited.Artifact);
        var fields = string.Join(",", Enumerable.Range(0, 1025).Select(index => "\"p" + index + "\":" + index));
        var rows = MermaidRenderer.Render("usecase-beta\njson Data @ {" + fields + "}"); Assert.True(rows.HasErrors); Assert.Null(rows.Artifact);
    }
}
