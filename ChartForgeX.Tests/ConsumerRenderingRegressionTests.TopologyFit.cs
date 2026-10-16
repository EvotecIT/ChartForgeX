using ChartForgeX.Primitives;
using ChartForgeX.Topology;
using ChartForgeX.Rendering;
using ChartForgeX.Raster;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace ChartForgeX.Tests;

public sealed partial class ConsumerRenderingRegressionTests {
    [Theory]
    [InlineData(900, 400, 1)]
    [InlineData(520, 260, 2)]
    public void FittedTopologyKeepsSelectedNodeStrokeInsideTheContentClip(int width, int height, int pngScale) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLayout(TopologyLayoutMode.Manual)
            .AddGroup("selected-group", "Selected group", 24, 24, 240, 150)
            .AddNode("selected", "Selected service", 30, 30, width: 160, height: 84, groupId: "selected-group");
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, PngOutputScale = pngScale };
        options.SelectedNodeIds.Add("selected");
        options.SelectedGroupIds.Add("selected-group");
        var svg = XDocument.Parse(chart.ToSvg(options));
        var clip = svg.Descendants().Where(element => element.Name.LocalName == "clipPath")
            .SelectMany(element => element.Elements()).Single(element => element.Name.LocalName == "rect");
        var surface = svg.Descendants().Single(element => Role(element) == "topology-node-surface");
        var groupSurface = svg.Descendants().Single(element => Role(element) == "topology-group-surface");
        AssertStrokeInsideClip(surface);
        AssertStrokeInsideClip(groupSurface);
        Assert.NotEmpty(chart.ToPng(options));

        void AssertStrokeInsideClip(XElement strokedSurface) {
            var halfStroke = N(strokedSurface, "stroke-width") / 2;
            Assert.True(N(strokedSurface, "x") - halfStroke >= N(clip, "x") - .1);
            Assert.True(N(strokedSurface, "y") - halfStroke >= N(clip, "y") - .1);
            Assert.True(N(strokedSurface, "x") + N(strokedSurface, "width") + halfStroke <= N(clip, "x") + N(clip, "width") + .1);
            Assert.True(N(strokedSurface, "y") + N(strokedSurface, "height") + halfStroke <= N(clip, "y") + N(clip, "height") + .1);
        }
    }

    [Fact]
    public void FittedTopologyPreservesDisabledNodeLabelWrapping() {
        var chart = TopologyChart.Create().WithViewport(520, 260).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("service", "A long service label that should stay on one line", 40, 80, width: 230, height: 72);
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, WrapNodeLabels = false };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var labelGroup = svg.Descendants().Single(element => Role(element) == "topology-node-label");
        var labelLines = labelGroup.Descendants().Where(element => element.Name.LocalName == "text").ToArray();

        Assert.Single(labelLines);
        Assert.DoesNotContain(labelGroup.Descendants(), element => element.Name.LocalName == "tspan");
        Assert.Equal("A long service label that should stay on one line", labelLines[0].Value);
        Assert.False(options.WrapNodeLabels);
    }

    [Theory]
    [InlineData(560, 300, 1)]
    [InlineData(480, 260, 2)]
    public void FittedCloudGeometryStaysInsideTheContentClip(int width, int height, int pngScale) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("cloud", "Cloud", 10, 10, width: 180, height: 120);
        chart.Nodes[0].Shape = TopologyNodeShape.Cloud;
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, IncludeNodeLabels = false, PngOutputScale = pngScale };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var clip = svg.Descendants().Single(element => element.Name.LocalName == "clipPath")
            .Elements().Single(element => element.Name.LocalName == "rect");
        var cloud = svg.Descendants().Single(element => Role(element) == "topology-node-surface");
        var bounds = CubicPathBounds((string)cloud.Attribute("d")!);
        var halfStroke = N(cloud, "stroke-width") / 2;

        Assert.True(bounds.Left - halfStroke >= N(clip, "x") - .2, "The cloud's left lobe and stroke must remain inside the fitted clip.");
        Assert.True(bounds.Top - halfStroke >= N(clip, "y") - .2, "The cloud's upper lobe and stroke must remain inside the fitted clip.");
        Assert.True(bounds.Right + halfStroke <= N(clip, "x") + N(clip, "width") + .2, "The cloud's right lobe and stroke must remain inside the fitted clip.");
        Assert.True(bounds.Bottom + halfStroke <= N(clip, "y") + N(clip, "height") + .2, "The cloud's lower lobe and stroke must remain inside the fitted clip.");
        var png = RasterImageDecoder.Decode(chart.ToPng(options));
        Assert.Equal(width * pngScale, png.Width);
        Assert.Equal(height * pngScale, png.Height);

        static ChartRect CubicPathBounds(string path) {
            var tokens = Regex.Matches(path, @"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")
                .Select(match => match.Value).ToArray();
            var points = new List<ChartPoint>();
            var current = default(ChartPoint);
            for (var index = 0; index < tokens.Length;) {
                var command = tokens[index++];
                if (command == "M") {
                    current = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    points.Add(current);
                } else if (command == "C") {
                    var first = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    var second = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    var end = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    for (var step = 1; step <= 256; step++) {
                        var t = step / 256d; var inverse = 1 - t;
                        var a = inverse * inverse * inverse; var b = 3 * inverse * inverse * t;
                        var c = 3 * inverse * t * t; var d = t * t * t;
                        points.Add(new ChartPoint(current.X * a + first.X * b + second.X * c + end.X * d,
                            current.Y * a + first.Y * b + second.Y * c + end.Y * d));
                    }
                    current = end;
                } else if (command != "Z") throw new InvalidOperationException("Unexpected cloud path command: " + command);
            }
            return new ChartRect(points.Min(point => point.X), points.Min(point => point.Y),
                points.Max(point => point.X) - points.Min(point => point.X), points.Max(point => point.Y) - points.Min(point => point.Y));
        }
    }

    [Fact]
    public void CloudStatusBadgeSitsInsideTheCloudOutline() {
        var chart = TopologyChart.Create().WithViewport(560, 300).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("cloud", "Cloud", 10, 10, width: 180, height: 120);
        chart.Nodes[0].Shape = TopologyNodeShape.Cloud;
        var svg = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, IncludeNodeLabels = false }));
        var cloud = svg.Descendants().Single(element => Role(element) == "topology-node-surface");
        var status = svg.Descendants().Single(element => Role(element) == "topology-status");

        Assert.True(CubicPathContains((string)cloud.Attribute("d")!, N(status, "cx"), N(status, "cy")),
            "The cloud status marker must sit inside the visible cloud outline.");
        Assert.NotEmpty(chart.ToPng(new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, IncludeNodeLabels = false }));

        static bool CubicPathContains(string path, double x, double y) {
            var tokens = Regex.Matches(path, @"[A-Za-z]|[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")
                .Select(match => match.Value).ToArray();
            var polygon = new List<ChartPoint>();
            var current = default(ChartPoint);
            for (var index = 0; index < tokens.Length;) {
                var command = tokens[index++];
                if (command == "M") {
                    current = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    polygon.Add(current);
                } else if (command == "C") {
                    var first = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    var second = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    var end = new ChartPoint(double.Parse(tokens[index++], CultureInfo.InvariantCulture), double.Parse(tokens[index++], CultureInfo.InvariantCulture));
                    for (var step = 1; step <= 256; step++) {
                        var t = step / 256d; var inverse = 1 - t;
                        var a = inverse * inverse * inverse; var b = 3 * inverse * inverse * t;
                        var c = 3 * inverse * t * t; var d = t * t * t;
                        polygon.Add(new ChartPoint(current.X * a + first.X * b + second.X * c + end.X * d,
                            current.Y * a + first.Y * b + second.Y * c + end.Y * d));
                    }
                    current = end;
                } else if (command == "Z") break;
                else throw new InvalidOperationException("Unexpected cloud path command: " + command);
            }
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++) {
                var a = polygon[i]; var b = polygon[j];
                if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X) inside = !inside;
            }
            return inside;
        }
    }

    [Theory]
    [InlineData(1300, 560, 1)]
    [InlineData(900, 400, 1)]
    [InlineData(520, 260, 1)]
    [InlineData(1300, 560, 2)]
    [InlineData(900, 400, 2)]
    [InlineData(520, 260, 2)]
    public void FittedTopologyRoutesMeetNodeBordersAtEveryScale(int width, int height, int pngOutputScale) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLegend(null)
            .WithLayout(TopologyLayoutMode.Layered).WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddAutoGroup("edge", "Edge").AddAutoGroup("core", "Core")
            .AddAutoNode("gateway", "Gateway", groupId: "edge")
            .AddAutoNode("identity", "Identity service", groupId: "core")
            .AddAutoNode("database", "Directory database", groupId: "core")
            .AddEdge("https", "gateway", "identity", "HTTPS")
            .AddEdge("long", "gateway", "database", "Long route");
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, PngOutputScale = pngOutputScale };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var surfaces = svg.Descendants().Where(element => Role(element) == "topology-node-surface")
            .Select(element => (Id: (string?)element.Parent?.Attribute("data-node-id") ?? "",
                X: N(element, "x"), Y: N(element, "y"), Width: N(element, "width"), Height: N(element, "height")))
            .ToDictionary(item => item.Id, StringComparer.Ordinal);

        foreach (var edge in svg.Descendants().Where(element => Role(element) == "topology-edge")) {
            var source = surfaces[(string)edge.Attribute("data-source-node-id")!];
            var target = surfaces[(string)edge.Attribute("data-target-node-id")!];
            AssertOnBorder(N(edge, "data-route-start-x"), N(edge, "data-route-start-y"), source);
            AssertOnBorder(N(edge, "data-route-end-x"), N(edge, "data-route-end-y"), target);
        }
        var png = RasterImageDecoder.Decode(chart.ToPng(options));
        Assert.Equal(width * pngOutputScale, png.Width);
        Assert.Equal(height * pngOutputScale, png.Height);
        foreach (var edge in svg.Descendants().Where(element => Role(element) == "topology-edge")) {
            var source = surfaces[(string)edge.Attribute("data-source-node-id")!];
            var target = surfaces[(string)edge.Attribute("data-target-node-id")!];
            AssertRasterEndpoint(png, N(edge, "data-route-start-x"), N(edge, "data-route-start-y"), source, pngOutputScale);
            AssertRasterEndpoint(png, N(edge, "data-route-end-x"), N(edge, "data-route-end-y"), target, pngOutputScale);
        }

        static void AssertOnBorder(double x, double y, (string Id, double X, double Y, double Width, double Height) node) {
            const double tolerance = 1.5;
            var onHorizontal = x >= node.X - tolerance && x <= node.X + node.Width + tolerance &&
                (Math.Abs(y - node.Y) <= tolerance || Math.Abs(y - (node.Y + node.Height)) <= tolerance);
            var onVertical = y >= node.Y - tolerance && y <= node.Y + node.Height + tolerance &&
                (Math.Abs(x - node.X) <= tolerance || Math.Abs(x - (node.X + node.Width)) <= tolerance);
            Assert.True(onHorizontal || onVertical, $"Route endpoint ({x:0.##}, {y:0.##}) misses node '{node.Id}' border.");
        }

        static void AssertRasterEndpoint(ChartForgeX.Raster.RgbaImage image, double x, double y,
            (string Id, double X, double Y, double Width, double Height) node, int scale) {
            var side = Math.Abs(x - node.X) <= 1.5 ? -1
                : Math.Abs(x - (node.X + node.Width)) <= 1.5 ? 1
                : Math.Abs(y - node.Y) <= 1.5 ? -2 : 2;
            var background = new[] { image.Pixels[0], image.Pixels[1], image.Pixels[2] };
            for (var distance = 1; distance <= 2; distance++) {
                for (var tangent = -3; tangent <= 3; tangent++) {
                    var px = (int)Math.Round((x + (side == -1 ? -distance : side == 1 ? distance : 0)) * scale);
                    var py = (int)Math.Round((y + (side == -2 ? -distance : side == 2 ? distance : 0)) * scale);
                    if (side is -1 or 1) py += tangent * scale;
                    else px += tangent * scale;
                    if (px < 0 || py < 0 || px >= image.Width || py >= image.Height) continue;
                    var offset = (py * image.Width + px) * 4;
                    var difference = Math.Abs(image.Pixels[offset] - background[0])
                        + Math.Abs(image.Pixels[offset + 1] - background[1])
                        + Math.Abs(image.Pixels[offset + 2] - background[2]);
                    if (difference > 36) return;
                }
            }
            Assert.Fail($"The raster route from '{node.Id}' has no visible ink just outside its border at ({x:0.##}, {y:0.##}).");
        }
    }

    [Theory]
    [InlineData(900, 340)]
    [InlineData(600, 280)]
    public void FittedTopologyRoutesPreserveNamedPortOffsets(int width, int height) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", 50, 70, width: 130, height: 72)
            .AddNode("target", "Target", 390, 70, width: 130, height: 72)
            .AddNodePort("source", "upper", TopologyEdgePort.Right, 0.2)
            .AddNodePort("target", "lower", TopologyEdgePort.Left, 0.8)
            .AddEdge("link", "source", "target", "HTTPS")
            .WithEdgeNamedPorts("link", "upper", "lower");
        var svg = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false }));
        var edge = svg.Descendants().Single(element => Role(element) == "topology-edge");
        var surfaces = svg.Descendants().Where(element => Role(element) == "topology-node-surface")
            .ToDictionary(element => (string)element.Parent!.Attribute("data-node-id")!, element =>
                (X: N(element, "x"), Y: N(element, "y"), Width: N(element, "width"), Height: N(element, "height")), StringComparer.Ordinal);
        var source = surfaces["source"];
        var target = surfaces["target"];

        Assert.Equal(source.X + source.Width, N(edge, "data-route-start-x"), 2);
        Assert.Equal(source.Y + source.Height * 0.2, N(edge, "data-route-start-y"), 2);
        Assert.Equal(target.X, N(edge, "data-route-end-x"), 2);
        Assert.Equal(target.Y + target.Height * 0.8, N(edge, "data-route-end-y"), 2);
        Assert.NotEmpty(chart.ToPng(new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false }));
    }

    [Theory]
    [InlineData(900, 400, 1)]
    [InlineData(620, 300, 2)]
    public void FittedTopologyRoutesPreservePortFanOutAtEveryScale(int width, int height, int outputScale) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", 50, 150, width: 120, height: 72)
            .AddNode("upper", "Upper target", 420, 40, width: 120, height: 72)
            .AddNode("lower", "Lower target", 420, 270, width: 120, height: 72)
            .AddEdge("upper-link", "source", "upper", "Primary")
            .AddEdge("lower-link", "source", "lower", "Backup");
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, PngSupersamplingScale = outputScale };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var sourceSurface = svg.Descendants().Single(element => Role(element) == "topology-node-surface"
            && (string?)element.Parent?.Attribute("data-node-id") == "source");
        var edges = svg.Descendants().Where(element => Role(element) == "topology-edge").ToArray();

        Assert.Equal(2, edges.Length);
        Assert.Equal(N(sourceSurface, "x") + N(sourceSurface, "width"), N(edges[0], "data-route-start-x"), 2);
        Assert.Equal(N(sourceSurface, "x") + N(sourceSurface, "width"), N(edges[1], "data-route-start-x"), 2);
        Assert.All(edges, edge => Assert.InRange(N(edge, "data-route-start-y"), N(sourceSurface, "y") - 1,
            N(sourceSurface, "y") + N(sourceSurface, "height") + 1));
        Assert.NotEqual(Math.Round(N(edges[0], "data-route-start-y"), 2), Math.Round(N(edges[1], "data-route-start-y"), 2));
        var image = RasterImageDecoder.Decode(chart.ToPng(options));
        Assert.Equal(width, image.Width);
        Assert.Equal(height, image.Height);
    }

    [Fact]
    public void FittedContentIgnoresGroupShellsThatAreNotRendered() {
        TopologyChart Create(bool addOmittedGroup) {
            var chart = TopologyChart.Create().WithViewport(1000, 420).WithLayout(TopologyLayoutMode.Manual)
                .AddNode("visible", "Visible service", 40, 50, width: 110, height: 64);
            if (addOmittedGroup) chart.AddGroup("omitted", "Omitted group", 12000, 12000, 1800, 1200);
            return chart;
        }
        double FontSize(TopologyChart chart) => XDocument.Parse(chart.ToSvg(new TopologyRenderOptions {
            FitContentToViewport = true, IncludeGroups = false, IncludeLegend = false
        })).Descendants().Where(element => Role(element) == "topology-node-label")
            .SelectMany(element => element.DescendantsAndSelf()).Where(element => element.Name.LocalName == "text")
            .Select(element => N(element, "font-size")).DefaultIfEmpty(0).Max();

        Assert.Equal(FontSize(Create(false)), FontSize(Create(true)), 2);
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Tile, 720, 300)]
    [InlineData(TopologyNodeDisplayMode.Tile, 440, 220)]
    [InlineData(TopologyNodeDisplayMode.Icon, 720, 300)]
    [InlineData(TopologyNodeDisplayMode.Icon, 440, 220)]
    public void FittedTopologyKeepsExternalBadgesInsideTheContentClip(TopologyNodeDisplayMode mode, int width, int height) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.TopToBottom)
            .AddAutoNode("one", "Database cluster")
            .AddAutoNode("two", "Directory service")
            .AddAutoNode("three", "Recovery controller")
            .AddEdge("first", "one", "two").AddEdge("second", "two", "three");
        foreach (var node in chart.Nodes) {
            node.DisplayMode = mode;
            node.Badge = "CRITICAL STATUS";
        }
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, IncludeIconLabels = true };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var clipPath = svg.Descendants().Single(element => element.Name.LocalName == "clipPath");
        var clip = clipPath.Elements().Single(element => element.Name.LocalName == "rect");
        var badges = svg.Descendants().Where(element => Role(element) == "topology-node-badge-surface").ToArray();

        Assert.Equal(3, badges.Length);
        Assert.All(badges, badge => {
            Assert.True(N(badge, "x") >= N(clip, "x") - 1, "The fitted badge must not cross the left content clip.");
            Assert.True(N(badge, "y") >= N(clip, "y") - 1, "The fitted badge must not cross the top content clip.");
            Assert.True(N(badge, "x") + N(badge, "width") <= N(clip, "x") + N(clip, "width") + 1,
                "The fitted badge must not cross the right content clip.");
            Assert.True(N(badge, "y") + N(badge, "height") <= N(clip, "y") + N(clip, "height") + 1,
                "The fitted badge must not cross the bottom content clip.");
        });
        Assert.NotEmpty(chart.ToPng(options));
    }

    [Theory]
    [InlineData(900, 360, 1)]
    [InlineData(560, 260, 1)]
    [InlineData(900, 360, 2)]
    [InlineData(560, 260, 2)]
    public void FittedTopologyKeepsLongEndpointLabelsInsideTheContentClip(int width, int height, int pngScale) {
        var chart = TopologyChart.Create().WithViewport(width, height).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", 120, 100, width: 80, height: 60)
            .AddNode("target", "Target", 330, 100, width: 80, height: 60)
            .AddEdge("link", "source", "target")
            .WithEdgeEndpointLabels("link", "Long source relationship label", "Long target relationship label");
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false, PngOutputScale = pngScale };
        var svg = XDocument.Parse(chart.ToSvg(options));
        var clipPath = svg.Descendants().Single(element => element.Name.LocalName == "clipPath");
        var clip = clipPath.Elements().Single(element => element.Name.LocalName == "rect");
        var labels = svg.Descendants().Where(element => Role(element) == "topology-endpoint-label-surface").ToArray();

        Assert.Equal(2, labels.Length);
        Assert.All(labels, label => {
            Assert.True(N(label, "x") >= N(clip, "x") - 1, "The fitted endpoint label must remain inside the left clip edge.");
            Assert.True(N(label, "y") >= N(clip, "y") - 1, "The fitted endpoint label must remain inside the top clip edge.");
            Assert.True(N(label, "x") + N(label, "width") <= N(clip, "x") + N(clip, "width") + 1,
                "The fitted endpoint label must remain inside the right clip edge.");
            Assert.True(N(label, "y") + N(label, "height") <= N(clip, "y") + N(clip, "height") + 1,
                "The fitted endpoint label must remain inside the bottom clip edge.");
        });
        Assert.NotEmpty(chart.ToPng(options));
    }

    [Fact]
    public void LayeredLayoutPacksOneAutomaticGroupAgainstAnExplicitShell() {
        var chart = TopologyChart.Create().WithViewport(1000, 520).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddGroup("fixed", "Fixed shell", 0, 0, 520, 360)
            .AddAutoGroup("automatic", "Automatic shell")
            .AddAutoNode("fixed-node", "Fixed member", groupId: "fixed")
            .AddAutoNode("automatic-node", "Automatic member", groupId: "automatic")
            .AddEdge("link", "fixed-node", "automatic-node");
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var groups = prepared.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var fixedGroup = groups["fixed"];
        var automatic = groups["automatic"];
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        Assert.True(automatic.X >= fixedGroup.X + fixedGroup.Width,
            $"The single automatic shell must follow the explicit shell along the left-to-right flow axis. fixed=({fixedGroup.X},{fixedGroup.Y},{fixedGroup.Width},{fixedGroup.Height}), automatic=({automatic.X},{automatic.Y},{automatic.Width},{automatic.Height}).");
        Assert.True(nodes["automatic-node"].X > nodes["fixed-node"].X,
            "Group packing must preserve the authored left-to-right node order.");
    }

    [Fact]
    public void LayeredLayoutKeepsAnUpstreamAutomaticGroupBeforeAnExplicitShell() {
        var chart = TopologyChart.Create().WithViewport(1000, 520).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(TopologyLayoutDirection.LeftToRight)
            .AddGroup("explicit", "Explicit downstream shell", 0, 0, 520, 360)
            .AddAutoGroup("automatic", "Automatic upstream shell")
            .AddAutoNode("automatic-source", "Automatic source", groupId: "automatic")
            .AddAutoNode("explicit-target", "Explicit target", groupId: "explicit")
            .AddEdge("dependency", "automatic-source", "explicit-target");
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var groups = prepared.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);

        Assert.True(groups["automatic"].X + groups["automatic"].Width <= groups["explicit"].X,
            "An upstream automatic group must be packed before the explicit downstream shell.");
        Assert.True(nodes["automatic-source"].X < nodes["explicit-target"].X,
            "Group packing must preserve the dependency's left-to-right direction.");
    }

    [Theory]
    [InlineData(TopologyLayoutDirection.RightToLeft)]
    [InlineData(TopologyLayoutDirection.BottomToTop)]
    public void LayeredLayoutPreservesAutomaticGroupFlowOrderWithoutCrossGroupEdges(TopologyLayoutDirection direction) {
        var chart = TopologyChart.Create().WithViewport(1000, 520).WithLayout(TopologyLayoutMode.Layered)
            .WithLayoutDirection(direction)
            .AddGroup("fixed", "Fixed shell", 0, 0, 520, 360)
            .AddAutoGroup("automatic", "Automatic shell")
            .AddAutoNode("fixed-node", "Fixed member", groupId: "fixed")
            .AddAutoNode("automatic-node", "Automatic member", groupId: "automatic");
        var prepared = TopologyLayoutEngine.Prepare(chart);
        var groups = prepared.Groups.ToDictionary(group => group.Id, StringComparer.Ordinal);
        var fixedGroup = groups["fixed"];
        var automatic = groups["automatic"];

        if (direction == TopologyLayoutDirection.RightToLeft)
            Assert.True(automatic.X + automatic.Width <= fixedGroup.X, "The automatic shell must retain its prior left-of-fixed placement in RTL flow.");
        else
            Assert.True(automatic.Y + automatic.Height <= fixedGroup.Y, "The automatic shell must retain its prior above-fixed placement in bottom-to-top flow.");
    }

    [Fact]
    public void FittedContentIgnoresHiddenNonGenericManualNodesWhenChoosingItsScale() {
        TopologyChart Create(bool addHidden) {
            var chart = TopologyChart.Create().WithViewport(1000, 420).WithLayout(TopologyLayoutMode.Manual)
                .AddNode("visible", "Visible service", 40, 50, width: 110, height: 64);
            if (addHidden) {
                var hidden = chart.AddNode("anchor", "Hidden anchor", 12000, 12000, width: 110, height: 64).Nodes.Single(node => node.Id == "anchor");
                hidden.Shape = TopologyNodeShape.Cloud;
                hidden.DisplayMode = TopologyNodeDisplayMode.Hidden;
            }
            return chart;
        }
        double FontSize(TopologyChart chart) => XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false }))
            .Descendants().Where(element => Role(element) == "topology-node-label").SelectMany(element => element.DescendantsAndSelf())
            .Where(element => element.Name.LocalName == "text").Select(element => N(element, "font-size")).DefaultIfEmpty(0).Max();

        Assert.Equal(FontSize(Create(false)), FontSize(Create(true)), 2);
    }

    [Fact]
    public void FittedContentKeepsSelectedRouteStrokeInsideTheContentClip() {
        var chart = TopologyChart.Create().WithViewport(560, 320).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("source", "Source", 70, 130, width: 100, height: 64)
            .AddNode("target", "Target", 390, 130, width: 100, height: 64)
            .AddEdge("link", "source", "target", "Selected route")
            .WithEdgeWaypoints("link", new ChartPoint(260, 18));
        var options = new TopologyRenderOptions { FitContentToViewport = true, IncludeLegend = false };
        options.SelectedEdgeIds.Add("link");
        var svg = XDocument.Parse(chart.ToSvg(options));
        var clip = svg.Descendants().Single(element => element.Name.LocalName == "clipPath")
            .Elements().Single(element => element.Name.LocalName == "rect");
        var routeInk = svg.Descendants().Where(element => element.Name.LocalName == "path"
            && (Role(element).StartsWith("topology-edge-line", StringComparison.Ordinal)
                || Role(element) == "topology-edge-route-halo")).ToArray();

        Assert.NotEmpty(routeInk);
        foreach (var path in routeInk) {
            var coordinates = Regex.Matches((string)path.Attribute("d")!, @"[-+]?(?:\d+\.?\d*|\.\d+)(?:[eE][-+]?\d+)?")
                .Select(match => double.Parse(match.Value, CultureInfo.InvariantCulture)).ToArray();
            var halfStroke = N(path, "stroke-width") / 2;
            var minX = coordinates.Where((_, index) => index % 2 == 0).Min();
            var maxX = coordinates.Where((_, index) => index % 2 == 0).Max();
            var minY = coordinates.Where((_, index) => index % 2 == 1).Min();
            var maxY = coordinates.Where((_, index) => index % 2 == 1).Max();
            Assert.True(minX - halfStroke >= N(clip, "x") - .2, "The fitted route stroke must remain inside the left clip edge.");
            Assert.True(minY - halfStroke >= N(clip, "y") - .2, "The fitted route stroke must remain inside the top clip edge.");
            Assert.True(maxX + halfStroke <= N(clip, "x") + N(clip, "width") + .2, "The fitted route stroke must remain inside the right clip edge.");
            Assert.True(maxY + halfStroke <= N(clip, "y") + N(clip, "height") + .2, "The fitted route stroke must remain inside the bottom clip edge.");
        }
        Assert.NotEmpty(chart.ToPng(options));
    }

}
