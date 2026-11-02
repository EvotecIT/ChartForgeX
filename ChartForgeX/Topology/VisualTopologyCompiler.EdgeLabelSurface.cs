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

    /// <summary>Hugs the painted caption rows inside the reserved label box, centered like the text.</summary>
    private ChartRect QuietEdgeLabelSurface(TopologyEdge edge, ChartRect bounds, string?[] labels, double units) {
        if (labels.Length == 0 || EdgeCaptionContent(edge, labels[0]!).IsStructured) return bounds;
        var preferred = TextPolicySize(_context.Theme.Typography.DataLabelSize) * (edge.TextStyle?.FontSize.HasValue == true ? 1 : units) * _scale;
        double width = 0, height = 0;
        for (var i = 0; i < labels.Length; i++) {
            var size = preferred * (i == 0 ? 1 : .85);
            var weight = i == 0 ? EdgeLabelPrimaryWeight(_options) : 400;
            foreach (var row in TopologyTextLayout.AuthoredRows(labels[i]!, _options.AllowMultilineNodeLabels)) {
                var metrics = MeasurePolicyText(row, size, weight);
                width = Math.Max(width, metrics.Width); height += metrics.LineHeight;
            }
        }
        var w = Math.Min(bounds.Width, width + 2 * EdgeLabelQuietPaddingX * units * _scale);
        var h = Math.Min(bounds.Height, height + 2 * EdgeLabelQuietPaddingY * units * _scale);
        return new ChartRect(bounds.X + (bounds.Width - w) / 2, bounds.Y + (bounds.Height - h) / 2, w, h);
    }
}
