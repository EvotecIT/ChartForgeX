using System.Globalization;
using System.Text.Json;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyRoundedContourAttachmentTests {
    [Theory]
    [InlineData(TopologyNodeShape.Stadium, 30, 180, false)]
    [InlineData(TopologyNodeShape.Stadium, 30, 180, true)]
    [InlineData(TopologyNodeShape.Rounded, 14, 14, false)]
    [InlineData(TopologyNodeShape.Rounded, 14, 14, true)]
    [InlineData(TopologyNodeShape.Stadium, 180, 60, false)]
    [InlineData(TopologyNodeShape.Stadium, 180, 60, true)]
    [InlineData(TopologyNodeShape.Rounded, 140, 60, false)]
    [InlineData(TopologyNodeShape.Rounded, 140, 60, true)]
    public void FittedRouteEndpointMeetsTheSameRoundedContourPaintedByNativeAndSvg(
        TopologyNodeShape shape, double width, double height, bool reversed) {
        var chart = TopologyChart.Create().WithViewport(500, 360, 0)
            .WithLayout(TopologyLayoutMode.Manual).WithLegend(null);
        chart.Nodes.Add(new TopologyNode { Id = "source", Label = "Source", X = 100, Y = 100,
            Width = width, Height = height, Shape = shape, PreserveDisplayModeSize = true,
            FitLabelToContent = false, ShowLabel = false, ShowStatusBadge = false,
            BackgroundColor = "#E53935", Color = "#E53935" });
        chart.AddNode("target", "Target", 150, 0, width: 40, height: 40);
        chart.Nodes[1].ShowLabel = false; chart.Nodes[1].ShowStatusBadge = false;
        chart.AddEdge("route", reversed ? "target" : "source", reversed ? "source" : "target",
            routing: TopologyEdgeRouting.Straight);
        var prepared = chart.Prepare(new TopologyRenderOptions {
            IncludeTitle = false, IncludeLegend = false, IncludeNodeLabels = false,
            IncludeIconLabels = false, IncludeStatusBadges = false, IncludeEdgeLabels = false,
            IncludeEndpointLabels = false, IncludeDirectionMarkers = false, FitContentToViewport = true
        });
        var bounds = prepared.Analyze().Nodes.Single(node => node.Id == "source").Bounds;
        var scale = bounds.Width / width;
        var surface = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualScenePath>(),
            mark => mark.Role == "topology-node-surface" && mark.Fill.HasValue);
        var nativeRoute = Assert.Single(prepared.Visual.Scene.Nodes.OfType<VisualScenePath>(),
            mark => mark.Role == "topology-edge-line");
        var endpointCommand = reversed ? nativeRoute.Commands[^1] : nativeRoute.Commands[0];
        var endpoint = new ChartPoint(endpointCommand.X, endpointCommand.Y);
        var nativeDistance = Distance(endpoint, VisualSceneGeometry.Flatten(surface, 64 / scale));
        var svgText = prepared.ToSvg();
        var svg = XDocument.Parse(svgText);
        var sourceGroup = Assert.Single(svg.Descendants(), node => Role(node) == "topology-node"
            && (string?)node.Attribute("data-node-id") == "source");
        var svgSurface = Assert.Single(sourceGroup.Descendants(), node => Role(node) == "topology-node-surface");
        var svgRoute = Assert.Single(svg.Descendants(), node => Role(node) == "topology-edge-line");
        var svgRoutePoints = Assert.Single(ChartMapPathParser.ParseSubpaths((string)svgRoute.Attribute("d")!, 64 / scale)).Points;
        var svgEndpoint = reversed ? svgRoutePoints[^1] : svgRoutePoints[0];
        var svgDistance = Distance(svgEndpoint, ChartMapPathParser.ParseSubpaths(
            (string)svgSurface.Attribute("d")!, 64 / scale).Select(path => path.Points));
        var png = prepared.ToPng();
        Capture(shape, width, height, reversed, svgText, png, new {
            endpoint, svgEndpoint, nativeDistance, svgDistance, scale, bounds,
            nativeCommands = nativeRoute.Commands, paintedCommands = surface.Commands,
            svgRoute = (string?)svgRoute.Attribute("d"), svgSurface = (string?)svgSurface.Attribute("d")
        });
        // Routing flattens at the existing 0.05-device-pixel budget. The independent
        // painted contour uses 64x resolution; SVG additionally rounds coordinates to 0.001.
        Assert.InRange(nativeDistance, 0, (ChartCurveFlattening.Tolerance + .001) * scale);
        Assert.InRange(svgDistance, 0, (ChartCurveFlattening.Tolerance + .001) * scale + .002);
        // SVG's 0.001 coordinate serialization can straddle a coarser decimal
        // rounding boundary while retaining the original half-unit precision.
        Assert.InRange(Math.Abs(endpoint.X - svgEndpoint.X), 0, .0005 + 1e-9);
        Assert.InRange(Math.Abs(endpoint.Y - svgEndpoint.Y), 0, .0005 + 1e-9);
        Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
        Assert.Equal(width, chart.Nodes[0].Width); Assert.Equal(height, chart.Nodes[0].Height);
    }

    private static double Distance(ChartPoint point, IEnumerable<IEnumerable<ChartPoint>> contours) {
        var minimum = double.PositiveInfinity;
        foreach (var contour in contours) {
            var points = contour.ToArray();
            for (var index = 1; index < points.Length; index++) {
                var a = points[index - 1]; var b = points[index];
                var dx = b.X - a.X; var dy = b.Y - a.Y;
                var squared = dx * dx + dy * dy;
                var fraction = squared == 0 ? 0 : Math.Max(0, Math.Min(1,
                    ((point.X - a.X) * dx + (point.Y - a.Y) * dy) / squared));
                var x = point.X - (a.X + fraction * dx); var y = point.Y - (a.Y + fraction * dy);
                minimum = Math.Min(minimum, Math.Sqrt(x * x + y * y));
            }
        }
        return minimum;
    }

    private static string? Role(XElement node) => (string?)node.Attribute("data-cfx-role");
    private static void Capture(TopologyNodeShape shape, double width, double height, bool reversed,
        string svg, byte[] png, object facts) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        var prefix = Path.Combine(output, "rounded-contour-" + shape + "-" + width.ToString(CultureInfo.InvariantCulture)
            + "x" + height.ToString(CultureInfo.InvariantCulture) + (reversed ? "-reverse" : "-forward"));
        File.WriteAllText(prefix + ".svg", svg); File.WriteAllBytes(prefix + ".png", png);
        File.WriteAllText(prefix + ".json", JsonSerializer.Serialize(facts, new JsonSerializerOptions { WriteIndented = true }));
    }
}
