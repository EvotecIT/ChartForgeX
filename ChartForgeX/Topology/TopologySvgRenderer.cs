using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

/// <summary>
/// Renders topology charts to static SVG markup.
/// </summary>
public sealed class TopologySvgRenderer {
    /// <summary>
    /// Renders a topology chart to complete SVG markup.
    /// </summary>
    /// <param name="chart">The topology chart.</param>
    /// <param name="options">Optional render options.</param>
    /// <returns>Complete SVG markup.</returns>
    public string Render(TopologyChart chart, TopologyRenderOptions? options = null) {
        if (chart == null) throw new ArgumentNullException(nameof(chart));
        options ??= new TopologyRenderOptions();
        if (options.Preset != TopologyViewPreset.Default) options.ApplyPreset(options.Preset);
        var prepared = TopologyLayoutEngine.Prepare(chart, options.View, options);
        var validation = new TopologyChartValidator().Validate(prepared);
        if (!validation.IsValid) throw new TopologyValidationException(validation);

        var theme = prepared.Theme ?? TopologyTheme.Light();
        var prefix = string.IsNullOrWhiteSpace(options.CssClassPrefix) ? "cfx-topology" : options.CssClassPrefix!;
        var id = SanitizeId(string.IsNullOrWhiteSpace(prepared.Id) ? "topology" : prepared.Id!);
        var w = prepared.Viewport.Width;
        var h = prepared.Viewport.Height;
        var highlight = TopologyHighlightState.From(prepared, options);
        var style = options.UseResponsiveSvg ? " style=\"max-width:100%;height:auto;display:block\"" : string.Empty;
        var sb = new StringBuilder();

        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + F(w) + "\" height=\"" + F(h) + "\" viewBox=\"0 0 " + F(w) + " " + F(h) + "\" role=\"img\" aria-labelledby=\"" + id + "-title " + id + "-desc\"" + style + " shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\">");
        sb.AppendLine("<title id=\"" + id + "-title\">" + Escape(string.IsNullOrWhiteSpace(prepared.Title) ? "ChartForgeX topology" : prepared.Title!) + "</title>");
        sb.AppendLine("<desc id=\"" + id + "-desc\">" + Escape(BuildDescription(prepared)) + "</desc>");
        DrawDefs(sb, id, prefix, theme, options);
        sb.AppendLine("<g id=\"" + id + "\" class=\"" + prefix + "\" data-cfx-role=\"topology\" data-chart-id=\"" + EscapeAttr(prepared.Id ?? id) + "\" data-layout-mode=\"" + prepared.LayoutMode + "\" data-layout-direction=\"" + prepared.LayoutDirection + "\" data-node-display-mode=\"" + options.NodeDisplayMode + "\">");
        sb.AppendLine("<rect class=\"" + prefix + "__background\" width=\"100%\" height=\"100%\" fill=\"" + EscapeAttr(theme.Background) + "\"/>");
        if (options.IncludeTitle) DrawHeader(sb, prepared, prefix, theme);
        if (options.IncludeGroups) DrawGroups(sb, prepared, prefix, theme, options, highlight);
        DrawEdges(sb, prepared, prefix, theme, options, id, highlight);
        DrawEdgeLabels(sb, prepared, prefix, theme, options, highlight);
        DrawNodes(sb, prepared, prefix, theme, options, highlight);
        if (options.IncludeStatusBadges) DrawNodeStatuses(sb, prepared, prefix, theme, options, highlight);
        if (options.IncludeLegend && prepared.Legend != null) DrawLegend(sb, prepared, prefix, theme);
        sb.AppendLine("</g>");
        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static void DrawDefs(StringBuilder sb, string id, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        sb.AppendLine("<defs>");
        if (options.IncludeCss) {
            sb.Append("<style>");
            sb.Append("#" + id + " text{font-family:" + CssFontFamily(theme.FontFamily) + ";font-synthesis:none;letter-spacing:0}");
            sb.Append("#" + id + " ." + prefix + "__link{cursor:pointer}");
            sb.Append("#" + id + " ." + prefix + "__edge{fill:none;stroke-linecap:round;stroke-linejoin:round;vector-effect:non-scaling-stroke}");
            sb.Append("#" + id + " ." + prefix + "__node-card,#" + id + " ." + prefix + "__group-card{vector-effect:non-scaling-stroke}");
            sb.Append("#" + id + " ." + prefix + "--highlighted{filter:url(#" + id + "-shadow)}");
            sb.Append("#" + id + " ." + prefix + "--selected{filter:url(#" + id + "-selected-shadow)}");
            sb.Append("</style>");
            sb.AppendLine();
        }

        sb.AppendLine("<filter id=\"" + id + "-shadow\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"150%\"><feDropShadow dx=\"0\" dy=\"10\" stdDeviation=\"12\" flood-color=\"#0F172A\" flood-opacity=\"0.10\"/></filter>");
        sb.AppendLine("<filter id=\"" + id + "-selected-shadow\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"150%\"><feDropShadow dx=\"0\" dy=\"10\" stdDeviation=\"12\" flood-color=\"#2563EB\" flood-opacity=\"0.18\"/></filter>");
        foreach (var status in Enum.GetValues(typeof(TopologyHealthStatus)).Cast<TopologyHealthStatus>()) {
            var color = theme.StatusColor(status);
            sb.AppendLine("<marker id=\"" + id + "-arrow-" + StatusMarkerToken(status) + "\" viewBox=\"0 0 10 10\" refX=\"8\" refY=\"5\" markerWidth=\"7\" markerHeight=\"7\" orient=\"auto-start-reverse\"><path d=\"M 0 0 L 10 5 L 0 10 z\" fill=\"" + EscapeAttr(color) + "\"/></marker>");
        }

        sb.AppendLine("</defs>");
    }

