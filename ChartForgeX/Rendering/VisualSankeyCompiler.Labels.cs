using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSankeyCompiler {
    private static void Labels(VisualSceneBuilder builder, ChartRect plot, ChartSankeyModel model, TextStyle[] styles, string[] labels, VisualThemeColors colors, double gap, ChartSankeyOptions options) {
        var columns = LabelColumns(plot, model, options);
        var requests = new List<LabelPlacementRequest>();
        var requestedNodes = new List<ChartSankeyNode>();
        for (int layer = 0; layer <= model.MaxLayer; layer++) {
            var nodes = model.Nodes.Where(n => n.Layer == layer).OrderBy(n => n.Y).ToArray(); if (nodes.Length == 0) continue;
            var column = columns[layer]; var bounds = column.Bounds;
            if (bounds.Width < 1) { builder.AddDiagnostic(new VisualDiagnostic("sankey.label-overflow", "No label column remains; full text is retained in node semantics.")); continue; }
            foreach (var node in nodes) {
                var anchor = new ChartPoint(column.Anchor, node.Y + node.Height / 2);
                var text = labels[node.Index]; var style = styles[node.Index]; var measured = builder.MeasureText(text, style);
                if (measured.Width > bounds.Width) {
                    var lines = ChartLabelWrapping.BalancedTwoLine(text, style.EffectiveFontSize, bounds.Width,
                        (value, _) => builder.MeasureText(value, style).Width);
                    var wrapped = string.Join("\n", lines); var wrappedSize = builder.MeasureText(wrapped, style);
                    if (lines.Length == 2 && wrappedSize.Width <= bounds.Width && wrappedSize.Height <= node.Height) {
                        text = wrapped; measured = wrappedSize;
                    }
                }
                requests.Add(new LabelPlacementRequest(text, anchor, style, new[] {
                    new LabelCandidate(0, 0, column.Alignment, .5), new LabelCandidate(0, -gap, column.Alignment, 1),
                    new LabelCandidate(0, gap, column.Alignment, 0), new LabelCandidate(0, -gap * 2, column.Alignment, 1), new LabelCandidate(0, gap * 2, column.Alignment, 0)
                }, priority: 40) { MeasuredSize = measured, Bounds = bounds });
                requestedNodes.Add(node);
            }
        }
        var placed = new LabelPlacementService().Place(requests, plot, null, 2, builder.MeasureText);
        for (int i = 0; i < placed.Count; i++) {
            var result = placed[i];
            if (result.IsDropped || result.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("sankey.label-overflow", "Sankey labels were shortened or omitted to fit; full text is retained in node semantics."));
            if (result.IsDropped) continue;
            var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
            var side = columns[requestedNodes[i].Layer].Side;
            style.TextCase = TextCaseTransform.None;
            style.Alignment = side == ChartSankeyLabelPlacement.Left ? TextAlignment.Right : side == ChartSankeyLabelPlacement.Center ? TextAlignment.Center : TextAlignment.Left;
            builder.Rect(result.Bounds, ChartColorMath.WithOpacity(colors.Surface, .92), radius: 2, role: "sankey-label-backdrop",
                paint: VisualChartPaint.Fill(SvgPaint.Of(colors.Surface, SvgColorRole.Surface).WithOpacity(ChartColorMath.WithOpacity(colors.Surface, .92), .92)));
            double x = side == ChartSankeyLabelPlacement.Left ? result.Bounds.Right : side == ChartSankeyLabelPlacement.Center ? result.Bounds.X + result.Bounds.Width / 2 : result.Bounds.Left;
            builder.Text(result.Text, x, result.Bounds.Y + builder.TextAscent(style), style,
                "sankey-node-label", ChartRelationshipMetadata.SourceId("node-label", requestedNodes[i].Id), paint: VisualChartPaint.Text(style));
        }
    }
}
