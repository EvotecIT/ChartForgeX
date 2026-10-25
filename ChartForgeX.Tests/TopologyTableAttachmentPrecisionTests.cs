using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyTableAttachmentPrecisionTests {
    [Theory]
    [InlineData(TopologyNodeShape.Rectangle, TopologyEdgePort.Right, false)]
    [InlineData(TopologyNodeShape.Rectangle, TopologyEdgePort.Right, true)]
    [InlineData(TopologyNodeShape.Rectangle, TopologyEdgePort.Bottom, false)]
    [InlineData(TopologyNodeShape.Rectangle, TopologyEdgePort.Bottom, true)]
    [InlineData(TopologyNodeShape.Rounded, TopologyEdgePort.Right, false)]
    [InlineData(TopologyNodeShape.Rounded, TopologyEdgePort.Right, true)]
    [InlineData(TopologyNodeShape.Rounded, TopologyEdgePort.Bottom, false)]
    [InlineData(TopologyNodeShape.Rounded, TopologyEdgePort.Bottom, true)]
    public void FractionalTableBodyUsesItsActualNativeBoundaryForAxisAndNamedPortEndpoints(TopologyNodeShape shape, TopologyEdgePort side, bool named) {
        var chart = Source(shape, side, named, table: true); var prepared = chart.Prepare(Options());
        var facts = prepared.ToInterchangeEnvelope(); var node = facts.Nodes.Single(item => item.Id == "facts");
        var endpoint = facts.Edges.Single(edge => edge.Id == "arrive").ResolvedRoute[^1];
        Capture(prepared, "table-attachment-" + shape + "-" + side + (named ? "-named" : "-axis"));
        // Standard native routes retain a seven-unit endpoint gap outside the actual numeric surface.
        // The exact surface plus that gap, not a serialized 0.### path, is geometry authority.
        if (side == TopologyEdgePort.Right) Assert.Equal(node.X!.Value + node.Width!.Value + 7, endpoint.X);
        else Assert.Equal(node.Y!.Value + node.Height!.Value + 7, endpoint.Y);
        if (named) {
            if (side == TopologyEdgePort.Right) Assert.Equal(node.Y!.Value + node.Height!.Value * .5, endpoint.Y);
            else Assert.Equal(node.X!.Value + node.Width!.Value * .5, endpoint.X);
        }
        Assert.False(endpoint.X > node.X!.Value && endpoint.X < node.X.Value + node.Width!.Value
            && endpoint.Y > node.Y!.Value && endpoint.Y < node.Y.Value + node.Height!.Value);
        Assert.Equal(2, prepared.Visual.Scene.Regions.Count(region => region.Role == "table-cell"));
        var json = facts.ToJson(); var loaded = VisualArtifactInterchangeEnvelope.FromJson(json).ToTopologyChart();
        var restored = loaded.Prepare(); var next = restored.ToInterchangeEnvelope().Edges.Single(edge => edge.Id == "arrive").ResolvedRoute[^1];
        Assert.Equal(endpoint.X, next.X); Assert.Equal(endpoint.Y, next.Y);
        Assert.Equal("0", loaded.Nodes.Single(item => item.Id == "facts").TableContent!.Rows[0].Cells[1].DisplayText);
        Assert.NotEmpty(restored.ToPng()); Assert.Equal(220.345678, chart.Nodes[0].Width);
    }

    [Fact]
    public void OrdinaryNoTableShapesKeepTheirIncumbentAxisEndpointsAndClearedSvgPngWireBytes() {
        foreach (var shape in new[] { TopologyNodeShape.Rectangle, TopologyNodeShape.Rounded })
        foreach (var side in new[] { TopologyEdgePort.Right, TopologyEdgePort.Bottom }) {
            var chart = Source(shape, side, false, table: false); var ordinary = chart.Prepare(Options());
            var facts = ordinary.ToInterchangeEnvelope(); var node = facts.Nodes.Single(item => item.Id == "facts");
            var endpoint = facts.Edges.Single(edge => edge.Id == "arrive").ResolvedRoute[^1];
            var edge = side == TopologyEdgePort.Right ? node.X!.Value + node.Width!.Value : node.Y!.Value + node.Height!.Value;
            var incumbent = double.Parse(edge.ToString("0.###", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture) + 7;
            Assert.Equal(incumbent, side == TopologyEdgePort.Right ? endpoint.X : endpoint.Y);
            var svg = ordinary.ToSvg(); var png = ordinary.ToPng(); var json = facts.ToJson();
            Capture(ordinary, "ordinary-attachment-" + shape + "-" + side);
            chart.Nodes[0].TableContent = Table(); _ = chart.Prepare(Options()); chart.Nodes[0].TableContent = null;
            var cleared = chart.Prepare(Options());
            Assert.Equal(svg, cleared.ToSvg()); Assert.Equal(png, cleared.ToPng()); Assert.Equal(json, cleared.ToInterchangeEnvelope().ToJson());
        }
    }

    private static TopologyChart Source(TopologyNodeShape shape, TopologyEdgePort side, bool named, bool table) {
        const double x = 100.123456, y = 100.234567, width = 220.345678, height = 180.456789;
        var horizontal = side == TopologyEdgePort.Right;
        var chart = TopologyChart.Create().WithId("fractional-table-attachments").WithViewport(900, 800, 24).WithLegend(null)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithLayout(TopologyLayoutMode.Manual)
            .AddNode("facts", "Facts", x, y, width: width, height: height)
            .AddNode("outside", "Outside", horizontal ? x + width + 140 : x + width / 2 - 40,
                horizontal ? y + height / 2 - 20 : y + height + 140, width: 80, height: 40)
            .AddEdge("arrive", "outside", "facts", routing: TopologyEdgeRouting.Straight)
            .WithEdgePorts("arrive", horizontal ? TopologyEdgePort.Left : TopologyEdgePort.Top, side);
        foreach (var node in chart.Nodes) {
            node.Kind = TopologyNodeKind.Generic; node.Shape = TopologyNodeShape.Rectangle;
            node.PreserveDisplayModeSize = true; node.ShowStatusBadge = false; node.ShowLabel = false;
        }
        chart.Nodes[0].Shape = shape; chart.Nodes[0].TableContent = table ? Table() : null;
        if (named) chart.AddNodePort("facts", "incoming", side, .5).WithEdgeNamedPorts("arrive", null, "incoming");
        return chart;
    }
    private static TableArtifact Table() => TableArtifact.Create("readings").AddColumn("property", "Property").AddColumn("value", "Value").AddRow("/count", "count", "0");
    private static TopologyRenderOptions Options() => new() {
        IncludeLegend = false, IncludeTitle = false, IncludeNodeLabels = false, IncludeEdgeLabels = false,
        IncludeStatusBadges = false, FitContentToViewport = false
    };
    private static void Capture(PreparedTopology prepared, string name) {
        if (Environment.GetEnvironmentVariable("CFX_BROWSER_CAPTURE_DIRECTORY") is not { Length: > 0 } output) return;
        Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, name + ".svg"), prepared.ToSvg());
        File.WriteAllBytes(Path.Combine(output, name + ".native.png"), prepared.ToPng()); File.WriteAllText(Path.Combine(output, name + ".json"), prepared.ToInterchangeEnvelope().ToJson());
    }
}
