using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Raster;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidEventModelingDepthTests {
    internal static string Source {
        get {
            using var stream = typeof(ExampleRecipes).Assembly.GetManifestResourceStream("ChartForgeX.Examples.MermaidFixtures.eventmodeling-native-depth.mmd")
                ?? throw new InvalidOperationException("The maintained Event Modeling source fixture must be embedded.");
            using var reader = new StreamReader(stream); return reader.ReadToEnd();
        }
    }
    internal static MermaidRenderOptions Options(bool dark, int width) => new() {
        EventModeling = new MermaidTopologyRenderOptions { Id = "event-native-depth", Width = width, Height = 700 },
        Presentation = new MermaidPresentationOptions { Theme = VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family),
            ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light }
    };

    [Fact]
    public void FrameNumbersIdentifySourceOrderedStepsAndResetSourcesRemainAvailableInNativeRelations() {
        var parsed = new MermaidParser().ParseEventModeling(Source); Assert.False(parsed.HasErrors); Assert.Empty(parsed.Diagnostics);
        var document = parsed.Document!; Assert.Equal(new[] { "30", "10", "20", "05", "06", "04", "08", "01", "02" }, document.Frames.Select(frame => frame.Number));
        Assert.Equal(new[] { "08", "20" }, document.Frames[7].ExplicitSources);
        Assert.Equal(new[] { ("30", "10"), ("10", "20"), ("05", "06"), ("06", "04"), ("04", "08"), ("20", "01"), ("08", "01"), ("01", "02") },
            document.Relations.Select(relation => (relation.SourceNumber, relation.TargetNumber)));
        Assert.DoesNotContain(document.Relations, relation => relation.TargetNumber == "05");
        var chart = document.ToTopologyChart(); Assert.Equal(TopologyLayoutMode.Swimlane, chart.LayoutMode);
        Assert.Equal(6, chart.Groups.Count); Assert.Equal(9, chart.Nodes.Count); Assert.Equal(8, chart.Edges.Count);
        Assert.All(chart.Nodes, node => { Assert.Equal(TopologyNodeKind.Generic, node.Kind); Assert.Null(node.IconId); Assert.Null(node.Symbol); });
        Assert.Equal("reset", chart.Nodes.Single(node => node.Id == "05").Badge);
        Assert.Equal("eventmodeling-edge-05-06", chart.Edges.Single(edge => edge.SourceNodeId == "05" && edge.TargetNodeId == "06").Id);
        Assert.Equal(Source, document.SourceText); Assert.Equal(Source, document.ToVisualArtifact().Metadata["mermaid.source"]);
    }

    [Theory]
    [InlineData(false, 390)] [InlineData(true, 390)]
    [InlineData(false, 1200)] [InlineData(true, 1200)]
    public void MaintainedTimelineHasCompleteTypedDataAndOrderedDisjointLanesInDirectFenceSvgPngAndDetachedPortableFacts(bool dark, int width) {
        var options = Options(dark, width); var direct = MermaidRenderer.Render(Source, options); Assert.False(direct.HasErrors); Assert.Empty(direct.Diagnostics);
        var artifact = direct.Artifact!; var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var svg = artifact.ToSvg(); var png = artifact.ToPng(); var image = RasterImageDecoder.Decode(png);
        Assert.True(image.Width >= width && image.Height >= 700); var xml = XDocument.Parse(svg);
        var text = string.Join("\n", xml.Descendants().Where(node => node.Name.LocalName == "text").Select(node => node.Value));
        foreach (var expected in new[] { "Cart information flow", "Screen", "reset", "/sku", "String: ABC", "Number: 1", "Alex Morgan", "/tags/1", "review", "/escaped~1key/~0value", "Boolean: true", "Null: null" })
            Assert.Contains(expected, text, StringComparison.Ordinal);
        Assert.DoesNotContain(xml.Descendants(), node => node.Name.LocalName is "script" or "foreignObject" or "image");
        var fence = new MermaidVisualMarkupParser(options).Parse("Before\n\n```mermaid\n" + Source + "\n```\nAfter");
        Assert.False(fence.HasErrors); Assert.Equal(svg, Assert.Single(fence.Artifacts).ToSvg()); Assert.Equal(png, fence.Artifacts[0].ToPng());
        var saved = artifact.ToInterchangeEnvelope(); Assert.Equal(VisualArtifactInterchangeFamily.Topology, saved.Family);
        Assert.Equal("Cart timeline with authored data facts", saved.AccessibleName); Assert.Equal(Source, saved.Extensions["mermaid.source"]);
        var sourceOrder = saved.Nodes.OrderBy(node => int.Parse(node.Extensions["mermaid.order"])).ToArray();
        Assert.True(sourceOrder.Zip(sourceOrder.Skip(1), (left, right) => left.X!.Value + left.Width!.Value <= right.X!.Value).All(value => value));
        foreach (var node in saved.Nodes) {
            var group = saved.Groups.Single(item => item.Id == node.GroupId);
            Assert.True(node.X!.Value >= group.X!.Value && node.Y!.Value >= group.Y!.Value && node.X.Value + node.Width!.Value <= group.X.Value + group.Width!.Value && node.Y.Value + node.Height!.Value <= group.Y.Value + group.Height!.Value);
        }
        var cartStart = saved.Nodes.Single(node => node.Id == "30"); var cartReturn = saved.Nodes.Single(node => node.Id == "02");
        Assert.Equal(cartStart.GroupId, cartReturn.GroupId); Assert.Equal(cartStart.Y, cartReturn.Y);
        Assert.Equal(9, saved.Nodes.Single(node => node.Id == "20").Details.Count);
        var payload = Assert.IsType<MermaidEventModelingDocument>(direct.Document).DataBlocks.Single(block => block.Id == "Payload");
        Assert.Equal(payload.Content, saved.Nodes.Single(node => node.Id == "20").Extensions["mermaid.data.reference.source"]);
        Assert.Equal("json", saved.Nodes.Single(node => node.Id == "20").Extensions["mermaid.data.reference.type"]);
        Assert.Equal(payload.Span.Line.ToString(), saved.Nodes.Single(node => node.Id == "20").Extensions["mermaid.data.reference.source.line"]);
        Assert.Contains(saved.Nodes.Single(node => node.Id == "20").Details, detail => detail.Extensions["mermaid.data.path"] == "/tags/1" && detail.Extensions["mermaid.data.kind"] == "String");
        var json = saved.ToJson(); var restored = VisualArtifactInterchangeEnvelope.FromUtf8Json(saved.ToUtf8Json());
        Assert.Equal(json, restored.ToJson()); restored.Nodes.Single(node => node.Id == "20").Details[0].Value = "String: EDITED";
        Assert.Contains("EDITED", restored.ToJson()); Assert.Equal(json, saved.ToJson()); Assert.DoesNotContain("EDITED", artifact.ToSvg());
        var edited = ((MermaidEventModelingDocument)direct.Document!).ToTopologyChart(options.EventModeling);
        edited.Nodes.Single(node => node.Id == "20").Details[0].Value = "String: EDITED";
        Assert.Contains("EDITED", edited.ToSvg()); Assert.NotEmpty(edited.ToPng()); Assert.DoesNotContain("EDITED", artifact.ToSvg());
        Assert.DoesNotContain("<script", artifact.ToHtmlPage(new VisualArtifactRenderOptions { HtmlSizing = VisualArtifactHtmlSizing.PreserveSize }), StringComparison.OrdinalIgnoreCase);
        Capture(artifact, "event-native-" + width + (dark ? "-dark" : "-light"));
    }

    [Theory]
    [InlineData("eventmodeling\ntf 1 ui Screen ->> 2 ->> 2\ntf 2 rmo View")]
    [InlineData("eventmodeling\ntf 1 ui Screen ->> 1")]
    [InlineData("eventmodeling\ntf 1 ui Screen [[Missing]]")]
    [InlineData("eventmodeling\ntf 1 ui Screen\ndata Duplicate {}\ndata Duplicate {}")]
    [InlineData("eventmodeling\ntf 1 ui Screen ->>")]
    [InlineData("eventmodeling\ntf 1 ui Screen ->> 2\ntf 2 evt Happened")]
    [InlineData("eventmodeling\ntf not-a-number ui Screen")]
    [InlineData("eventmodeling\ntf 1 ui Broken..Name")]
    [InlineData("eventmodeling\ntf 1 ui Screen `json`{\"x\":1,\"x\":2}")]
    [InlineData("eventmodeling\ntf 1 ui Screen { a: 1 } { b: 2 }")]
    public void MalformedSourceFailsWithLocationsAndNeverReturnsAPartialArtifact(string source) {
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line >= 2 && diagnostic.Span.Column > 0 && diagnostic.Span.Length > 0);
    }

    [Fact]
    public void LegacyListAndSuffixOrderStayExplicitlyDiagnosedWhileUnknownStatementFormsRemainInert() {
        const string source = "eventmodeling\nrf 1 evt First\nrf 2 evt Second\ntf 3 rmo View `json`{\"field\":\"[[literal]]\"} ->> 1, 2 [[Payload]]\ndata Payload { value: string }\nnote 3 { raw note }";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors); Assert.NotNull(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.ApproximateMapping);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == MermaidDiagnosticCodes.UnsupportedStatement);
        var strict = MermaidRenderer.Render(source, new MermaidRenderOptions { WarningPolicy = MermaidWarningPolicy.Reject }); Assert.True(strict.HasErrors); Assert.Null(strict.Artifact);
        var document = Assert.IsType<MermaidEventModelingDocument>(result.Document); var table = document.ToDataTable("3");
        Assert.Contains(table.Rows, row => row.Key == "/inline/field" && row.Cells[2].DisplayText == "[[literal]]");
        Assert.Contains(table.Rows, row => row.Key == "/data/Payload/value" && row.Cells[2].DisplayText == "string");
    }

    [Fact]
    public void OpaqueBodiesKeepDirectiveAccessibilityAndCommentTextWhileOuterDeclarationsStayActive() {
        const string body = "\naccTitle: Data title\naccDescr: Data description\n%%{init: {\"theme\":\"dark\"}}%%\n";
        var source = "eventmodeling\naccTitle: Outer title\naccDescr: Outer description\ntf 1 ui Screen `text`{literal %% retained} %% ordinary comment\ndata Payload `custom{format`{" + body + "}";
        var result = MermaidRenderer.Render(source); Assert.False(result.HasErrors); var document = Assert.IsType<MermaidEventModelingDocument>(result.Document);
        Assert.Equal("Outer title", document.Accessibility.Name); Assert.Equal("Outer description", document.Accessibility.Description);
        Assert.Null(document.Configuration.Theme); Assert.Equal(body, Assert.Single(document.DataBlocks).Content);
        Assert.Equal("literal %% retained", document.ToDataTable("1").Rows[0].Cells[2].DisplayText);
        Assert.Equal(body, Assert.Single(document.DataBlocks[0].ToDataTable().Rows).Cells[2].DisplayText);
    }

    [Fact]
    public void DerivedPortableRelationIdsRejectAtTheirSourceInsteadOfFailingOnlyDuringSave() {
        var source = "eventmodeling\ntf " + new string('1', 250) + " ui Screen\ntf " + new string('2', 250) + " cmd Save";
        var result = MermaidRenderer.Render(source); Assert.True(result.HasErrors); Assert.Null(result.Artifact);
        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Severity == MermaidDiagnosticSeverity.Error && diagnostic.Span.Line == 3 && diagnostic.Message.Contains("512-character"));
    }

    internal static void Capture(VisualArtifact artifact, string stem) {
        var directory = Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, stem + ".svg"), artifact.ToSvg());
        File.WriteAllBytes(Path.Combine(directory, stem + ".native.png"), artifact.ToPng()); File.WriteAllText(Path.Combine(directory, stem + ".json"), artifact.ToInterchangeJson());
    }
}
