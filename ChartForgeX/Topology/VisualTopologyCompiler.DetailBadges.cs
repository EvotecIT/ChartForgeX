using System;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Typography;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    private TextMetrics DetailBadgeMetrics(TopologyNodeDetail detail, double size) {
        var outputSize = RoleTextSize(size, detail.TextStyle) * _scale;
        var style = detail.TextStyle == null ? TextPolicyStyle(outputSize, 400) : RoleTextStyle(outputSize, 400, detail.TextStyle);
        return TextLayoutEngine.Measure(DisplayRows(DetailContent(detail)), style);
    }
    private double DetailRowHeight(TopologyNodeDetail detail, double size, double legacyLine) {
        if (detail.BadgeStyle == null) return legacyLine * TopologyTextLayout.AuthoredRowCount(NodeDetailText(detail), _options.AllowMultilineNodeLabels);
        var style = detail.BadgeStyle; var metrics = DetailBadgeMetrics(detail, size);
        return Math.Max(style.MinimumHeight, metrics.Height / _scale + 4) + style.Gap;
    }
    private double DetailRowsHeight(TopologyNode node, double size, double legacyLine) => node.Details.All(detail => detail.BadgeStyle == null)
        ? NodeDetailRows(node, _options) * legacyLine : node.Details.Sum(detail => DetailRowHeight(detail, size, legacyLine));
    private void BuildDetailBadgeOrText(TopologyNodeDetail detail, ChartRect bounds, double size, bool active) {
        var style = detail.BadgeStyle;
        if (style == null) { BuildDetailText(detail, bounds, size, active); return; }
        var metrics = DetailBadgeMetrics(detail, size);
        var plan = NativeBadgePaintPlan.Measured(new ChartRect(bounds.X, bounds.Y, bounds.Width, Math.Max(0, bounds.Height - style.Gap * _scale)),
            metrics.Width, metrics.Height, RoleTextAscent(RoleTextSize(size, detail.TextStyle) * _scale, 400, detail.TextStyle),
            style.HorizontalPadding * _scale, style.MinimumHeight * _scale, style.CornerRadius * _scale, 4 * _scale);
        ChartColor? Ink(ChartColor? color) => color.HasValue ? Highlight(color.Value, active) : null;
        plan.AppendSurfaceTo(_builder, Ink(style.Fill), Ink(style.Stroke), style.StrokeWidth * _scale, "topology-detail-badge");
        if (metrics.Width > plan.Text.Width + 1e-8 || metrics.Height > plan.Text.Height + 1e-8)
            _builder.AddDiagnostic(new VisualDiagnostic("topology.detail-badge-overflow", "A fixed detail badge cannot contain its complete native text; full text remains semantic data. Enlarge the node or enable FitLabelToContent."));
        BuildDetailText(detail, plan.Text, size, active);
    }
}
