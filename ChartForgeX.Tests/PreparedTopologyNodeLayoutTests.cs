using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyNodeLayoutTests {
    [Theory]
    [InlineData(TopologyNodeDisplayMode.Pill, TopologyNodeKind.Hub, 34)]
    [InlineData(TopologyNodeDisplayMode.Pill, TopologyNodeKind.Generic, 10)]
    [InlineData(TopologyNodeDisplayMode.Card, TopologyNodeKind.Hub, 44)]
    public void InlineLabelsReserveTheirGlyphStrip(TopologyNodeDisplayMode mode, TopologyNodeKind kind, double inset) {
        var chart = NodeChart(mode, kind);
        var prepared = Prepare(chart);
        var body = Assert.Single(prepared.Regions, region => region.Role == "topology-node").Bounds;
        var label = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "topology-node-label");
        Assert.Equal(body.X + inset, label.X, 8);
        Assert.Equal("AMER", Assert.Single(label.Text.Lines).Text);
        Assert.True(label.X + label.Text.Metrics.Width <= body.Right);
        if (kind != TopologyNodeKind.Generic) {
            var icon = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), mark => mark.Role == "topology-icon-surface");
            Assert.True(icon.Bounds.Right < label.X, "The inline label must start after its icon.");
        }
        Assert.True(prepared.ToPng().Length > 64);
    }

    [Theory]
    [InlineData(TopologyNodeDisplayMode.Dot)]
    [InlineData(TopologyNodeDisplayMode.Icon)]
    [InlineData(TopologyNodeDisplayMode.Tile)]
    [InlineData(TopologyNodeDisplayMode.Card)]
    public void ExplicitBadgesUseReservedModeGeometryAndRetainDotSymbols(TopologyNodeDisplayMode mode) {
        var chart = NodeChart(mode, TopologyNodeKind.Server);
        chart.Nodes[0].Symbol = "DC";
        chart.Nodes[0].Badge = "DC";
        var prepared = Prepare(chart);
        var body = Assert.Single(prepared.Regions, region => region.Role == "topology-node").Bounds;
        var badge = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneRectangle>(), mark => mark.Role == "topology-node-badge-surface").Bounds;
        var text = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-badge");
        Assert.Equal("DC", Assert.Single(text.Text.Lines).Text);
        Assert.Equal(18, badge.Height);
        Assert.True(badge.Width >= text.Text.Metrics.Width + 12);
        Assert.True(text.LineLeft(text.Text.Lines[0]) >= badge.X);
        Assert.True(text.LineLeft(text.Text.Lines[0]) + text.Text.Metrics.Width <= badge.Right);
        Assert.True(text.Baseline - text.Text.Ascent >= badge.Y);
        Assert.True(text.Baseline - text.Text.Ascent + text.Text.Metrics.Height <= badge.Bottom);
        if (mode == TopologyNodeDisplayMode.Dot) {
            Assert.Equal(body.X + body.Width / 2 + 8, badge.X, 8);
            Assert.Equal(body.Y + body.Height / 2 - 21, badge.Y, 8);
            var symbol = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-symbol");
            Assert.Equal("DC", Assert.Single(symbol.Text.Lines).Text);
            Assert.True(badge.Y < body.Y);
        } else if (mode == TopologyNodeDisplayMode.Icon) {
            Assert.Equal(body.X + (body.Width - badge.Width) / 2, badge.X, 8);
            Assert.Equal(body.Bottom + 4, badge.Y, 8);
        } else {
            Assert.Equal(body.Right - badge.Width - 6, badge.X, 8);
            Assert.Equal(mode == TopologyNodeDisplayMode.Tile ? body.Y - 8 : body.Bottom - 24, badge.Y, 8);
        }
        var svg = prepared.ToSvg(); var png = prepared.ToPng();
        Assert.Equal("DC", chart.Nodes[0].Badge);
        chart.Nodes[0].Badge = null; chart.Nodes[0].Symbol = null;
        Assert.Equal(svg, prepared.ToSvg());
        Assert.Equal(png, prepared.ToPng());
    }

    private static TopologyChart NodeChart(TopologyNodeDisplayMode mode, TopologyNodeKind kind) =>
        TopologyChart.Create().WithViewport(300, 240, 20).WithLegend(null)
            .AddNode("node", "AMER", 100, 100, kind, TopologyHealthStatus.Healthy,
                width: mode == TopologyNodeDisplayMode.Dot ? 22 : 104, height: mode == TopologyNodeDisplayMode.Pill ? 34 : mode == TopologyNodeDisplayMode.Dot ? 22 : 80)
            .WithNodeDisplay("node", mode);

    private static PreparedVisual Prepare(TopologyChart chart) => chart.Prepare(
        new VisualRenderContext(new VisualLayoutOptions(new VisualSize(300, 240), 20), frame: new VisualFrame(showLegend: false)),
        new TopologyRenderOptions { IncludeLegend = false, IncludeStatusBadges = false });
}
