using System.Xml.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using ChartForgeX.Topology;
using ChartForgeX.Typography;
using ChartForgeX.VisualArtifacts;
using Xunit;

namespace ChartForgeX.Tests;

[Collection(nameof(FontRegistryCollection))]
public sealed class TopologyStyledSubtitleFootprintTests {
    [Fact]
    public void TwoPublicTilesReserveTheirAuthoredSubtitleInkForLayerSpacingDiagnosticsAndNativePaintWithoutChangingSource() {
        var chart = Source(); var options = Options();
        foreach (var node in chart.Nodes) node.SubtitleTextStyle = new TextStyleOverride { FontFamily = BrowserProofFonts.Family, FontSize = 64, Color = ChartColor.FromHex("#2367AB") };
        var authored = chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height, node.Subtitle, node.SubtitleTextStyle!.FontSize)).ToArray();
        var prepared = chart.Prepare(options); var report = prepared.Analyze(); var svg = XDocument.Parse(prepared.ToSvg());
        var source = report.Nodes.Single(node => node.Id == "gateway"); var target = report.Nodes.Single(node => node.Id == "worker");
        var sourceChip = Chip(svg, "gateway"); var targetChip = Chip(svg, "worker");
        foreach (var item in new[] { (Node: source, Chip: sourceChip), (Node: target, Chip: targetChip) }) {
            var caption = item.Node.CaptionBounds!.Value;
            Assert.True(caption.Left <= item.Chip.Left + .01 && caption.Right + .01 >= item.Chip.Right);
            Assert.True(caption.Top <= item.Chip.Top + .01 && caption.Bottom + .01 >= item.Chip.Bottom);
            Assert.True(item.Chip.Height > 17); Assert.True(item.Chip.Width > item.Node.Bounds.Width);
        }
        Assert.True(sourceChip.Bottom <= target.Bounds.Top + .01, "The next tile must be placed after the complete authored subtitle chip.");
        var saved = prepared.ToInterchangeEnvelope(); var loaded = VisualArtifactInterchangeEnvelope.FromJson(saved.ToJson()).ToTopologyChart();
        Assert.All(loaded.Nodes, node => Assert.Equal(64, node.SubtitleTextStyle!.FontSize));
        Assert.Equal(authored, chart.Nodes.Select(node => (node.Id, node.X, node.Y, node.Width, node.Height, node.Subtitle, node.SubtitleTextStyle!.FontSize)));
        Assert.NotEmpty(prepared.ToPng());
    }

    [Fact]
    public void NullAndClearedRoleKeepOrdinaryTwoTileCoordinatesDiagnosticsSvgPngAndWireBytes() {
        var chart = Source(); var ordinary = chart.Prepare(Options());
        var json = ordinary.ToInterchangeEnvelope().ToJson(); var svg = ordinary.ToSvg(); var png = ordinary.ToPng();
        foreach (var node in chart.Nodes) node.SubtitleTextStyle = new TextStyleOverride { FontSize = 64 };
        var styled = chart.Prepare(Options()); Assert.NotEqual(svg, styled.ToSvg());
        foreach (var node in chart.Nodes) node.SubtitleTextStyle = null;
        var cleared = chart.Prepare(Options());
        Assert.Equal(json, cleared.ToInterchangeEnvelope().ToJson()); Assert.Equal(svg, cleared.ToSvg()); Assert.Equal(png, cleared.ToPng());
        Assert.Equal(ordinary.Analyze().Nodes.Select(node => (node.Id, node.Bounds, node.CaptionBounds)), cleared.Analyze().Nodes.Select(node => (node.Id, node.Bounds, node.CaptionBounds)));
    }

    private static TopologyChart Source() {
        var chart = TopologyChart.Create().WithId("service-tiles").WithViewport(1200, 1000, 24).WithLegend(null)
            .WithTheme(VisualTheme.Graphite().WithFontFamily(BrowserProofFonts.Family)).WithLayout(TopologyLayoutMode.Layered)
            .AddAutoNode("gateway", "Gateway", width: 100, height: 80)
            .AddAutoNode("worker", "Worker", width: 100, height: 80)
            .AddEdge("dispatch", "gateway", "worker", "Dispatch");
        chart.LayoutDirection = TopologyLayoutDirection.TopToBottom;
        chart.Nodes[0].Metadata["layer"] = "0"; chart.Nodes[1].Metadata["layer"] = "1";
        foreach (var node in chart.Nodes) { node.Subtitle = "Platform standby"; node.ShowStatusBadge = false; }
        return chart;
    }
    private static TopologyRenderOptions Options() => new() {
        NodeDisplayMode = TopologyNodeDisplayMode.Tile, IncludeTileSubtitles = true, ReadableDenseLayout = true,
        IncludeLegend = false, IncludeStatusBadges = false, LayeredRankSpacing = 8
    };
    private static ChartRect Chip(XDocument svg, string id) {
        var element = Assert.Single(svg.Descendants(), item => item.Name.LocalName == "rect" && (string?)item.Attribute("data-cfx-role") == "topology-subtitle-chip"
            && item.Ancestors().Any(parent => (string?)parent.Attribute("data-node-id") == id));
        return new ChartRect((double)element.Attribute("x")!, (double)element.Attribute("y")!, (double)element.Attribute("width")!, (double)element.Attribute("height")!);
    }
}
