using System.Globalization;
using System.Xml.Linq;
using ChartForgeX.Mermaid;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class DiagramDesignTests {
    [Theory]
    [InlineData("classDiagram", "class Order\nclass Customer\nCustomer --> Order")]
    [InlineData("erDiagram", "CUSTOMER ||--o{ ORDER : places")]
    [InlineData("stateDiagram-v2", "Ready --> Done")]
    [InlineData("C4Container", "Container(api, \"API\", \"HTTP\")\nContainerDb(db, \"Store\", \"SQL\")\nRel(api, db, \"Saves\")")]
    public void AutomaticDirectionsFollowTheHostAspectAndExplicitDirectionsRemainAuthoritative(string header, string body) {
        foreach (var width in new[] { 390, 1100 }) {
            var topology = new MermaidTopologyRenderOptions { Width = width, Height = 620 };
            var options = new MermaidRenderOptions { Class = topology, State = topology, EntityRelationship = topology, C4 = topology };
            var automatic = MermaidRenderer.Render(header + "\n" + body, options);
            Assert.False(automatic.HasErrors);
            Assert.Equal(width == 390 ? TopologyLayoutDirection.TopToBottom : TopologyLayoutDirection.LeftToRight,
                Assert.IsType<TopologyChart>(automatic.Artifact!.Model).LayoutDirection);
            var authored = MermaidRenderer.Render(header + "\ndirection BT\n" + body, options);
            Assert.False(authored.HasErrors);
            Assert.Equal(TopologyLayoutDirection.BottomToTop, Assert.IsType<TopologyChart>(authored.Artifact!.Model).LayoutDirection);
        }
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void RankedEdgesEnterFacingSidesAtDistinctPortsAndOpaqueLabelsCoverTheirOwnMidpoint(bool dark) {
        const string source = "flowchart TD\na[First] -->|first path| c[Complete]\nb[Second] -->|second path| c";
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions {
            Presentation = new MermaidPresentationOptions { ThemeMode = dark ? VisualThemeMode.Dark : VisualThemeMode.Light }
        });
        Assert.Empty(result.Diagnostics);
        var chart = Assert.IsType<TopologyChart>(result.Artifact!.Model);
        var prepared = chart.Prepare();
        var envelope = prepared.ToInterchangeEnvelope();
        var target = Assert.Single(prepared.Analyze().Nodes, node => node.Id == "c").Bounds;
        var endpoints = envelope.Edges.Select(edge => edge.ResolvedRoute[^1]).ToArray();
        Assert.All(endpoints, point => Assert.InRange(Math.Abs(point.Y - target.Top), 0, .001));
        Assert.True(Math.Abs(endpoints[0].X - endpoints[1].X) >= 8);
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(node => Role(node) == "topology-marker"));
        var ordered = svg.Descendants().ToArray();
        foreach (var label in ordered.Where(node => Role(node) == "topology-edge-label")) {
            var pill = Assert.Single(label.Descendants(), node => Role(node) == "topology-edge-label-surface");
            Assert.True(ChartColor.TryParse((string?)pill.Attribute("fill"), out var fill));
            Assert.Equal(255, fill.A);
            var colors = chart.Theme.Resolve(chart.ThemeMode);
            Assert.Equal(ChartColorMath.Blend(colors.Background, colors.Foreground, TopologyRenderPrimitives.EdgeLabelQuietTint), fill);
            var id = (string)label.Attribute("data-edge-id")!;
            var edge = Assert.Single(envelope.Edges, item => item.Id == id);
            var x = Number(label, "data-label-x"); var y = Number(label, "data-label-y");
            var nearest = edge.ResolvedRoute.Zip(edge.ResolvedRoute.Skip(1)).Min(pair =>
                DistanceToSegment(x, y, pair.First.X, pair.First.Y, pair.Second.X, pair.Second.Y));
            Assert.True(nearest < .01, id + " label (" + x + ", " + y + ") must sit on its route; distance=" + nearest);
            Assert.True(Array.IndexOf(ordered, pill) > Array.FindLastIndex(ordered, node => Role(node) == "topology-edge-line"));
        }
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void ClassContentHasMeasuredNameAttributeAndMethodCompartmentsThatSurviveInterchange() {
        const string source = "classDiagram\nclass Order {\n+string Id\n+Save() bool\n}";
        var document = new MermaidParser().ParseClass(source).Document!;
        var chart = document.ToTopologyChart();
        var prepared = chart.Prepare();
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(2, svg.Descendants().Count(node => Role(node) == "topology-node-detail-separator"));
        var bounds = Assert.Single(prepared.Analyze().Nodes).Bounds;
        Assert.InRange(bounds.Height, 45, 100);
        var labels = svg.Descendants().Where(node => Role(node) == "topology-node-detail").ToArray();
        Assert.Equal(new[] { "+string Id", "+Save() bool" }, labels.Select(node => node.Value));
        var memberText = labels.SelectMany(node => node.DescendantsAndSelf()).Where(node => node.Attribute("font-size") != null).ToArray();
        Assert.Equal(2, memberText.Length);
        Assert.All(memberText, node => Assert.InRange(Number(node, "font-size"), 10, 12));
        var restored = VisualArtifactInterchangeEnvelope.FromJson(prepared.ToInterchangeEnvelope().ToJson()).ToTopologyChart();
        Assert.True(Assert.Single(restored.Nodes).Details[1].StartsSection);
        Assert.Equal(prepared.ToPng(), restored.Prepare().ToPng());
    }

    [Fact]
    public void EntityAttributesUseNativeTypeNameKeyCellsAndEmptyEntitiesStayCompact() {
        const string source = "erDiagram\nCUSTOMER {\nstring id PK\nstring name\n}\nCUSTOMER ||--o{ ORDER : places";
        var chart = new MermaidParser().ParseEntityRelationship(source).Document!.ToTopologyChart();
        var table = chart.Nodes.Single(node => node.Id == "CUSTOMER").TableContent!;
        Assert.Equal(new[] { "type", "name", "key" }, table.Columns.Select(column => column.Id));
        Assert.Equal(new[] { "string", "id", "PK" }, table.Rows[0].Cells.Select(cell => cell.DisplayText));
        var prepared = chart.Prepare();
        var svg = XDocument.Parse(prepared.ToSvg());
        Assert.Equal(6, svg.Descendants().Count(node => Role(node) == "table-cell"));
        Assert.True(prepared.Analyze().Nodes.Single(node => node.Id == "ORDER").Bounds.Height < 50);
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void CompactScheduleWrapsCompleteRowNamesAndOmitsARedundantLegend() {
        // Row titles no longer carry their section, so a long title exercises the wrap.
        const string source = "gantt\nsection Engine\nLabel placement and collision review :labels,2026-01-01,3d\nRoute layout :route,after labels,2d";
        var chart = new MermaidParser().ParseGantt(source).Document!.ToChart(new MermaidGanttRenderOptions { Width = 390 });
        var prepared = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(node => node.Role == "schedule-row-label").ToArray();
        Assert.Equal(2, labels.Length);
        Assert.Contains(labels, label => label.Text.Lines.Count > 1);
        Assert.Equal(chart.Series.Select(series => series.Name), labels.Select(label => string.Join(" ", label.Text.Lines.Select(line => line.Text))));
        Assert.DoesNotContain(prepared.Scene.Nodes, node => node.Role == "legend-entry");
        Assert.Contains(prepared.Scene.Nodes, node => node.Role == "gantt-section-band");
        Assert.NotEmpty(prepared.ToPng());
        chart.WithAxes(false).WithLegend(true);
        var withoutRows = chart.Prepare(VisualExportRequest.ForChart(chart).Context);
        Assert.DoesNotContain(withoutRows.Scene.Nodes, node => node.Role == "schedule-row-label");
        Assert.Equal(2, withoutRows.Scene.Nodes.Count(node => node.Role == "legend-entry"));
    }

    [Fact]
    public void RelationshipsReachEntityBordersFromOutsideSoTheirCardinalityStaysVisible() {
        // Two relationships share LINE_ITEM's top; the corridor search once let PRODUCT's run pass below that
        // border and turn back up, drawing its crow's foot inside the entity where the card fill hides it.
        const string source = "erDiagram\nCUSTOMER ||--o{ ORDER : places\nORDER ||--|{ LINE_ITEM : contains\nPRODUCT ||--o{ LINE_ITEM : \"ordered in\"\n"
            + "CUSTOMER {\nstring id PK\nstring name\nstring email\n}\nORDER {\nstring id PK\nstring customerId FK\ndate placed\n}\n";
        var result = MermaidRenderer.Render(source, new MermaidRenderOptions { EntityRelationship = new MermaidTopologyRenderOptions { Width = 760, Height = 570 } });
        Assert.False(result.HasErrors);
        var prepared = Assert.IsType<TopologyChart>(result.Artifact!.Model).Prepare();
        var nodes = prepared.Analyze().Nodes.ToDictionary(node => node.Id, node => node.Bounds);
        foreach (var edge in prepared.ToInterchangeEnvelope().Edges) {
            var route = edge.ResolvedRoute;
            Assert.False(Inside(nodes[edge.SourceId], route[1].X, route[1].Y), edge.Id + " leaves its source from inside.");
            Assert.False(Inside(nodes[edge.TargetId], route[^2].X, route[^2].Y), edge.Id + " approaches its target from inside.");
        }

        static bool Inside(ChartRect bounds, double x, double y) => x > bounds.Left + .5 && x < bounds.Right - .5 && y > bounds.Top + .5 && y < bounds.Bottom - .5;
    }

    private static string? Role(XElement node) => (string?)node.Attribute("data-cfx-role");
    private static double Number(XElement node, string attribute) => double.Parse((string)node.Attribute(attribute)!, CultureInfo.InvariantCulture);
    private static double DistanceToSegment(double x, double y, double ax, double ay, double bx, double by) {
        var dx = bx - ax; var dy = by - ay;
        var squared = dx * dx + dy * dy;
        var t = squared == 0 ? 0 : Math.Clamp(((x - ax) * dx + (y - ay) * dy) / squared, 0, 1);
        return Math.Sqrt(Math.Pow(x - ax - t * dx, 2) + Math.Pow(y - ay - t * dy, 2));
    }
}
