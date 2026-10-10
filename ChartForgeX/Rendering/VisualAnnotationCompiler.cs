using System;
using System.Collections.Generic;
using System.Globalization;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static class VisualAnnotationCompiler {
    private static string Number(double value) => value.ToString("G17", CultureInfo.InvariantCulture);
    internal static void Draw(IReadOnlyList<ChartAnnotation> annotations, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, Func<double, double> mapX, Func<double, double> mapY, VisualThemeColors colors, bool bands, List<LabelObstacle> obstacles, Func<ChartAnnotation, bool>? include = null, string overflowCode = "cartesian.annotation-label-overflow", string idPrefix = "annotation-") {
        for (var index = 0; index < annotations.Count; index++) {
            var annotation = annotations[index];
            if (annotation.EndValue.HasValue != bands || (include != null && !include(annotation))) continue;
            var horizontal = annotation.Kind == ChartAnnotationKind.HorizontalLine || annotation.Kind == ChartAnnotationKind.HorizontalBand;
            var position = horizontal ? Math.Max(plot.Top, Math.Min(plot.Bottom, mapY(annotation.Value))) : Math.Max(plot.Left, Math.Min(plot.Right, mapX(annotation.Value)));
            var color = annotation.Color;
            var bounds = horizontal ? new ChartRect(plot.Left, position, plot.Width, 0) : new ChartRect(position, plot.Top, 0, plot.Height);
            var id = idPrefix + Number(index);
            var description = annotation.Kind + ": " + Number(annotation.Value)
                + (annotation.EndValue.HasValue ? " to " + Number(annotation.EndValue.Value) : string.Empty)
                + (annotation.Label.Length > 0 ? ": " + annotation.Label : string.Empty);
            using (builder.PushGroup(id, "annotation", new Dictionary<string, string> {
                ["data-cfx-kind"] = annotation.Kind.ToString(), ["data-cfx-value"] = Number(annotation.Value),
                ["data-cfx-end-value"] = annotation.EndValue.HasValue ? Number(annotation.EndValue.Value) : string.Empty,
                ["data-cfx-label"] = annotation.Label, ["data-cfx-show-label"] = annotation.ShowLabel ? "true" : "false", ["aria-label"] = description
            })) {
            if (annotation.EndValue.HasValue) {
                var end = horizontal ? Math.Max(plot.Top, Math.Min(plot.Bottom, mapY(annotation.EndValue.Value))) : Math.Max(plot.Left, Math.Min(plot.Right, mapX(annotation.EndValue.Value)));
                bounds = horizontal ? new ChartRect(plot.Left, Math.Min(position, end), plot.Width, Math.Abs(end - position))
                    : new ChartRect(Math.Min(position, end), plot.Top, Math.Abs(end - position), plot.Height);
                builder.Rect(bounds, ChartColorMath.WithOpacity(color, annotation.Opacity), role: "annotation-band",
                    paint: VisualChartPaint.Fill(SvgPaint.Of(color, SvgColorRole.Axis).WithOpacity(ChartColorMath.WithOpacity(color, annotation.Opacity), annotation.Opacity)));
            } else {
                var start = horizontal ? new ChartPoint(plot.Left, position) : new ChartPoint(position, plot.Top);
                var end = horizontal ? new ChartPoint(plot.Right, position) : new ChartPoint(position, plot.Bottom);
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y) }),
                    stroke: color, strokeWidth: context.Theme.AxisStrokeWidth, role: "annotation-line", cap: VisualStrokeCap.Butt,
                    dash: new[] { ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap }, paint: VisualChartPaint.Stroke(color, SvgColorRole.Axis));
            }
            builder.AddRegion(new VisualSemanticRegion(id, "annotation", bounds, description));
            if (annotation.ShowLabel && !string.IsNullOrEmpty(annotation.Label)) {
                var plate = new ChartColorBlend(colors.Surface, SvgColorRole.Surface, color, SvgColorRole.Axis, .12);
                var backplate = plate.Color;
                var ink = ChartColorBlend.Contrast(plate);
                var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = ink.Color };
                var anchor = new ChartPoint(horizontal ? plot.Left + context.Theme.Spacing : position,
                    horizontal ? position : plot.Top + context.Theme.Spacing);
                const double labelPadding = 4;
                var metrics = builder.MeasureText(annotation.Label, style);
                var candidates = AnnotationCandidates(horizontal, bands, anchor, plot, context.Theme.Spacing, metrics.Height + labelPadding * 2);
                var request = new LabelPlacementRequest(annotation.Label, anchor, style, candidates) { Padding = labelPadding, MeasuredSize = metrics };
                var label = new LabelPlacementService().Place(new[] { request }, plot, obstacles, 2, builder.MeasureText)[0];
                if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic(overflowCode, "An annotation label was shortened or omitted to avoid marks and other labels within the plot."));
                if (!label.IsDropped) {
                    obstacles.Add(new LabelObstacle(id + "-label", label.Bounds));
                    builder.Rect(label.Bounds, backplate, ChartColorMath.WithOpacity(color, .36), radius: Math.Min(4, context.Theme.BarRadius),
                        role: "annotation-label-backplate", paint: new VisualScenePaintBinding(
                            fill: plate.Paint,
                            stroke: SvgPaint.Of(color, SvgColorRole.Axis).WithOpacity(ChartColorMath.WithOpacity(color, .36), .36)));
                    builder.Text(label.Text, label.Bounds.Left + labelPadding, label.Bounds.Top + labelPadding + builder.TextAscent(style), style,
                        role: "annotation-label", paint: ink.Paint);
                }
            }
            }
        }
    }

    /// <summary>Keeps captions close to their guide, then tries measured parallel lanes before shortening.</summary>
    private static IReadOnlyList<LabelCandidate> AnnotationCandidates(bool horizontal, bool band, ChartPoint anchor,
        ChartRect plot, double spacing, double height) {
        var candidates = new List<LabelCandidate>();
        if (horizontal) {
            candidates.Add(new LabelCandidate(0, band ? spacing : -spacing, 0, band ? 0 : 1));
            candidates.Add(new LabelCandidate(0, spacing, 0, 0));
            var right = plot.Right - spacing - anchor.X;
            candidates.Add(new LabelCandidate(right, band ? spacing : -spacing, 1, band ? 0 : 1));
            candidates.Add(new LabelCandidate(right, spacing, 1, 0));
            return candidates;
        }
        // The caption follows the vertical guide's upper edge. Left and right retain
        // its relationship to the same x coordinate, even when a nearby peak is tall.
        candidates.Add(new LabelCandidate(spacing, 0));
        candidates.Add(new LabelCandidate(-spacing, 0, 1, 0));
        foreach (var lane in new[] { height + spacing, (height + spacing) * 2 }) {
            candidates.Add(new LabelCandidate(spacing, lane));
            candidates.Add(new LabelCandidate(-spacing, lane, 1, 0));
        }
        var bottom = plot.Bottom - spacing - anchor.Y;
        candidates.Add(new LabelCandidate(spacing, bottom, 0, 1));
        candidates.Add(new LabelCandidate(-spacing, bottom, 1, 1));
        return candidates;
    }
}
