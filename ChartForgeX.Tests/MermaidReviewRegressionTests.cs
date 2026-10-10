using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Core;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Raster;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using ChartForgeX.VisualBlocks;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class MermaidReviewRegressionTests {
    [Fact]
    public void ClassInheritanceAndAggregationHaveSeparateSourceMarkers() {
        var result = MermaidRenderer.Render("classDiagram\nnamespace Services {\nclass User {\n+string name\n+save() void\n}\nclass Admin\n}\n<<interface>> User\nUser <|-- Admin\nUser \"1\" o-- \"0..*\" Session : opens");
        Assert.False(result.HasErrors);
        var svg = XDocument.Parse(result.Artifact!.ToSvg());
        var edges = svg.Descendants().Where(element => (string?)element.Attribute("data-cfx-role") == "topology-edge").ToArray();
        Assert.Equal(new[] { "OpenTriangle", "OpenDiamond" }, edges.Select(element => (string?)element.Attribute("data-source-marker")));
        var first = Number(edges[0], "data-route-start-y");
        var second = Number(edges[1], "data-route-start-y");
        Assert.True(Math.Abs(first - second) >= 16, "Different UML source markers must not cover each other.");
        var multiplicity = svg.Descendants().Single(element => element.Name.LocalName == "text" && element.AncestorsAndSelf().Any(owner => (string?)owner.Attribute("data-cfx-role") == "topology-endpoint-label") && element.Value == "1");
        foreach (var edge in edges) {
            var dx = Number(multiplicity, "x") - Number(edge, "data-route-start-x");
            var dy = Number(multiplicity, "y") - Number(edge, "data-route-start-y");
            Assert.True(Math.Sqrt(dx * dx + dy * dy) >= 20, "A multiplicity must not cover a neighboring relationship marker.");
            var path = edge.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
            AssertPathAvoidsLabel(path, multiplicity, 9.5);
        }
    }

    [Theory]
    [InlineData("TB", false, "User")]
    [InlineData("TB", true, "User")]
    [InlineData("BT", false, "User")]
    [InlineData("BT", true, "User")]
    [InlineData("LR", false, "User")]
    [InlineData("TB", false, "Project administrator")]
    public void UseCaseAssociationsAvoidTheActorCaption(string direction, bool incoming, string actorLabel) {
        var source = "usecase-beta\n" + (direction == "TB" ? "" : "direction " + direction + "\n") + "actor User[\"" + actorLabel + "\"]\n" +
            (incoming ? "Action(Do work) --> User" : "User --> Action(Do work)");
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors);
        var artifact = result.Artifact!;
        var svg = XDocument.Parse(artifact.ToSvg());
        var body = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node" && (string?)element.Attribute("data-node-id") == "User");
        var path = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
        foreach (var label in body.Descendants().Where(element => element.Name.LocalName == "text" && element.Ancestors().Any(owner => (string?)owner.Attribute("data-cfx-role") == "topology-node-label"))) {
            AssertPathAvoidsLabel(path, label, 12);
        }
        var chart = Assert.IsType<TopologyChart>(artifact.Model);
        var prepared = TopologyLayoutEngine.Prepare(chart, options: chart.ResolveRenderOptions(null));
        var nodes = prepared.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        var points = TopologyRenderPrimitives.EdgePoints(prepared, prepared.Edges[0], nodes);
        Assert.InRange(DistanceFromSurface(nodes["User"], incoming ? points[^1] : points[0]), 0.5, 8);
    }

    [Theory]
    [InlineData(210, 130, TopologyEdgeRouting.Orthogonal)]
    [InlineData(-210, 130, TopologyEdgeRouting.Orthogonal)]
    [InlineData(130, 210, TopologyEdgeRouting.Orthogonal)]
    [InlineData(130, -210, TopologyEdgeRouting.Orthogonal)]
    [InlineData(210, 130, TopologyEdgeRouting.Straight)]
    [InlineData(210, 130, TopologyEdgeRouting.Curved)]
    public void AssociationsAttachToPaintedSurfacesInTheirFinalRouteDirection(int dx, int dy, TopologyEdgeRouting routing) {
        foreach (var shape in new[] { TopologyNodeShape.Actor, TopologyNodeShape.Ellipse, TopologyNodeShape.Diamond }) {
            var chart = TopologyChart.Create().WithViewport(800, 700, 20)
                .AddNode("a", "A", 300, 300, width: 180, height: 108)
                .AddNode("b", "B", 300 + dx, 300 + dy, width: 180, height: 108)
                .AddEdge("ab", "a", "b");
            foreach (var node in chart.Nodes) node.Shape = shape;
            chart.Edges[0].Routing = routing;
            var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
            var points = TopologyRenderPrimitives.EdgePoints(chart, chart.Edges[0], nodes);
            Assert.InRange(DistanceFromSurface(chart.Nodes[0], points[0]), 0.5, 8);
            Assert.InRange(DistanceFromSurface(chart.Nodes[1], points[^1]), 0.5, 8);
            if (routing == TopologyEdgeRouting.Orthogonal) {
                Assert.True(Math.Abs(points[0].X - points[1].X) < .001 || Math.Abs(points[0].Y - points[1].Y) < .001);
                Assert.True(Math.Abs(points[^1].X - points[^2].X) < .001 || Math.Abs(points[^1].Y - points[^2].Y) < .001);
            }
        }
    }

    [Fact]
    public void MultiplicitiesStayOutsideClassCardsAndHaveVisibleRasterInk() {
        var result = MermaidRenderer.Render("classDiagram\nnamespace Domain {\nclass Customer {\n+string name\n}\nclass Order {\n+string id\n}\nCustomer \"1\" o-- \"0..*\" Order : places\n}");
        Assert.False(result.HasErrors);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        var svg = XDocument.Parse(artifact.ToSvg());
        var envelope = artifact.ToInterchangeEnvelope();
        var labels = svg.Descendants().Where(element => element.Name.LocalName == "text" && element.AncestorsAndSelf().Any(owner => (string?)owner.Attribute("data-cfx-role") == "topology-endpoint-label")).ToArray();
        Assert.Equal(2, labels.Length);
        var image = RasterImageDecoder.Decode(artifact.ToPng());
        foreach (var label in labels) {
            var x = double.Parse(label.Attribute("x")!.Value, CultureInfo.InvariantCulture);
            var y = double.Parse(label.Attribute("y")!.Value, CultureInfo.InvariantCulture);
            var halfWidth = RgbaCanvas.MeasureTextEmphasizedWidth(label.Value, 9.5, null) / 2 + 2;
            foreach (var node in envelope.Nodes) {
                Assert.False(x + halfWidth > node.X && x - halfWidth < node.X + node.Width && y + 7 > node.Y && y - 7 < node.Y + node.Height,
                    "Multiplicity " + label.Value + " must not be covered by " + node.Label);
            }
            var ink = 0;
            for (var py = Math.Max(0, (int)y - 7); py <= Math.Min(image.Height - 1, (int)y + 7); py++) {
                for (var px = Math.Max(0, (int)(x - halfWidth)); px <= Math.Min(image.Width - 1, (int)(x + halfWidth)); px++) {
                    var offset = (py * image.Width + px) * 4;
                    if (image.Pixels[offset] < 180 && image.Pixels[offset + 1] < 180 && image.Pixels[offset + 2] < 180) ink++;
                }
            }
            Assert.True(ink > 5, "Multiplicity " + label.Value + " must have visible PNG glyphs.");
        }
    }

    [Theory]
    [InlineData(0, TopologyEdgeRouting.Orthogonal)]
    [InlineData(12, TopologyEdgeRouting.Orthogonal)]
    [InlineData(22, TopologyEdgeRouting.Orthogonal)]
    [InlineData(0, TopologyEdgeRouting.Straight)]
    [InlineData(12, TopologyEdgeRouting.Straight)]
    [InlineData(22, TopologyEdgeRouting.Straight)]
    public void ShortActorAssociationsKeepCaptionsClearAndApproachBothPaintedEndpoints(int gap, TopologyEdgeRouting routing) {
        foreach (var incoming in new[] { false, true }) {
            foreach (var caption in new[] { "User", "Project administrator" }) {
                var chart = TopologyChart.Create().WithViewport(800, 700, 20)
                    .AddNode("actor", caption, 300, 300, width: 180, height: 108)
                    .AddNode("action", "Do work", 300, 408 + gap, width: 180, height: 70)
                    .AddEdge("association", incoming ? "action" : "actor", incoming ? "actor" : "action");
                chart.Nodes[0].Shape = TopologyNodeShape.Actor;
                chart.Nodes[1].Shape = TopologyNodeShape.Ellipse;
                foreach (var node in chart.Nodes) { node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false; }
                chart.Edges[0].Routing = routing;
                chart.Edges[0].Direction = VisualLinkDirection.Forward;
                var svg = XDocument.Parse(chart.ToSvg());
                var path = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-edge-line");
                var actor = svg.Descendants().Single(element => (string?)element.Attribute("data-cfx-role") == "topology-node" && (string?)element.Attribute("data-node-id") == "actor");
                foreach (var label in actor.Descendants().Where(element => element.Name.LocalName == "text" && element.Ancestors().Any(owner => (string?)owner.Attribute("data-cfx-role") == "topology-node-label"))) AssertPathAvoidsLabel(path, label, 12);
                var points = Assert.Single(ChartMapPathParser.ParseSubpaths(path.Attribute("d")!.Value, 1)).Points;
                AssertApproachHitsSurface(chart.Nodes[incoming ? 1 : 0], points[0], points[1]);
                AssertApproachHitsSurface(chart.Nodes[incoming ? 0 : 1], points[^1], points[^2]);
                if (routing == TopologyEdgeRouting.Orthogonal) {
                    for (var i = 1; i < points.Count; i++) Assert.True(Math.Abs(points[i].X - points[i - 1].X) < .001 || Math.Abs(points[i].Y - points[i - 1].Y) < .001);
                }
            }
        }
    }

    [Theory]
    [InlineData("block-basic.mmd")]
    [InlineData("packet-basic.mmd")]
    [InlineData("gitgraph-basic.mmd")]
    [InlineData("venn-basic.mmd")]
    [InlineData("ishikawa-basic.mmd")]
    [InlineData("wardley-basic.mmd")]
    public void VisualBlockFamiliesRetainAuthoredAccessibilityInSvgAndHtml(string fixture) {
        var root = new DirectoryInfo(TestRepository.Root);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "ChartForgeX.sln"))) root = root.Parent;
        Assert.NotNull(root);
        var source = File.ReadAllText(Path.Combine(root!.FullName, "tests", "mermaid-conformance", "fixtures", fixture));
        var split = source.IndexOf('\n');
        source = source.Insert(split + 1, "accTitle: Authored name\naccDescr: Authored description\n");
        var result = MermaidRenderer.Render(source);
        Assert.False(result.HasErrors, string.Join("; ", result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var block = Assert.IsAssignableFrom<IVisualBlock>(result.Artifact!.Model);
        Assert.Equal("Authored name", block.AccessibleName);
        foreach (var output in new[] { result.Artifact.ToSvg(), result.Artifact.ToHtmlPage() }) {
            Assert.Contains(">Authored name</title>", output);
            Assert.Contains(">Authored description</desc>", output);
        }
    }

    [Fact]
    public void UseCaseStereotypesStayBesideTheirRelationshipInsideTheBoundary() {
        var result = MermaidRenderer.Render("usecase-beta\ndirection LR\nsystemBoundary App\nA(First)\nB(Second)\nend\nA ..> : include B");
        Assert.False(result.HasErrors);
        var artifact = Assert.IsType<VisualArtifact>(result.Artifact);
        var group = Assert.Single(artifact.ToInterchangeEnvelope().Groups);
        var label = XDocument.Parse(artifact.ToSvg()).Descendants().Single(element => element.Name.LocalName == "text" && element.Value == "<<include>>");
        var y = double.Parse(label.Attribute("y")!.Value, CultureInfo.InvariantCulture);
        Assert.NotNull(group.Y);
        Assert.NotNull(group.Height);
        Assert.InRange(y, group.Y!.Value + 68, group.Y.Value + group.Height!.Value - 8);
    }

    private static double Number(XElement element, string attribute) => double.Parse(element.Attribute(attribute)!.Value, CultureInfo.InvariantCulture);

    private static void AssertPathAvoidsLabel(XElement path, XElement label, double fontSize) {
        var x = Number(label, "x"); var y = Number(label, "y");
        var halfWidth = RgbaCanvas.MeasureTextEmphasizedWidth(label.Value, fontSize, null) / 2 + 3;
        foreach (var subpath in ChartMapPathParser.ParseSubpaths(path.Attribute("d")!.Value, 1)) {
            for (var i = 1; i < subpath.Points.Count; i++) {
                var start = subpath.Points[i - 1]; var end = subpath.Points[i];
                for (var step = 0; step <= 100; step++) {
                    var px = start.X + (end.X - start.X) * step / 100;
                    var py = start.Y + (end.Y - start.Y) * step / 100;
                    Assert.False(px >= x - halfWidth && px <= x + halfWidth && py >= y - fontSize && py <= y + 3,
                        "A relationship must leave its neighboring labels readable.");
                }
            }
        }
    }

    private static double DistanceFromSurface(TopologyNode node, ChartPoint point) {
        var distance = double.PositiveInfinity;
        foreach (var path in ChartMapPathParser.ParseSubpaths(TopologyNodeShapeGeometry.Path(node), 1)) {
            for (var i = 1; i < path.Points.Count; i++) Measure(path.Points[i - 1], path.Points[i]);
            if (path.IsClosed && path.Points.Count > 1) Measure(path.Points[^1], path.Points[0]);
        }
        return distance;
        void Measure(ChartPoint start, ChartPoint end) {
            var dx = end.X - start.X; var dy = end.Y - start.Y;
            var denominator = dx * dx + dy * dy;
            var t = denominator < .000001 ? 0 : Math.Max(0, Math.Min(1, ((point.X - start.X) * dx + (point.Y - start.Y) * dy) / denominator));
            var px = point.X - start.X - t * dx; var py = point.Y - start.Y - t * dy;
            distance = Math.Min(distance, Math.Sqrt(px * px + py * py));
        }
    }

    private static void AssertApproachHitsSurface(TopologyNode node, ChartPoint endpoint, ChartPoint adjacent) {
        var dx = endpoint.X - adjacent.X; var dy = endpoint.Y - adjacent.Y;
        var length = Math.Sqrt(dx * dx + dy * dy);
        Assert.True(length > .001, "An endpoint must have a directed approach leg.");
        var closest = double.PositiveInfinity;
        for (var step = 0; step <= 32; step++) closest = Math.Min(closest,
            DistanceFromSurface(node, new ChartPoint(endpoint.X + dx / length * step / 2, endpoint.Y + dy / length * step / 2)));
        Assert.True(closest <= 1, "The rendered approach must continue into the painted node surface: " + node.Id);
    }
}
