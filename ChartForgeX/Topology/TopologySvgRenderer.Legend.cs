using System;
using System.Text;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void DrawLegend(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var height = LegendHeight(legend);
        var y = chart.Viewport.Height - chart.Viewport.Padding - height;
        var width = Math.Min(LegendMaxWidth, chart.Viewport.Width - chart.Viewport.Padding * 2);
        var layer = new SvgElement("g")
            .Class(prefix + "__legend")
            .Attribute("data-cfx-role", "topology-legend");
        layer.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", 12)
            .Attribute("fill", theme.Card)
            .Attribute("stroke", theme.Border));
        if (!string.IsNullOrWhiteSpace(legend.Title)) {
            layer.Element("text", text => text
                .Attribute("x", x + 16)
                .Attribute("y", y + 23)
                .Attribute("fill", theme.Foreground)
                .Attribute("font-size", 12)
                .Attribute("font-weight", "700")
                .Text(legend.Title!));
        }

        for (var i = 0; i < legend.Items.Count; i++) {
            var item = legend.Items[i];
            var col = i % LegendColumns;
            var row = i / LegendColumns;
            var itemX = x + 18 + col * LegendItemColumnWidth;
            var itemY = y + LegendFirstItemOffsetY + row * LegendItemRowHeight;
            var color = item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent);
            layer.Element("g", group => {
                group
                    .Class(prefix + "__legend-item")
                    .Attribute("data-cfx-role", "topology-legend-item")
                    .Attribute("data-legend-kind", LegendKindToken(item.Kind));
                if (item.Kind == TopologyLegendItemKind.Edge) {
                    group.Element("line", line => line
                        .Attribute("x1", itemX)
                        .Attribute("y1", itemY - 4)
                        .Attribute("x2", itemX + 24)
                        .Attribute("y2", itemY - 4)
                        .Attribute("stroke", color)
                        .Attribute("stroke-width", 2)
                        .Attribute("stroke-dasharray", "6 4"));
                } else if (item.Kind == TopologyLegendItemKind.Node && !string.IsNullOrWhiteSpace(item.Symbol)) {
                    group.Element("rect", rect => rect
                        .Attribute("x", itemX)
                        .Attribute("y", itemY - 13)
                        .Attribute("width", 16)
                        .Attribute("height", 16)
                        .Attribute("rx", 4)
                        .Attribute("fill", StatusFill(color, theme.Background))
                        .Attribute("stroke", color));
                    group.Element("text", text => text
                        .Attribute("x", itemX + 8)
                        .Attribute("y", itemY - 2)
                        .Attribute("text-anchor", "middle")
                        .Attribute("fill", color)
                        .Attribute("font-size", 7)
                        .Attribute("font-weight", "800")
                        .Text(TrimTo(item.Symbol!.Trim(), 4)));
                } else {
                    group.Element("circle", circle => circle
                        .Attribute("cx", itemX + 8)
                        .Attribute("cy", itemY - 5)
                        .Attribute("r", 6)
                        .Attribute("fill", color));
                }

                group.Element("text", text => text
                    .Attribute("x", itemX + 32)
                    .Attribute("y", itemY)
                    .Attribute("fill", theme.MutedForeground)
                    .Attribute("font-size", 11)
                    .Text(item.Label));
            });
        }

        sb.Append(ElementMarkup(layer));
    }
}
