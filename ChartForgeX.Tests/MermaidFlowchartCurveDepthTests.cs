using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidFlowchartCurveDepthTests {
    public static IEnumerable<object[]> Kinds() {
        var names = new[] { "linear", "basis", "bumpX", "bumpY", "cardinal", "catmullRom", "monotoneX", "monotoneY", "natural", "step", "stepAfter", "stepBefore", "rounded" };
        var kinds = new[] { TopologyEdgeInterpolation.Linear, TopologyEdgeInterpolation.Basis, TopologyEdgeInterpolation.BumpX, TopologyEdgeInterpolation.BumpY,
            TopologyEdgeInterpolation.Cardinal, TopologyEdgeInterpolation.CatmullRom, TopologyEdgeInterpolation.MonotoneX, TopologyEdgeInterpolation.MonotoneY,
            TopologyEdgeInterpolation.Natural, TopologyEdgeInterpolation.Step, TopologyEdgeInterpolation.StepAfter, TopologyEdgeInterpolation.StepBefore, TopologyEdgeInterpolation.Rounded };
        for (var i = 0; i < names.Length; i++) foreach (var lane in new[] { false, true }) yield return new object[] { names[i], kinds[i], lane };
    }
    [Theory] [MemberData(nameof(Kinds))]
    public void SourceNamesSelectFiniteNativeGeometryThroughDirectFenceAndCommonJson(string name, TopologyEdgeInterpolation kind, bool lane) {
        var source = "---\nconfig:\n  flowchart:\n    curve: " + name + "\n---\n" + Body(lane); var result = Observe(name + "-" + lane, source);
        var artifact = result.Artifact ?? throw new InvalidOperationException("A supported source curve must produce its native artifact.");
        var fence = new MermaidVisualMarkupParser(Options()).Parse("```mermaid\n" + source + "\n```"); var fenced = fence.Artifacts.SingleOrDefault();
        if (fenced != null) Capture(name + "-" + lane + "-fenced", fenced);
        var json = artifact.ToInterchangeJson(); var envelope = VisualArtifactInterchangeEnvelope.FromJson(json);
        var restoredChart = envelope.ToTopologyChart(); var restored = restoredChart.ToVisualArtifact(); Capture(name + "-" + lane + "-restored", restored);
        var expected = lane && kind == TopologyEdgeInterpolation.Basis ? TopologyEdgeInterpolation.Rounded : kind;
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics); Assert.False(fence.HasErrors); Assert.NotNull(fenced);
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        Assert.All(chart.Edges, edge => { Assert.Equal(expected, edge.Interpolation); Assert.Equal(expected == TopologyEdgeInterpolation.Rounded ? (double?)null : 5, edge.InterpolationCornerBlendLength); });
        Assert.Equal(16, chart.Theme.Typography.DataLabelSize); Assert.Equal(2, envelope.Version);
        Assert.Equal(artifact.ToSvg(), fenced!.ToSvg()); Assert.Equal(artifact.ToPng(), fenced.ToPng());
        Assert.Equal(EdgeInk(artifact.ToSvg()), EdgeInk(restored.ToSvg())); Assert.NotEmpty(restored.ToPng());
        Assert.DoesNotContain("<script", artifact.ToHtmlPage(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("", TopologyEdgeInterpolation.StepBefore)]
    [InlineData("linkStyle 0 interpolate natural", TopologyEdgeInterpolation.Natural)]
    [InlineData("linkStyle 0 interpolate natural\ne@{curve:bumpY}", TopologyEdgeInterpolation.BumpY)]
    [InlineData("e@{curve:bumpY}\nlinkStyle 0 interpolate natural", TopologyEdgeInterpolation.Natural)]
    public void CreationTimeDefaultAndLaterSpecificStatementsKeepTheirActualSourceOrder(string final, TopologyEdgeInterpolation first) {
        var source = "---\nconfig:\n  flowchart:\n    curve: linear\n---\nflowchart LR\nlinkStyle default interpolate stepBefore\nA e@--> B\nlinkStyle default interpolate bumpX\nB f@--> C\nlinkStyle default interpolate cardinal\nC g@--> D\n" + final;
        var result = Observe("order-" + first + "-" + final.Length, source); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Empty(result.Diagnostics); Assert.Equal(new[] { "A", "B", "C", "D" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal(new[] { first, TopologyEdgeInterpolation.BumpX, TopologyEdgeInterpolation.Cardinal }, chart.Edges.Select(edge => edge.Interpolation!.Value));
        Assert.Contains(Assert.IsType<MermaidFlowchartDocument>(result.Document).Statements, statement => statement.Text.Contains("interpolate", StringComparison.Ordinal));
    }

    [Fact]
    public void LaterDefaultFallbackDoesNotOverwriteAValueAlreadyInheritedAtCreation() {
        const string source = "flowchart LR\nA e@--> B\nlinkStyle default interpolate step\nB f@--> C\nlinkStyle default interpolate natural\nlinkStyle 0 stroke:#123456";
        var result = Observe("late-default", source); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Empty(result.Diagnostics); Assert.Equal(TopologyEdgeInterpolation.Natural, chart.Edges[0].Interpolation);
        Assert.Equal(TopologyEdgeInterpolation.Step, chart.Edges[1].Interpolation); Assert.Equal("#123456", chart.Edges[0].Color);
        Assert.Equal("default", chart.Edges[0].Metadata["mermaid.curve.origin"]); Assert.Equal("creation-default", chart.Edges[1].Metadata["mermaid.curve.origin"]);
    }

    [Fact]
    public void ExistingEdgeMetadataKeepsItsRawScalarSpansAndUnknownAnimationInert() {
        const string source = "flowchart LR\nA e@--> B\ne@{curve:'basis', animate:true, animation:'fast'}";
        var result = Observe("edge-metadata", source); var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        var edge = Assert.Single(document.Edges); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.False(result.HasErrors); Assert.Equal(new[] { "A", "B" }, chart.Nodes.Select(node => node.Id));
        Assert.Equal("basis", edge.Properties["curve"]); Assert.Equal("'basis'", edge.PropertyTokens["curve"]);
        Assert.Equal(3, edge.PropertySpans["curve"].Line); Assert.Equal(4, edge.PropertySpans["curve"].Column);
        Assert.All(result.Diagnostics, item => Assert.Equal(MermaidDiagnosticSeverity.Warning, item.Severity)); Assert.Equal(2, result.Diagnostics.Count);
        Assert.Contains(result.Diagnostics, item => item.Span.Equals(edge.PropertySpans["animate"])); Assert.Contains(result.Diagnostics, item => item.Span.Equals(edge.PropertySpans["animation"]));
        Assert.Equal("true", chart.Edges[0].Metadata["mermaid.property.animate"]);
        Assert.Null(MermaidRenderer.Render(source, Options(reject: true)).Artifact);
        Assert.DoesNotContain("<script", result.Artifact!.ToHtmlPage(), StringComparison.OrdinalIgnoreCase);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void UnknownCurveKeepsItsLocatedDiagnosticAndPinnedFallback(bool lane) {
        var source = "---\nconfig:\n  flowchart:\n    curve: not-a-curve\n---\n" + Body(lane); var result = Observe("unknown-" + lane, source);
        Assert.False(result.HasErrors); Assert.Contains(result.Diagnostics, item => item.Code == MermaidDiagnosticCodes.UnsupportedConfiguration && item.Span.Line == 4);
        Assert.All(Assert.IsType<TopologyChart>(result.Artifact!.Model).Edges, edge => Assert.Equal(TopologyEdgeInterpolation.Basis, edge.Interpolation));
        Assert.Null(MermaidRenderer.Render(source, Options(reject: true)).Artifact);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public void UnconfiguredFamilyDefaultsStaySeparateFromNativeAndHostTypographyDefaults(bool lane) {
        var result = Observe("implicit-" + lane, Body(lane)); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Empty(result.Diagnostics); Assert.All(chart.Edges, edge => Assert.Equal(lane ? TopologyEdgeInterpolation.Rounded : TopologyEdgeInterpolation.Basis, edge.Interpolation));
        var host = Options(); var presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, FontSize = 14, TopologySizing = MermaidTopologySizing.Natural };
        if (lane) host.Swimlane!.Presentation = presentation; else host.Flowchart!.Presentation = presentation;
        var explicitResult = MermaidRenderer.Render(Body(lane), host); Capture("implicit-" + lane + "-host14", explicitResult.Artifact!);
        Assert.Equal(14, Assert.IsType<TopologyChart>(explicitResult.Artifact!.Model).Theme.Typography.DataLabelSize);
        var native = TopologyChart.Create(); Assert.Equal(12, native.Theme.Typography.DataLabelSize);
    }

    [Fact]
    public void SharedPropertyLexerPreservesOrdinaryQuotedNodeMetadata() {
        const string source = "flowchart LR\nA@{shape:rect,label:'a,b'} --> B@{shape:circle,label:'quoted value'}";
        var result = Observe("ordinary-node-properties", source); var document = Assert.IsType<MermaidFlowchartDocument>(result.Document);
        Assert.Empty(result.Diagnostics); Assert.Equal("a,b", document.Nodes[0].Text); Assert.Equal("a,b", document.Nodes[0].Properties["label"]);
        Assert.Equal("quoted value", document.Nodes[1].Text); Assert.Equal(MermaidFlowchartNodeShape.Circle, document.Nodes[1].Shape);
    }

    [Fact]
    public void ExistingAgentflowFamilyKeepsItsIndependentNativeRoutePolicy() {
        var source = File.ReadAllText(Path.Combine(TestRepository.Root, "tests", "mermaid-conformance", "fixtures", "agentflow-native-workflow.mmd"));
        var result = Observe("agentflow-independent", source); var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        Assert.Empty(result.Diagnostics); Assert.All(chart.Edges, edge => { Assert.Null(edge.Interpolation); Assert.Null(edge.InterpolationCornerBlendLength); });
    }

    private static string Body(bool lane) => lane ? "swimlane-beta LR\nsubgraph Source\nA[Start]\nB[Upper]\nend\nsubgraph Target\nC[Lower]\nD[Finish]\nend\nA --> B\nA --> C\nB --> D\nC --> D"
        : "flowchart LR\nA[Start] --> B[Upper]\nA --> C[Lower]\nB --> D[Finish]\nC --> D";
    private static MermaidRenderOptions Options(bool reject = false) => new() { WarningPolicy = reject ? MermaidWarningPolicy.Reject : MermaidWarningPolicy.Allow,
        Flowchart = FamilyOptions(), Swimlane = FamilyOptions() };
    private static MermaidFlowchartRenderOptions FamilyOptions() => new() { Width = 960, Height = 640, FitContent = false, Title = "",
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family, TopologySizing = MermaidTopologySizing.Natural } };
    private static MermaidRenderResult Observe(string name, string source) {
        Write(name + ".input.mmd", source); var result = MermaidRenderer.Render(source, Options());
        Write(name + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
        if (result.Artifact != null) Capture(name, result.Artifact); return result;
    }
    private static string[] EdgeInk(string svg) => XDocument.Parse(svg).Descendants().Where(element => (string?)element.Attribute("data-cfx-role") is "topology-edge-line" or "topology-marker")
        .Select(element => string.Join("|", element.Attributes().Where(attribute => attribute.Name.LocalName is "d" or "stroke" or "fill" or "stroke-width" or "stroke-dasharray").Select(attribute => attribute.Name.LocalName + "=" + attribute.Value))).ToArray();
    private static void Capture(string name, VisualArtifact artifact) {
        Write(name + ".svg", artifact.ToSvg()); Write(name + ".json", artifact.ToInterchangeJson()); Write(name + ".html", artifact.ToHtmlPage());
        var output = Output(); if (output != null) File.WriteAllBytes(Path.Combine(output, "source-curve-depth-" + name + ".native.png"), artifact.ToPng());
    }
    private static void Write(string name, string value) { var output = Output(); if (output != null) File.WriteAllText(Path.Combine(output, "source-curve-depth-" + name), value); }
    private static string? Output() { var output = Environment.GetEnvironmentVariable("CFX_CURVE_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return null; Directory.CreateDirectory(output); return output; }
}
