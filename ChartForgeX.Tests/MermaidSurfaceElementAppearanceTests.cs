using System.Text.Json.Nodes;
using ChartForgeX.Markup.Mermaid;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidSurfaceElementAppearanceTests {
    [Theory]
    [InlineData("rect")] [InlineData("person")] [InlineData("dbl-circ")] [InlineData("bucket")] [InlineData("stadium")]
    public void ObservedElementPaintAndIsolationSurviveDirectFenceAndPortableRendering(string shape) {
        var source = Source(shape); var result = MermaidRenderer.Render(source, Options());
        MermaidSurfacePaintCausalTests.Capture(result, "qualified-surface-elements-" + shape);
        Assert.Empty(result.Diagnostics); Assert.Equal(source, result.Document!.SourceText);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact); var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var scene = chart.Prepare().Visual.Scene; var marks = MermaidSourceContourPaintCausalTests.OwnedMarks(scene, "work");
        Assert.Equal(shape == "rect" ? 1 : 2, marks.Count);
        Assert.All(marks.Take(shape == "bucket" ? 1 : marks.Count), mark => Assert.Equal(.5, mark.Opacity));
        var red = ChartColor.FromHex("#ff0000"); var blue = ChartColor.FromHex("#0000ff");
        if (shape == "stadium") {
            Assert.Equal(red, marks[0].Fill); Assert.Null(marks[0].Stroke);
            Assert.Null(marks[1].Fill); Assert.Equal(blue, marks[1].Stroke);
            Assert.Equal(Assert.IsType<VisualScenePath>(marks[0]).Commands, Assert.IsType<VisualScenePath>(marks[1]).Commands);
        } else {
            foreach (var mark in marks.Take(shape == "bucket" ? 1 : marks.Count)) { Assert.Equal(red, mark.Fill); Assert.Equal(blue, mark.Stroke); }
            if (shape == "bucket") { Assert.Null(marks[1].Fill); Assert.Equal(1, marks[1].Opacity); Assert.Equal(ChartColor.FromHex("#9370DB"), marks[1].Stroke); }
        }
        var isolated = scene.Nodes.OfType<VisualSceneGroup>().Where(group => group.Opacity == .5).ToArray();
        if (shape == "dbl-circ") Assert.Single(isolated); else Assert.Empty(isolated);
        Assert.All(scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-node-label"), text => Assert.Equal(255, text.Color.A));
        var fenced = new MermaidVisualMarkupParser(Options()).Parse("```mermaid\n" + source + "\n```");
        Assert.False(fenced.HasErrors); var fromFence = Assert.Single(fenced.Artifacts);
        Assert.Equal(artifact.ToSvg(), fromFence.ToSvg()); Assert.Equal(artifact.ToPng(), fromFence.ToPng());
        var envelope = artifact.ToInterchangeEnvelope(); Assert.Equal(2, envelope.Version);
        var restored = VisualArtifactInterchangeEnvelope.FromJson(envelope.ToJson()).ToTopologyChart();
        Assert.Equal(TopologyStaticPaintObservation.Paint(artifact.ToSvg()), TopologyStaticPaintObservation.Paint(restored.ToSvg()));
        Assert.Equal(artifact.ToPng(), restored.ToPng());
        Assert.Equal(shape is "dbl-circ" or "stadium", HasCapture(envelope.ToJson()));
    }

    [Theory] [InlineData("dbl-circ")] [InlineData("stadium")]
    public void AuthoredEditsInvalidateCaptureWhilePositionAndPaintEditsRetainIt(string shape) {
        var source = MermaidRenderer.Render(Source(shape), Options()).Artifact!;
        var chart = VisualArtifactInterchangeEnvelope.FromJson(source.ToInterchangeJson()).ToTopologyChart();
        var node = Assert.Single(chart.Nodes); node.X += 13; node.Y += 9;
        node.BackgroundColor = "#00ff00"; node.BorderColor = "#000000";
        var edited = chart.ToVisualArtifact(); Assert.True(HasCapture(edited.ToInterchangeJson()));
        var marks = MermaidSourceContourPaintCausalTests.OwnedMarks(chart.Prepare().Visual.Scene, "work");
        Assert.Equal(ChartColor.FromHex("#00ff00"), marks[0].Fill);
        Assert.Equal(ChartColor.FromHex("#000000"), marks[marks.Count - 1].Stroke);
        var beforeReset = edited.ToPng(); node.SurfaceOpacity = .5;
        var ordinary = chart.ToVisualArtifact(); Assert.False(HasCapture(ordinary.ToInterchangeJson())); Assert.NotEqual(beforeReset, ordinary.ToPng());
        // Returning to the old shape/mode cannot revive a discarded private capture.
        foreach (var modeEdit in new[] { false, true }) {
            var detached = VisualArtifactInterchangeEnvelope.FromJson(source.ToInterchangeJson()).ToTopologyChart();
            var target = detached.Nodes[0];
            if (modeEdit) { target.DisplayMode = TopologyNodeDisplayMode.CompactCard; target.DisplayMode = TopologyNodeDisplayMode.Card; }
            else { var previous = target.Shape; target.Shape = TopologyNodeShape.Rectangle; target.Shape = previous; }
            Assert.False(HasCapture(detached.ToVisualArtifact().ToInterchangeJson()));
        }
    }

    [Theory] [InlineData(TopologyNodeShape.DoubleCircle)] [InlineData(TopologyNodeShape.Stadium)]
    public void NativeDefaultAndExplicitNullPreserveExistingBytes(TopologyNodeShape shape) {
        var chart = TopologyChart.Create().WithId("ordinary-elements").WithTitle("").WithViewport(320, 240, 16).WithLegend(null);
        var node = new TopologyNode { Id = "work", Shape = shape, X = 60, Y = 60, Width = 160, Height = 100,
            ShowLabel = false, ShowStatusBadge = false, PreserveDisplayModeSize = true, BackgroundColor = "#ff0000", BorderColor = "#0000ff" };
        chart.Nodes.Add(node); var before = chart.ToVisualArtifact();
        var svg = before.ToSvg(); var png = before.ToPng(); var json = before.ToInterchangeJson(); Assert.False(HasCapture(json));
        node.SurfaceOpacity = null; node.Shape = shape; node.TableContent = null;
        var after = chart.ToVisualArtifact(); Assert.Equal(svg, after.ToSvg()); Assert.Equal(png, after.ToPng()); Assert.Equal(json, after.ToInterchangeJson());
    }

    [Theory]
    [InlineData("flowchart LR\nstyle work opacity:0.5\nwork@{ shape: browser, label: 'Work' }", 2)]
    [InlineData("flowchart LR\nwork@{ shape: browser, label: 'Work' }\nclassDef faded opacity:0.5\nclass work faded", 2)]
    [InlineData("agentflow-beta LR\nstyle review opacity:0.5\nflow review[Review]\nwork[Work]\nend", 2)]
    public void UnrepresentedFinalTargetsKeepTheirLocatedOpacityWarning(string source, int line) {
        var result = MermaidRenderer.Render(source, Options()); Assert.False(result.HasErrors);
        var warning = Assert.Single(result.Diagnostics); Assert.Equal(MermaidDiagnosticCodes.UnsupportedStatement, warning.Code);
        Assert.Equal(line, warning.Span.Line); Assert.Equal(source, result.Document!.SourceText);
    }

    [Fact]
    public void QualifiedCaptureRejectsUnknownFieldsAndMismatchedShapeOrOpacity() {
        var json = MermaidRenderer.Render(Source("stadium"), Options()).Artifact!.ToInterchangeJson();
        foreach (var mutation in new[] { "unknown", "shape", "opacity", "group", "basis" }) {
            var root = JsonNode.Parse(json)!; var topology = root["nodes"]![0]!["topology"]!; var elements = topology["surfaceElements"]!;
            if (mutation == "unknown") elements["extra"] = true;
            else if (mutation == "shape") elements["shape"] = "Rectangle";
            else if (mutation == "opacity") elements["opacity"] = 2;
            else if (mutation == "group") elements["groupOpacity"] = .25;
            else topology["surfaceOpacity"] = .25;
            Assert.ThrowsAny<ArgumentException>(() => VisualArtifactInterchangeEnvelope.FromJson(root.ToJsonString()));
        }
    }
    private static string Source(string shape) => "flowchart LR\nwork@{ shape: " + shape + ", label: 'Work' }\nstyle work fill:#ff0000,stroke:#0000ff,opacity:0.5";
    private static MermaidRenderOptions Options() => MermaidSourceContourPaintCausalTests.Options();
    private static bool HasCapture(string json) => JsonNode.Parse(json)!["nodes"]![0]!["topology"]!["surfaceElements"] != null;
}
