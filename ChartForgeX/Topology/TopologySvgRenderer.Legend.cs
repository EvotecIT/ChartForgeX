using System;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
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
            // Line and dot swatches carry their status, so a pinned render keeps them in their colour in forced-colours mode;
            // a node swatch is left out because its glyph follows forced colours.
            var swatchStatus = item.Status?.ToString();
            var paint = item.Color == null && item.Status.HasValue ? StatusColorPaint(color) : SvgPaint.Plain(color);
            layer.Element("g", group => {
                group
                    .Class(prefix + "__legend-item")
                    .Attribute("data-cfx-role", "topology-legend-item")
                    .Attribute("data-legend-kind", LegendKindToken(item.Kind));
                if (!string.IsNullOrWhiteSpace(item.IconId)) group.Attribute("data-legend-icon-id", item.IconId!.Trim());
                if (item.Kind == TopologyLegendItemKind.Edge) {
                    group.Element("line", line => line
                        .Attribute("data-cfx-status", swatchStatus)
                        .Attribute("x1", itemX)
                        .Attribute("y1", markerCenterY)
                        .Attribute("x2", itemX + 24)
                        .Attribute("y2", markerCenterY)
                        .Paint("stroke", paint)
                        .Attribute("stroke-width", 2)
                        .Attribute("stroke-dasharray", EdgeDash(LegendLineStyle(chart, item))));
                } else if (item.Kind == TopologyLegendItemKind.Node) {
                    var fill = string.IsNullOrWhiteSpace(item.BackgroundColor)
                        ? StatusPaint(color, theme.Background, 0.10, item.Color == null && item.Status.HasValue ? SvgColorRole.Status : SvgColorRole.Any)
                        : SvgPaint.Plain(item.BackgroundColor!.Trim());
                    group.Element("rect", rect => rect
                        .Attribute("x", itemX)
                        .Attribute("y", markerCenterY - 11)
                        .Attribute("width", 22)
                        .Attribute("height", 22)
                        .Attribute("rx", 6)
                        .Paint("fill", fill)
                        .Paint("stroke", paint));
                    var legendNode = LegendNode(item);
                    var iconDefinition = ResolveNodeIcon(legendNode, options);
                    if (iconDefinition != null) group.Attribute("data-legend-icon-shape", iconDefinition.Shape.ToString());
                    var artwork = iconDefinition?.Artwork;
                    if (!TryDrawIconArtwork(group, artwork, prefix, options, itemX + 11, markerCenterY, 18) && !AddInfrastructureGlyph(group, legendNode, itemX + 11, markerCenterY, color, options, paint)) {
                        group.Element("text", text => text
                            .Attribute("x", itemX + 11)
                            .Attribute("y", markerCenterY)
                            .Attribute("text-anchor", "middle")
                            .Attribute("dominant-baseline", "central")
                            .Paint("fill", paint)
                            .Attribute("font-size", 8)
                            .Attribute("font-weight", "800")
                            .Text(NodeGlyph(legendNode, options)));
                    }
                } else {
                    group.Element("circle", circle => circle
                        .Attribute("data-cfx-status", swatchStatus)
                        .Attribute("cx", itemX + 8)
                        .Attribute("cy", markerCenterY)
                        .Attribute("r", 6)
                        .Paint("fill", paint));
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
