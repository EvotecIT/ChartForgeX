using System.Text.Json;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyAuthoredDashTangentTests {
    [Theory]
    [InlineData(false, false)] [InlineData(false, true)]
    [InlineData(true, false)] [InlineData(true, true)]
    public void AuthoredDashConnectorsUseThePaintPathEndpointDirectionThroughFitAndPortableReplay(bool cubic, bool fit) {
        var chart = TopologyChart.Create().WithId("authored-dash-tangent").WithTitle("").WithLegend(null)
            .WithViewport(760, 440, 24).WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family))
            .WithLayout(TopologyLayoutMode.Manual)
            .AddNode("from", "From", 50, 160, width: 150, height: 70)
            .AddNode("to", "To", 540, 160, width: 150, height: 70)
            .AddEdge("route", "from", "to", direction: VisualLinkDirection.Forward);
        foreach (var node in chart.Nodes) { node.ShowLabel = false; node.ShowStatusBadge = false; }
        var edge = chart.Edges[0];
        edge.PathGeometry = new TopologyPathGeometry(cubic ? "M 200 195 C 300 25 440 25 540 195" : "M 200 195 L 540 195");
        edge.DashPattern.Add(6); edge.DashPattern.Add(3); edge.StrokeWidth = 2;
        var authored = edge.PathGeometry.PathData;
        var options = new TopologyRenderOptions { IncludeTitle = false, IncludeLegend = false, FitContentToViewport = fit };
        var request = VisualExportRequest.ForTopology(chart, options).Context;
        var before = fit ? chart.Prepare(new VisualRenderContext(new VisualLayoutOptions(new VisualSize(400, 280), 20),
            request.Theme, request.ThemeMode, request.Frame, request.Font), options) : chart.Prepare(options).Visual;
        Capture(cubic + "-" + fit + "-direct-before-wire", before);
        var saved = before.ToArtifact().ToInterchangeEnvelope();
        var json = saved.ToJson();
        var after = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart().Prepare();
        Capture(cubic + "-" + fit + "-restored", after.Visual);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output)
            File.WriteAllText(Path.Combine(output, "authored-dash-tangent-" + cubic + "-" + fit + ".json"), json);

        var units = saved.Nodes[0].Width!.Value / chart.Nodes[0].Width;
        if (fit) Assert.NotEqual(1, units); else Assert.Equal(1, units);
        AssertEndpointDirections(before, units, cubic);
        AssertEndpointDirections(after.Visual, units, cubic);
        Assert.Equal(TopologyStaticPaintObservation.Paint(before.ToSvg()), TopologyStaticPaintObservation.Paint(after.ToSvg()));
        Assert.Equal(before.ToPng(), after.ToPng());
        Assert.Null(edge.Interpolation); Assert.Equal(authored, edge.PathGeometry.PathData);
        Assert.Equal(chart.Id, saved.Id); Assert.Equal(TopologyLayoutMode.Manual, saved.Topology!.LayoutMode);
    }

    private static void AssertEndpointDirections(PreparedVisual value, double units, bool cubic) {
        var body = Assert.Single(value.Scene.Nodes.OfType<VisualScenePath>(), mark => mark.Role == "topology-edge-line").Commands;
        Assert.Equal(cubic ? ChartPathCommandKind.CubicTo : ChartPathCommandKind.LineTo, body[1].Kind);
        var first = body[0]; var last = body[body.Count - 1];
        var connectors = value.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role == "topology-edge-endpoint-connector").ToArray();
        Assert.NotEmpty(connectors);
        Assert.Contains(connectors, mark => mark.Commands[0].X == first.X && mark.Commands[0].Y == first.Y);
        Assert.Contains(connectors, mark => mark.Commands[0].X == last.X && mark.Commands[0].Y == last.Y);
        Assert.All(connectors, mark => {
            Assert.Equal(2, mark.Commands.Count);
            var start = mark.Commands[0]; var end = mark.Commands[1];
            var source = start.X == first.X && start.Y == first.Y;
            Assert.True(source || start.X == last.X && start.Y == last.Y);
            var adjacent = source ? cubic ? new ChartPoint(last.Control1X, last.Control1Y) : new ChartPoint(last.X, last.Y)
                : cubic ? new ChartPoint(last.Control2X, last.Control2Y) : new ChartPoint(first.X, first.Y);
            var dx = adjacent.X - start.X; var dy = adjacent.Y - start.Y;
            var distance = Math.Sqrt(dx * dx + dy * dy); var length = Math.Min(7 * units, distance);
            Assert.Equal(start.X + dx / distance * length, end.X, 10);
            Assert.Equal(start.Y + dy / distance * length, end.Y, 10);
        });
    }

    private static void Capture(string name, PreparedVisual value) {
        TopologyPortableFontObservation.Capture("authored-dash-tangent-" + name, value);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "authored-dash-tangent-" + name + ".geometry.json"), JsonSerializer.Serialize(
            value.Scene.Nodes.OfType<VisualScenePath>().Where(mark => mark.Role?.StartsWith("topology-edge-", StringComparison.Ordinal) == true)
                .Select(mark => new { mark.Role, mark.StrokeWidth, mark.Commands })));
    }
}
