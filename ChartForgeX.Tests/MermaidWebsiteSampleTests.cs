using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

/// <summary>Regression coverage for Mermaid sources used as published website samples.</summary>
public sealed class MermaidWebsiteSampleTests {
    private const string Pipeline = "---\ntitle: Nightly report pipeline\n---\nflowchart TD\n" +
        "  collect[Collect directory data] --> check{Checks passed?}\n" +
        "  check -->|Yes| render[Render charts]\n" +
        "  check -->|No| alert[Open incident]\n" +
        "  alert --> render\n" +
        "  render --> publish[Publish report]\n";

    private const string Surfaces = "flowchart LR\n" +
        "  data[(Report data)] --> chart[Chart engine]\n" +
        "  chart --> svg[SVG]\n" +
        "  chart --> png[PNG]\n" +
        "  svg --> site[Website]\n" +
        "  png --> site\n" +
        "  data --> site\n";

    [Theory]
    [InlineData(Pipeline)]
    [InlineData(Surfaces)]
    [InlineData("flowchart BT\n  a[Start] --> b{Ready?}\n  b -->|No| c[Wait]\n  c --> d[Ship]\n  b -->|Yes| d\n")]
    [InlineData("flowchart RL\n  a[Start] --> b[Build]\n  b --> c[Test]\n  a --> c\n")]
    public void FlowchartEdgesStayOutOfForeignNodes(string source) {
        var svg = Svg(source);
        var nodes = NodeBounds(svg);
        foreach (var edge in Edges(svg)) {
            foreach (var node in nodes.Where(node => node.Key != edge.Source && node.Key != edge.Target)) {
                Assert.False(Crosses(edge.Points, Deflate(node.Value, 1)), "Edge " + edge.Source + "->" + edge.Target + " must not pass through node " + node.Key + ".");
            }
        }
    }

    [Theory]
    [InlineData("TD")]
    [InlineData("BT")]
    [InlineData("LR")]
    [InlineData("RL")]
    public void FlowchartDecisionBranchesKeepSeparateRoutesAndLabels(string direction) {
        var rendered = MermaidRenderer.Render(Pipeline.Replace("flowchart TD", "flowchart " + direction, StringComparison.Ordinal));
        Assert.False(rendered.HasErrors); var artifact = rendered.Artifact!;
        var sourceSvg = artifact.ToSvg(); var svg = XDocument.Parse(sourceSvg);
        if (Environment.GetEnvironmentVariable("CFX_NATIVE_CAPTURE_DIRECTORY") is { Length: > 0 } capture) {
            Directory.CreateDirectory(capture); var stem = Path.Combine(capture, "website-decision-branches-" + direction);
            File.WriteAllText(stem + ".svg", sourceSvg); File.WriteAllBytes(stem + ".native.png", artifact.ToPng());
        }
        var edges = Edges(svg);
        var yes = edges.Single(edge => edge.Source == "check" && edge.Target == "render");
        var no = edges.Single(edge => edge.Source == "check" && edge.Target == "alert");
        Assert.False(SharesSegment(yes.Points, no.Points), "Decision branches must not overlap.");

        var labels = svg.Descendants().Where(element => Role(element) == "topology-edge-label-surface").Select(Rect).ToArray();
        Assert.Equal(2, labels.Length);
        Assert.False(Overlaps(labels[0], labels[1]), "Branch labels must not collide.");
        var labelOwners = svg.Descendants().Where(element => Role(element) == "topology-edge-label").Select(element => (string?)element.Attribute("data-edge-id")).ToArray();
        for (var i = 0; i < labels.Length; i++) {
            foreach (var edge in edges.Where(edge => edge.Id != labelOwners[i])) {
                Assert.False(Crosses(edge.Points, Deflate(labels[i], 1)), "Label of " + labelOwners[i] + " must not cover edge " + edge.Id + ".");
            }
        }
    }

