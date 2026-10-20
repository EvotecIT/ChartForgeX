using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using Microsoft.Playwright;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class TopologyOrdinaryPortFanTests {
    [Theory]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [InlineData(false, false)]
    [InlineData(false, true)]
    public void AutomaticOrdinaryPortsMatchTheirOrthogonalAxisBeforeFanSpreading(bool horizontal, bool reversed) {
        var chart = Fan(horizontal, reversed);
        var nodes = chart.Nodes.ToDictionary(node => node.Id);
        var source = nodes["source"];
        var routes = chart.Edges.Select(edge => TopologyRenderPrimitives.EdgePoints(chart, edge, nodes)).ToArray();
        Capture(chart, horizontal, reversed, routes);
        for (var index = 0; index < routes.Length; index++) {
            var route = routes[index];
            var point = route[0];
            Assert.Equal(horizontal ? source.X + (reversed ? -7 : source.Width + 7)
                : source.Y + (reversed ? -7 : source.Height + 7), horizontal ? point.X : point.Y, 6);
            Assert.InRange(horizontal ? point.Y : point.X, horizontal ? source.Y : source.X,
                horizontal ? source.Y + source.Height : source.X + source.Width);
            Assert.True((horizontal ? route[1].X - point.X : route[1].Y - point.Y) * (reversed ? -1 : 1) > 0);
            var target = nodes["target-" + index];
            var end = route[^1];
            Assert.Equal(horizontal ? target.X + (reversed ? target.Width + 7 : -7)
                : target.Y + (reversed ? target.Height + 7 : -7), horizontal ? end.X : end.Y, 6);
            Assert.InRange(horizontal ? end.Y : end.X, horizontal ? target.Y : target.X,
                horizontal ? target.Y + target.Height : target.X + target.Width);
            Orthogonal(route);
        }
        foreach (var fitted in new[] { false, true }) {
            var document = XDocument.Parse(chart.ToSvg(Options(fitted)));
            var group = document.Descendants().Single(node => Role(node) == "topology-node" && (string?)node.Attribute("data-node-id") == "source");
            var surface = group.Descendants().First(node => Role(node) == "topology-node-surface");
            var x = Number(surface, "x"); var y = Number(surface, "y");
            var right = x + Number(surface, "width"); var bottom = y + Number(surface, "height");
            foreach (var path in document.Descendants().Where(node => Role(node) == "topology-edge-line")) {
                var points = Points(path);
                Orthogonal(points);
                Assert.False(points[0].X > x && points[0].X < right && points[0].Y > y && points[0].Y < bottom,
                    "An automatic fan endpoint must not start inside the source card: " + (string?)path.Attribute("d"));
            }
        }
    }

    [Theory]
    [InlineData("explicit")]
    [InlineData("named")]
    [InlineData("waypoint")]
    public void AuthoredPortAndWaypointChoicesRemainAuthoritative(string attachment) {
        var chart = Fan(true, false);
        var edge = chart.Edges[0];
        if (attachment == "explicit") { edge.SourcePort = TopologyEdgePort.Bottom; edge.TargetPort = TopologyEdgePort.Top; }
        if (attachment == "named") { chart.AddNodePort("source", "exit", TopologyEdgePort.Right, .3); edge.SourcePortId = "exit"; }
        if (attachment == "waypoint") edge.Waypoints.AddRange(new[] { new ChartPoint(300, 100), new ChartPoint(300, 175) });
        var authoredWaypoints = edge.Waypoints.ToArray();
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var actual = prepared.Edges.Single(item => item.Id == edge.Id);
        Assert.Equal(attachment == "named" ? TopologyEdgePort.Right : edge.SourcePort, actual.SourcePort);
        Assert.Equal(edge.TargetPort, actual.TargetPort);
        Assert.Equal(edge.SourcePortId, actual.SourcePortId);
        Assert.Equal(authoredWaypoints, actual.Waypoints);
        var nodes = chart.Nodes.ToDictionary(node => node.Id);
        var route = TopologyRenderPrimitives.EdgePoints(chart, edge, nodes);
        if (attachment == "named") {
            Assert.Equal(207, route[0].X, 6); Assert.Equal(15, route[0].Y, 6);
        } else if (attachment == "explicit") Assert.Equal(57, route[0].Y, 6);
        else Assert.Equal(authoredWaypoints, route.Skip(1).Take(authoredWaypoints.Length));
        if (attachment != "waypoint") Orthogonal(route);
    }

    [BrowserTheory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task DirectAndFittedOrdinaryFansStayOutsideTheirSourceInSvgAndNativePng(bool horizontal, bool reversed) {
        var chart = Fan(horizontal, reversed);
        var content = "<style>body{margin:16px;font-family:Arial}svg,img{width:100%;height:auto;display:block}section{max-width:850px}</style>";
        foreach (var fitted in new[] { false, true }) content += "<h2>" + (fitted ? "Fitted" : "Direct") + " SVG / native PNG</h2><section>"
            + chart.ToSvg(Options(fitted)) + "<img src='data:image/png;base64," + Convert.ToBase64String(chart.ToPng(Options(fitted))) + "'></section>";
        await using var session = await InteractiveChartBrowser.OpenAsync(content, 900, 900);
        await session.Page.EvaluateAsync("async()=>{await Promise.all([...document.images].map(image=>image.decode()));await document.fonts.ready;}");
        Assert.Equal(8, await session.Page.Locator("[data-cfx-role=topology-edge-line]").CountAsync());
        InteractiveChartBrowser.AssertNoConsoleErrors(session);
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is { Length: > 0 } output) {
            Directory.CreateDirectory(output);
            await session.Page.ScreenshotAsync(new PageScreenshotOptions { Path = Path.Combine(output, Name(horizontal, reversed) + "-browser.png"), FullPage = true });
        }
    }

    private static TopologyChart Fan(bool horizontal, bool reversed) {
        var chart = TopologyChart.Create().WithViewport(horizontal ? 850 : 500, horizontal ? 500 : 850, 0)
            .WithLayout(TopologyLayoutMode.Manual).WithLegend(null);
        chart.AddNode("source", horizontal ? "Source" : "S", horizontal && reversed ? 600 : 0, !horizontal && reversed ? 600 : 0,
            width: horizontal ? 200 : 50, height: horizontal ? 50 : 200);
        for (var index = 0; index < 4; index++) {
            chart.AddNode("target-" + index, horizontal ? "Target " + (index + 1) : (index + 1).ToString(CultureInfo.InvariantCulture), horizontal ? reversed ? 0 : 400 : 150 + index * 50,
                horizontal ? 150 + index * 50 : reversed ? 0 : 400, width: horizontal ? 200 : 50, height: horizontal ? 50 : 200);
            chart.AddEdge("edge-" + index, "source", "target-" + index, routing: TopologyEdgeRouting.Orthogonal);
        }
        return chart;
    }

    private static TopologyRenderOptions Options(bool fitted) => new() { IncludeLegend = false, FitContentToViewport = fitted };
    private static string Name(bool horizontal, bool reversed) => "ordinary-fan-" + (horizontal ? "horizontal" : "vertical") + (reversed ? "-reversed" : "-forward");
    private static string? Role(XElement node) => (string?)node.Attribute("data-cfx-role");
    private static double Number(XElement node, string name) => double.Parse((string)node.Attribute(name)!, CultureInfo.InvariantCulture);
    private static ChartPoint[] Points(XElement path) {
        var values = Regex.Matches((string)path.Attribute("d")!, @"-?\d+(?:\.\d+)?").Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
        return Enumerable.Range(0, values.Length / 2).Select(index => new ChartPoint(values[index * 2], values[index * 2 + 1])).ToArray();
    }
    private static void Orthogonal(IReadOnlyList<ChartPoint> points) {
        for (var index = 1; index < points.Count; index++) Assert.True(Math.Abs(points[index].X - points[index - 1].X) < .002
            || Math.Abs(points[index].Y - points[index - 1].Y) < .002, "Route must stay orthogonal: " + string.Join(";", points.Select(point => $"{point.X},{point.Y}")));
    }
    private static void Capture(TopologyChart chart, bool horizontal, bool reversed, IReadOnlyList<List<ChartPoint>> routes) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output);
        var name = Name(horizontal, reversed);
        File.WriteAllText(Path.Combine(output, name + "-routes.json"), JsonSerializer.Serialize(routes.Select(route => route.Select(point => new { point.X, point.Y }))));
        foreach (var fitted in new[] { false, true }) {
            var prefix = Path.Combine(output, name + (fitted ? "-fitted" : "-direct"));
            File.WriteAllText(prefix + ".svg", chart.ToSvg(Options(fitted)));
            File.WriteAllBytes(prefix + ".png", chart.ToPng(Options(fitted)));
        }
    }
}
