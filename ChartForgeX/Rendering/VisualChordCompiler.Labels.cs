using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualChordCompiler {
    private static void Labels(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartChordModel model, TextStyle[] styles, string[] labels, VisualThemeColors colors) {
        var nodes = model.Nodes.Where(node => node.IsVisible).ToArray();
        var requests = new List<LabelPlacementRequest>();
        var gap = context.Theme.Spacing;
        foreach (var node in nodes) {
            var angle = node.Start + node.Sweep / 2;
            var anchor = model.On(angle, model.OuterRadius + gap);
            var horizontal = Math.Cos(angle) > .25 ? 0d : Math.Cos(angle) < -.25 ? 1d : .5;
            var vertical = Math.Sin(angle) > .25 ? 0d : Math.Sin(angle) < -.25 ? 1d : .5;
            requests.Add(new LabelPlacementRequest(labels[node.Index], anchor, styles[node.Index], new[] {
                new LabelCandidate(0, 0, horizontal, vertical), new LabelCandidate(0, -gap, horizontal, 1),
                new LabelCandidate(0, gap, horizontal, 0), new LabelCandidate(0, -gap * 2, horizontal, 1), new LabelCandidate(0, gap * 2, horizontal, 0)
            }, priority: 40) { HasLeaderLine = true });
        }
        var contour = ChartCurveFlattening.Arc(model.CenterX, model.CenterY, model.OuterRadius, 0, Math.PI * 2, 1);
        var obstacles = new[] { new LabelObstacle("chord-marks", new LabelMarkShape(new[] { contour }, true, 0)) };
        var placed = new LabelPlacementService().Place(requests, plot, obstacles, 2, builder.MeasureText);
        for (var index = 0; index < placed.Count; index++) {
            var result = placed[index];
            if (result.IsDropped || result.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("chord.label-overflow", "Chord labels were shortened or omitted to fit; full labels and endpoint totals remain in node semantics."));
            if (result.IsDropped) continue;
            if (result.HasLeaderLine) builder.Line(result.Request.Anchor.X, result.Request.Anchor.Y, result.LeaderEnd.X, result.LeaderEnd.Y,
                colors.MutedForeground, 1, "chord-label-leader", paint: VisualChartPaint.Stroke(colors.MutedForeground, SvgColorRole.Text));
            var style = result.Request.Style.Clone(); style.FontSize = style.EffectiveFontSize; style.Baseline = TextBaseline.Normal;
            style.TextCase = TextCaseTransform.None; style.Alignment = TextAlignment.Left;
            builder.Text(result.Text, result.Bounds.Left, result.Bounds.Top + builder.TextAscent(style), style, "chord-node-label",
                ChartRelationshipMetadata.SourceId("node-label", nodes[index].Fact.Id), paint: VisualChartPaint.Text(style));
        }
    }
}
