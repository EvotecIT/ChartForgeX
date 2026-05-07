using System;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddNodes(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__nodes")
            .Attribute("data-cfx-role", "topology-nodes");
        foreach (var node in chart.Nodes) {
            var color = NodeAccentColor(node, theme);
            var highlighted = highlight.IsNodeHighlighted(node);
            var selected = IsSelected(options.SelectedNodeIds, node.Id);
            var parent = AddOptionalLink(layer, node.Href, prefix, options);
            var displayMode = EffectiveNodeDisplayMode(node, options);
            var group = parent.Element("g", element => {
                element
                    .Attribute("id", SafeElementId(chart.Id, "node", node.Id))
                    .Class(prefix + "__node " + prefix + "__node--" + CssToken(node.Kind.ToString()) + " " + prefix + "__node--" + CssToken(node.Status.ToString()) + " " + prefix + "__node--" + CssToken(displayMode.ToString()) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(node.CssClass))
                    .Attribute("data-cfx-role", "topology-node")
                    .Attribute("data-node-id", node.Id)
                    .Attribute("data-node-kind", node.Kind.ToString())
                    .Attribute("data-node-display-mode", displayMode.ToString())
                    .Attribute("data-cfx-status", node.Status.ToString())
                    .Attribute("data-cfx-selected", selected);
                if (node.Longitude.HasValue) element.Attribute("data-node-longitude", F(node.Longitude.Value));
                if (node.Latitude.HasValue) element.Attribute("data-node-latitude", F(node.Latitude.Value));
                if (node.Metadata.TryGetValue("geoVisible", out var nodeGeoVisible)) element.Attribute("data-node-geo-visible", nodeGeoVisible);
                if (!string.IsNullOrWhiteSpace(node.GroupId)) element.Attribute("data-group-id", node.GroupId!);
                if (!string.IsNullOrWhiteSpace(node.Badge)) element.Attribute("data-node-badge", NodeBadge(node));
                if (!string.IsNullOrWhiteSpace(node.Color)) element.Attribute("data-node-color", color);
                AddTopologyDataAttributes(element, "data-cfx-meta-", node.Metadata, options.IncludeDataAttributes);
                AddTopologyDataAttributes(element, "data-cfx-metric-", node.Metrics, options.IncludeDataAttributes);
                if (highlight.IsActive && !highlighted) element.Attribute("opacity", highlight.DimmedOpacity);
            });

            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(node.Tooltip)) group.Element("title", title => title.Text(node.Tooltip!));
            group.AddElement(BuildNodeBody(node, prefix, theme, color, options, chart.Id, selected));
            group.AddElement(BuildNodeBadge(node, prefix, theme, color, displayMode));
        }

        root.AddElement(layer);
    }

    private static string NodeAccentColor(TopologyNode node, TopologyTheme theme) => string.IsNullOrWhiteSpace(node.Color) ? theme.StatusColor(node.Status) : node.Color!.Trim();

    private static SvgElement BuildNodeBody(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyRenderOptions options, string? chartId, bool selected) {
        var displayMode = EffectiveNodeDisplayMode(node, options);
        var body = new SvgElement("g")
            .Class(prefix + "__node-body")
            .Attribute("data-cfx-role", "topology-node-body")
            .Attribute("data-node-id", node.Id);
        if (displayMode == TopologyNodeDisplayMode.Dot) {
            var cx = CenterX(node);
            var cy = CenterY(node);
            if (selected) {
                body.Element("circle", circle => circle
                    .Class(prefix + "__node-selected-ring")
                    .Attribute("cx", cx)
                    .Attribute("cy", cy)
                    .Attribute("r", Math.Max(8, Math.Min(node.Width, node.Height) / 2 + 5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 2.4)
                    .Attribute("opacity", 0.5));
            }

            body.Element("circle", circle => circle
                .Class(prefix + "__node-dot")
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", Math.Max(5, Math.Min(node.Width, node.Height) / 2))
                .Attribute("fill", color)
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", 2));
            return body;
        }

        var radius = displayMode == TopologyNodeDisplayMode.Pill ? node.Height / 2 : displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? 12 : 10;
        body.Element("rect", rect => rect
            .Class(prefix + "__node-card")
            .Attribute("x", node.X)
            .Attribute("y", node.Y)
            .Attribute("width", node.Width)
            .Attribute("height", node.Height)
            .Attribute("rx", radius)
            .Attribute("fill", theme.Card)
            .Attribute("stroke", color)
            .Attribute("stroke-width", selected ? 2.8 : 1.5)
            .Attribute("filter", "url(#" + SanitizeId(chartId ?? "topology") + "-shadow)"));
        DrawNodeIcon(body, node, prefix, theme, color, displayMode);
        if (!options.IncludeNodeLabels || displayMode == TopologyNodeDisplayMode.Icon) return body;
        if (displayMode == TopologyNodeDisplayMode.Tile) {
            body.Element("text", text => text
                .Attribute("x", CenterX(node))
                .Attribute("y", node.Y + node.Height + 15)
                .Attribute("text-anchor", "middle")
                .Attribute("fill", theme.Foreground)
                .Attribute("font-size", 11)
                .Attribute("font-weight", "700")
                .Text(TrimTo(node.Label, 14)));
            if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) body.AddElement(BuildTileSubtitle(node, prefix, theme, color));
            return body;
        }

        var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
        var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 + 4 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 23 : 28);
        var subtitleY = displayMode == TopologyNodeDisplayMode.CompactCard ? node.Y + 40 : node.Y + 47;
        var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
        body.Element("text", text => text
            .Attribute("x", textX)
            .Attribute("y", titleY)
            .Attribute("fill", theme.Foreground)
            .Attribute("font-size", titleSize)
            .Attribute("font-weight", "700")
            .Text(TrimTo(node.Label, NodeTitleMaxLength(displayMode))));
        if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) {
            if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) body.AddElement(BuildCardSubtitleChip(node, prefix, theme, color, displayMode));
            else {
                body.Element("text", text => text
                    .Attribute("x", textX)
                    .Attribute("y", subtitleY)
                    .Attribute("fill", theme.MutedForeground)
                    .Attribute("font-size", 10.5)
                    .Text(TrimTo(node.Subtitle!, NodeLabelMaxLength)));
            }
        }

        return body;
    }

    private static SvgElement BuildCardSubtitleChip(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode) {
        var subtitle = TrimTo(node.Subtitle!, displayMode == TopologyNodeDisplayMode.CompactCard ? 12 : 16);
        var width = Math.Min(Math.Max(48, subtitle.Length * 6 + 18), Math.Max(48, node.Width - 50));
        var height = 17.0;
        var x = node.X + 42;
        var y = node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : node.Height - 22);
        var group = new SvgElement("g")
            .Class(prefix + "__node-card-subtitle")
            .Attribute("data-cfx-role", "topology-node-card-subtitle")
            .Attribute("data-node-id", node.Id);
        group.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", 8.5)
            .Attribute("fill", StatusFill(color, theme.Background))
            .Attribute("stroke", color)
            .Attribute("stroke-opacity", 0.45));
        group.Element("text", text => text
            .Attribute("x", x + width / 2)
            .Attribute("y", y + 11.8)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", color)
            .Attribute("font-size", 9.5)
            .Attribute("font-weight", "700")
            .Text(subtitle));
        return group;
    }

    private static SvgElement BuildTileSubtitle(TopologyNode node, string prefix, TopologyTheme theme, string color) {
        var subtitle = TrimTo(node.Subtitle!, 16);
        var width = Math.Min(Math.Max(46, subtitle.Length * 5.8 + 18), Math.Max(46, node.Width + 28));
        var x = CenterX(node) - width / 2;
        var y = node.Y + node.Height + 21;
        var group = new SvgElement("g")
            .Class(prefix + "__node-tile-subtitle")
            .Attribute("data-cfx-role", "topology-node-subtitle")
            .Attribute("data-node-id", node.Id);
        group.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", 17)
            .Attribute("rx", 8.5)
            .Attribute("fill", StatusFill(color, theme.Background))
            .Attribute("stroke", color)
            .Attribute("stroke-opacity", 0.45));
        group.Element("text", text => text
            .Attribute("x", CenterX(node))
            .Attribute("y", y + 11.8)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", theme.MutedForeground)
            .Attribute("font-size", 9.5)
            .Attribute("font-weight", "700")
            .Text(subtitle));
        return group;
    }

    private static SvgElement? BuildNodeBadge(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode) {
        var badge = NodeBadge(node);
        if (string.IsNullOrWhiteSpace(badge)) return null;
        var width = Math.Max(18, badge.Length * 6.5 + 12);
        var height = 18.0;
        var x = displayMode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8 : displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
        var y = displayMode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21 : displayMode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4 : node.Y + node.Height - height - 6;
        var group = new SvgElement("g")
            .Class(prefix + "__node-badge")
            .Attribute("data-cfx-role", "topology-node-badge")
            .Attribute("data-node-id", node.Id);
        group.Element("rect", rect => rect
            .Attribute("x", x)
            .Attribute("y", y)
            .Attribute("width", width)
            .Attribute("height", height)
            .Attribute("rx", 9)
            .Attribute("fill", StatusFill(color, theme.Background))
            .Attribute("stroke", color));
        group.Element("text", text => text
            .Attribute("x", x + width / 2)
            .Attribute("y", y + 12.5)
            .Attribute("text-anchor", "middle")
            .Attribute("fill", color)
            .Attribute("font-size", 9)
            .Attribute("font-weight", "800")
            .Text(badge));
        return group;
    }

    private static void DrawNodeIcon(SvgElement parent, TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode) {
        var cx = displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? CenterX(node) : node.X + 22;
        var cy = displayMode == TopologyNodeDisplayMode.Tile ? node.Y + node.Height / 2 - 1 : node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : displayMode == TopologyNodeDisplayMode.Tile ? 24 : 22;
        var icon = parent.Element("g", group => group
            .Class(prefix + "__node-icon")
            .Attribute("data-node-kind", node.Kind.ToString()));
        if (node.Kind == TopologyNodeKind.Cloud) {
            icon.Element("circle", circle => circle
                .Attribute("cx", cx - 5)
                .Attribute("cy", cy)
                .Attribute("r", 7)
                .Attribute("fill", StatusFill(color, theme.Background))
                .Attribute("stroke", color));
            icon.Element("circle", circle => circle
                .Attribute("cx", cx + 4)
                .Attribute("cy", cy - 2)
                .Attribute("r", 8)
                .Attribute("fill", StatusFill(color, theme.Background))
                .Attribute("stroke", color));
        } else if (node.Kind is TopologyNodeKind.Database) {
            icon.Element("ellipse", ellipse => ellipse
                .Attribute("cx", cx)
                .Attribute("cy", cy - 7)
                .Attribute("rx", 10)
                .Attribute("ry", 4)
                .Attribute("fill", StatusFill(color, theme.Background))
                .Attribute("stroke", color));
            icon.Element("path", path => path
                .Attribute("d", "M " + F(cx - 10) + " " + F(cy - 7) + " V " + F(cy + 7) + " A 10 4 0 0 0 " + F(cx + 10) + " " + F(cy + 7) + " V " + F(cy - 7))
                .Attribute("fill", StatusFill(color, theme.Background))
                .Attribute("stroke", color));
        } else {
            icon.Element("rect", rect => rect
                .Attribute("x", cx - size / 2)
                .Attribute("y", cy - size / 2)
                .Attribute("width", size)
                .Attribute("height", size)
                .Attribute("rx", 6)
                .Attribute("fill", StatusFill(color, theme.Background))
                .Attribute("stroke", color));
            if (!AddInfrastructureGlyph(icon, node, cx, cy, color)) {
                icon.Element("text", text => text
                    .Attribute("x", cx)
                    .Attribute("y", cy + 4)
                    .Attribute("text-anchor", "middle")
                    .Attribute("fill", color)
                    .Attribute("font-size", 9)
                    .Attribute("font-weight", "800")
                    .Text(NodeGlyph(node)));
            }
        }
    }

    private static bool AddInfrastructureGlyph(SvgElement parent, TopologyNode node, double cx, double cy, string color) {
        switch (node.Kind) {
            case TopologyNodeKind.Hub:
            case TopologyNodeKind.Branch:
            case TopologyNodeKind.Location:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 6) + " " + F(cy + 7) + " V " + F(cy - 7) + " H " + F(cx + 6) + " V " + F(cy + 7) + " M " + F(cx - 2) + " " + F(cy + 7) + " V " + F(cy + 2) + " H " + F(cx + 2) + " V " + F(cy + 7) + " M " + F(cx - 3.5) + " " + F(cy - 3) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy - 3) + " H " + F(cx + 5.5) + " M " + F(cx - 3.5) + " " + F(cy + 1) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy + 1) + " H " + F(cx + 5.5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.7)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyNodeKind.Server:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " V " + F(cy - 1) + " H " + F(cx - 7) + " Z M " + F(cx - 7) + " " + F(cy + 2) + " H " + F(cx + 7) + " V " + F(cy + 7) + " H " + F(cx - 7) + " Z M " + F(cx + 4.5) + " " + F(cy - 3.5) + " H " + F(cx + 5.5) + " M " + F(cx + 4.5) + " " + F(cy + 4.5) + " H " + F(cx + 5.5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.7)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyNodeKind.Network:
            case TopologyNodeKind.NetworkSegment:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 6) + " " + F(cy + 5) + " L " + F(cx) + " " + F(cy - 5) + " L " + F(cx + 6) + " " + F(cy + 5) + " M " + F(cx) + " " + F(cy - 5) + " V " + F(cy + 7))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.6)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                parent.Element("circle", circle => circle
                    .Attribute("cx", cx)
                    .Attribute("cy", cy - 6)
                    .Attribute("r", 2.3)
                    .Attribute("fill", color));
                parent.Element("circle", circle => circle
                    .Attribute("cx", cx - 7)
                    .Attribute("cy", cy + 6)
                    .Attribute("r", 2.3)
                    .Attribute("fill", color));
                parent.Element("circle", circle => circle
                    .Attribute("cx", cx + 7)
                    .Attribute("cy", cy + 6)
                    .Attribute("r", 2.3)
                    .Attribute("fill", color));
                return true;
            case TopologyNodeKind.Service:
                parent.Element("circle", circle => circle
                    .Attribute("cx", cx)
                    .Attribute("cy", cy)
                    .Attribute("r", 5.5)
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.7));
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx) + " " + F(cy - 9) + " V " + F(cy - 7) + " M " + F(cx) + " " + F(cy + 7) + " V " + F(cy + 9) + " M " + F(cx - 9) + " " + F(cy) + " H " + F(cx - 7) + " M " + F(cx + 7) + " " + F(cy) + " H " + F(cx + 9))
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.7)
                    .Attribute("stroke-linecap", "round"));
                return true;
            case TopologyNodeKind.Queue:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy + 6) + " H " + F(cx + 7))
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 2)
                    .Attribute("stroke-linecap", "round"));
                return true;
            default:
                return false;
        }
    }

    private static int NodeTitleMaxLength(TopologyNodeDisplayMode displayMode) {
        return displayMode switch {
            TopologyNodeDisplayMode.CompactCard => 11,
            TopologyNodeDisplayMode.Pill => 14,
            _ => NodeLabelMaxLength
        };
    }

    private static void AddNodeStatuses(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__status-badges")
            .Attribute("data-cfx-role", "topology-status-badges");
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Dot) continue;
            var color = theme.StatusColor(node.Status);
            var highlighted = highlight.IsNodeHighlighted(node);
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            layer.Element("g", group => {
                group
                    .Class(prefix + "__status-badge" + highlight.CssClass(prefix, highlighted))
                    .Attribute("data-cfx-role", "topology-node-status")
                    .Attribute("data-node-id", node.Id)
                    .Attribute("data-cfx-status", node.Status.ToString());
                if (highlight.IsActive && !highlighted) group.Attribute("opacity", highlight.DimmedOpacity);
                group.Element("circle", circle => circle
                    .Attribute("cx", cx)
                    .Attribute("cy", cy)
                    .Attribute("r", 8)
                    .Attribute("fill", color)
                    .Attribute("stroke", theme.Background)
                    .Attribute("stroke-width", 2));
                group.Element("text", text => text
                    .Attribute("x", cx)
                    .Attribute("y", cy + 3)
                    .Attribute("text-anchor", "middle")
                    .Attribute("fill", "#FFFFFF")
                    .Attribute("font-size", 9)
                    .Attribute("font-weight", "800")
                    .Text(StatusGlyph(node.Status)));
            });
        }

        root.AddElement(layer);
    }
}
