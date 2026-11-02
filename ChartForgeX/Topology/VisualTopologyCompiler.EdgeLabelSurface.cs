using System;
using ChartForgeX.Primitives;
using static ChartForgeX.Topology.TopologyRenderPrimitives;

namespace ChartForgeX.Topology;

internal sealed partial class VisualTopologyCompiler {
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
