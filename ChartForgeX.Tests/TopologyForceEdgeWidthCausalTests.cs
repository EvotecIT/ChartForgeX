using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyForceEdgeWidthCausalTests {
    [Theory]
    [InlineData(false, false)] [InlineData(true, false)] [InlineData(true, true)]
    public void IncumbentForceWidthRetainsItsActualOutputUnitsThroughSemanticSnapshot(bool fit, bool selected) => Observe(fit, selected, 2);

    [Fact]
    public void IncumbentDefaultForceWidthAlreadyRetainsItsOutputUnits() => Observe(true, false, null);

    private static void Observe(bool fit, bool selected, double? width) {
        var chart = TopologyChart.Create().WithId("force-width-before").WithViewport(760, 440, 24).WithLayout(TopologyLayoutMode.Manual).WithLegend(null)
            .AddNode("a", "A", 50, 160, width: 150, height: 70).AddNode("b", "B", 540, 160, width: 150, height: 70)
            .AddEdge("handoff", "a", "b", direction: VisualLinkDirection.None);
        foreach (var node in chart.Nodes) { node.DisplayMode = TopologyNodeDisplayMode.Hidden; node.ShowStatusBadge = false; }
        chart.Edges[0].PathGeometry = new TopologyPathGeometry("M200 195L540 195"); chart.Edges[0].StrokeWidth = width;
        chart.Edges[0].Status = TopologyHealthStatus.Healthy; chart.Edges[0].LineStyle = TopologyEdgeLineStyle.Solid;
        var options = Options(); options.FitContentToViewport = fit; if (selected) options.SelectedEdgeIds.Add("handoff");
        var direct = fit ? chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(360, 240), 16)), options) : chart.Prepare(options).Visual;
        var saved = direct.ToArtifact().ToInterchangeEnvelope(); var json = saved.ToJson(); var svg = direct.ToSvg(); var png = direct.ToPng();
        var name = "force-width-" + fit + "-" + selected + "-" + (width.HasValue ? "explicit" : "default"); Capture(name + "-direct", svg, png, json, direct);
        var restoredChart = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var request = Options(); if (selected) request.SelectedEdgeIds.Add("handoff");
        var after = restoredChart.Prepare(request); var afterSvg = after.ToSvg(); var afterPng = after.ToPng(); var afterJson = after.ToInterchangeEnvelope().ToJson();
        Capture(name + "-restored", afterSvg, afterPng, afterJson, after.Visual);
        Assert.Equal(fit, saved.Nodes[0].Width!.Value < TopologyLayoutEngine.Prepare(chart, options: options).Nodes[0].Width);
        Assert.Equal(width, saved.Edges[0].Topology!.StrokeWidth); // Force paint already treats an authored width as output units.
        Assert.Equal(Ink(svg), Ink(afterSvg)); Assert.Equal(png, afterPng); Assert.Equal(width, chart.Edges[0].StrokeWidth);
    }
    private static TopologyRenderOptions Options() => new() { UseForceGraphPresentation = true, EdgeVisualStyle = ChartLineVisualStyle.Plain(),
        IncludeLegend = false, IncludeNodeLabels = false, IncludeEdgeLabels = false, IncludeEndpointLabels = false };
    private static string[] Ink(string svg) => XDocument.Parse(svg).Descendants()
        .Where(element => ((string?)element.Attribute("data-cfx-role"))?.StartsWith("topology-edge-line", StringComparison.Ordinal) == true)
        .Select(element => string.Join("|", new[] { "d", "stroke", "stroke-width", "stroke-dasharray", "fill" }.Select(name => (string?)element.Attribute(name) ?? ""))).ToArray();
    private static void Capture(string name, string svg, byte[] png, string json, PreparedVisual visual) {
        var output = Environment.GetEnvironmentVariable("CFX_EDGE_UNITS_CAPTURE_DIRECTORY"); if (string.IsNullOrWhiteSpace(output)) return;
        Directory.CreateDirectory(output); var prefix = Path.Combine(output, name);
        File.WriteAllText(prefix + ".svg", svg); File.WriteAllBytes(prefix + ".native.png", png); File.WriteAllText(prefix + ".json", json);
        File.WriteAllText(prefix + ".scene-paths.json", JsonSerializer.Serialize(visual.Scene.Nodes.OfType<VisualScenePath>().Select(path => new {
            path.Role, path.StrokeWidth, path.Dash, path.DashOffset, path.Close,
            Commands = path.Commands.Select(command => new { command.Kind, command.X, command.Y, command.Control1X, command.Control1Y, command.Control2X, command.Control2Y })
        })));
    }
}
