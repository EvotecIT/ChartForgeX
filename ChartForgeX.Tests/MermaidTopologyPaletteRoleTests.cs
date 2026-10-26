using System.Text.Json;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class MermaidTopologyPaletteRoleTests {
    [Theory]
    [InlineData("flowchart", false)] [InlineData("flowchart", true)]
    [InlineData("swimlane", false)] [InlineData("swimlane", true)]
    [InlineData("agentflow", false)] [InlineData("agentflow", true)]
    public void ExplicitSourceRolesReachIndependentNativePaintsAndStylesKeepPrecedence(string family, bool directOverride) {
        var source = Source(family, directOverride); var options = Options(); var result = MermaidRenderer.Render(source, options);
        Capture(family + "-" + directOverride + "-direct", source, result);
        var fenced = new MermaidVisualMarkupParser(options).Parse("```mermaid\n" + source + "\n```");
        if (fenced.Artifacts.Count == 1) CaptureArtifact(family + "-" + directOverride + "-fence", fenced.Artifacts[0]);
        Assert.False(result.HasErrors); Assert.Empty(result.Diagnostics); Assert.False(fenced.HasErrors); Assert.Empty(fenced.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); var chart = Assert.IsType<TopologyChart>(artifact.Model); var work = Assert.Single(chart.Nodes, node => node.Id == "work"); var group = Assert.Single(chart.Groups);
        var fill = directOverride ? "#102030" : "#eeeeff"; var border = directOverride ? "#203040" : "#123456"; var label = directOverride ? "#304050" : "#234567";
        Assert.Equal(ChartColor.FromHex(fill), ChartColor.Parse(work.BackgroundColor!)); Assert.Equal(ChartColor.FromHex(border), ChartColor.Parse(work.BorderColor!)); Assert.Equal(ChartColor.FromHex(label), ChartColor.Parse(work.LabelColor!));
        Assert.Equal(directOverride ? ChartColor.FromHex("#102030") : family == "agentflow" ? ChartColor.Transparent : ChartColor.FromHex("#ffeddd"), ChartColor.Parse(group.BackgroundColor!));
        Assert.Equal(ChartColor.FromHex(directOverride ? "#203040" : "#876543"), ChartColor.Parse(group.BorderColor!));
        Assert.Equal(ChartColor.FromHex(directOverride ? "#304050" : "#567890"), ChartColor.Parse(group.LabelColor!));
        Assert.Equal(ChartColor.FromRgba(204, 221, 238, 128), ChartColor.Parse(Assert.Single(chart.Edges).LabelBackgroundColor!));
        var setting = Assert.Single(result.Document!.Configuration.Settings, item => item.Path == "themeVariables.nodeBorder");
        Assert.True(setting.IsString); Assert.Equal("#123456", setting.Value); Assert.Equal(1, setting.Span.Line); Assert.True(setting.Span.Column > 0); Assert.True(setting.Span.Length > 0);
        var fence = Assert.Single(fenced.Artifacts); var facts = artifact.ToInterchangeEnvelope();
        var restored = VisualArtifactInterchangeEnvelope.FromJson(facts.ToJson()).ToTopologyChart().Prepare(); CaptureArtifact(family + "-" + directOverride + "-restored", restored.Visual.ToArtifact());
        Assert.Equal(artifact.ToSvg(), fence.ToSvg()); Assert.Equal(artifact.ToPng(), fence.ToPng()); Assert.Equal(artifact.ToPng(), restored.ToPng());
        Assert.Equal(source, result.Document!.SourceText); Assert.Null(work.Opacity); Assert.Null(work.SurfaceOpacity);
    }
    [Theory]
    [InlineData("f-circ")] [InlineData("fr-circ")] [InlineData("fork")]
    public void SourceAccentFillAndContourStrokeUseIndependentNativeInk(string shape) {
        var source = "flowchart LR\nwork@{shape:" + shape + ",label:'Work'}\nstyle work fill:#ff0000,stroke:#0000ff";
        var result = MermaidRenderer.Render(source, Options()); Capture("accent-fill-" + shape, source, result); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var prepared = chart.Prepare();
        var marks = prepared.Visual.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "topology-node-surface").ToArray();
        Assert.NotEmpty(marks); Assert.All(marks, mark => { Assert.Equal(ChartColor.FromHex("#ff0000"), mark.Fill); Assert.Equal(ChartColor.FromHex("#0000ff"), mark.Stroke); });
        var restored = prepared.ToInterchangeEnvelope().ToTopologyChart().Prepare(); CaptureArtifact("accent-fill-" + shape + "-restored", restored.Visual.ToArtifact());
        Assert.Equal(prepared.ToPng(), restored.ToPng());
    }
    [Fact]
    public void UnframedSourceIconKeepsOnlyItsGlyphAndActualCaptionInk() {
        const string source = "flowchart LR\nwork@{icon:'common:server',label:'Server'}\nstyle work fill:#ff0000,stroke:#0000ff";
        var result = MermaidRenderer.Render(source, Options()); Capture("unframed-icon", source, result); Assert.Empty(result.Diagnostics);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); var node = Assert.Single(Assert.IsType<TopologyChart>(artifact.Model).Nodes);
        Assert.Equal(0, node.SurfaceOpacity); Assert.Equal("#0000ff", node.Color); Assert.True(node.ShowLabel); Assert.Equal("Server", node.Label);
        Assert.Equal("none", node.BackgroundColor); Assert.Equal("none", node.BorderColor);
        var restored = artifact.ToInterchangeEnvelope().ToTopologyChart().Prepare(); CaptureArtifact("unframed-icon-restored", restored.Visual.ToArtifact()); Assert.Equal(artifact.ToPng(), restored.ToPng());
    }
    [Theory]
    [InlineData("fill:#ff0000", true)]
    [InlineData("stroke:#0000ff", false)]
    [InlineData("fill:#ff0000,stroke:#0000ff,stroke-width:3", true)]
    public void TextSourceRevealsItsLogicalRectangleOnlyForActuallyVisibleAuthoredPaint(string styles, bool visible) {
        var source = "flowchart LR\nwork@{shape:text,label:'Work'}\nstyle work " + styles; var result = MermaidRenderer.Render(source, Options()); Capture("text-paint-" + styles.Length, source, result); Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model); var node = Assert.Single(chart.Nodes); var prepared = chart.Prepare();
        var marks = prepared.Visual.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "topology-node-surface").ToArray();
        Assert.Equal(visible ? 1 : 0, marks.Length); Assert.Equal("TextBlock", node.Metadata["mermaid.shape"]);
        Assert.Equal(visible ? TopologyNodeShape.Rectangle : TopologyNodeShape.TextBlock, node.Shape);
        var restored = prepared.ToInterchangeEnvelope().ToTopologyChart().Prepare(); CaptureArtifact("text-paint-" + styles.Length + "-restored", restored.Visual.ToArtifact()); Assert.Equal(prepared.ToPng(), restored.ToPng());
    }
    [Fact]
    public void NativeGraphiteAndOrdinarySourceHaveSeparateUnmodifiedControls() {
        var chart = TopologyChart.Create().WithId("palette-native-control").WithTitle("").WithViewport(300, 250, 24).WithLegend(null).AddNode("node", "Work", 60, 60, width: 140, height: 70);
        var artifact = chart.ToVisualArtifact(); CaptureArtifact("ordinary-native-graphite", artifact);
        Assert.Equal(ChartForgeX.Themes.VisualTheme.Graphite().ToJson(), chart.Theme.ToJson());
        Assert.Null(chart.Nodes[0].BorderColor); Assert.Null(chart.Nodes[0].BackgroundColor); Assert.Null(chart.Nodes[0].LabelColor);
        const string source = "flowchart LR\na[Plan] --> b[Done]"; var result = MermaidRenderer.Render(source, Options()); Capture("ordinary-source", source, result); Assert.Empty(result.Diagnostics);
    }
    internal static MermaidRenderOptions Options() => new() {
        Flowchart = new MermaidFlowchartRenderOptions { Width = 640, Height = 440 },
        Swimlane = new MermaidFlowchartRenderOptions { Width = 640, Height = 440 },
        Agentflow = new MermaidTopologyRenderOptions { Width = 640, Height = 440 },
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
    };
    internal static string Source(string family, bool directOverride, bool explicitRoles = true) {
        const string init = "%%{init: {\"theme\":\"default\",\"themeVariables\":{\"mainBkg\":\"#eeeeff\",\"nodeBorder\":\"#123456\",\"nodeTextColor\":\"#234567\",\"clusterBkg\":\"#ffeddd\",\"clusterBorder\":\"#876543\",\"titleColor\":\"#567890\",\"edgeLabelBackground\":\"#ccddeeff\"}}}%%\n";
        var header = family == "flowchart" ? "flowchart LR\nsubgraph scope[Scope]" : family == "swimlane" ? "swimlane-beta LR\nsubgraph scope[Scope]" : "agentflow-beta LR\nflow scope[Scope]";
        return (explicitRoles ? init : "") + header + "\nwork[Work] -->|Route| done[Done]\nend"
            + (directOverride ? "\nstyle work fill:#102030,stroke:#203040,color:#304050\nstyle scope fill:#102030,stroke:#203040,color:#304050" : "");
    }
    private static void Capture(string name, string source, MermaidRenderResult result) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, "source-palette-" + name); File.WriteAllText(stem + ".mmd", source);
            File.WriteAllText(stem + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
        }
        if (result.Artifact != null) CaptureArtifact(name, result.Artifact);
    }
    private static void CaptureArtifact(string name, VisualArtifact artifact) {
        if (Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var stem = Path.Combine(output, "source-palette-" + name);
        File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
    }
}