    [Theory]
    [InlineData("source")]
    [InlineData("target")]
    [InlineData("named")]
    [InlineData("waypoint")]
    public void LongEdgeLanesPreserveAuthoredAttachments(string attachment) {
        var parsed = new MermaidParser().ParseFlowchart(Pipeline);
        var chart = parsed.Document!.ToTopologyChart();
        var edge = chart.Edges.Single(item => item.SourceNodeId == "check" && item.TargetNodeId == "render");
        if (attachment == "source") edge.SourcePort = TopologyEdgePort.Left;
        if (attachment == "target") edge.TargetPort = TopologyEdgePort.Right;
        if (attachment == "named") {
            chart.AddNodePort("check", "exit", TopologyEdgePort.Left, .3);
            edge.SourcePortId = "exit";
        }
        if (attachment == "waypoint") edge.Waypoints.Add(new ChartForgeX.Primitives.ChartPoint(100, 300));

        var prepared = TopologyLayoutEngine.Prepare(chart).Edges.Single(item => item.Id == edge.Id);
        Assert.Equal(attachment is "source" or "named" ? TopologyEdgePort.Left : TopologyEdgePort.Auto, prepared.SourcePort);
        Assert.Equal(edge.TargetPort, prepared.TargetPort);
        Assert.Equal(edge.SourcePortId, prepared.SourcePortId);
        Assert.Equal(edge.Waypoints, prepared.Waypoints);
        Assert.False(prepared.Metadata.ContainsKey("layout.route"));
    }

