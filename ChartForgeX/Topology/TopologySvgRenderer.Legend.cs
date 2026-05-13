using System;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddLegend(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var width = LegendWidth(legend, chart.Viewport);
        var columns = LegendColumnCount(legend, width);
        var columnWidth = LegendColumnWidth(width, columns);
        var height = LegendHeight(legend, width);
        var y = chart.Viewport.Height - chart.Viewport.Padding - height;
        var layer = new SvgElement("g")
            .Class(prefix + "__legend")
            .Attribute("data-cfx-role", "topology-legend")
            .Attribute("data-legend-columns", columns)
            .Attribute("data-legend-column-width", columnWidth);
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
            var col = i % columns;
            var row = i / columns;
            var itemX = x + 18 + col * columnWidth;
            var itemY = y + LegendFirstItemOffsetY + row * LegendItemRowHeight;
            var markerCenterY = itemY - 5;
            var color = item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent);
            layer.Element("g", group => {
                group
                    .Class(prefix + "__legend-item")
                    .Attribute("data-cfx-role", "topology-legend-item")
                    .Attribute("data-legend-kind", LegendKindToken(item.Kind));
                if (!string.IsNullOrWhiteSpace(item.IconId)) group.Attribute("data-legend-icon-id", item.IconId!.Trim());
                if (item.Kind == TopologyLegendItemKind.Edge) {
                    group.Element("line", line => line
                        .Attribute("x1", itemX)
                        .Attribute("y1", markerCenterY)
                        .Attribute("x2", itemX + 24)
                        .Attribute("y2", markerCenterY)
                        .Attribute("stroke", color)
                        .Attribute("stroke-width", 2)
                        .Attribute("stroke-dasharray", EdgeDash(item.LineStyle)));
                } else if (item.Kind == TopologyLegendItemKind.Node) {
                    var fill = string.IsNullOrWhiteSpace(item.BackgroundColor) ? StatusFill(color, theme.Background) : item.BackgroundColor!.Trim();
                    group.Element("rect", rect => rect
                        .Attribute("x", itemX)
                        .Attribute("y", markerCenterY - 11)
                        .Attribute("width", 22)
                        .Attribute("height", 22)
                        .Attribute("rx", 6)
                        .Attribute("fill", fill)
                        .Attribute("stroke", color));
                    var legendNode = LegendNode(item);
                    var iconDefinition = ResolveNodeIcon(legendNode, options);
                    if (iconDefinition != null) group.Attribute("data-legend-icon-shape", iconDefinition.Shape.ToString());
                    var artwork = iconDefinition?.Artwork;
                    if (!TryDrawIconArtwork(group, artwork, prefix, itemX + 11, markerCenterY, 18) && !AddInfrastructureGlyph(group, legendNode, itemX + 11, markerCenterY, color, options)) {
                        group.Element("text", text => text
                            .Attribute("x", itemX + 11)
                            .Attribute("y", markerCenterY)
                            .Attribute("text-anchor", "middle")
                            .Attribute("dominant-baseline", "central")
                            .Attribute("fill", color)
                            .Attribute("font-size", 8)
                            .Attribute("font-weight", "800")
                            .Text(NodeGlyph(legendNode, options)));
                    }
                } else {
                    group.Element("circle", circle => circle
                        .Attribute("cx", itemX + 8)
                        .Attribute("cy", markerCenterY)
                        .Attribute("r", 6)
                        .Attribute("fill", color));
                }

                group.Element("text", text => text
                    .Attribute("x", itemX + (item.Kind == TopologyLegendItemKind.Node ? 38 : 32))
                    .Attribute("y", markerCenterY)
                    .Attribute("dominant-baseline", "central")
                    .Attribute("fill", theme.MutedForeground)
                    .Attribute("font-size", 11)
                    .Text(item.Label));
            });
        }

        root.AddElement(layer);
    }

    private static TopologyNode LegendNode(TopologyLegendItem item) {
        return new TopologyNode {
            Id = "__legend",
            Label = item.Label,
            Kind = item.NodeKind ?? TopologyNodeKind.Generic,
            Symbol = item.Symbol,
            IconId = item.IconId,
            Color = item.Color
        };
    }
}
