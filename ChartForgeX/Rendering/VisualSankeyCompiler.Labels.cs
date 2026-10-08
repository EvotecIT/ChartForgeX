using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualSankeyCompiler {
    private static void Labels(VisualSceneBuilder builder, ChartRect plot, ChartSankeyModel model, TextStyle[] styles, string[] labels, VisualThemeColors colors, double gap) {
        for (int layer = 0; layer <= model.MaxLayer; layer++) {
            var nodes = model.Nodes.Where(n => n.Layer == layer).OrderBy(n => n.Y).ToArray(); if (nodes.Length == 0) continue;
            double x = nodes[0].X; bool left = layer == 0;
            double next = layer == model.MaxLayer ? plot.Right : model.Nodes.Where(n => n.Layer > layer).Min(n => n.X);
            var bounds = left ? new ChartRect(plot.X, plot.Y, Math.Max(0, x - plot.X - 8), plot.Height)
                : new ChartRect(x + model.NodeWidth + 8, plot.Y, Math.Max(0, next - x - model.NodeWidth - (layer == model.MaxLayer ? 8 : 16)), plot.Height);
            if (bounds.Width < 1) { builder.AddDiagnostic(new VisualDiagnostic("sankey.label-overflow", "No label column remains; full text is retained in node semantics.")); continue; }
            var requests = new List<LabelPlacementRequest>();
            foreach (var node in nodes) {
                var anchor = new ChartPoint(left ? bounds.Right : bounds.X, node.Y + node.Height / 2);
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
                    new LabelCandidate(0, 0, left ? 1 : 0, .5), new LabelCandidate(0, -gap, left ? 1 : 0, 1),
                    new LabelCandidate(0, gap, left ? 1 : 0, 0), new LabelCandidate(0, -gap * 2, left ? 1 : 0, 1), new LabelCandidate(0, gap * 2, left ? 1 : 0, 0)
                }, priority: 40) { MeasuredSize = measured });
            }
            var placed = new LabelPlacementService().Place(requests, bounds, null, 2, builder.MeasureText);
            for (int i = 0; i < placed.Count; i++) {
                var result = placed[i];
                if (result.IsDropped || result.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("sankey.label-overflow", "Sankey labels were shortened or omitted to fit; full text is retained in node semantics."));
                if (result.IsDropped) continue;
                var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
                style.TextCase = TextCaseTransform.None; style.Alignment = left ? TextAlignment.Right : TextAlignment.Left;
                builder.Rect(result.Bounds, ChartColorMath.WithOpacity(colors.Surface, .92), radius: 2, role: "sankey-label-backdrop",
                    paint: VisualChartPaint.Fill(SvgPaint.Of(colors.Surface, SvgColorRole.Surface).WithOpacity(ChartColorMath.WithOpacity(colors.Surface, .92), .92)));
                builder.Text(result.Text, left ? result.Bounds.Right : result.Bounds.Left, result.Bounds.Y + builder.TextAscent(style), style,
                    "sankey-node-label", Id("node-label", nodes[i].Index), paint: VisualChartPaint.Text(style));
            }
        }
    }
}
