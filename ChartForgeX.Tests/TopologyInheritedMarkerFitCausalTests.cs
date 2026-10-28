using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Current public API causal controls; no crossing or future capture API is referenced.</summary>
[Trait("BrowserSuite", "Compatibility")]
public sealed class TopologyInheritedMarkerFitCausalTests {
    [Theory]
    [InlineData(false, "inherited")] [InlineData(true, "inherited")]
    [InlineData(false, "explicit")] [InlineData(true, "explicit")]
    [InlineData(false, "none")] [InlineData(true, "none")]
    public void PreparedEndpointMarkerInkSurvivesEditableCommonJsonRestore(bool fit, string policy) {
        var source = Source(policy);
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false,
            IncludeDirectionMarkers = true, FitContentToViewport = fit };
        var prepared = fit ? source.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 240), 16)), options)
            : source.Prepare(options).Visual;
        var saved = prepared.ToArtifact().ToInterchangeEnvelope(); var json = saved.ToJson();
        var restoredSource = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var restored = restoredSource.Prepare(options: new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false }).Visual;
        var originalMarkers = Markers(prepared); var restoredMarkers = Markers(restored);
        var scale = saved.Nodes[0].Width!.Value / source.Nodes[0].Width;
        Capture(prepared, restored, saved, restoredSource, "inherited-marker-fit-" + fit + "-" + policy,
            scale, originalMarkers, restoredMarkers);
        // Every artifact and raw ink observation is retained before the causal assertion can fail.
        Assert.Equal(fit, scale < .9); Assert.Equal(policy == "none" ? 0 : 2, originalMarkers.Length);
        Assert.Equal(originalMarkers.Length, restoredMarkers.Length);
        for (var index = 0; index < originalMarkers.Length; index++) {
            Assert.Equal(originalMarkers[index].Name, restoredMarkers[index].Name);
            Assert.Equal(originalMarkers[index].Path, restoredMarkers[index].Path);
            Assert.Equal(originalMarkers[index].Stroke, restoredMarkers[index].Stroke);
            Assert.Equal(originalMarkers[index].Width, restoredMarkers[index].Width);
        }
        if (policy == "explicit") {
            Assert.Equal(1.25 * scale, saved.Edges[0].Topology!.SourceMarkerStyle!.Scale, 8);
            Assert.Equal(2 * scale, saved.Edges[0].Topology!.TargetMarkerStyle!.Scale, 8);
            Assert.Equal(1.25, source.Edges[0].SourceMarkerStyle!.Scale);
        }
        if (!fit && policy == "inherited" || policy == "none") {
            Assert.Null(saved.Edges[0].Topology!.SourceMarkerStyle);
            Assert.Null(saved.Edges[0].Topology!.TargetMarkerStyle);
        }
    }

    [Fact]
    public void NullStyleRoundTripAssignmentKeepsOrdinaryUnitOutputBytes() {
        var chart = Source("inherited"); var options = new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false };
        var first = chart.Prepare(options); var svg = first.ToSvg(); var png = first.ToPng(); var json = first.ToInterchangeEnvelope().ToJson();
        chart.Edges[0].SourceMarkerStyle = new TopologyMarkerStyle(); chart.Edges[0].TargetMarkerStyle = new TopologyMarkerStyle();
        chart.Edges[0].SourceMarkerStyle = null; chart.Edges[0].TargetMarkerStyle = null;
        var second = chart.Prepare(options);
        Capture(first.Visual, second.Visual, first.ToInterchangeEnvelope(), chart, "inherited-marker-null-unit-control", 1, Markers(first.Visual), Markers(second.Visual));
        Assert.Equal(svg, second.ToSvg()); Assert.Equal(png, second.ToPng()); Assert.Equal(json, second.ToInterchangeEnvelope().ToJson());
    }

    private static TopologyChart Source(string policy) {
        var chart = TopologyChart.Create().WithId("marker-fit-causal").WithViewport(760, 440, 24).WithLayout(TopologyLayoutMode.Manual).WithLegend(null);
        chart.AddNode("a", "A", 50, 160, width: 150, height: 70); chart.AddNode("b", "B", 540, 160, width: 150, height: 70);
        foreach (var node in chart.Nodes) { node.Kind = TopologyNodeKind.Generic; node.ShowStatusBadge = false; }
        chart.AddEdge("handoff", "a", "b", direction: policy == "none" ? VisualLinkDirection.None : VisualLinkDirection.Bidirectional);
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M 200 195 L 540 195");
        chart.Edges[0].StrokeWidth = 2;
        if (policy == "explicit") {
            chart.Edges[0].SourceMarkerStyle = new TopologyMarkerStyle { Scale = 1.25, Offset = 2, StrokeWidth = .5 };
            chart.Edges[0].TargetMarkerStyle = new TopologyMarkerStyle { Scale = 2, Offset = 3, StrokeWidth = .75 };
        }
        return chart;
    }
    private sealed record Marker(string Name, string Path, string? Stroke, string? Width);
    private static Marker[] Markers(PreparedVisual prepared) => XDocument.Parse(prepared.ToSvg()).Descendants()
        .Where(element => (string?)element.Attribute("data-cfx-role") == "topology-marker")
        .Select(element => new Marker(element.Name.LocalName, (string?)element.Attribute("d") ?? "", (string?)element.Attribute("stroke"), (string?)element.Attribute("stroke-width"))).ToArray();
    private static void Capture(PreparedVisual direct, PreparedVisual restored, VisualArtifactInterchangeEnvelope saved,
        TopologyChart restoredSource, string name, double scale, Marker[] before, Marker[] after) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); var path = Path.Combine(output, name);
        File.WriteAllText(path + ".direct.svg", direct.ToSvg()); File.WriteAllBytes(path + ".direct.native.png", direct.ToPng()); File.WriteAllText(path + ".direct.portable.json", saved.ToJson());
        File.WriteAllText(path + ".restored.svg", restored.ToSvg()); File.WriteAllBytes(path + ".restored.native.png", restored.ToPng()); File.WriteAllText(path + ".restored.portable.json", restored.ToArtifact().ToInterchangeEnvelope().ToJson());
        File.WriteAllText(path + ".observations.json", System.Text.Json.JsonSerializer.Serialize(new { scale,
            sourceStyle = saved.Edges[0].Topology!.SourceMarkerStyle, targetStyle = saved.Edges[0].Topology!.TargetMarkerStyle,
            restoredSourceStyle = restoredSource.Edges[0].SourceMarkerStyle, restoredTargetStyle = restoredSource.Edges[0].TargetMarkerStyle,
            directMarkers = before, restoredMarkers = after }));
    }
}
