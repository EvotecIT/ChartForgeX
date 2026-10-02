using System;
using System.Collections.Generic;
using ChartForgeX.Svg;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

public sealed partial class TopologySvgRenderer {
    private static void AddNodes(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__nodes")
            .Attribute("data-cfx-role", "topology-nodes");
        var groupLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var groupInfo in chart.Groups) {
            if (!string.IsNullOrWhiteSpace(groupInfo.Id)) groupLabels[groupInfo.Id] = groupInfo.Label;
        }

        foreach (var node in chart.Nodes) {
            var color = NodeAccentColor(node, theme, options);
            var iconDefinition = ResolveNodeIcon(node, options);
            var artwork = ResolveRenderableNodeArtwork(node, options);
            var artworkSource = ResolveNodeArtworkSource(node, options);
            var highlighted = highlight.IsNodeHighlighted(node);
            var selected = IsSelected(options.SelectedNodeIds, node.Id);
            var parent = AddOptionalLink(layer, node.Href, prefix, options);
            var displayMode = EffectiveNodeDisplayMode(node, options);
            if (displayMode == TopologyNodeDisplayMode.Hidden) continue;
            var group = parent.Element("g", element => {
                element
                    .Attribute("id", TopologySvgIds.Element(chart, options, "node", node.Id))
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
                element.Attribute("data-node-label", node.Label);
                if (!string.IsNullOrWhiteSpace(node.GroupId) && groupLabels.TryGetValue(node.GroupId!, out var groupLabel)) element.Attribute("data-group-label", groupLabel);
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
                }
                if (artwork != null) {
                    element
                        .Attribute("data-node-icon-artwork", ArtworkKind(artwork))
                        .Attribute("data-node-artwork-source", artworkSource);
                }
                element.Attribute("data-node-detail-count", node.Details.Count);
                AddScenarioDataAttributes(element, chart, TopologyScenarioStepKind.Node, node.Id);
                AddTopologyDataAttributes(element, "data-cfx-meta-", node.Metadata, options.IncludeDataAttributes);
                AddTopologyDataAttributes(element, "data-cfx-metric-", node.Metrics, options.IncludeDataAttributes);
                if (highlight.IsActive && !highlighted) element.Attribute("opacity", highlight.DimmedOpacity);
            });

            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(node.Tooltip)) group.Element("title", title => title.Text(node.Tooltip!));
            group.AddElement(BuildNodeBody(node, prefix, theme, color, options, TopologySvgIds.Root(chart, options), selected));
            group.AddElement(BuildNodeBadge(node, prefix, theme, color, displayMode, options));
        }

        root.AddElement(layer);
    }

    private static string NodeAccentColor(TopologyNode node, TopologyTheme theme, TopologyRenderOptions options) {
        if (!string.IsNullOrWhiteSpace(node.Color)) return node.Color!.Trim();
        var icon = ResolveNodeIcon(node, options);
        return !string.IsNullOrWhiteSpace(icon?.Color) ? icon!.Color!.Trim() : theme.StatusColor(node.Status);
    }

    private static SvgElement BuildNodeBody(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyRenderOptions options, string rootId, bool selected) {
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
                .Attribute("filter", "url(#" + rootId + "-shadow)"));
            DrawNodeIcon(body, node, prefix, theme, color, displayMode, options);
            return body;
        }

        if (displayMode == TopologyNodeDisplayMode.Artwork) return BuildArtworkNodeBody(node, prefix, theme, color, options);

        var radius = displayMode == TopologyNodeDisplayMode.Pill ? node.Height / 2 : displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? 12 : 10;
        body.Element("rect", rect => rect
            .Class(prefix + "__node-card")
            .Attribute("x", node.X)
            .Attribute("y", node.Y)
            .Attribute("width", node.Width)
            .Attribute("height", node.Height)
            .Attribute("rx", radius)
            .Paint("fill", NodePaint(node, theme, color, options))
            .Attribute("stroke", color)
            .Attribute("stroke-width", selected ? 2.8 : 1.5)
            .Attribute("data-node-surface-style", EffectiveNodeSurfaceStyle(options).ToString())
            .Attribute("filter", "url(#" + rootId + "-shadow)"));
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
                var label = IconLabelText(node, options.TextMeasurement);
                var labelWidth = IconLabelPlateWidth(node, options.TextMeasurement);
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
            AddNodeTextLines(body, TileCaptionLines(node, options), CenterX(node), node.Y + node.Height + 15, theme.Foreground, 11, "700", "middle", 14);
            if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) body.AddElement(BuildTileSubtitle(node, prefix, theme, color, options));
            return body;
        }

        var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
        var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 + 4 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 23 : 28);
        var subtitleY = displayMode == TopologyNodeDisplayMode.CompactCard ? node.Y + 40 : node.Y + 47;
        var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
        var textRightPadding = 10;
        var textWidth = Math.Max(24, node.Width - (textX - node.X) - textRightPadding);
        var titleCharacterLimit = NodeTitleMaxLength(node, displayMode);
        var titleValue = TrimTo(node.Label, options.AllowMultilineNodeLabels || options.WrapNodeLabels ? titleCharacterLimit * Math.Max(1, options.MaxNodeLabelLines) : titleCharacterLimit);
        titleSize = FitFontSize(NodeTextFitProbe(titleValue, textWidth, titleSize, true, options.MaxNodeLabelLines, options), textWidth, titleSize, 10, true, options.TextMeasurement);
        var titleLines = NodeTextLines(titleValue, textWidth, titleSize, true, options.MaxNodeLabelLines, options, titleCharacterLimit);
        AddNodeTextLines(body, titleLines, textX, titleY, theme.Foreground, titleSize, "700", null, displayMode == TopologyNodeDisplayMode.CompactCard ? 13 : 14);
        if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) {
            if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) body.AddElement(BuildCardSubtitleChip(node, prefix, theme, color, displayMode, options));
            else {
                var subtitleStartY = Math.Max(subtitleY, titleY + titleLines.Count * (displayMode == TopologyNodeDisplayMode.CompactCard ? 12 : 13) + 3);
                var subtitleLines = NodeTextLines(node.Subtitle!, textWidth, 10.5, false, options.MaxNodeSubtitleLines, options);
                AddNodeTextLines(body, subtitleLines, textX, subtitleStartY, theme.MutedForeground, 10.5, null, null, 12);
            }
        }

        if (displayMode == TopologyNodeDisplayMode.Card && node.Details.Count > 0) AddNodeDetails(body, node, theme, options);

        return body;
    }

    private static void AddNodeDetails(SvgElement body, TopologyNode node, TopologyTheme theme, TopologyRenderOptions options) {
        var startY = node.Y + NodeDetailStartOffset(node, options);
        var left = node.X + 12;
        var right = node.X + node.Width - 12;
        for (var i = 0; i < node.Details.Count; i++) {
            var detail = node.Details[i];
            var y = startY + i * 18;
            var color = !string.IsNullOrWhiteSpace(detail.Color) ? detail.Color!.Trim() : detail.Status.HasValue ? theme.StatusColor(detail.Status.Value) : theme.MutedForeground;
            body.Element("line", line => line.Attribute("x1", left).Attribute("y1", y - 8).Attribute("x2", right).Attribute("y2", y - 8).Attribute("stroke", theme.Border).Attribute("stroke-opacity", 0.45).Attribute("stroke-width", 1));
            body.Element("circle", circle => circle.Attribute("data-cfx-role", "topology-node-detail-status").Attribute("data-cfx-status", detail.Status?.ToString()).Attribute("cx", left + 3).Attribute("cy", y - 1).Attribute("r", 2.5).Attribute("fill", color));
            body.Element("text", text => text.Attribute("data-cfx-role", "topology-node-detail-label").Attribute("x", left + 10).Attribute("y", y + 2).Attribute("fill", theme.MutedForeground).Attribute("font-size", 8.5).Text(TrimTo(detail.Label, 14)));
            body.Element("text", text => text.Attribute("data-cfx-role", "topology-node-detail-value").Attribute("x", right).Attribute("y", y + 2).Attribute("fill", theme.Foreground).Attribute("font-size", 8.5).Attribute("font-weight", 700).Attribute("text-anchor", "end").Text(TrimTo(detail.Value, 16)));
        }
    }

    private static SvgElement BuildArtworkNodeBody(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyRenderOptions options) {
        var body = new SvgElement("g")
            .Class(prefix + "__node-body " + prefix + "__node-artwork-body")
            .Attribute("data-cfx-role", "topology-node-body")
            .Attribute("data-node-id", node.Id)
            .Attribute("data-node-surface-style", "Artwork");
        var artwork = ResolveRenderableNodeArtwork(node, options);
        if (TryDrawIconArtwork(body, artwork, prefix, options, node.X, node.Y, node.Width, node.Height)) {
            return body;
        }

        body.Element("rect", rect => rect
            .Class(prefix + "__node-card")
            .Attribute("x", node.X)
            .Attribute("y", node.Y)
            .Attribute("width", node.Width)
            .Attribute("height", node.Height)
            .Attribute("rx", 12)
            .Paint("fill", NodePaint(node, theme, color, options))
            .Attribute("stroke", color)
            .Attribute("stroke-width", 1.5)
            .Attribute("data-node-surface-style", "ArtworkFallback"));
        DrawNodeIcon(body, node, prefix, theme, color, TopologyNodeDisplayMode.Card, options);
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
            var marks = TopologyInfrastructureGlyphs.DotServer(cx, cy);
            body.Element("path", path => path
                .Attribute("data-cfx-role", "topology-node-dot-symbol")
                .Attribute("data-node-id", node.Id)
                .Attribute("d", marks[0].PathData)
                .Attribute("fill", "none")
                .Paint("stroke", ContrastWhite)
                .Attribute("stroke-width", marks[0].StrokeWidth)
                .Attribute("stroke-linejoin", "round"));
            AddGlyphMark(body, marks[1], ContrastWhite);
            return;
        }

        body.Element("text", text => text
            .Attribute("data-cfx-role", "topology-node-dot-symbol")
            .Attribute("data-node-id", node.Id)
            .Attribute("x", cx)
            .Attribute("y", cy)
            .Attribute("text-anchor", "middle")
            .Attribute("dominant-baseline", "central")
            .Paint("fill", ContrastWhite)
            .Attribute("font-size", DotNodeSymbolFontSize)
            .Attribute("font-weight", "800")
            .Text(TrimTo(symbol, 2)));
    }

    private static SvgElement BuildCardSubtitleChip(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode, TopologyRenderOptions options) {
        var (subtitle, width) = SubtitleChip(node, displayMode, options);
        var height = 17.0;
        var x = node.X + 42;
        var y = node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : CardSubtitleChipOffset(node, options));
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
            .Paint("fill", StatusPaint(color, theme.Background))
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
        var (subtitle, width) = SubtitleChip(node, TopologyNodeDisplayMode.Tile, options);
        var labelLineCount = TileCaptionLines(node, options).Count;
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
            .Paint("fill", StatusPaint(color, theme.Background))
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

    private static SvgElement? BuildNodeBadge(TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode, TopologyRenderOptions options) {
        var badge = NodeBadge(node);
        if (string.IsNullOrWhiteSpace(badge)) return null;
        var width = NodeBadgeWidth(node, options);
        var height = 18.0;
        var x = displayMode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8 : displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
        var y = displayMode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21 : displayMode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4 : displayMode == TopologyNodeDisplayMode.Tile ? node.Y - 8 : node.Y + node.Height - height - 6;
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
            .Paint("fill", StatusPaint(color, theme.Background))
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
        var cy = displayMode == TopologyNodeDisplayMode.Tile
            ? node.Y + node.Height / 2 - 1
            : displayMode == TopologyNodeDisplayMode.Card && options.IncludeNodeLabels && node.Details.Count > 0 ? node.Y + 28 : node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : displayMode == TopologyNodeDisplayMode.Tile ? 24 : 22;
        var icon = parent.Element("g", group => group
            .Class(prefix + "__node-icon")
            .Attribute("data-node-kind", node.Kind.ToString()));
        var artwork = ResolveRenderableNodeArtwork(node, options);
        if (TryDrawIconArtwork(icon, artwork, prefix, options, cx, cy, size)) return;
        var shape = EffectiveIconShape(node, options);
        if (shape == TopologyIconShape.Cloud) {
            var cloudStroke = IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? ContrastWhite : SvgPaint.Plain(color);
            var cloudFill = IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? SvgPaint.Plain("none") : StatusPaint(color, theme.Background);
            icon.Element("circle", circle => circle
                .Attribute("cx", cx - 5)
                .Attribute("cy", cy)
                .Attribute("r", 7)
                .Paint("fill", cloudFill)
                .Paint("stroke", cloudStroke)
                .Attribute("stroke-width", IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? 2.4 : 1));
            icon.Element("circle", circle => circle
                .Attribute("cx", cx + 4)
                .Attribute("cy", cy - 2)
                .Attribute("r", 8)
                .Paint("fill", cloudFill)
                .Paint("stroke", cloudStroke)
                .Attribute("stroke-width", IsMonitoringDashboardStyle(options) && displayMode == TopologyNodeDisplayMode.Icon ? 2.4 : 1));
        } else if (node.Kind is TopologyNodeKind.Database) {
            icon.Element("ellipse", ellipse => ellipse
                .Attribute("cx", cx)
                .Attribute("cy", cy - 7)
                .Attribute("rx", 10)
                .Attribute("ry", 4)
                .Paint("fill", StatusPaint(color, theme.Background))
                .Attribute("stroke", color));
            icon.Element("path", path => path
                .Attribute("d", "M " + F(cx - 10) + " " + F(cy - 7) + " V " + F(cy + 7) + " A 10 4 0 0 0 " + F(cx + 10) + " " + F(cy + 7) + " V " + F(cy - 7))
                .Paint("fill", StatusPaint(color, theme.Background))
                .Attribute("stroke", color));
        } else {
            icon.Element("rect", rect => rect
                .Attribute("x", cx - size / 2)
                .Attribute("y", cy - size / 2)
                .Attribute("width", size)
                .Attribute("height", size)
                .Attribute("rx", 6)
                .Paint("fill", StatusPaint(color, theme.Background))
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

    private static bool TryDrawIconArtwork(SvgElement parent, TopologyIconArtwork? artwork, string prefix, TopologyRenderOptions options, double cx, double cy, double size) {
        return TryDrawIconArtwork(parent, artwork, prefix, options, cx - size / 2, cy - size / 2, size, size);
    }

    private static bool TryDrawIconArtwork(SvgElement parent, TopologyIconArtwork? artwork, string prefix, TopologyRenderOptions options, double x, double y, double width, double height) {
        if (artwork == null || !artwork.IsSafe) return false;
        if (artwork.HasSvgBody) {
            parent.Element("svg", svg => svg
                .Class(prefix + "__icon-artwork")
                .Attribute("data-cfx-role", "topology-icon-artwork")
                .Attribute("x", x)
                .Attribute("y", y)
                .Attribute("width", width)
                .Attribute("height", height)
                .Attribute("viewBox", artwork.SvgViewBox)
                .Attribute("preserveAspectRatio", artwork.PreserveAspectRatio)
                .Raw(SvgPaint.Sanitize(TopologySvgIds.ScopeIconArtwork(artwork.SvgBody!, options))));
            return true;
        }

        if (artwork.HasImageHref) {
            parent.Element("image", image => image
                .Class(prefix + "__icon-artwork")
                .Attribute("data-cfx-role", "topology-icon-artwork")
                .Attribute("href", artwork.ImageHref)
                .Attribute("x", x)
                .Attribute("y", y)
                .Attribute("width", width)
                .Attribute("height", height)
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
        var marks = TopologyInfrastructureGlyphs.Build(EffectiveIconShape(node, options), node.Kind, cx, cy);
        if (marks == null) return false;
        foreach (var mark in marks) AddGlyphMark(parent, mark, SvgPaint.Plain(color));
        return true;
    }

    private static void AddGlyphMark(SvgElement parent, TopologyGlyphMark mark, SvgPaint color) {
        if (mark.PathData != null) {
            parent.Element("path", path => {
                path.Attribute("d", mark.PathData);
                if (mark.FillNone) path.Attribute("fill", "none");
                path.Paint("stroke", color).Attribute("stroke-width", mark.StrokeWidth);
                if (mark.RoundCap) path.Attribute("stroke-linecap", "round");
                if (mark.RoundJoin) path.Attribute("stroke-linejoin", "round");
            });
            return;
        }

        parent.Element(mark.IsCircle ? "circle" : "ellipse", shape => {
            shape.Attribute("cx", mark.Cx).Attribute("cy", mark.Cy);
            if (mark.IsCircle) shape.Attribute("r", mark.Rx);
            else shape.Attribute("rx", mark.Rx).Attribute("ry", mark.Ry);
            if (mark.Filled) shape.Paint("fill", color);
            else shape.Attribute("fill", "none").Paint("stroke", color).Attribute("stroke-width", mark.StrokeWidth);
        });
    }

    private static void AddNodeStatuses(SvgElement root, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        var layer = new SvgElement("g")
            .Class(prefix + "__status-badges")
            .Attribute("data-cfx-role", "topology-status-badges");
        foreach (var node in chart.Nodes) {
            if (!ShouldRenderNodeStatusBadge(node, options)) continue;
            var color = theme.StatusColor(node.Status);
            var highlighted = highlight.IsNodeHighlighted(node);
            var cx = NodeStatusBadgeCenterX(node);
            var cy = NodeStatusBadgeCenterY(node);
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
                    .Attribute("r", NodeStatusBadgeOuterRadius)
                    .Attribute("fill", theme.Background));
                group.Element("circle", circle => circle
                    .Attribute("cx", cx)
                    .Attribute("cy", cy)
                    .Attribute("r", NodeStatusBadgeInnerRadius)
                    .Attribute("fill", color));
                group.Element("text", text => text
                    .Attribute("x", cx)
                    .Attribute("y", cy + NodeStatusBadgeGlyphYOffset)
                    .Attribute("text-anchor", "middle")
                    .Paint("fill", ContrastWhite)
                    .Attribute("font-size", NodeStatusBadgeGlyphFontSize)
                    .Attribute("font-weight", "800")
                    .Text(ShouldDrawNodeStatusBadgeCheck(node, options) ? string.Empty : StatusGlyph(node.Status)));
                if (ShouldDrawNodeStatusBadgeCheck(node, options)) {
                    var check = NodeStatusBadgeCheckPoints(cx, cy);
                    group.Element("path", path => path
                        .Attribute("d", "M " + F(check[0].X) + " " + F(check[0].Y) + " L " + F(check[1].X) + " " + F(check[1].Y) + " L " + F(check[2].X) + " " + F(check[2].Y))
                        .Attribute("fill", "none")
                        .Paint("stroke", ContrastWhite)
                        .Attribute("stroke-width", NodeStatusBadgeCheckStrokeWidth)
                        .Attribute("stroke-linecap", "round")
                        .Attribute("stroke-linejoin", "round"));
                }
            });
        }

        root.AddElement(layer);
    }
}
