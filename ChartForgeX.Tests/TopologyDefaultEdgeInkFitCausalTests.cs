using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyDefaultEdgeInkFitCausalTests {
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void ExistingDefaultLineLayersKeepTheirFittedNativeInkThroughPortableRestore(bool fit) {
        var chart = TopologyChart.Create().WithId("default-edge-ink-before").WithViewport(760, 440, 24).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("a", "A", 50, 160, width: 150, height: 70).AddNode("b", "B", 540, 160, width: 150, height: 70)
            .AddEdge("handoff", "a", "b", direction: VisualLinkDirection.None);
        foreach (var node in chart.Nodes) { node.DisplayMode = TopologyNodeDisplayMode.Hidden; node.ShowStatusBadge = false; }
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M200 195L540 195"); chart.Edges[0].StrokeWidth = 2;
        chart.Edges[0].Status = TopologyHealthStatus.Healthy; chart.Edges[0].LineStyle = TopologyEdgeLineStyle.Solid;
        var options = new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false, IncludeEdgeLabels = false, IncludeEndpointLabels = false, FitContentToViewport = fit };
        var direct = fit ? chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 240), 16)), options) : chart.Prepare(options).Visual;
        var saved = direct.ToArtifact().ToInterchangeEnvelope(); var svg = direct.ToSvg(); var png = direct.ToPng(); var json = saved.ToJson();
        Capture("default-edge-ink-" + fit + "-direct", svg, png, json);
        var restoredChart = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var restored = restoredChart.Prepare(new TopologyRenderOptions { IncludeLegend = false, IncludeNodeLabels = false, IncludeEdgeLabels = false, IncludeEndpointLabels = false });
        var afterSvg = restored.ToSvg(); var afterPng = restored.ToPng(); var afterJson = restored.ToInterchangeEnvelope().ToJson();
        Capture("default-edge-ink-" + fit + "-restored", afterSvg, afterPng, afterJson);
        Assert.Equal(fit, saved.Nodes[0].Width!.Value < TopologyLayoutEngine.Prepare(chart, options: options).Nodes[0].Width);
        Assert.Equal(Ink(svg), Ink(afterSvg)); Assert.Equal(png, afterPng);
        Assert.Null(saved.Edges[0].Topology!.SourceMarkerStyle); Assert.Null(saved.Edges[0].Topology!.TargetMarkerStyle);
        Assert.Equal(svg, direct.ToSvg()); Assert.Equal(png, direct.ToPng());
    }
    private static string[] Ink(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("topology-edge-line", StringComparison.Ordinal) == true)
        .Select(element => string.Join("|", new[] { "data-cfx-role", "d", "stroke", "stroke-width", "fill" }.Select(name => (string?)element.Attribute(name) ?? ""))).ToArray();
    private static void Capture(string name, string svg, byte[] png, string json) {
        var output = Environment.GetEnvironmentVariable("CFX_EDGE_UNITS_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var prefix = Path.Combine(output, name);
        File.WriteAllText(prefix + ".svg", svg); File.WriteAllBytes(prefix + ".native.png", png); File.WriteAllText(prefix + ".json", json);
    }
}
