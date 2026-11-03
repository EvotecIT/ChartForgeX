using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;
using ChartForgeX.Themes;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
    /// <summary>The painted surface beneath a relationship caption: the canvas or its containing group.</summary>
    private (ChartColor Fill, SvgPaint Paint, TopologyGroup? Group) LabelUnderlay(TopologyEdgeLabelLayout layout) {
        var group = EdgeLabelClearanceGroup(_chart, layout);
        if (group == null || !_options.IncludeGroups) return (_colors.Background, BackgroundPaint(_colors.Background), group);
        if (!string.IsNullOrWhiteSpace(group.BackgroundColor)) {
            var authored = Color(group.BackgroundColor, _colors.Surface);
            return (authored, GroupFillPaint(group, authored, true, true), group);
        }
        if (UseNeutralGroupSurface(_options)) return (_colors.Surface, CardPaint(_colors.Surface), group);
        var accent = Color(group.Color ?? ResolveGroupIcon(group, _options)?.Color, Status(group.Status));
        var tint = IsMonitoringDashboardStyle(_options) ? .055 : .10;
        var fill = ChartColorMath.BlendPremultiplied(_colors.Background, accent, tint);
        return (fill, SvgPaint.Mix(fill, BackgroundPaint(_colors.Background), GroupAccentPaint(group, accent, true), tint), group);
    }

    /// <summary>
    /// One chip for a caption and its technology/secondary lines: as wide as the widest line, as tall as the
    /// stacked lines, centered in the reserved label box. Returns the row cells the text must use, or null
    /// when structured content keeps the ordinary row allocation.
    /// </summary>
    private (ChartRect Surface, ChartRect[]? Rows) QuietEdgeLabelSurface(TopologyEdge edge, ChartRect bounds, string?[] labels, double units) {
        if (labels.Length == 0 || EdgeCaptionContent(edge, labels[0]!).IsStructured) return (bounds, null);
        var preferred = TextPolicySize(_context.Theme.Typography.DataLabelSize) * (edge.TextStyle?.FontSize.HasValue == true ? 1 : units) * _scale;
        double width = 0, height = 0;
        var heights = new double[labels.Length];
        for (var i = 0; i < labels.Length; i++) {
            var size = preferred * (i == 0 ? 1 : .85);
            var weight = i == 0 ? EdgeLabelPrimaryWeight(_options) : 400;
            foreach (var row in TopologyTextLayout.AuthoredRows(labels[i]!, _options.AllowMultilineNodeLabels)) {
                var metrics = MeasurePolicyText(row, size, weight);
                width = Math.Max(width, metrics.Width); heights[i] += metrics.LineHeight;
            }
            height += heights[i];
        }
        var padY = EdgeLabelQuietPaddingY * units * _scale;
        var w = Math.Min(bounds.Width, width + 2 * EdgeLabelQuietPaddingX * units * _scale);
        var h = Math.Min(bounds.Height, height + 2 * padY);
        var surface = new ChartRect(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
        var rows = new ChartRect[labels.Length];
        var y = surface.Y + Math.Max(0, (h - height) / 2);
        for (var i = 0; i < labels.Length; i++) { rows[i] = new ChartRect(bounds.X, y, bounds.Width, heights[i]); y += heights[i]; }
        return (surface, rows);
    }
}