    private static void DrawHeader(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        if (string.IsNullOrWhiteSpace(chart.Title) && string.IsNullOrWhiteSpace(chart.Subtitle)) return;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Padding + 8;
        sb.AppendLine("<g class=\"" + prefix + "__header\" data-cfx-role=\"topology-header\">");
        if (!string.IsNullOrWhiteSpace(chart.Title)) {
            sb.AppendLine("<text x=\"" + F(x) + "\" y=\"" + F(y + 18) + "\" fill=\"" + EscapeAttr(theme.Foreground) + "\" font-size=\"22\" font-weight=\"700\">" + Escape(chart.Title!) + "</text>");
        }

        if (!string.IsNullOrWhiteSpace(chart.Subtitle)) {
            sb.AppendLine("<text x=\"" + F(x) + "\" y=\"" + F(y + 42) + "\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"13\">" + Escape(chart.Subtitle!) + "</text>");
        }

        sb.AppendLine("</g>");
    }

    private static void DrawGroups(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        sb.AppendLine("<g class=\"" + prefix + "__groups\" data-cfx-role=\"topology-groups\">");
        foreach (var group in chart.Groups) {
            var highlighted = highlight.IsGroupHighlighted(group);
            var selected = IsSelected(options.SelectedGroupIds, group.Id);
            OpenLink(sb, group.Href, prefix, options);
            var accent = GroupAccentColor(group, theme);
            sb.AppendLine("<g id=\"" + SafeElementId(chart.Id, "group", group.Id) + "\" class=\"" + prefix + "__group " + prefix + "__group--" + CssToken(group.Status.ToString()) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(group.CssClass) + "\" data-cfx-role=\"topology-group\" data-group-id=\"" + EscapeAttr(group.Id) + "\" data-cfx-status=\"" + group.Status + "\" data-cfx-selected=\"" + (selected ? "true" : "false") + "\"" + (string.IsNullOrWhiteSpace(group.Symbol) ? string.Empty : " data-group-symbol=\"" + EscapeAttr(TrimTo(group.Symbol!.Trim(), 12)) + "\"") + (string.IsNullOrWhiteSpace(group.Color) ? string.Empty : " data-group-color=\"" + EscapeAttr(accent) + "\"") + MetadataAttributes(group.Metadata, null, options.IncludeDataAttributes) + highlight.SvgOpacity(highlighted) + ">");
            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(group.Tooltip)) sb.AppendLine("<title>" + Escape(group.Tooltip!) + "</title>");
            sb.AppendLine("<rect class=\"" + prefix + "__group-card\" x=\"" + F(group.X) + "\" y=\"" + F(group.Y) + "\" width=\"" + F(group.Width) + "\" height=\"" + F(group.Height) + "\" rx=\"12\" fill=\"" + EscapeAttr(StatusFill(accent, theme.Background)) + "\" stroke=\"" + EscapeAttr(accent) + "\" stroke-width=\"" + F(selected ? 2.4 : 1) + "\" stroke-opacity=\"" + F(selected ? 0.9 : 0.48) + "\"/>");
            if (options.IncludeGroupLabels) {
                var cx = group.X + group.Width / 2;
                sb.AppendLine("<circle cx=\"" + F(cx - 52) + "\" cy=\"" + F(group.Y + 26) + "\" r=\"10\" fill=\"" + EscapeAttr(StatusFill(accent, theme.Background)) + "\" stroke=\"" + EscapeAttr(accent) + "\"/>");
                DrawGroupSymbol(sb, group, cx - 52, group.Y + 26, accent);
                sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(group.Y + 30) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(accent) + "\" font-size=\"16\" font-weight=\"700\">" + Escape(group.Label) + "</text>");
                if (!string.IsNullOrWhiteSpace(group.Subtitle)) sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(group.Y + 50) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"12\">" + Escape(group.Subtitle!) + "</text>");
            }
            sb.AppendLine("</g>");
            CloseLink(sb, group.Href);
        }

        sb.AppendLine("</g>");
    }

    private static string GroupAccentColor(TopologyGroup group, TopologyTheme theme) => string.IsNullOrWhiteSpace(group.Color) ? theme.StatusColor(group.Status) : group.Color!.Trim();

    private static void DrawGroupSymbol(StringBuilder sb, TopologyGroup group, double cx, double cy, string color) {
        var symbol = string.IsNullOrWhiteSpace(group.Symbol) ? string.Empty : group.Symbol!.Trim();
        if (symbol.Equals("region", StringComparison.OrdinalIgnoreCase) || symbol.Equals("globe", StringComparison.OrdinalIgnoreCase)) {
            sb.AppendLine("<circle cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"5.8\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.3\"/>");
            sb.AppendLine("<path d=\"M " + F(cx - 5.2) + " " + F(cy) + " H " + F(cx + 5.2) + " M " + F(cx) + " " + F(cy - 5.8) + " C " + F(cx - 3.2) + " " + F(cy - 2.6) + " " + F(cx - 3.2) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + 5.8) + " M " + F(cx) + " " + F(cy - 5.8) + " C " + F(cx + 3.2) + " " + F(cy - 2.6) + " " + F(cx + 3.2) + " " + F(cy + 2.6) + " " + F(cx) + " " + F(cy + 5.8) + "\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.1\" stroke-linecap=\"round\"/>");
            return;
        }

        if (string.IsNullOrWhiteSpace(symbol)) {
            sb.AppendLine("<circle cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"4\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\"/>");
            return;
        }

        sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + 3.2) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"8\" font-weight=\"800\">" + Escape(TrimTo(symbol, 3)) + "</text>");
    }

    private static void DrawEdges(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, string svgId, TopologyHighlightState highlight) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        sb.AppendLine("<g class=\"" + prefix + "__edges\" data-cfx-role=\"topology-edges\">");
        foreach (var edge in chart.Edges) {
            var points = EdgePoints(chart, edge, nodes);
            var routeOffset = EdgeRouteOffset(chart, edge);
            var color = edge.IsMuted ? theme.Border : theme.StatusColor(edge.Status);
            var dash = edge.IsMuted ? "none" : EdgeDash(edge);
            var highlighted = highlight.IsEdgeHighlighted(edge);
            var selected = IsSelected(options.SelectedEdgeIds, edge.Id);
            var markerEnd = options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional ? " marker-end=\"url(#" + svgId + "-arrow-" + StatusMarkerToken(edge.Status) + ")\"" : string.Empty;
            var markerStart = options.IncludeDirectionMarkers && edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional ? " marker-start=\"url(#" + svgId + "-arrow-" + StatusMarkerToken(edge.Status) + ")\"" : string.Empty;
            OpenLink(sb, edge.Href, prefix, options);
            sb.AppendLine("<g id=\"" + SafeElementId(chart.Id, "edge", edge.Id) + "\" class=\"" + prefix + "__edge-wrap " + prefix + "__edge-wrap--" + CssToken(edge.Status.ToString()) + (edge.IsMuted ? " " + prefix + "__edge-wrap--muted" : string.Empty) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(edge.CssClass) + "\" data-cfx-role=\"topology-edge\" data-edge-id=\"" + EscapeAttr(edge.Id) + "\" data-source-node-id=\"" + EscapeAttr(edge.SourceNodeId) + "\" data-target-node-id=\"" + EscapeAttr(edge.TargetNodeId) + "\" data-edge-kind=\"" + edge.Kind + "\" data-cfx-status=\"" + edge.Status + "\" data-cfx-selected=\"" + (selected ? "true" : "false") + "\" data-edge-muted=\"" + (edge.IsMuted ? "true" : "false") + "\" data-edge-line-style=\"" + edge.LineStyle + "\" data-route-offset=\"" + F(routeOffset) + "\" data-source-port=\"" + edge.SourcePort + "\" data-target-port=\"" + edge.TargetPort + "\" data-route-lane=\"" + F(edge.RouteLane) + "\" data-waypoint-count=\"" + edge.Waypoints.Count.ToString(CultureInfo.InvariantCulture) + "\"" + MetadataAttributes(edge.Metadata, edge.Metrics, options.IncludeDataAttributes) + highlight.SvgOpacity(highlighted) + ">");
            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(edge.Tooltip)) sb.AppendLine("<title>" + Escape(edge.Tooltip!) + "</title>");
            sb.AppendLine("<path class=\"" + prefix + "__edge\" d=\"" + EdgePath(points, edge.Routing) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"" + F(selected ? 3.4 : edge.IsMuted ? 1.45 : 2.2) + "\" stroke-dasharray=\"" + dash + "\" opacity=\"" + F(edge.IsMuted ? 0.72 : 0.94) + "\"" + markerStart + markerEnd + "/>");
            sb.AppendLine("</g>");
            CloseLink(sb, edge.Href);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawEdgeLabels(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        if (!options.IncludeEdgeLabels) return;
        sb.AppendLine("<g class=\"" + prefix + "__edge-labels\" data-cfx-role=\"topology-edge-labels\">");
        foreach (var layout in EdgeLabelLayouts(chart, options)) {
            var edge = layout.Edge;
            var cx = layout.CenterX;
            var cy = layout.CenterY;
            var highlighted = highlight.IsEdgeHighlighted(edge);
            var selected = IsSelected(options.SelectedEdgeIds, edge.Id);
            sb.AppendLine("<g class=\"" + prefix + "__edge-label" + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + "\" data-cfx-role=\"topology-edge-label\" data-edge-id=\"" + EscapeAttr(edge.Id) + "\" data-label-x=\"" + F(cx) + "\" data-label-y=\"" + F(cy) + "\" data-cfx-selected=\"" + (selected ? "true" : "false") + "\"" + highlight.SvgOpacity(highlighted) + ">");
            if (options.IncludeEdgeLabelBackplates) sb.AppendLine("<rect x=\"" + F(cx - layout.Width / 2) + "\" y=\"" + F(cy - layout.Height / 2) + "\" width=\"" + F(layout.Width) + "\" height=\"" + F(layout.Height) + "\" rx=\"9\" fill=\"" + EscapeAttr(theme.Background) + "\" stroke=\"" + EscapeAttr(theme.Border) + "\"/>");
            DrawEdgeLabelLines(sb, layout, cx, cy, edge.IsMuted ? theme.MutedForeground : theme.StatusColor(edge.Status), theme.MutedForeground);
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static void DrawEdgeLabelLines(StringBuilder sb, TopologyEdgeLabelLayout layout, double cx, double cy, string primaryColor, string secondaryColor) {
        var lines = new List<(string Text, bool Primary)>();
        if (!string.IsNullOrWhiteSpace(layout.Label)) lines.Add((layout.Label, true));
        if (!string.IsNullOrWhiteSpace(layout.SecondaryLabel)) lines.Add((layout.SecondaryLabel, false));
        if (!string.IsNullOrWhiteSpace(layout.TertiaryLabel)) lines.Add((layout.TertiaryLabel, false));
        var start = cy - (lines.Count - 1) * 8;
        for (var i = 0; i < lines.Count; i++) {
            var line = lines[i];
            var size = line.Primary ? 12 : 10;
            var weight = line.Primary ? "700" : "500";
            var color = line.Primary ? primaryColor : secondaryColor;
            sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(start + i * 16 + (line.Primary ? 4 : 3)) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"" + F(size) + "\" font-weight=\"" + weight + "\">" + Escape(line.Text) + "</text>");
        }
    }

    private static void DrawNodes(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        sb.AppendLine("<g class=\"" + prefix + "__nodes\" data-cfx-role=\"topology-nodes\">");
        foreach (var node in chart.Nodes) {
            var color = NodeAccentColor(node, theme);
            var highlighted = highlight.IsNodeHighlighted(node);
            var selected = IsSelected(options.SelectedNodeIds, node.Id);
            OpenLink(sb, node.Href, prefix, options);
            var displayMode = EffectiveNodeDisplayMode(node, options);
            sb.AppendLine("<g id=\"" + SafeElementId(chart.Id, "node", node.Id) + "\" class=\"" + prefix + "__node " + prefix + "__node--" + CssToken(node.Kind.ToString()) + " " + prefix + "__node--" + CssToken(node.Status.ToString()) + " " + prefix + "__node--" + CssToken(displayMode.ToString()) + (selected ? " " + prefix + "--selected" : string.Empty) + highlight.CssClass(prefix, highlighted) + CustomCssClasses(node.CssClass) + "\" data-cfx-role=\"topology-node\" data-node-id=\"" + EscapeAttr(node.Id) + "\" data-node-kind=\"" + node.Kind + "\" data-node-display-mode=\"" + displayMode + "\" data-cfx-status=\"" + node.Status + "\" data-cfx-selected=\"" + (selected ? "true" : "false") + "\"" + (string.IsNullOrWhiteSpace(node.GroupId) ? string.Empty : " data-group-id=\"" + EscapeAttr(node.GroupId!) + "\"") + (string.IsNullOrWhiteSpace(node.Badge) ? string.Empty : " data-node-badge=\"" + EscapeAttr(NodeBadge(node)) + "\"") + (string.IsNullOrWhiteSpace(node.Color) ? string.Empty : " data-node-color=\"" + EscapeAttr(color) + "\"") + MetadataAttributes(node.Metadata, node.Metrics, options.IncludeDataAttributes) + highlight.SvgOpacity(highlighted) + ">");
            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(node.Tooltip)) sb.AppendLine("<title>" + Escape(node.Tooltip!) + "</title>");
            DrawNodeBody(sb, node, prefix, theme, color, options, chart.Id, selected);
            DrawNodeBadge(sb, node, prefix, theme, color, displayMode);
            sb.AppendLine("</g>");
            CloseLink(sb, node.Href);
        }

        sb.AppendLine("</g>");
    }

    private static string NodeAccentColor(TopologyNode node, TopologyTheme theme) => string.IsNullOrWhiteSpace(node.Color) ? theme.StatusColor(node.Status) : node.Color!.Trim();

    private static bool IsSelected(List<string> ids, string id) {
        foreach (var selectedId in ids) {
            if (string.Equals(selectedId, id, StringComparison.Ordinal)) return true;
        }

        return false;
    }

    private static void DrawNodeBody(StringBuilder sb, TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyRenderOptions options, string? chartId, bool selected) {
        var displayMode = EffectiveNodeDisplayMode(node, options);
        if (displayMode == TopologyNodeDisplayMode.Dot) {
            var cx = CenterX(node);
            var cy = CenterY(node);
            if (selected) sb.AppendLine("<circle class=\"" + prefix + "__node-selected-ring\" cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"" + F(Math.Max(8, Math.Min(node.Width, node.Height) / 2 + 5)) + "\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"2.4\" opacity=\"0.5\"/>");
            sb.AppendLine("<circle class=\"" + prefix + "__node-dot\" cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"" + F(Math.Max(5, Math.Min(node.Width, node.Height) / 2)) + "\" fill=\"" + EscapeAttr(color) + "\" stroke=\"" + EscapeAttr(theme.Background) + "\" stroke-width=\"2\"/>");
            return;
        }

        var radius = displayMode == TopologyNodeDisplayMode.Pill ? node.Height / 2 : displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? 12 : 10;
        sb.AppendLine("<rect class=\"" + prefix + "__node-card\" x=\"" + F(node.X) + "\" y=\"" + F(node.Y) + "\" width=\"" + F(node.Width) + "\" height=\"" + F(node.Height) + "\" rx=\"" + F(radius) + "\" fill=\"" + EscapeAttr(theme.Card) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"" + F(selected ? 2.8 : 1.5) + "\" filter=\"url(#" + SanitizeId(chartId ?? "topology") + "-shadow)\"/>");
        DrawNodeIcon(sb, node, prefix, theme, color, displayMode);
        if (!options.IncludeNodeLabels || displayMode == TopologyNodeDisplayMode.Icon) return;
        if (displayMode == TopologyNodeDisplayMode.Tile) {
            sb.AppendLine("<text x=\"" + F(CenterX(node)) + "\" y=\"" + F(node.Y + node.Height + 15) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(theme.Foreground) + "\" font-size=\"11\" font-weight=\"700\">" + Escape(TrimTo(node.Label, 14)) + "</text>");
            if (options.IncludeTileSubtitles && !string.IsNullOrWhiteSpace(node.Subtitle)) DrawTileSubtitle(sb, node, prefix, theme, color);
            return;
        }

        var textX = displayMode == TopologyNodeDisplayMode.Pill ? node.X + 34 : node.X + 42;
        var titleY = displayMode == TopologyNodeDisplayMode.Pill ? node.Y + node.Height / 2 + 4 : node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 23 : 28);
        var subtitleY = displayMode == TopologyNodeDisplayMode.CompactCard ? node.Y + 40 : node.Y + 47;
        var titleSize = displayMode == TopologyNodeDisplayMode.Pill ? 11.5 : displayMode == TopologyNodeDisplayMode.CompactCard ? 11.5 : 12.5;
        sb.AppendLine("<text x=\"" + F(textX) + "\" y=\"" + F(titleY) + "\" fill=\"" + EscapeAttr(theme.Foreground) + "\" font-size=\"" + F(titleSize) + "\" font-weight=\"700\">" + Escape(TrimTo(node.Label, NodeTitleMaxLength(displayMode))) + "</text>");
        if (displayMode != TopologyNodeDisplayMode.Pill && !string.IsNullOrWhiteSpace(node.Subtitle)) {
            if (options.CardSubtitleMode == TopologyCardSubtitleMode.Chip) DrawCardSubtitleChip(sb, node, prefix, theme, color, displayMode);
            else sb.AppendLine("<text x=\"" + F(textX) + "\" y=\"" + F(subtitleY) + "\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"10.5\">" + Escape(TrimTo(node.Subtitle!, NodeLabelMaxLength)) + "</text>");
        }
    }

    private static void DrawCardSubtitleChip(StringBuilder sb, TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode) {
        var subtitle = TrimTo(node.Subtitle!, displayMode == TopologyNodeDisplayMode.CompactCard ? 12 : 16);
        var width = Math.Min(Math.Max(48, subtitle.Length * 6 + 18), Math.Max(48, node.Width - 50));
        var height = 17.0;
        var x = node.X + 42;
        var y = node.Y + (displayMode == TopologyNodeDisplayMode.CompactCard ? 31 : node.Height - 22);
        sb.AppendLine("<g class=\"" + prefix + "__node-card-subtitle\" data-cfx-role=\"topology-node-card-subtitle\" data-node-id=\"" + EscapeAttr(node.Id) + "\">");
        sb.AppendLine("<rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" rx=\"8.5\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-opacity=\"0.45\"/>");
        sb.AppendLine("<text x=\"" + F(x + width / 2) + "\" y=\"" + F(y + 11.8) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"9.5\" font-weight=\"700\">" + Escape(subtitle) + "</text>");
        sb.AppendLine("</g>");
    }

    private static void DrawTileSubtitle(StringBuilder sb, TopologyNode node, string prefix, TopologyTheme theme, string color) {
        var subtitle = TrimTo(node.Subtitle!, 16);
        var width = Math.Min(Math.Max(46, subtitle.Length * 5.8 + 18), Math.Max(46, node.Width + 28));
        var x = CenterX(node) - width / 2;
        var y = node.Y + node.Height + 21;
        sb.AppendLine("<g class=\"" + prefix + "__node-tile-subtitle\" data-cfx-role=\"topology-node-subtitle\" data-node-id=\"" + EscapeAttr(node.Id) + "\">");
        sb.AppendLine("<rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(width) + "\" height=\"17\" rx=\"8.5\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-opacity=\"0.45\"/>");
        sb.AppendLine("<text x=\"" + F(CenterX(node)) + "\" y=\"" + F(y + 11.8) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"9.5\" font-weight=\"700\">" + Escape(subtitle) + "</text>");
        sb.AppendLine("</g>");
    }

    private static void DrawNodeBadge(StringBuilder sb, TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode) {
        var badge = NodeBadge(node);
        if (string.IsNullOrWhiteSpace(badge)) return;
        var width = Math.Max(18, badge.Length * 6.5 + 12);
        var height = 18.0;
        var x = displayMode == TopologyNodeDisplayMode.Dot ? CenterX(node) + 8 : displayMode == TopologyNodeDisplayMode.Icon ? CenterX(node) - width / 2 : node.X + node.Width - width - 6;
        var y = displayMode == TopologyNodeDisplayMode.Dot ? CenterY(node) - 21 : displayMode == TopologyNodeDisplayMode.Icon ? node.Y + node.Height + 4 : node.Y + node.Height - height - 6;
        sb.AppendLine("<g class=\"" + prefix + "__node-badge\" data-cfx-role=\"topology-node-badge\" data-node-id=\"" + EscapeAttr(node.Id) + "\">");
        sb.AppendLine("<rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" rx=\"9\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
        sb.AppendLine("<text x=\"" + F(x + width / 2) + "\" y=\"" + F(y + 12.5) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"9\" font-weight=\"800\">" + Escape(badge) + "</text>");
        sb.AppendLine("</g>");
    }

    private static void DrawNodeIcon(StringBuilder sb, TopologyNode node, string prefix, TopologyTheme theme, string color, TopologyNodeDisplayMode displayMode) {
        var cx = displayMode is TopologyNodeDisplayMode.Icon or TopologyNodeDisplayMode.Tile ? CenterX(node) : node.X + 22;
        var cy = displayMode == TopologyNodeDisplayMode.Tile ? node.Y + node.Height / 2 - 1 : node.Y + node.Height / 2;
        var size = displayMode == TopologyNodeDisplayMode.Pill ? 18 : displayMode == TopologyNodeDisplayMode.Icon ? 26 : displayMode == TopologyNodeDisplayMode.Tile ? 24 : 22;
        sb.AppendLine("<g class=\"" + prefix + "__node-icon\" data-node-kind=\"" + node.Kind + "\">");
        if (node.Kind == TopologyNodeKind.Cloud) {
            sb.AppendLine("<circle cx=\"" + F(cx - 5) + "\" cy=\"" + F(cy) + "\" r=\"7\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/><circle cx=\"" + F(cx + 4) + "\" cy=\"" + F(cy - 2) + "\" r=\"8\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
        } else if (node.Kind is TopologyNodeKind.Database) {
            sb.AppendLine("<ellipse cx=\"" + F(cx) + "\" cy=\"" + F(cy - 7) + "\" rx=\"10\" ry=\"4\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/><path d=\"M " + F(cx - 10) + " " + F(cy - 7) + " V " + F(cy + 7) + " A 10 4 0 0 0 " + F(cx + 10) + " " + F(cy + 7) + " V " + F(cy - 7) + "\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
        } else {
            sb.AppendLine("<rect x=\"" + F(cx - size / 2) + "\" y=\"" + F(cy - size / 2) + "\" width=\"" + F(size) + "\" height=\"" + F(size) + "\" rx=\"6\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
            if (!DrawInfrastructureGlyph(sb, node, cx, cy, color)) sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + 4) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"9\" font-weight=\"800\">" + Escape(NodeGlyph(node)) + "</text>");
        }

        sb.AppendLine("</g>");
    }

    private static bool DrawInfrastructureGlyph(StringBuilder sb, TopologyNode node, double cx, double cy, string color) {
        switch (node.Kind) {
            case TopologyNodeKind.Hub:
            case TopologyNodeKind.Branch:
            case TopologyNodeKind.Location:
                sb.AppendLine("<path d=\"M " + F(cx - 6) + " " + F(cy + 7) + " V " + F(cy - 7) + " H " + F(cx + 6) + " V " + F(cy + 7) + " M " + F(cx - 2) + " " + F(cy + 7) + " V " + F(cy + 2) + " H " + F(cx + 2) + " V " + F(cy + 7) + " M " + F(cx - 3.5) + " " + F(cy - 3) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy - 3) + " H " + F(cx + 5.5) + " M " + F(cx - 3.5) + " " + F(cy + 1) + " H " + F(cx - 0.5) + " M " + F(cx + 2.5) + " " + F(cy + 1) + " H " + F(cx + 5.5) + "\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
                return true;
            case TopologyNodeKind.Server:
                sb.AppendLine("<path d=\"M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " V " + F(cy - 1) + " H " + F(cx - 7) + " Z M " + F(cx - 7) + " " + F(cy + 2) + " H " + F(cx + 7) + " V " + F(cy + 7) + " H " + F(cx - 7) + " Z M " + F(cx + 4.5) + " " + F(cy - 3.5) + " H " + F(cx + 5.5) + " M " + F(cx + 4.5) + " " + F(cy + 4.5) + " H " + F(cx + 5.5) + "\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.7\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>");
                return true;
            case TopologyNodeKind.Network:
            case TopologyNodeKind.NetworkSegment:
                sb.AppendLine("<path d=\"M " + F(cx - 6) + " " + F(cy + 5) + " L " + F(cx) + " " + F(cy - 5) + " L " + F(cx + 6) + " " + F(cy + 5) + " M " + F(cx) + " " + F(cy - 5) + " V " + F(cy + 7) + "\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><circle cx=\"" + F(cx) + "\" cy=\"" + F(cy - 6) + "\" r=\"2.3\" fill=\"" + EscapeAttr(color) + "\"/><circle cx=\"" + F(cx - 7) + "\" cy=\"" + F(cy + 6) + "\" r=\"2.3\" fill=\"" + EscapeAttr(color) + "\"/><circle cx=\"" + F(cx + 7) + "\" cy=\"" + F(cy + 6) + "\" r=\"2.3\" fill=\"" + EscapeAttr(color) + "\"/>");
                return true;
            case TopologyNodeKind.Service:
                sb.AppendLine("<circle cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"5.5\" fill=\"none\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.7\"/><path d=\"M " + F(cx) + " " + F(cy - 9) + " V " + F(cy - 7) + " M " + F(cx) + " " + F(cy + 7) + " V " + F(cy + 9) + " M " + F(cx - 9) + " " + F(cy) + " H " + F(cx - 7) + " M " + F(cx + 7) + " " + F(cy) + " H " + F(cx + 9) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.7\" stroke-linecap=\"round\"/>");
                return true;
            case TopologyNodeKind.Queue:
                sb.AppendLine("<path d=\"M " + F(cx - 7) + " " + F(cy - 6) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy) + " H " + F(cx + 7) + " M " + F(cx - 7) + " " + F(cy + 6) + " H " + F(cx + 7) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"2\" stroke-linecap=\"round\"/>");
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

    private static void DrawNodeStatuses(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, TopologyHighlightState highlight) {
        sb.AppendLine("<g class=\"" + prefix + "__status-badges\" data-cfx-role=\"topology-status-badges\">");
        foreach (var node in chart.Nodes) {
            if (EffectiveNodeDisplayMode(node, options) == TopologyNodeDisplayMode.Dot) continue;
            var color = theme.StatusColor(node.Status);
            var highlighted = highlight.IsNodeHighlighted(node);
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            sb.AppendLine("<g class=\"" + prefix + "__status-badge" + highlight.CssClass(prefix, highlighted) + "\" data-cfx-role=\"topology-node-status\" data-node-id=\"" + EscapeAttr(node.Id) + "\" data-cfx-status=\"" + node.Status + "\"" + highlight.SvgOpacity(highlighted) + ">");
            sb.AppendLine("<circle cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"8\" fill=\"" + EscapeAttr(color) + "\" stroke=\"" + EscapeAttr(theme.Background) + "\" stroke-width=\"2\"/>");
            sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + 3) + "\" text-anchor=\"middle\" fill=\"#FFFFFF\" font-size=\"9\" font-weight=\"800\">" + Escape(StatusGlyph(node.Status)) + "</text>");
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static void DrawLegend(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var height = LegendHeight(legend);
        var y = chart.Viewport.Height - chart.Viewport.Padding - height;
        var width = Math.Min(LegendMaxWidth, chart.Viewport.Width - chart.Viewport.Padding * 2);
        sb.AppendLine("<g class=\"" + prefix + "__legend\" data-cfx-role=\"topology-legend\">");
        sb.AppendLine("<rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" rx=\"12\" fill=\"" + EscapeAttr(theme.Card) + "\" stroke=\"" + EscapeAttr(theme.Border) + "\"/>");
        if (!string.IsNullOrWhiteSpace(legend.Title)) sb.AppendLine("<text x=\"" + F(x + 16) + "\" y=\"" + F(y + 23) + "\" fill=\"" + EscapeAttr(theme.Foreground) + "\" font-size=\"12\" font-weight=\"700\">" + Escape(legend.Title!) + "</text>");
        for (var i = 0; i < legend.Items.Count; i++) {
            var item = legend.Items[i];
            var col = i % LegendColumns;
            var row = i / LegendColumns;
            var itemX = x + 18 + col * LegendItemColumnWidth;
            var itemY = y + LegendFirstItemOffsetY + row * LegendItemRowHeight;
            var color = item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent);
            sb.AppendLine("<g class=\"" + prefix + "__legend-item\" data-cfx-role=\"topology-legend-item\" data-legend-kind=\"" + LegendKindToken(item.Kind) + "\">");
            if (item.Kind == TopologyLegendItemKind.Edge) sb.AppendLine("<line x1=\"" + F(itemX) + "\" y1=\"" + F(itemY - 4) + "\" x2=\"" + F(itemX + 24) + "\" y2=\"" + F(itemY - 4) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"2\" stroke-dasharray=\"6 4\"/>");
            else if (item.Kind == TopologyLegendItemKind.Node && !string.IsNullOrWhiteSpace(item.Symbol)) {
                sb.AppendLine("<rect x=\"" + F(itemX) + "\" y=\"" + F(itemY - 13) + "\" width=\"16\" height=\"16\" rx=\"4\" fill=\"" + EscapeAttr(StatusFill(color, theme.Background)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
                sb.AppendLine("<text x=\"" + F(itemX + 8) + "\" y=\"" + F(itemY - 2) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"7\" font-weight=\"800\">" + Escape(TrimTo(item.Symbol!.Trim(), 4)) + "</text>");
            } else sb.AppendLine("<circle cx=\"" + F(itemX + 8) + "\" cy=\"" + F(itemY - 5) + "\" r=\"6\" fill=\"" + EscapeAttr(color) + "\"/>");
            sb.AppendLine("<text x=\"" + F(itemX + 32) + "\" y=\"" + F(itemY) + "\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"11\">" + Escape(item.Label) + "</text>");
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static string EdgePath(IReadOnlyList<ChartPoint> points, TopologyEdgeRouting routing) {
        var x1 = points[0].X;
        var y1 = points[0].Y;
        var x2 = points[points.Count - 1].X;
        var y2 = points[points.Count - 1].Y;
        if (routing == TopologyEdgeRouting.Straight) return "M " + F(x1) + " " + F(y1) + " L " + F(x2) + " " + F(y2);
        if (routing == TopologyEdgeRouting.Curved && points.Count == 2) {
            var lift = Math.Max(40, Math.Abs(x2 - x1) * 0.12);
            return "M " + F(x1) + " " + F(y1) + " C " + F(x1) + " " + F(y1 - lift) + " " + F(x2) + " " + F(y2 - lift) + " " + F(x2) + " " + F(y2);
        }

        var path = new StringBuilder("M " + F(points[0].X) + " " + F(points[0].Y));
        for (var i = 1; i < points.Count; i++) path.Append(" L " + F(points[i].X) + " " + F(points[i].Y));
        return path.ToString();
    }

    private static void OpenLink(StringBuilder sb, string? href, string prefix, TopologyRenderOptions options) {
        var safe = SafeHref(href);
        if (safe == null) return;
        var target = options.OpenLinksInNewTab ? " target=\"_blank\" rel=\"noopener noreferrer\"" : string.Empty;
        sb.AppendLine("<a class=\"" + prefix + "__link\" href=\"" + EscapeAttr(safe) + "\"" + target + ">");
    }

    private static void CloseLink(StringBuilder sb, string? href) {
        if (SafeHref(href) != null) sb.AppendLine("</a>");
    }

    private static string BuildDescription(TopologyChart chart) {
        return (string.IsNullOrWhiteSpace(chart.Title) ? "Topology chart" : chart.Title) + " with " + chart.Groups.Count.ToString(CultureInfo.InvariantCulture) + " groups, " + chart.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes, and " + chart.Edges.Count.ToString(CultureInfo.InvariantCulture) + " edges.";
    }

    private static string CssToken(string value) => value.ToLowerInvariant();
}
