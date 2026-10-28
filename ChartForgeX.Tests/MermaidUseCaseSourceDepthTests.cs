using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidUseCaseSourceDepthTests {
    internal static MermaidRenderOptions Options(int width, bool dark) => new() {
        WarningPolicy = MermaidWarningPolicy.Reject,
        UseCase = new MermaidFlowchartRenderOptions { Width = width, Height = 620,
            Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family,
                ThemeMode = dark ? ChartForgeX.Themes.VisualThemeMode.Dark : ChartForgeX.Themes.VisualThemeMode.Light,
                TopologySizing = MermaidTopologySizing.FixedViewport } }
    };

    [Theory]
    [InlineData(390, false)] [InlineData(390, true)] [InlineData(900, false)] [InlineData(900, true)]
    public void NotesStereotypesNamedStylesAndSourcePolicyReachTheSameNativeAndFenceScene(int width, bool dark) {
        var source = UseCaseSourceDepthFixture.Read(width == 390); var options = Options(width, dark);
        var direct = MermaidRenderer.Render(source, options); Assert.Empty(direct.Diagnostics);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```"); Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(direct.Artifact); var other = Assert.Single(fenced.Artifacts);
        Assert.Equal(artifact.ToSvg(), other.ToSvg()); Assert.Equal(artifact.ToPng(), other.ToPng());
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.Equal(new[] { "Operator", "Publish", "Validate" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal("Release coordinator", chart.Nodes[0].Label); Assert.Equal("«primary»", chart.Nodes[0].Subtitle);
        Assert.Equal("«business action»", chart.Nodes[1].Subtitle);
        Assert.Equal("request", chart.Edges[0].Id); Assert.Equal("mermaid-edge-1", chart.Edges[1].Id);
        Assert.Equal("#2563EB", chart.Edges[0].Color); Assert.Equal("#0F766E", chart.Edges[0].LabelColor);
        Assert.Equal(2.5, chart.Edges[0].StrokeWidth); Assert.Equal(.8, chart.Edges[0].Opacity);
        Assert.Equal(new[] { 6d, 3, 2, 6, 3, 2 }, chart.Edges[0].DashPattern);
        Assert.Equal("Publish", Assert.Single(chart.Annotations).TargetId);
        var prepared = chart.Prepare(); AssertPainted(prepared);
        var envelope = VisualArtifactInterchangeEnvelope.FromJson(artifact.ToInterchangeJson());
        Assert.Equal(source, envelope.Extensions["mermaid.source"]);
        Assert.Equal("primary", envelope.Nodes.Single(node => node.Id == "Operator").Extensions["mermaid.stereotype"]);
        Assert.Single(envelope.Annotations);
        Assert.Equal("request", envelope.Edges[0].Id);
        Capture(artifact, "usecase-source-" + width + (dark ? "-dark" : "-light"));
    }

    [Fact]
    public void RawStatementsStereotypesNotesAndAuthoredEdgeIdentityKeepActualPhysicalSpans() {
        var source = UseCaseSourceDepthFixture.Read(); var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidUseCaseDocument>(parsed.Document); Assert.Equal(source, document.SourceText);
        var lines = source.Split('\n');
        foreach (var statement in document.Statements) Assert.Equal(statement.Text, lines[statement.Span.Line - 1].Substring(statement.Span.Column - 1, statement.Span.Length));
        var actor = document.Nodes.Single(node => node.Id == "Operator");
        Assert.Equal("<<primary>>", actor.Properties["usecase.stereotype.raw"]);
        var native = document.ToTopologyChart();
        Assert.Equal("<<primary>>", SourceToken(native.Nodes[0].Metadata, "mermaid.stereotype"));
        Assert.Equal("request", SourceToken(native.Edges[0].Metadata, "mermaid.id"));
        string SourceToken(Dictionary<string, string> metadata, string prefix) =>
            lines[int.Parse(metadata[prefix + ".line"], CultureInfo.InvariantCulture) - 1].Substring(
                int.Parse(metadata[prefix + ".column"], CultureInfo.InvariantCulture) - 1,
                int.Parse(metadata[prefix + ".length"], CultureInfo.InvariantCulture));
        var note = Assert.Single(document.Notes);
        Assert.StartsWith("note for Publish", lines[note.Span.Line - 1].Substring(note.Span.Column - 1, note.Span.Length));
        Assert.DoesNotContain(document.Nodes, node => node.Id is "request" or "note");
    }

    [Theory]
    [InlineData("A check@..>:include B", "check", "<<include>>")]
    [InlineData("A check@..> : extend B", "check", "<<extend>>")]
    [InlineData("A parent@ --|> B", "parent", "")]
    public void NamedSemanticRelationsRetainEndpointColumnsAndDoNotRewriteCaptionLiterals(string relation, string id, string label) {
        var source = "usecase-beta\n" + relation;
        var parsed = new MermaidParser().ParseFlowchart(source); Assert.Empty(parsed.Diagnostics);
        var document = Assert.IsType<MermaidUseCaseDocument>(parsed.Document); var edge = Assert.Single(document.Edges);
        Assert.Equal(id, edge.Id); Assert.Equal(label, edge.Label ?? "");
        var target = document.Nodes.Single(node => node.Id == "B"); Assert.Equal(relation.LastIndexOf('B') + 1, target.Span.Column);
        var literal = MermaidRenderer.Render("usecase-beta\nA(\"Literal @ --|> caption\")\nA --> B"); Assert.Empty(literal.Diagnostics);
        Assert.Equal("Literal @ --|> caption", Assert.IsType<TopologyChart>(literal.Artifact!.Model).Nodes[0].Label);
    }

    [Theory]
    [InlineData("A x@--> B\nC x@--> D")]
    [InlineData("A A@--> B")]
    [InlineData("A x@--> B\nstyle missing stroke:red")]
    [InlineData("A --> B\nclass mermaid-edge-0 named")]
    [InlineData("A --> B\nnote for missing \"caption\"")]
    public void UnknownOrCollidingSourceReferencesAreLocatedErrorsWithoutFakeNodesOrArtifacts(string body) {
        var source = "usecase-beta\n" + body; var direct = MermaidRenderer.Render(source);
        Assert.True(direct.HasErrors); Assert.Null(direct.Artifact);
        Assert.Contains(direct.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line > 1);
        var fence = new MermaidVisualMarkupParser().Parse("```mermaid\n" + source + "\n```"); Assert.True(fence.HasErrors); Assert.Empty(fence.Artifacts);
        var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.DoesNotContain(parsed.Document!.Nodes, node => node.Id is "missing" or "mermaid-edge-0");
    }

    [Theory]
    [InlineData("A --> B\nstyle A filter:blur(4px)", "filter")]
    [InlineData("A x@--> B\nstyle x stroke-dasharray:0 4", "stroke-dasharray")]
    public void UnloweredPaintRetainsFactsAndCannotBypassStrictPolicy(string body, string fact) {
        var source = "usecase-beta\n" + body; var parsed = new MermaidParser().ParseFlowchart(source);
        Assert.False(parsed.HasErrors); Assert.Contains(parsed.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Warning);
        Assert.Contains(fact, parsed.Document!.SourceText);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
    }

    [Theory]
    [InlineData("actorFontSize: -1")]
    [InlineData("actorFontFamily: 'Arial;fill:red'")]
    [InlineData("actorFontWeight: 'url(x)'")]
    [InlineData("wrappingWidth: 0")]
    public void InvalidSourcePolicyStopsBeforeNativeConversion(string setting) {
        var source = "---\nconfig:\n  usecase:\n    " + setting + "\n---\nusecase-beta\nactor A";
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line == 4 && diagnostic.Severity == MermaidDiagnosticSeverity.Error);
    }

    [Fact]
    public void JsonVerticesUseTheCanonicalLinkedNativeTableContentOwner() {
        var result = MermaidRenderer.Render("usecase-beta\njson Details @{\"version\":1}\nactor User\nUser --> Details");
        Assert.Empty(result.Diagnostics);
        var graph = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal("1", graph.Nodes.Single(node => node.Id == "Details").TableContent!.Rows[0].Cells[1].DisplayText);
        Assert.Equal("Details", graph.Edges[0].TargetNodeId);
    }

    [Fact]
    public void NativeCaptureIsDetachedFromTypedSourceOwnersAndExistingDefaultActorsStayExact() {
        var document = Assert.IsType<MermaidUseCaseDocument>(new MermaidParser().ParseFlowchart(UseCaseSourceDepthFixture.Read()).Document);
        var chart = document.ToTopologyChart(Options(900, false).UseCase); var prepared = chart.Prepare();
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        document.Notes.Clear(); document.Nodes[0].Properties["usecase.stereotype"] = "changed";
        document.Edges[0].Styles["stroke"] = "red"; chart.Nodes[0].Subtitle = "changed"; chart.Edges[0].DashPattern.Clear(); chart.Nodes.Clear(); chart.Edges.Clear();
        Assert.Equal(svg, prepared.ToSvg()); Assert.Equal(png, prepared.ToPng());
        var plain = MermaidRenderer.Render("usecase-beta\nactor User\nUser --> Action(Do work)").Artifact!;
        var defaults = Assert.IsType<TopologyChart>(plain.Model); var before = defaults.Prepare();
        defaults.Nodes[0].Subtitle = ""; var after = defaults.Prepare();
        Assert.Equal(before.ToSvg(), after.ToSvg()); Assert.Equal(before.ToPng(), after.ToPng());
    }

    [Fact]
    public void ExplicitHostTypographyAndPaddingOverrideOnlyTheirCorrespondingSourceDefaults() {
        var source = UseCaseSourceDepthFixture.Read(); var document = new MermaidParser().ParseFlowchart(source).Document!;
        var authored = document.ToTopologyChart(); Assert.Equal(28, authored.Viewport.Padding);
        Assert.Equal(14, authored.Nodes[0].TextStyle!.FontSize); Assert.Equal("500", authored.Nodes[0].TextStyle!.FontWeight);
        var explicitHost = document.ToTopologyChart(new MermaidFlowchartRenderOptions { Padding = 17,
            Presentation = new MermaidPresentationOptions { FontSize = 20, FontFamily = BrowserProofFonts.Family } });
        Assert.Equal(17, explicitHost.Viewport.Padding);
        Assert.All(explicitHost.Nodes, node => { Assert.Equal(20, node.TextStyle!.FontSize); Assert.Equal(BrowserProofFonts.Family, node.TextStyle.FontFamily); });
        Assert.Equal("500", explicitHost.Nodes[0].TextStyle!.FontWeight);
        Assert.Equal("400", explicitHost.Nodes[1].TextStyle!.FontWeight);
    }

    [Fact]
    public void DeferredNamedAssociationSelectorsDoNotCreateNodesAndSharedFlowchartIdsKeepAnonymousDefaults() {
        var result = MermaidRenderer.Render("usecase-beta\nclassDef marked stroke:red\nclass delivery marked\nstyle delivery color:blue\nA delivery@--> B");
        Assert.Empty(result.Diagnostics); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Equal(new[] { "A", "B" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal(("delivery", "red", "blue"), (chart.Edges[0].Id, chart.Edges[0].Color, chart.Edges[0].LabelColor));
        var anonymous = new MermaidParser().ParseFlowchart("flowchart LR\nA[Literal @ caption] --> B").Document!;
        var before = anonymous.ToTopologyChart().Prepare(); anonymous.Edges[0].Id = null;
        Assert.Equal(before.ToSvg(), anonymous.ToTopologyChart().ToSvg()); Assert.Equal(before.ToPng(), anonymous.ToTopologyChart().ToPng());
        var named = new MermaidParser().ParseFlowchart("flowchart LR\nA[Literal @ caption] delivery@--> B"); Assert.Empty(named.Diagnostics);
        var prepared = named.Document!.ToTopologyChart().Prepare(); var svg = prepared.ToSvg();
        Assert.Equal("delivery", Assert.Single(prepared.ToInterchangeEnvelope().Edges).Id);
        named.Document!.Edges[0].Id = "changed"; Assert.Equal(svg, prepared.ToSvg());
    }

    [Fact]
    public void SettingsWithoutNativeOwnersRemainLocatedWarningsAndRejectPolicyStillStopsRendering() {
        const string source = "---\nconfig:\n  usecase:\n    nodeSpacing: 32\n---\nusecase-beta\nactor A";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Span.Line == 4 && diagnostic.Code == MermaidDiagnosticCodes.UnsupportedConfiguration);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject });
        Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
    }

    internal static void AssertPainted(PreparedTopology prepared) {
        var scene = prepared.Visual.Scene;
        var captions = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "inline-text-run" or "topology-node-label" or "topology-node-subtitle").ToArray();
        var all = string.Join(" ", captions.SelectMany(text => text.Text.Lines.Select(line => line.Text)));
        Assert.Contains("primary", all); Assert.Contains("business", all); Assert.Contains("coordinator", all);
        var document = XDocument.Parse(prepared.ToSvg());
        Assert.Contains(document.Descendants(), node => (string?)node.Attribute("data-cfx-role") == "topology-node-subtitle");
        var noteText = string.Join(" ", scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-annotation-text")
            .SelectMany(text => text.Text.Lines.Select(line => line.Text)));
        Assert.Equal("Approval required before delivery", noteText);
        Assert.DoesNotContain(document.Descendants(), node => node.Name.LocalName is "script" or "image" or "foreignObject");
        Assert.NotEmpty(prepared.ToPng());
    }
    internal static void Capture(VisualArtifact artifact, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
        File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".native.png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
