using ChartForgeX.Rendering;
using ChartForgeX.Topology;
using Xunit;

namespace ChartForgeX.Tests;

public sealed class PreparedTopologyNodeLayoutTests {
    [Theory]
    [InlineData(TopologyNodeShape.Rectangle)]
    [InlineData(TopologyNodeShape.Diamond)]
    [InlineData(TopologyNodeShape.Circle)]
    [InlineData(TopologyNodeShape.DoubleCircle)]
    [InlineData(TopologyNodeShape.Cylinder)]
    [InlineData(TopologyNodeShape.Parallelogram)]
    public void CaptionOnlyShapesCenterTheirCompleteMeasuredTextInsideTheSurface(TopologyNodeShape shape) {
        foreach (var caption in new[] { "Ready", "Ready\nnow" }) {
            var chart = NodeChart(TopologyNodeDisplayMode.Card, TopologyNodeKind.Generic);
            var node = chart.Nodes[0]; node.Shape = shape; node.Label = caption; node.Width = 160; node.Height = 100;
            var prepared = Prepare(chart);
            var body = Assert.Single(prepared.Regions, region => region.Role == "topology-node").Bounds;
            var labels = prepared.Scene.Nodes.OfType<VisualSceneText>().Where(text => text.Role == "topology-node-label").ToArray();
            Assert.Equal(caption.Split('\n'), labels.SelectMany(text => text.Text.Lines).Select(line => line.Text));
            var top = labels.Min(text => text.Baseline - text.Text.Ascent);
            var bottom = labels.Max(text => text.Baseline - text.Text.Ascent + text.Text.Metrics.Height);
            var centerY = body.Y + body.Height / 2 + (shape == TopologyNodeShape.Cylinder ? 7 : 0);
            Assert.Equal(centerY, (top + bottom) / 2, 7);
            foreach (var text in labels) {
                Assert.Equal(body.X + body.Width / 2, text.X, 7);
                foreach (var line in text.Text.Lines) {
                    var left = text.LineLeft(line); var right = left + line.Width;
                    Assert.True(left >= body.Left && right <= body.Right);
                    Assert.True(top >= body.Top && bottom <= body.Bottom);
                    if (shape == TopologyNodeShape.Diamond)
                        Assert.True(Math.Abs((right - (body.X + body.Width / 2)) / (body.Width / 2))
                            + Math.Max(Math.Abs(top - centerY), Math.Abs(bottom - centerY)) / (body.Height / 2) < 1);
                    if (shape is TopologyNodeShape.Circle or TopologyNodeShape.DoubleCircle) {
                        var radius = Math.Min(body.Width, body.Height) / 2 - (shape == TopologyNodeShape.DoubleCircle ? 5 : 0);
                        var dx = Math.Max(Math.Abs(left - text.X), Math.Abs(right - text.X));
                        var dy = Math.Max(Math.Abs(top - centerY), Math.Abs(bottom - centerY));
                        Assert.True(dx * dx + dy * dy < radius * radius);
                    }
                }
            }
            Assert.DoesNotContain(prepared.Diagnostics, diagnostic => diagnostic.Code.StartsWith("topology.label-", StringComparison.Ordinal));
            Assert.NotEmpty(prepared.ToPng());
        }
    }

    [Fact]
    public void AuthoredSymbolsAndRichContentRetainTheirCardHeaderLayoutInsideExplicitShapes() {
        foreach (var symbol in new[] { false, true }) {
            var chart = NodeChart(TopologyNodeDisplayMode.Card, TopologyNodeKind.Generic);
            var node = chart.Nodes[0]; node.Shape = TopologyNodeShape.Rectangle;
            node.X = 60;
            if (symbol) node.Symbol = "API";
            else {
                node.Subtitle = "Service";
                node.Details.Add(new TopologyNodeDetail { Label = "Owner", Value = "Platform" });
                node.Height = 120;
            }
            var prepared = Prepare(chart);
            var body = Assert.Single(prepared.Regions, region => region.Role == "topology-node").Bounds;
            var title = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "topology-node-label");
            Assert.True(title.Baseline < body.Y + body.Height / 2);
            if (symbol) {
                Assert.Equal(body.X + 44 + (body.Width - 44 - 12) / 2, title.X, 7);
                var glyph = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), mark => mark.Role == "topology-node-icon");
                Assert.Equal("API", Assert.Single(glyph.Text.Lines).Text);
            } else {
                var subtitle = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "topology-node-subtitle");
                var detail = Assert.Single(prepared.Scene.Nodes.OfType<VisualSceneText>(), text => text.Role == "topology-node-detail");
                Assert.True(title.Baseline < subtitle.Baseline && subtitle.Baseline < detail.Baseline);
                Assert.Equal("Owner Platform", Assert.Single(detail.Text.Lines).Text);
            }
        }
    }

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
