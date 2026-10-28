using System.Text.Json;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Incumbent public source/render contracts, captured before missing-paint assertions.</summary>
public sealed class MermaidSourceContourPaintCausalTests {
    [Theory]
    [InlineData("flowchart")]
    [InlineData("agentflow")]
    public void SourceOpacityTargetsSeparateOwnContourElementsAndLeavesLabelsAndChildrenOpaque(string family) {
        var result = MermaidRenderer.Render(OpacitySource(family), Options());
        var chart = Capture(result, family + "-source-element-opacity");
        Assert.False(result.HasErrors);
        Assert.Empty(result.Diagnostics);
        var person = Assert.Single(chart.Nodes, node => node.Id == "work");
        var child = Assert.Single(chart.Nodes, node => node.Id == "child");
        var group = Assert.Single(chart.Groups);
        Assert.Equal(.5, person.SurfaceOpacity); Assert.Null(person.Opacity);
        if (family == "flowchart") Assert.Equal(.5, group.SurfaceOpacity); else Assert.Null(group.SurfaceOpacity); Assert.Null(group.Opacity);
        Assert.Null(child.SurfaceOpacity); Assert.Null(child.Opacity);
        var scene = chart.Prepare().Visual.Scene;
        var own = OwnedMarks(scene, "work");
        Assert.Equal(family == "flowchart" ? 2 : 1, own.Count); Assert.All(own, mark => Assert.Equal(.5, mark.Opacity));
        Assert.All(OwnedMarks(scene, "child"), mark => Assert.Equal(1, mark.Opacity));
        var captions = scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role is "topology-node-label" or "topology-group-label").ToArray();
        Assert.NotEmpty(captions); Assert.All(captions, text => Assert.Equal(255, text.Color.A));
    }

    [Theory]
    [InlineData("f-circ")]
    [InlineData("fork")]
    [InlineData("rect")]
    public void AuthoredSourceFillAndStrokeReachIndependentSurfaceInk(string shape) {
        var source = "flowchart LR\nwork@{ shape: " + shape + ", label: 'Work' }\nstyle work fill:#ff0000,stroke:#0000ff";
        var result = MermaidRenderer.Render(source, Options()); var chart = Capture(result, "source-fill-stroke-" + shape);
        Assert.Empty(result.Diagnostics);
        var marks = OwnedMarks(chart.Prepare().Visual.Scene, "work"); Assert.NotEmpty(marks);
        Assert.All(marks, mark => { Assert.Equal(ChartColor.FromHex("#ff0000"), mark.Fill); Assert.Equal(ChartColor.FromHex("#0000ff"), mark.Stroke); });
    }

    [Fact]
    public void UnframedSourceIconHasNoPlaceholderContourEvenWhenFillAndStrokeAreAuthored() {
        const string source = "flowchart LR\nwork@{icon:'common:server',label:'Server'}\nstyle work fill:#ff0000,stroke:#0000ff";
        var result = MermaidRenderer.Render(source, Options()); var chart = Capture(result, "source-unframed-icon-contour");
        Assert.Empty(result.Diagnostics); var node = Assert.Single(chart.Nodes);
        Assert.Equal(TopologyNodeDisplayMode.Icon, node.DisplayMode);
        Assert.Equal("#0000ff", node.Color);
        Assert.Equal(0, node.SurfaceOpacity);
        Assert.All(OwnedMarks(chart.Prepare().Visual.Scene, "work"), mark => Assert.Equal(0, mark.Opacity));
        Assert.True(node.ShowLabel); Assert.Equal("Server", node.Label);
    }

    internal static string OpacitySource(string family) => (family == "agentflow" ? "agentflow-beta LR\nflow review[Review]" : "flowchart LR\nsubgraph review[Review]")
        + (family == "agentflow" ? "\nwork[Work]" : "\nwork@{ shape: person, label: 'Work' }")
        + "\nchild[Child]\nend\nstyle work opacity:0.5" + (family == "flowchart" ? "\nstyle review opacity:0.5" : "");
    internal static MermaidRenderOptions Options() => new() {
        Flowchart = new MermaidFlowchartRenderOptions { Width = 600, Height = 440 },
        Agentflow = new MermaidTopologyRenderOptions { Width = 600, Height = 440 },
        Presentation = new MermaidPresentationOptions { FontFamily = BrowserProofFonts.Family }
    };
    internal static IReadOnlyList<VisualSceneMark> OwnedMarks(VisualScene scene, string id) {
        var result = new List<VisualSceneMark>(); var stack = new Stack<string?>(); string? current = null;
        foreach (var node in scene.Nodes) {
            if (node is VisualSceneGroup group) { stack.Push(current); if (group.Role == "topology-node") current = group.Id; }
            else if (node is VisualSceneEndGroup) current = stack.Pop();
            else if (current == id && node is VisualSceneMark mark && mark.Role == "topology-node-surface") result.Add(mark);
        }
        return result;
    }
    private static TopologyChart Capture(MermaidRenderResult result, string name) {
        var output = Environment.GetEnvironmentVariable("CFX_SOURCE_CONTOUR_CAPTURE_DIRECTORY");
        if (!string.IsNullOrWhiteSpace(output)) {
            Directory.CreateDirectory(output); var stem = Path.Combine(output, name);
            File.WriteAllText(stem + ".mmd", result.Document?.SourceText ?? "");
            File.WriteAllText(stem + ".diagnostics.json", JsonSerializer.Serialize(result.Diagnostics.Select(item => new { item.Code, item.Message, item.Severity, item.Span.Line, item.Span.Column, item.Span.Length })));
            if (result.Artifact is {} artifact) {
                File.WriteAllText(stem + ".svg", artifact.ToSvg()); File.WriteAllBytes(stem + ".png", artifact.ToPng()); File.WriteAllText(stem + ".json", artifact.ToInterchangeJson());
                var native = Assert.IsType<TopologyChart>(artifact.Model).Prepare();
                File.WriteAllText(stem + ".scene.json", JsonSerializer.Serialize(native.Visual.Scene.Nodes.Select(node => new {
                    node.Role, node.Id, Opacity = node is VisualSceneMark mark ? (double?)mark.Opacity : node is VisualSceneGroup group ? group.Opacity : null,
                    Fill = node is VisualSceneMark fill ? fill.Fill?.ToHexRgba() : null, Stroke = node is VisualSceneMark stroke ? stroke.Stroke?.ToHexRgba() : null
                })));
            }
        }
        return Assert.IsType<TopologyChart>(result.Artifact?.Model);
    }
}