    [Theory]
    [InlineData(TopologyEdgePort.Top)]
    [InlineData(TopologyEdgePort.Bottom)]
    [InlineData(TopologyEdgePort.Left)]
    [InlineData(TopologyEdgePort.Right)]
    public void NativeBranchFansFollowCrossAxisOrderOnEveryPort(TopologyEdgePort port) {
        var acrossX = port is TopologyEdgePort.Top or TopologyEdgePort.Bottom;
        var chart = TopologyChart.Create().WithViewport(600, 500, 20)
            .AddNode("source", "Ready", 250, 220, width: 80, height: 60)
            .AddNode("first", "First", acrossX ? 140 : port == TopologyEdgePort.Left ? 40 : 480,
                acrossX ? port == TopologyEdgePort.Top ? 40 : 400 : 90, width: 80, height: 60)
            .AddNode("second", "Second", acrossX ? 360 : port == TopologyEdgePort.Left ? 40 : 480,
                acrossX ? port == TopologyEdgePort.Top ? 40 : 400 : 350, width: 80, height: 60)
            .AddEdge("a", "source", "first")
            .AddEdge("b", "source", "second");
        var opposite = port switch {
            TopologyEdgePort.Top => TopologyEdgePort.Bottom, TopologyEdgePort.Bottom => TopologyEdgePort.Top,
            TopologyEdgePort.Left => TopologyEdgePort.Right, _ => TopologyEdgePort.Left
        };
        foreach (var edge in chart.Edges) { edge.SourcePort = port; edge.TargetPort = opposite; }
        var svg = XDocument.Parse(chart.ToSvg(new TopologyRenderOptions { IncludeLegend = false }));
        var routes = svg.Descendants().Where(element => Role(element) == "topology-edge-line").Select(PathPoints).ToArray();
        Assert.Equal(2, routes.Length);
        Assert.False(SharesSegment(routes[0], routes[1]), "Native fans must leave distinct branches on the " + port + " port.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("%%{init: {'theme':'dark'}}%%\n")]
    public void StateNodesDrawPlainSurfacesAndMermaidPseudoStates(string directive) {
        var source = directive + "stateDiagram-v2\n  state check <<choice>>\n  [*] --> Idle\n  Idle --> check : submit\n  check --> Done : ok\n  check --> Idle : retry\n  Done --> [*]\n";
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        var chart = Assert.IsType<ChartForgeX.Topology.TopologyChart>(result.Artifact!.Model);
        Assert.All(chart.Nodes, node => Assert.Equal(ChartForgeX.Topology.TopologyNodeKind.Generic, node.Kind));
        Assert.All(chart.Nodes, node => Assert.True(string.IsNullOrWhiteSpace(node.Subtitle)));
        Assert.Equal("choice", chart.Nodes.Single(node => node.Id == "check").Metadata["mermaid.kind"]);
        var svg = XDocument.Parse(result.Artifact.ToSvg());
        Assert.DoesNotContain(svg.Descendants(), element => Role(element) is "topology-node-icon" or "topology-node-badge" or "topology-node-symbol" or "topology-status");
        Assert.All(Edges(svg), edge => Assert.Equal("Solid", (string?)svg.Descendants().Single(element => Role(element) == "topology-edge" && (string?)element.Attribute("data-edge-id") == edge.Id).Attribute("data-edge-line-style")));

        var nodes = svg.Descendants().Where(element => Role(element) == "topology-node").ToDictionary(element => (string)element.Attribute("data-node-id")!);
        var start = nodes.Single(node => node.Key.StartsWith("mermaid-start", StringComparison.Ordinal)).Value;
        var end = nodes.Single(node => node.Key.StartsWith("mermaid-end", StringComparison.Ordinal)).Value;
        Assert.Equal("Dot", (string?)start.Attribute("data-node-display-mode"));
        Assert.Equal("Dot", (string?)end.Attribute("data-node-display-mode"));
        Assert.Single(start.Elements(), element => Role(element) == "topology-node-surface");
        Assert.Equal(2, end.Elements().Count(element => Role(element) == "topology-node-surface"));
        Assert.DoesNotContain(start.Descendants(), element => element.Name.LocalName == "text");
        Assert.Equal("Diamond", (string?)nodes["check"].Attribute("data-node-shape"));

        Assert.False(chart.Nodes.Single(node => node.Id == "check").ShowLabel);
        Assert.Equal("check", chart.Nodes.Single(node => node.Id == "check").Label);
        Assert.DoesNotContain(nodes["check"].Descendants(), element => element.Name.LocalName == "text");
        var bounds = NodeBounds(svg);
        foreach (var id in new[] { "Idle", "Done" }) {
            var text = nodes[id].Descendants().Single(element => element.Name.LocalName == "text");
            var size = Number(text, "font-size");
            var box = bounds[id];
            Assert.InRange(Number(text, "y") - size * .25 - (box.Y + box.Height / 2), -4, 4);
        }

        var light = MermaidRenderer.Render(source.Substring(directive.Length)).Artifact!;
        var nativeLight = VisualTheme.Graphite().Resolve(VisualThemeMode.Light).Background;
        var expected = directive.Length > 0 ? ChartColor.FromHex("#333333") : nativeLight;
        Assert.Equal(directive.Length > 0 ? VisualThemeMode.Dark : VisualThemeMode.Light, chart.ThemeMode);
        Assert.Equal(expected, ChartColor.Parse(Background(svg)));
        Assert.Equal(expected, CornerColor(result.Artifact.ToPng()));
        Assert.Equal(nativeLight, ChartColor.Parse(Background(XDocument.Parse(light.ToSvg()))));
        Assert.Equal(nativeLight, CornerColor(light.ToPng()));
        if (directive.Length > 0) {
            Assert.NotEqual(nativeLight, expected);
            Assert.NotEqual(light.ToPng(), result.Artifact.ToPng());
        }
    }

    [Theory]
    [InlineData("pie title Output by surface\n  \"SVG\" : 45\n  \"PNG\" : 30\n", "Output by surface", false)]
    [InlineData("pie showData title \"Output by surface\"\n  \"SVG\" : 45\n", "Output by surface", true)]
    [InlineData("pie showData\n  title Output by surface\n  \"SVG\" : 45\n", "Output by surface", true)]
    public void PieHeaderTitleIsTheChartTitle(string source, string title, bool showData) {
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        var pie = Assert.IsType<MermaidPieDocument>(result.Document);
        Assert.Equal(title, pie.Title);
        Assert.Equal(showData, pie.ShowData);
        Assert.Equal(title, result.Artifact!.Title);
        Assert.DoesNotContain(Texts(XDocument.Parse(result.Artifact.ToSvg())), text => text.StartsWith("pie", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("sankey-beta\nSources,Charts,10\nCharts,Website,8\n")]
    [InlineData("pie\n  \"SVG\" : 45\n")]
    [InlineData("flowchart LR\n  a --> b\n")]
    [InlineData("sequenceDiagram\n  A->>B: Hi\n")]
    [InlineData("stateDiagram-v2\n  [*] --> Ready\n")]
    [InlineData("xychart-beta\n  x-axis [a, b]\n  bar [1, 2]\n")]
    [InlineData("treemap-beta\n\"Root\"\n  \"Leaf\": 10\n")]
    [InlineData("gitGraph\n  commit\n")]
    public void FrontMatterTitleNamesEveryFamily(string body) {
        var result = MermaidRenderer.Render("---\ntitle: \"Report flow\"\n---\n" + body);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        Assert.Equal("Report flow", result.Artifact!.Title);
        Assert.Contains("Report flow", Texts(XDocument.Parse(result.Artifact.ToSvg())));
    }

    [Theory]
    [InlineData("gantt\n  dateFormat YYYY-MM-DD\n  section A\n  Task :a1, 2026-01-01, 5d\n", "gantt")]
    [InlineData("xychart-beta\n  x-axis [a, b]\n  bar [1, 2]\n", "xychart-beta")]
    [InlineData("flowchart TD\n  a --> b\n", "flowchart TD")]
    [InlineData("sequenceDiagram\n  A->>B: Hi\n", "sequenceDiagram")]
    [InlineData("sankey-beta\nA,B,1\n", "sankey-beta")]
    [InlineData("stateDiagram-v2\n  [*] --> Ready\n", "stateDiagram-v2")]
    [InlineData("pie\n  \"SVG\" : 45\n", "pie")]
    [InlineData("gitGraph\n  commit\n", "gitGraph")]
    public void HeaderKeywordIsNotRenderedAsASubtitle(string source, string header) {
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        var artifact = result.Artifact!;
        Assert.Equal(string.Empty, artifact.Subtitle);
        Assert.Equal(header, artifact.Metadata["mermaid.header"]);
        Assert.DoesNotContain(header, Texts(XDocument.Parse(artifact.ToSvg())));
    }

    [Fact]
    public void CallerSubtitleRemainsOptIn() {
        var options = new MermaidRenderOptions {
            Flowchart = new MermaidFlowchartRenderOptions { Subtitle = "Generated nightly" },
            Pie = new MermaidPieRenderOptions { Subtitle = "By surface" }
        };
        var flowchart = MermaidRenderer.Render("flowchart TD\n  a --> b\n", options).Artifact!;
        var pie = MermaidRenderer.Render("pie\n  \"SVG\" : 45\n", options).Artifact!;
        Assert.Contains("Generated nightly", Texts(XDocument.Parse(flowchart.ToSvg())));
        Assert.Contains("By surface", Texts(XDocument.Parse(pie.ToSvg())));
    }

    [Fact]
    public void SequenceThemeFollowsSourceInSvgAndPngRegardlessOfRenderOrder() {
        const string source = "sequenceDiagram\n  autonumber\n  participant U as User\n  participant S as Site\n  U->>S: Request\n  S-->>U: Report\n";
        var dark = MermaidRenderer.Render("%%{init: {'theme':'dark'}}%%\n" + source).Artifact!;
        var light = MermaidRenderer.Render(source).Artifact!;
        var darkSvg = dark.ToSvg();
        var lightSvg = light.ToSvg();
        Assert.NotEqual(darkSvg, lightSvg);
        // The finite dark preset is exactly #333333 (luminance .2), rather than the native Graphite dark canvas.
        var expectedLight = ChartColor.FromHex("#FFFFFF");
        var expectedDark = ChartColor.FromHex("#333333");
        Assert.Equal(expectedLight, ChartColor.Parse(Background(XDocument.Parse(lightSvg))));
        Assert.Equal(expectedDark, ChartColor.Parse(Background(XDocument.Parse(darkSvg))));
        var lightPng = light.ToPng(); var darkPng = dark.ToPng();
        Assert.Equal(expectedLight, CornerColor(lightPng));
        Assert.Equal(expectedDark, CornerColor(darkPng));
        Assert.NotEqual(darkPng, lightPng);
    }

    private static XDocument Svg(string source) {
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(item => item.Message)));
        return XDocument.Parse(result.Artifact!.ToSvg());
    }

    private static string? Role(XElement element) => (string?)element.Attribute("data-cfx-role");

    private static IEnumerable<string> Texts(XDocument svg) => svg.Descendants().Where(element => element.Name.LocalName == "text").Select(element => element.Value);

    private static Dictionary<string, Box> NodeBounds(XDocument svg) {
        var result = new Dictionary<string, Box>(StringComparer.Ordinal);
        foreach (var node in svg.Descendants().Where(element => Role(element) == "topology-node")) {
            var surfaces = node.Elements().Where(element => Role(element) == "topology-node-surface").ToArray();
            var points = surfaces.SelectMany(surface => surface.Name.LocalName switch {
                "rect" => Corners(Rect(surface)),
                "ellipse" => Corners(new Box(Number(surface, "cx") - Number(surface, "rx"), Number(surface, "cy") - Number(surface, "ry"), Number(surface, "rx") * 2, Number(surface, "ry") * 2)),
                _ => PathPoints(surface)
            }).ToArray();
            result[(string)node.Attribute("data-node-id")!] = new Box(points.Min(p => p.X), points.Min(p => p.Y), points.Max(p => p.X) - points.Min(p => p.X), points.Max(p => p.Y) - points.Min(p => p.Y));
        }

        return result;
    }

    private static List<EdgeRoute> Edges(XDocument svg) => svg.Descendants().Where(element => Role(element) == "topology-edge").Select(edge => new EdgeRoute(
        (string)edge.Attribute("data-edge-id")!,
        (string)edge.Attribute("data-cfx-meta-mermaid-source")!,
        (string)edge.Attribute("data-cfx-meta-mermaid-target")!,
        PathPoints(edge.Descendants().First(element => Role(element) == "topology-edge-line")))).ToList();

    private static Point[] PathPoints(XElement path) {
        // Sample the painted curve, not the off-curve Bezier control polygon.
        return ChartForgeX.Core.ChartMapPathParser.ParseSubpaths((string)path.Attribute("d")!, pixelsPerUnit: 8)
            .SelectMany(subpath => subpath.Points).Select(point => new Point(point.X, point.Y)).ToArray();
    }

    private static Box Rect(XElement element) => new(Number(element, "x"), Number(element, "y"), Number(element, "width"), Number(element, "height"));

    private static IEnumerable<Point> Corners(Box box) => new[] { new Point(box.X, box.Y), new Point(box.X + box.Width, box.Y + box.Height) };

    private static double Number(XElement element, string name) => double.Parse((string)element.Attribute(name)!, CultureInfo.InvariantCulture);

    private static Box Deflate(Box box, double amount) => new(box.X + amount, box.Y + amount, Math.Max(0, box.Width - amount * 2), Math.Max(0, box.Height - amount * 2));

    private static bool Overlaps(Box a, Box b) => a.X < b.X + b.Width && b.X < a.X + a.Width && a.Y < b.Y + b.Height && b.Y < a.Y + a.Height;

    // Curved routes are sampled into oblique segments; intersect the segment itself, not its bounding rectangle.
    private static bool Crosses(IReadOnlyList<Point> points, Box box) {
        for (var i = 0; i < points.Count - 1; i++) {
            var start = points[i]; var end = points[i + 1]; var enter = 0d; var leave = 1d;
            bool Clip(double direction, double distance) {
                if (direction == 0) return distance >= 0;
                var along = distance / direction;
                if (direction < 0) { if (along > leave) return false; enter = Math.Max(enter, along); }
                else { if (along < enter) return false; leave = Math.Min(leave, along); }
                return true;
            }
            if (Clip(start.X - end.X, start.X - box.X) && Clip(end.X - start.X, box.X + box.Width - start.X)
                && Clip(start.Y - end.Y, start.Y - box.Y) && Clip(end.Y - start.Y, box.Y + box.Height - start.Y)) return true;
        }
        return false;
    }

    private static bool SharesSegment(IReadOnlyList<Point> first, IReadOnlyList<Point> second) {
        for (var i = 0; i < first.Count - 1; i++) {
            for (var j = 0; j < second.Count - 1; j++) {
                var a = (first[i], first[i + 1]);
                var b = (second[j], second[j + 1]);
                var vertical = Math.Abs(a.Item1.X - a.Item2.X) < .5 && Math.Abs(b.Item1.X - b.Item2.X) < .5 && Math.Abs(a.Item1.X - b.Item1.X) < .5;
                var horizontal = Math.Abs(a.Item1.Y - a.Item2.Y) < .5 && Math.Abs(b.Item1.Y - b.Item2.Y) < .5 && Math.Abs(a.Item1.Y - b.Item1.Y) < .5;
                double Overlap(double a1, double a2, double b1, double b2) => Math.Min(Math.Max(a1, a2), Math.Max(b1, b2)) - Math.Max(Math.Min(a1, a2), Math.Min(b1, b2));
                if (vertical && Overlap(a.Item1.Y, a.Item2.Y, b.Item1.Y, b.Item2.Y) > 2) return true;
                if (horizontal && Overlap(a.Item1.X, a.Item2.X, b.Item1.X, b.Item2.X) > 2) return true;
            }
        }

        return false;
    }

    private static string Background(XDocument svg) => (string)svg.Descendants().First(element => Role(element) == "background").Attribute("fill")!;

    private static ChartColor CornerColor(byte[] png) {
        var image = RasterImageDecoder.Decode(png);
        var offset = (8 * image.Width + 8) * 4;
        return ChartColor.FromRgba(image.Pixels[offset], image.Pixels[offset + 1], image.Pixels[offset + 2], image.Pixels[offset + 3]);
    }

    private readonly record struct Point(double X, double Y);

    private readonly record struct Box(double X, double Y, double Width, double Height);

    private sealed record EdgeRoute(string Id, string Source, string Target, Point[] Points);
}
