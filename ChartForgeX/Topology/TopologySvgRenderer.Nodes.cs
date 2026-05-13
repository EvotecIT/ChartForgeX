using System;
using System.Collections.Generic;
using ChartForgeX.Svg;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddNodes(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__nodes")
            .Attribute("data-cfx-role", "topology-nodes");
        foreach (var node in chart.Nodes) {
            var color = NodeAccentColor(node, theme, options);
            var iconDefinition = ResolveNodeIcon(node, options);
            var highlighted = highlight.IsNodeHighlighted(node);
            var selected = IsSelected(options.SelectedNodeIds, node.Id);
            var parent = AddOptionalLink(layer, node.Href, prefix, options);
            var displayMode = EffectiveNodeDisplayMode(node, options);
            if (displayMode == TopologyNodeDisplayMode.Hidden) continue;
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
                if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) element.Attribute("data-node-background-color", node.BackgroundColor!.Trim());
                if (!string.IsNullOrWhiteSpace(node.IconId)) element.Attribute("data-node-icon-id", node.IconId);
                if (iconDefinition != null) {
                    element
                        .Attribute("data-node-icon-pack", iconDefinition.PackId)
                        .Attribute("data-node-icon-label", iconDefinition.Label)
                        .Attribute("data-node-icon-shape", iconDefinition.Shape.ToString());
                    if (iconDefinition.Artwork != null) element.Attribute("data-node-icon-artwork", ArtworkKind(iconDefinition.Artwork));
                }
                AddScenarioDataAttributes(element, chart, TopologyScenarioStepKind.Node, node.Id);
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

    private static string NodeAccentColor(TopologyNode node, TopologyTheme theme, TopologyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(node.Color)) return node.Color!.Trim();
        var icon = ResolveNodeIcon(node, options);
        return !string.IsNullOrWhiteSpace(icon?.Color) ? icon!.Color!.Trim() : theme.StatusColor(node.Status);
    }

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
            AddDotNodeSymbol(body, node, cx, cy, options);
            return body;
        }

        if (IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon && node.Kind == TopologyNodeKind.Cloud) {
            var cx = CenterX(node);
            var cy = CenterY(node);
            var r = Math.Min(node.Width, node.Height) / 2;
            body.Element("circle", circle => circle
                .Class(prefix + "__node-card")
                .Attribute("cx", cx)
                .Attribute("cy", cy)
                .Attribute("r", r)
                .Attribute("fill", color)
                .Attribute("stroke", theme.Background)
                .Attribute("stroke-width", selected ? 4 : 3)
                .Attribute("filter", "url(#" + SanitizeId(chartId ?? "topology") + "-shadow)"));
            DrawNodeIcon(body, node, prefix, theme, color, displayMode, options);
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
            .Attribute("fill", NodeFill(node, theme, color, options))
            .Attribute("stroke", color)
            .Attribute("stroke-width", selected ? 2.8 : 1.5)
            .Attribute("data-node-surface-style", EffectiveNodeSurfaceStyle(options).ToString())
            .Attribute("filter", "url(#" + SanitizeId(chartId ?? "topology") + "-shadow)"));
        if (UseNodeAccentBand(displayMode, options)) {
            body.Element("rect", rect => rect
                .Class(prefix + "__node-accent-band")
                .Attribute("data-cfx-role", "topology-node-accent-band")
                .Attribute("x", node.X)
                .Attribute("y", node.Y + 8)
                .Attribute("width", 4.5)
                .Attribute("height", Math.Max(6, node.Height - 16))
                .Attribute("rx", 2.25)
                .Attribute("fill", color)
                .Attribute("opacity", selected ? 0.95 : 0.82));
        }
        DrawNodeIcon(body, node, prefix, theme, color, displayMode, options);
        if (!options.IncludeNodeLabels) return body;
        if (displayMode == TopologyNodeDisplayMode.Icon) {
            if (options.IncludeIconLabels) {
                var label = IconLabelText(node);
                var labelWidth = IconLabelPlateWidth(node);
                var labelX = CenterX(node) - labelWidth / 2;
                var labelY = IconLabelPlateY(node);
                body.Element("rect", rect => rect
                    .Class(prefix + "__node-icon-label-plate")
                    .Attribute("data-cfx-role", "topology-node-icon-label")
                    .Attribute("x", labelX)
                    .Attribute("y", labelY)
                    .Attribute("width", labelWidth)
                    .Attribute("height", 15)
                    .Attribute("rx", 7.5)
                    .Attribute("fill", theme.Background)
                    .Attribute("stroke", theme.Border)
                    .Attribute("stroke-width", 0.75));
                body.Element("text", text => text
                    .Attribute("x", CenterX(node))
                    .Attribute("y", labelY + 11)
                    .Attribute("text-anchor", "middle")
                    .Attribute("fill", theme.Foreground)
                    .Attribute("font-size", 10.5)
                    .Attribute("font-weight", "700")
                    .Text(label));
            }

            return body;
        }

        if (displayMode == TopologyNodeDisplayMode.Tile) {
            AddNodeTextLines(body, NodeTextLines(node.Label, Math.Max(node.Width + 34, 54), 11, true, options.MaxNodeLabelLines, options), CenterX(node), node.Y + node.Height + 15, theme.Foreground, 11, "700", "middle", 14);
            if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) body.AddElement(BuildTileSubtitle(node, prefix, theme, color, options));
            return body;
        }

        var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
        var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 + 4 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 23 : 28);
        var subtitleY = displayMode == TopologyNodeDisplayMode.CompactCard ? node.Y + 40 : node.Y + 47;
        var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
        var textRightPadding = 10;
        var textWidth = Math.Max(24, node.Width - (textX - node.X) - textRightPadding);
        var titleValue = TrimTo(node.Label, options.AllowMultilineNodeLabels || options.WrapNodeLabels ? NodeLabelMaxLength * Math.Max(1, options.MaxNodeLabelLines) : NodeTitleMaxLength(displayMode));
        titleSize = FitFontSize(NodeTextFitProbe(titleValue, textWidth, titleSize, true, options.MaxNodeLabelLines, options), textWidth, titleSize, 10, true);
        var titleLines = NodeTextLines(titleValue, textWidth, titleSize, true, options.MaxNodeLabelLines, options);
        AddNodeTextLines(body, titleLines, textX, titleY, theme.Foreground, titleSize, "700", null, displayMode == TopologyNodeDisplayMode.CompactCard ? 13 : 14);
        if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) {
            if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) body.AddElement(BuildCardSubtitleChip(node, prefix, theme, color, displayMode));
            else {
                var subtitleStartY = Math.Max(subtitleY, titleY + titleLines.Count * (displayMode == TopologyNodeDisplayMode.CompactCard ? 12 : 13) + 3);
                var subtitleLines = NodeTextLines(node.Subtitle!, textWidth, 10.5, false, options.MaxNodeSubtitleLines, options);
                AddNodeTextLines(body, subtitleLines, textX, subtitleStartY, theme.MutedForeground, 10.5, null, null, 12);
            }
        }

        return body;
    }

    private static void AddNodeTextLines(SvgElement body, IReadOnlyList<string> lines, double x, double y, string color, double fontSize, string? fontWeight, string? textAnchor, double lineHeight) {
        for (var i = 0; i < lines.Count; i++) {
            body.Element("text", text => {
                text
                    .Attribute("x", x)
                    .Attribute("y", y + i * lineHeight)
                    .Attribute("fill", color)
                    .Attribute("font-size", fontSize);
                if (!string.IsNullOrWhiteSpace(fontWeight)) text.Attribute("font-weight", fontWeight);
                if (!string.IsNullOrWhiteSpace(textAnchor)) text.Attribute("text-anchor", textAnchor);
                text.Text(lines[i]);
            });
        }
    }

    private static void AddDotNodeSymbol(SvgElement body, TopologyNode node, double cx, double cy, TopologyRenderOptions options) {
        if (!IsMonitoringDashboardStyle(options)) return;
        if (string.IsNullOrWhiteSpace(node.Symbol) && node.Kind != TopologyNodeKind.Server) return;
        var symbol = string.IsNullOrWhiteSpace(node.Symbol) ? NodeGlyph(node) : node.Symbol!.Trim();
        if (node.Kind == TopologyNodeKind.Server || symbol.Equals("DC", StringComparison.OrdinalIgnoreCase)) {
            body.Element("path", path => path
                .Attribute("data-cfx-role", "topology-node-dot-symbol")
                .Attribute("data-node-id", node.Id)
                .Attribute("d", "M " + F(cx - 4.2) + " " + F(cy - 3.6) + " H " + F(cx + 4.2) + " V " + F(cy - 0.8) + " H " + F(cx - 4.2) + " Z M " + F(cx - 4.2) + " " + F(cy + 1.5) + " H " + F(cx + 4.2) + " V " + F(cy + 4.2) + " H " + F(cx - 4.2) + " Z")
                .Attribute("fill", "none")
                .Attribute("stroke", "#FFFFFF")
                .Attribute("stroke-width", 1.15)
                .Attribute("stroke-linejoin", "round"));
            body.Element("path", path => path
                .Attribute("d", "M " + F(cx + 2.1) + " " + F(cy - 2.2) + " H " + F(cx + 3.1) + " M " + F(cx + 2.1) + " " + F(cy + 2.9) + " H " + F(cx + 3.1))
                .Attribute("stroke", "#FFFFFF")
                .Attribute("stroke-width", 1.2)
                .Attribute("stroke-linecap", "round"));
            return;
        }

        body.Element("text", text => text
            .Attribute("data-cfx-role", "topology-node-dot-symbol")
            .Attribute("data-node-id", node.Id)
            .Attribute("x", cx)
            .Attribute("y", cy)
            .Attribute("text-anchor", "middle")
            .Attribute("dominant-baseline", "central")
            .Attribute("fill", "#FFFFFF")
            .Attribute("font-size", 5.8)
            .Attribute("font-weight", "800")
            .Text(TrimTo(symbol, 2)));
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

    private static SvgElement BuildTileSubtitle(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyRenderOptions options) {
        var subtitle = TrimTo(node.Subtitle!, 16);
        var width = Math.Min(Math.Max(46, subtitle.Length * 5.8 + 18), Math.Max(46, node.Width + 28));
        var labelLineCount = NodeTextLines(node.Label, Math.Max(node.Width + 34, 54), 11, true, options.MaxNodeLabelLines, options).Count;
        var x = CenterX(node) - width / 2;
        var y = node.Y + node.Height + 7 + labelLineCount * 14;
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

    private static void DrawNodeIcon(SvgElement parent, TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode, TopologyRenderOptions options) {
        var cx = displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? CenterX(node) : node.X + 22;
        var cy = displayMode == TopologyNodeDisplayMode.Tile ? node.Y + node.Height / 2 - 1 : node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : displayMode == TopologyNodeDisplayMode.Tile ? 24 : 22;
        var icon = parent.Element("g", group => group
            .Class(prefix + "__node-icon")
            .Attribute("data-node-kind", node.Kind.ToString()));
        var artwork = ResolveNodeIcon(node, options)?.Artwork;
        if (TryDrawIconArtwork(icon, artwork, prefix, cx, cy, size)) return;
        var shape = EffectiveIconShape(node, options);
        if (shape == TopologyIconShape.Cloud) {
            var cloudStroke = IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? "#FFFFFF" : color;
            var cloudFill = IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? "none" : StatusFill(color, theme.Background);
            icon.Element("circle", circle => circle
                .Attribute("cx", cx - 5)
                .Attribute("cy", cy)
                .Attribute("r", 7)
                .Attribute("fill", cloudFill)
                .Attribute("stroke", cloudStroke)
                .Attribute("stroke-width", IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? 2.4 : 1));
            icon.Element("circle", circle => circle
                .Attribute("cx", cx + 4)
                .Attribute("cy", cy - 2)
                .Attribute("r", 8)
                .Attribute("fill", cloudFill)
                .Attribute("stroke", cloudStroke)
                .Attribute("stroke-width", IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? 2.4 : 1));
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
            if (!AddInfrastructureGlyph(icon, node, cx, cy, color, options)) {
                icon.Element("text", text => text
                    .Attribute("x", cx)
                    .Attribute("y", cy)
                    .Attribute("text-anchor", "middle")
                    .Attribute("dominant-baseline", "central")
                    .Attribute("fill", color)
                    .Attribute("font-size", 9)
                    .Attribute("font-weight", "800")
                    .Text(NodeGlyph(node, options)));
            }
        }
    }

    private static bool TryDrawIconArtwork(SvgElement parent, TopologyIconArtwork? artwork, string prefix, double cx, double cy, double size) {
        if (artwork == null || !artwork.IsSafe) return false;
        var x = cx - size / 2;
        var y = cy - size / 2;
        if (artwork.HasSvgBody) {
            parent.Element("svg", svg => svg
                .Class(prefix + "__icon-artwork")
                .Attribute("data-cfx-role", "topology-icon-artwork")
                .Attribute("x", x)
                .Attribute("y", y)
                .Attribute("width", size)
                .Attribute("height", size)
                .Attribute("viewBox", artwork.SvgViewBox)
                .Attribute("preserveAspectRatio", artwork.PreserveAspectRatio)
                .Raw(artwork.SvgBody));
            return true;
        }

        if (artwork.HasImageHref) {
            parent.Element("image", image => image
                .Class(prefix + "__icon-artwork")
                .Attribute("data-cfx-role", "topology-icon-artwork")
                .Attribute("href", artwork.ImageHref)
                .Attribute("x", x)
                .Attribute("y", y)
                .Attribute("width", size)
                .Attribute("height", size)
                .Attribute("preserveAspectRatio", artwork.PreserveAspectRatio));
            return true;
        }

        return false;
    }

    private static string ArtworkKind(TopologyIconArtwork artwork) {
        if (artwork.HasSvgBody) return "svg";
        if (artwork.HasSvgPath) return "svg";
        if (artwork.HasImageHref) return "image";
        return "empty";
    }

    private static bool AddInfrastructureGlyph(SvgElement parent, TopologyNode node, double cx, double cy, string color, TopologyRenderOptions options) {
        switch (EffectiveIconShape(node, options)) {
            case TopologyIconShape.Site:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 6) + " " + F(cy + 7) + " V " + F(cy - 7) + " H " + F(cx + 6) + " V " + F(cy + 7) + " M " + F(cx - 2) + " " + F(cy + 7) + " V " + F(cy + 2) + " H " + F(cx + 2) + " V " + F(cy + 7) + " M " + F(cx - 3.5) + " " + F(cy - 3) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy - 3) + " H " + F(cx + 5.5) + " M " + F(cx - 3.5) + " " + F(cy + 1) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy + 1) + " H " + F(cx + 5.5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.7)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Server:
            case TopologyIconShape.DomainController:
            case TopologyIconShape.ReadOnlyDomainController:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " V " + F(cy - 1) + " H " + F(cx - 7) + " Z M " + F(cx - 7) + " " + F(cy + 2) + " H " + F(cx + 7) + " V " + F(cy + 7) + " H " + F(cx - 7) + " Z M " + F(cx + 4.5) + " " + F(cy - 3.5) + " H " + F(cx + 5.5) + " M " + F(cx + 4.5) + " " + F(cy + 4.5) + " H " + F(cx + 5.5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.7)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                if (EffectiveIconShape(node, options) == TopologyIconShape.ReadOnlyDomainController) parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy + 8) + " L " + F(cx + 8) + " " + F(cy - 8))
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.4)
                    .Attribute("stroke-linecap", "round"));
                return true;
            case TopologyIconShape.Network:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 7) + " " + F(cy + 5) + " L " + F(cx) + " " + F(cy - 6) + " L " + F(cx + 7) + " " + F(cy + 5) + " M " + F(cx - 7) + " " + F(cy + 5) + " H " + F(cx + 7))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.6)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                parent.Element("circle", circle => circle.Attribute("cx", cx).Attribute("cy", cy - 6).Attribute("r", 2.2).Attribute("fill", color));
                parent.Element("circle", circle => circle.Attribute("cx", cx - 7).Attribute("cy", cy + 5).Attribute("r", 2.2).Attribute("fill", color));
                parent.Element("circle", circle => circle.Attribute("cx", cx + 7).Attribute("cy", cy + 5).Attribute("r", 2.2).Attribute("fill", color));
                return true;
            case TopologyIconShape.NetworkSwitch:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy - 4) + " H " + F(cx + 8) + " V " + F(cy + 4) + " H " + F(cx - 8) + " Z M " + F(cx - 5) + " " + F(cy) + " H " + F(cx - 2) + " M " + F(cx + 2) + " " + F(cy) + " H " + F(cx + 5) + " M " + F(cx - 4) + " " + F(cy - 7) + " L " + F(cx - 1) + " " + F(cy - 4) + " M " + F(cx + 4) + " " + F(cy + 7) + " L " + F(cx + 1) + " " + F(cy + 4))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Router:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx) + " " + F(cy - 8) + " L " + F(cx + 8) + " " + F(cy) + " L " + F(cx) + " " + F(cy + 8) + " L " + F(cx - 8) + " " + F(cy) + " Z M " + F(cx - 4) + " " + F(cy) + " H " + F(cx + 4) + " M " + F(cx) + " " + F(cy - 4) + " V " + F(cy + 4))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.NetworkSegment:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 9) + " " + F(cy - 4) + " H " + F(cx + 9) + " M " + F(cx - 9) + " " + F(cy + 4) + " H " + F(cx + 9) + " M " + F(cx - 5) + " " + F(cy - 7) + " V " + F(cy + 7) + " M " + F(cx + 5) + " " + F(cy - 7) + " V " + F(cy + 7))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round"));
                return true;
            case TopologyIconShape.LoadBalancer:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx) + " " + F(cy - 8) + " V " + F(cy + 8) + " M " + F(cx - 8) + " " + F(cy - 3) + " H " + F(cx) + " L " + F(cx + 6) + " " + F(cy - 7) + " M " + F(cx - 8) + " " + F(cy + 3) + " H " + F(cx) + " L " + F(cx + 6) + " " + F(cy + 7))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.6)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Firewall:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy - 6) + " H " + F(cx + 8) + " V " + F(cy + 6) + " H " + F(cx - 8) + " Z M " + F(cx - 3) + " " + F(cy - 6) + " V " + F(cy - 1) + " M " + F(cx + 3) + " " + F(cy - 1) + " V " + F(cy + 6) + " M " + F(cx - 8) + " " + F(cy) + " H " + F(cx - 2) + " M " + F(cx + 2) + " " + F(cy) + " H " + F(cx + 8))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Service:
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
            case TopologyIconShape.Database:
                parent.Element("ellipse", ellipse => ellipse
                    .Attribute("cx", cx)
                    .Attribute("cy", cy - 6)
                    .Attribute("rx", 8)
                    .Attribute("ry", 3.5)
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5));
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy - 6) + " V " + F(cy + 6) + " A 8 3.5 0 0 0 " + F(cx + 8) + " " + F(cy + 6) + " V " + F(cy - 6))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5));
                return true;
            case TopologyIconShape.Person:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx) + " " + F(cy - 7) + " A 4 4 0 1 1 " + F(cx - 0.1) + " " + F(cy - 7) + " M " + F(cx - 8) + " " + F(cy + 8) + " A 8 7 0 0 1 " + F(cx + 8) + " " + F(cy + 8))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.6)
                    .Attribute("stroke-linecap", "round"));
                return true;
            case TopologyIconShape.Team:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 5) + " " + F(cy - 6) + " A 3 3 0 1 1 " + F(cx - 5.1) + " " + F(cy - 6) + " M " + F(cx + 5) + " " + F(cy - 6) + " A 3 3 0 1 1 " + F(cx + 4.9) + " " + F(cy - 6) + " M " + F(cx - 11) + " " + F(cy + 7) + " A 6 5 0 0 1 " + F(cx - 1) + " " + F(cy + 7) + " M " + F(cx + 1) + " " + F(cy + 7) + " A 6 5 0 0 1 " + F(cx + 11) + " " + F(cy + 7))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round"));
                return true;
            case TopologyIconShape.Storage:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy - 6) + " H " + F(cx + 8) + " V " + F(cy - 1) + " H " + F(cx - 8) + " Z M " + F(cx - 8) + " " + F(cy + 2) + " H " + F(cx + 8) + " V " + F(cy + 7) + " H " + F(cx - 8) + " Z M " + F(cx - 4.5) + " " + F(cy - 3.5) + " H " + F(cx + 1.5) + " M " + F(cx - 4.5) + " " + F(cy + 4.5) + " H " + F(cx + 1.5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                parent.Element("circle", circle => circle.Attribute("cx", cx + 5).Attribute("cy", cy - 3.5).Attribute("r", 1.2).Attribute("fill", color));
                parent.Element("circle", circle => circle.Attribute("cx", cx + 5).Attribute("cy", cy + 4.5).Attribute("r", 1.2).Attribute("fill", color));
                return true;
            case TopologyIconShape.Application:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy - 7) + " H " + F(cx + 8) + " V " + F(cy + 7) + " H " + F(cx - 8) + " Z M " + F(cx - 8) + " " + F(cy - 3) + " H " + F(cx + 8) + " M " + F(cx - 5) + " " + F(cy - 5) + " H " + F(cx - 4) + " M " + F(cx - 1.5) + " " + F(cy - 5) + " H " + F(cx - 0.5) + " M " + F(cx - 3) + " " + F(cy + 1) + " H " + F(cx + 3) + " M " + F(cx - 3) + " " + F(cy + 4) + " H " + F(cx + 3))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.45)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Certificate:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 6) + " " + F(cy - 8) + " H " + F(cx + 4) + " L " + F(cx + 8) + " " + F(cy - 4) + " V " + F(cy + 7) + " H " + F(cx - 6) + " Z M " + F(cx + 4) + " " + F(cy - 8) + " V " + F(cy - 4) + " H " + F(cx + 8) + " M " + F(cx - 3) + " " + F(cy - 1) + " H " + F(cx + 4) + " M " + F(cx - 3) + " " + F(cy + 2) + " H " + F(cx + 2))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.4)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                parent.Element("circle", circle => circle.Attribute("cx", cx - 4).Attribute("cy", cy + 7).Attribute("r", 2.5).Attribute("fill", "none").Attribute("stroke", color).Attribute("stroke-width", 1.3));
                return true;
            case TopologyIconShape.Desktop:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 8) + " " + F(cy - 7) + " H " + F(cx + 8) + " V " + F(cy + 4) + " H " + F(cx - 8) + " Z M " + F(cx) + " " + F(cy + 4) + " V " + F(cy + 8) + " M " + F(cx - 5) + " " + F(cy + 8) + " H " + F(cx + 5))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.5)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Laptop:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 7) + " " + F(cy - 7) + " H " + F(cx + 7) + " V " + F(cy + 3) + " H " + F(cx - 7) + " Z M " + F(cx - 10) + " " + F(cy + 7) + " H " + F(cx + 10) + " L " + F(cx + 7) + " " + F(cy + 3) + " H " + F(cx - 7) + " Z")
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.45)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Forest:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx) + " " + F(cy - 9) + " L " + F(cx + 6) + " " + F(cy) + " H " + F(cx + 2.5) + " L " + F(cx + 8) + " " + F(cy + 8) + " H " + F(cx - 8) + " L " + F(cx - 2.5) + " " + F(cy) + " H " + F(cx - 6) + " Z M " + F(cx) + " " + F(cy) + " V " + F(cy + 8))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.4)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Domain:
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx) + " " + F(cy - 9) + " L " + F(cx + 8) + " " + F(cy - 3) + " V " + F(cy + 5) + " L " + F(cx) + " " + F(cy + 9) + " L " + F(cx - 8) + " " + F(cy + 5) + " V " + F(cy - 3) + " Z M " + F(cx - 8) + " " + F(cy - 3) + " L " + F(cx) + " " + F(cy + 2) + " L " + F(cx + 8) + " " + F(cy - 3) + " M " + F(cx) + " " + F(cy + 2) + " V " + F(cy + 9))
                    .Attribute("fill", "none")
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 1.4)
                    .Attribute("stroke-linecap", "round")
                    .Attribute("stroke-linejoin", "round"));
                return true;
            case TopologyIconShape.Badge:
                return false;
            default:
                if (node.Kind == TopologyNodeKind.Queue) {
                parent.Element("path", path => path
                    .Attribute("d", "M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy + 6) + " H " + F(cx + 7))
                    .Attribute("stroke", color)
                    .Attribute("stroke-width", 2)
                    .Attribute("stroke-linecap", "round"));
                return true;
                }
                return false;
        }
    }

    private static void AddNodeStatuses(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__status-badges")
            .Attribute("data-cfx-role", "topology-status-badges");
        foreach (var node in chart.Nodes) {
            if (!ShouldRenderNodeStatusBadge(node, options)) continue;
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
                    .Text(IsMonitoringDashboardStyle(options) && node.Status == TopologyHealthStatus.Healthy ? string.Empty : StatusGlyph(node.Status)));
                if (IsMonitoringDashboardStyle(options) && node.Status == TopologyHealthStatus.Healthy) {
                    group.Element("path", path => path
                        .Attribute("d", "M " + F(cx - 3.8) + " " + F(cy) + " L " + F(cx - 1) + " " + F(cy + 3) + " L " + F(cx + 4.4) + " " + F(cy - 3.6))
                        .Attribute("fill", "none")
                        .Attribute("stroke", "#FFFFFF")
                        .Attribute("stroke-width", 1.8)
                        .Attribute("stroke-linecap", "round")
                        .Attribute("stroke-linejoin", "round"));
                }
            });
        }

        root.AddElement(layer);
    }
}
