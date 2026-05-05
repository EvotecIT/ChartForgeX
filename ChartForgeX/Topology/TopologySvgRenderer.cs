using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

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
        var prepared = TopologyLayoutEngine.Prepare(chart, options.View);
        var validation = new TopologyChartValidator().Validate(prepared);
        if (!validation.IsValid) throw new TopologyValidationException(validation);

        var theme = prepared.Theme ?? TopologyTheme.Light();
        var prefix = string.IsNullOrWhiteSpace(options.CssClassPrefix) ? "cfx-topology" : options.CssClassPrefix!;
        var id = SanitizeId(string.IsNullOrWhiteSpace(prepared.Id) ? "topology" : prepared.Id!);
        var w = prepared.Viewport.Width;
        var h = prepared.Viewport.Height;
        var style = options.UseResponsiveSvg ? " style=\"max-width:100%;height:auto;display:block\"" : string.Empty;
        var sb = new StringBuilder();

        sb.AppendLine("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"" + F(w) + "\" height=\"" + F(h) + "\" viewBox=\"0 0 " + F(w) + " " + F(h) + "\" role=\"img\" aria-labelledby=\"" + id + "-title " + id + "-desc\"" + style + " shape-rendering=\"geometricPrecision\" text-rendering=\"geometricPrecision\">");
        sb.AppendLine("<title id=\"" + id + "-title\">" + Escape(string.IsNullOrWhiteSpace(prepared.Title) ? "ChartForgeX topology" : prepared.Title!) + "</title>");
        sb.AppendLine("<desc id=\"" + id + "-desc\">" + Escape(BuildDescription(prepared)) + "</desc>");
        DrawDefs(sb, id, prefix, theme, options);
        sb.AppendLine("<g id=\"" + id + "\" class=\"" + prefix + "\" data-cfx-role=\"topology\" data-chart-id=\"" + EscapeAttr(prepared.Id ?? id) + "\" data-layout-mode=\"" + prepared.LayoutMode + "\">");
        sb.AppendLine("<rect class=\"" + prefix + "__background\" width=\"100%\" height=\"100%\" fill=\"" + EscapeAttr(theme.Background) + "\"/>");
        if (options.IncludeTitle) DrawHeader(sb, prepared, prefix, theme);
        DrawGroups(sb, prepared, prefix, theme, options);
        DrawEdges(sb, prepared, prefix, theme, options, id);
        DrawEdgeLabels(sb, prepared, prefix, theme);
        DrawNodes(sb, prepared, prefix, theme, options);
        DrawNodeStatuses(sb, prepared, prefix, theme);
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
            sb.Append("</style>");
            sb.AppendLine();
        }

        sb.AppendLine("<filter id=\"" + id + "-shadow\" x=\"-20%\" y=\"-20%\" width=\"140%\" height=\"150%\"><feDropShadow dx=\"0\" dy=\"10\" stdDeviation=\"12\" flood-color=\"#0F172A\" flood-opacity=\"0.10\"/></filter>");
        foreach (var status in Enum.GetValues(typeof(TopologyHealthStatus)).Cast<TopologyHealthStatus>()) {
            var color = theme.StatusColor(status);
            sb.AppendLine("<marker id=\"" + id + "-arrow-" + status.ToString().ToLowerInvariant() + "\" viewBox=\"0 0 10 10\" refX=\"8\" refY=\"5\" markerWidth=\"7\" markerHeight=\"7\" orient=\"auto-start-reverse\"><path d=\"M 0 0 L 10 5 L 0 10 z\" fill=\"" + EscapeAttr(color) + "\"/></marker>");
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

    private static void DrawGroups(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        sb.AppendLine("<g class=\"" + prefix + "__groups\" data-cfx-role=\"topology-groups\">");
        foreach (var group in chart.Groups) {
            OpenLink(sb, group.Href, prefix, options);
            sb.AppendLine("<g id=\"" + SafeElementId(chart.Id, "group", group.Id) + "\" class=\"" + prefix + "__group " + prefix + "__group--" + CssToken(group.Status.ToString()) + "\" data-cfx-role=\"topology-group\" data-group-id=\"" + EscapeAttr(group.Id) + "\" data-cfx-status=\"" + group.Status + "\">");
            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(group.Tooltip)) sb.AppendLine("<title>" + Escape(group.Tooltip!) + "</title>");
            var status = theme.StatusColor(group.Status);
            sb.AppendLine("<rect class=\"" + prefix + "__group-card\" x=\"" + F(group.X) + "\" y=\"" + F(group.Y) + "\" width=\"" + F(group.Width) + "\" height=\"" + F(group.Height) + "\" rx=\"16\" fill=\"" + EscapeAttr(Tint(status)) + "\" stroke=\"" + EscapeAttr(status) + "\" stroke-opacity=\"0.48\"/>");
            sb.AppendLine("<text x=\"" + F(group.X + 24) + "\" y=\"" + F(group.Y + 30) + "\" fill=\"" + EscapeAttr(status) + "\" font-size=\"16\" font-weight=\"700\">" + Escape(group.Label) + "</text>");
            if (!string.IsNullOrWhiteSpace(group.Subtitle)) sb.AppendLine("<text x=\"" + F(group.X + 24) + "\" y=\"" + F(group.Y + 50) + "\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"12\">" + Escape(group.Subtitle!) + "</text>");
            sb.AppendLine("</g>");
            CloseLink(sb, group.Href);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawEdges(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options, string svgId) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        sb.AppendLine("<g class=\"" + prefix + "__edges\" data-cfx-role=\"topology-edges\">");
        foreach (var edge in chart.Edges) {
            var source = nodes[edge.SourceNodeId];
            var target = nodes[edge.TargetNodeId];
            var color = theme.StatusColor(edge.Status);
            var dash = EdgeDash(edge.Status);
            var markerEnd = edge.Direction is TopologyDirection.Forward or TopologyDirection.Bidirectional ? " marker-end=\"url(#" + svgId + "-arrow-" + edge.Status.ToString().ToLowerInvariant() + ")\"" : string.Empty;
            var markerStart = edge.Direction is TopologyDirection.Backward or TopologyDirection.Bidirectional ? " marker-start=\"url(#" + svgId + "-arrow-" + edge.Status.ToString().ToLowerInvariant() + ")\"" : string.Empty;
            OpenLink(sb, edge.Href, prefix, options);
            sb.AppendLine("<g id=\"" + SafeElementId(chart.Id, "edge", edge.Id) + "\" class=\"" + prefix + "__edge-wrap " + prefix + "__edge-wrap--" + CssToken(edge.Status.ToString()) + "\" data-cfx-role=\"topology-edge\" data-edge-id=\"" + EscapeAttr(edge.Id) + "\" data-source-node-id=\"" + EscapeAttr(edge.SourceNodeId) + "\" data-target-node-id=\"" + EscapeAttr(edge.TargetNodeId) + "\" data-edge-kind=\"" + edge.Kind + "\" data-cfx-status=\"" + edge.Status + "\">");
            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(edge.Tooltip)) sb.AppendLine("<title>" + Escape(edge.Tooltip!) + "</title>");
            sb.AppendLine("<path class=\"" + prefix + "__edge\" d=\"" + EdgePath(source, target, edge.Routing) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"2.2\" stroke-dasharray=\"" + dash + "\" opacity=\"0.94\"" + markerStart + markerEnd + "/>");
            sb.AppendLine("</g>");
            CloseLink(sb, edge.Href);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawEdgeLabels(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        var nodes = chart.Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal);
        sb.AppendLine("<g class=\"" + prefix + "__edge-labels\" data-cfx-role=\"topology-edge-labels\">");
        foreach (var edge in chart.Edges.Where(edge => !string.IsNullOrWhiteSpace(edge.Label) || !string.IsNullOrWhiteSpace(edge.SecondaryLabel))) {
            var source = nodes[edge.SourceNodeId];
            var target = nodes[edge.TargetNodeId];
            var cx = (CenterX(source) + CenterX(target)) / 2;
            var cy = (CenterY(source) + CenterY(target)) / 2;
            var label = edge.Label ?? string.Empty;
            var secondary = edge.SecondaryLabel ?? string.Empty;
            var width = Math.Max(48, Math.Max(label.Length, secondary.Length) * 7.2 + 18);
            var height = string.IsNullOrWhiteSpace(secondary) ? 22 : 38;
            sb.AppendLine("<g class=\"" + prefix + "__edge-label\" data-cfx-role=\"topology-edge-label\" data-edge-id=\"" + EscapeAttr(edge.Id) + "\">");
            sb.AppendLine("<rect x=\"" + F(cx - width / 2) + "\" y=\"" + F(cy - height / 2) + "\" width=\"" + F(width) + "\" height=\"" + F(height) + "\" rx=\"9\" fill=\"" + EscapeAttr(theme.Background) + "\" stroke=\"" + EscapeAttr(theme.Border) + "\"/>");
            if (!string.IsNullOrWhiteSpace(label)) sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + (string.IsNullOrWhiteSpace(secondary) ? 4 : -2)) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(theme.StatusColor(edge.Status)) + "\" font-size=\"12\" font-weight=\"700\">" + Escape(label) + "</text>");
            if (!string.IsNullOrWhiteSpace(secondary)) sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + 15) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"10\">" + Escape(secondary) + "</text>");
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static void DrawNodes(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme, TopologyRenderOptions options) {
        sb.AppendLine("<g class=\"" + prefix + "__nodes\" data-cfx-role=\"topology-nodes\">");
        foreach (var node in chart.Nodes) {
            var color = theme.StatusColor(node.Status);
            OpenLink(sb, node.Href, prefix, options);
            sb.AppendLine("<g id=\"" + SafeElementId(chart.Id, "node", node.Id) + "\" class=\"" + prefix + "__node " + prefix + "__node--" + CssToken(node.Kind.ToString()) + " " + prefix + "__node--" + CssToken(node.Status.ToString()) + "\" data-cfx-role=\"topology-node\" data-node-id=\"" + EscapeAttr(node.Id) + "\" data-node-kind=\"" + node.Kind + "\" data-cfx-status=\"" + node.Status + "\"" + (string.IsNullOrWhiteSpace(node.GroupId) ? string.Empty : " data-group-id=\"" + EscapeAttr(node.GroupId!) + "\"") + ">");
            if (options.IncludeTooltips && !string.IsNullOrWhiteSpace(node.Tooltip)) sb.AppendLine("<title>" + Escape(node.Tooltip!) + "</title>");
            sb.AppendLine("<rect class=\"" + prefix + "__node-card\" x=\"" + F(node.X) + "\" y=\"" + F(node.Y) + "\" width=\"" + F(node.Width) + "\" height=\"" + F(node.Height) + "\" rx=\"10\" fill=\"" + EscapeAttr(theme.Card) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"1.5\" filter=\"url(#" + SanitizeId(chart.Id ?? "topology") + "-shadow)\"/>");
            DrawNodeIcon(sb, node, prefix, theme, color);
            sb.AppendLine("<text x=\"" + F(node.X + 42) + "\" y=\"" + F(node.Y + 28) + "\" fill=\"" + EscapeAttr(theme.Foreground) + "\" font-size=\"12.5\" font-weight=\"700\">" + Escape(TrimTo(node.Label, 18)) + "</text>");
            if (!string.IsNullOrWhiteSpace(node.Subtitle)) sb.AppendLine("<text x=\"" + F(node.X + 42) + "\" y=\"" + F(node.Y + 47) + "\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"10.5\">" + Escape(TrimTo(node.Subtitle!, 18)) + "</text>");
            sb.AppendLine("</g>");
            CloseLink(sb, node.Href);
        }

        sb.AppendLine("</g>");
    }

    private static void DrawNodeIcon(StringBuilder sb, TopologyNode node, string prefix, TopologyTheme theme, string color) {
        var cx = node.X + 22;
        var cy = node.Y + node.Height / 2;
        sb.AppendLine("<g class=\"" + prefix + "__node-icon\" data-node-kind=\"" + node.Kind + "\">");
        if (node.Kind is TopologyNodeKind.Cloud or TopologyNodeKind.Forest) {
            sb.AppendLine("<circle cx=\"" + F(cx - 5) + "\" cy=\"" + F(cy) + "\" r=\"7\" fill=\"" + EscapeAttr(Tint(color)) + "\" stroke=\"" + EscapeAttr(color) + "\"/><circle cx=\"" + F(cx + 4) + "\" cy=\"" + F(cy - 2) + "\" r=\"8\" fill=\"" + EscapeAttr(Tint(color)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
        } else if (node.Kind is TopologyNodeKind.Database) {
            sb.AppendLine("<ellipse cx=\"" + F(cx) + "\" cy=\"" + F(cy - 7) + "\" rx=\"10\" ry=\"4\" fill=\"" + EscapeAttr(Tint(color)) + "\" stroke=\"" + EscapeAttr(color) + "\"/><path d=\"M " + F(cx - 10) + " " + F(cy - 7) + " V " + F(cy + 7) + " A 10 4 0 0 0 " + F(cx + 10) + " " + F(cy + 7) + " V " + F(cy - 7) + "\" fill=\"" + EscapeAttr(Tint(color)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
        } else {
            sb.AppendLine("<rect x=\"" + F(cx - 11) + "\" y=\"" + F(cy - 11) + "\" width=\"22\" height=\"22\" rx=\"6\" fill=\"" + EscapeAttr(Tint(color)) + "\" stroke=\"" + EscapeAttr(color) + "\"/>");
            sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + 4) + "\" text-anchor=\"middle\" fill=\"" + EscapeAttr(color) + "\" font-size=\"9\" font-weight=\"800\">" + Escape(KindGlyph(node.Kind)) + "</text>");
        }

        sb.AppendLine("</g>");
    }

    private static void DrawNodeStatuses(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        sb.AppendLine("<g class=\"" + prefix + "__status-badges\" data-cfx-role=\"topology-status-badges\">");
        foreach (var node in chart.Nodes) {
            var color = theme.StatusColor(node.Status);
            var cx = node.X + node.Width - 11;
            var cy = node.Y + 11;
            sb.AppendLine("<g class=\"" + prefix + "__status-badge\" data-cfx-role=\"topology-node-status\" data-node-id=\"" + EscapeAttr(node.Id) + "\" data-cfx-status=\"" + node.Status + "\">");
            sb.AppendLine("<circle cx=\"" + F(cx) + "\" cy=\"" + F(cy) + "\" r=\"8\" fill=\"" + EscapeAttr(color) + "\" stroke=\"" + EscapeAttr(theme.Background) + "\" stroke-width=\"2\"/>");
            sb.AppendLine("<text x=\"" + F(cx) + "\" y=\"" + F(cy + 3) + "\" text-anchor=\"middle\" fill=\"#FFFFFF\" font-size=\"9\" font-weight=\"800\">" + Escape(StatusGlyph(node.Status)) + "</text>");
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static void DrawLegend(StringBuilder sb, TopologyChart chart, string prefix, TopologyTheme theme) {
        var legend = chart.Legend!;
        var x = chart.Viewport.Padding;
        var y = chart.Viewport.Height - chart.Viewport.Padding - 86;
        var width = Math.Min(560, chart.Viewport.Width - chart.Viewport.Padding * 2);
        sb.AppendLine("<g class=\"" + prefix + "__legend\" data-cfx-role=\"topology-legend\">");
        sb.AppendLine("<rect x=\"" + F(x) + "\" y=\"" + F(y) + "\" width=\"" + F(width) + "\" height=\"86\" rx=\"12\" fill=\"" + EscapeAttr(theme.Card) + "\" stroke=\"" + EscapeAttr(theme.Border) + "\"/>");
        if (!string.IsNullOrWhiteSpace(legend.Title)) sb.AppendLine("<text x=\"" + F(x + 16) + "\" y=\"" + F(y + 23) + "\" fill=\"" + EscapeAttr(theme.Foreground) + "\" font-size=\"12\" font-weight=\"700\">" + Escape(legend.Title!) + "</text>");
        for (var i = 0; i < legend.Items.Count; i++) {
            var item = legend.Items[i];
            var col = i % 4;
            var row = i / 4;
            var itemX = x + 18 + col * 132;
            var itemY = y + 46 + row * 24;
            var color = item.Color ?? (item.Status.HasValue ? theme.StatusColor(item.Status.Value) : theme.Accent);
            sb.AppendLine("<g class=\"" + prefix + "__legend-item\" data-cfx-role=\"topology-legend-item\" data-legend-kind=\"" + EscapeAttr(item.Kind) + "\">");
            if (item.Kind == "edge") sb.AppendLine("<line x1=\"" + F(itemX) + "\" y1=\"" + F(itemY - 4) + "\" x2=\"" + F(itemX + 24) + "\" y2=\"" + F(itemY - 4) + "\" stroke=\"" + EscapeAttr(color) + "\" stroke-width=\"2\" stroke-dasharray=\"6 4\"/>");
            else sb.AppendLine("<circle cx=\"" + F(itemX + 8) + "\" cy=\"" + F(itemY - 5) + "\" r=\"6\" fill=\"" + EscapeAttr(color) + "\"/>");
            sb.AppendLine("<text x=\"" + F(itemX + 32) + "\" y=\"" + F(itemY) + "\" fill=\"" + EscapeAttr(theme.MutedForeground) + "\" font-size=\"11\">" + Escape(item.Label) + "</text>");
            sb.AppendLine("</g>");
        }

        sb.AppendLine("</g>");
    }

    private static string EdgePath(TopologyNode source, TopologyNode target, TopologyEdgeRouting routing) {
        var x1 = CenterX(source);
        var y1 = CenterY(source);
        var x2 = CenterX(target);
        var y2 = CenterY(target);
        if (routing == TopologyEdgeRouting.Straight) return "M " + F(x1) + " " + F(y1) + " L " + F(x2) + " " + F(y2);
        if (routing == TopologyEdgeRouting.Curved) {
            var lift = Math.Max(40, Math.Abs(x2 - x1) * 0.12);
            return "M " + F(x1) + " " + F(y1) + " C " + F(x1) + " " + F(y1 - lift) + " " + F(x2) + " " + F(y2 - lift) + " " + F(x2) + " " + F(y2);
        }

        var mx = (x1 + x2) / 2;
        return "M " + F(x1) + " " + F(y1) + " L " + F(mx) + " " + F(y1) + " L " + F(mx) + " " + F(y2) + " L " + F(x2) + " " + F(y2);
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

    private static string? SafeHref(string? href) {
        if (href == null) return null;
        if (string.IsNullOrWhiteSpace(href)) return null;
        var value = href.Trim();
        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("javascript:", StringComparison.Ordinal) || lower.StartsWith("data:", StringComparison.Ordinal) || lower.StartsWith("vbscript:", StringComparison.Ordinal)) return null;
        return value;
    }

    private static string BuildDescription(TopologyChart chart) {
        return (string.IsNullOrWhiteSpace(chart.Title) ? "Topology chart" : chart.Title) + " with " + chart.Groups.Count.ToString(CultureInfo.InvariantCulture) + " groups, " + chart.Nodes.Count.ToString(CultureInfo.InvariantCulture) + " nodes, and " + chart.Edges.Count.ToString(CultureInfo.InvariantCulture) + " edges.";
    }

    private static double CenterX(TopologyNode node) => node.X + node.Width / 2;

    private static double CenterY(TopologyNode node) => node.Y + node.Height / 2;

    private static string EdgeDash(TopologyHealthStatus status) {
        return status switch {
            TopologyHealthStatus.Warning => "8 5",
            TopologyHealthStatus.Critical => "8 5",
            TopologyHealthStatus.Unknown => "5 6",
            TopologyHealthStatus.Disabled => "3 6",
            _ => "none"
        };
    }

    private static string KindGlyph(TopologyNodeKind kind) {
        return kind switch {
            TopologyNodeKind.HubSite => "H",
            TopologyNodeKind.BranchSite => "B",
            TopologyNodeKind.DomainController => "DC",
            TopologyNodeKind.BridgeheadServer => "BH",
            TopologyNodeKind.Subnet => "S",
            TopologyNodeKind.SubnetGroup => "SG",
            TopologyNodeKind.Gateway => "GW",
            TopologyNodeKind.Service => "SV",
            TopologyNodeKind.Endpoint => "EP",
            TopologyNodeKind.Certificate => "CA",
            TopologyNodeKind.Domain => "D",
            TopologyNodeKind.Region => "R",
            _ => "N"
        };
    }

    private static string StatusGlyph(TopologyHealthStatus status) {
        return status switch {
            TopologyHealthStatus.Healthy => "OK",
            TopologyHealthStatus.Warning => "!",
            TopologyHealthStatus.Critical => "!",
            TopologyHealthStatus.Disabled => "-",
            _ => "?"
        };
    }

    private static string Tint(string color) {
        return color switch {
            "#16A34A" or "#22C55E" => "#ECFDF3",
            "#F97316" or "#FB923C" => "#FFF7ED",
            "#EF4444" or "#F87171" => "#FEF2F2",
            "#64748B" or "#94A3B8" => "#F8FAFC",
            _ => "#EFF6FF"
        };
    }

    private static string TrimTo(string value, int max) {
        if (value.Length <= max) return value;
        return value.Substring(0, Math.Max(0, max - 3)) + "...";
    }

    private static string CssToken(string value) => value.ToLowerInvariant();

    private static string SafeElementId(string? chartId, string kind, string id) => SanitizeId((chartId ?? "topology") + "-" + kind + "-" + id);

    private static string SanitizeId(string value) {
        var sb = new StringBuilder(value.Length);
        foreach (var ch in value) {
            if (char.IsLetterOrDigit(ch) || ch == '-' || ch == '_') sb.Append(ch);
            else sb.Append('-');
        }

        return sb.Length == 0 ? "topology" : sb.ToString();
    }

    private static string F(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string CssFontFamily(string value) => value.Replace(";", " ").Replace("{", " ").Replace("}", " ").Replace("<", " ").Replace(">", " ");

    private static string Escape(string value) => value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string EscapeAttr(string value) => Escape(value).Replace("\"", "&quot;");
}
