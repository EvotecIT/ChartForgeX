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
    // A caller that already describes its mark can reuse annotation painting without a nested interaction target.
    internal static void Draw(IReadOnlyList<ChartAnnotation> annotations, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, Func<double, double> mapX, Func<double, double> mapY, VisualThemeColors colors, bool bands, List<LabelObstacle> obstacles, Func<ChartAnnotation, bool>? include = null, string overflowCode = "cartesian.annotation-label-overflow", string idPrefix = "annotation-", bool describeAnnotations = true, SvgColorRole colorRole = SvgColorRole.Axis, Func<ChartAnnotation, ChartAnnotationKind>? kindSelector = null, Func<ChartAnnotation, bool>? isVisible = null, ChartRect? labelBounds = null, bool clipGeometry = false) {
        for (var index = 0; index < annotations.Count; index++) {
            var annotation = annotations[index];
            if (annotation.EndValue.HasValue != bands || (include != null && !include(annotation))) continue;
            var kind = kindSelector != null ? kindSelector(annotation) : annotation.Kind;
            var horizontal = kind == ChartAnnotationKind.HorizontalLine || kind == ChartAnnotationKind.HorizontalBand;
            var visible = isVisible == null || isVisible(annotation);
            var position = horizontal ? Math.Max(plot.Top, Math.Min(plot.Bottom, mapY(annotation.Value))) : Math.Max(plot.Left, Math.Min(plot.Right, mapX(annotation.Value)));
            var color = annotation.UsesThemeColor ? colors.MutedForeground : annotation.Color;
            var annotationColorRole = annotation.UsesThemeColor ? SvgColorRole.Text : colorRole;
            var bounds = horizontal ? new ChartRect(plot.Left, position, plot.Width, 0) : new ChartRect(position, plot.Top, 0, plot.Height);
            var id = idPrefix + Number(index);
            var description = kind + ": " + Number(annotation.Value)
                + (annotation.EndValue.HasValue ? " to " + Number(annotation.EndValue.Value) : string.Empty)
                + (annotation.Label.Length > 0 ? ": " + annotation.Label : string.Empty);
            using (describeAnnotations ? builder.PushGroup(id, "annotation", new Dictionary<string, string> {
                ["data-cfx-kind"] = kind.ToString(), ["data-cfx-value"] = Number(annotation.Value),
                ["data-cfx-end-value"] = annotation.EndValue.HasValue ? Number(annotation.EndValue.Value) : string.Empty,
                ["data-cfx-label"] = annotation.Label, ["data-cfx-show-label"] = annotation.ShowLabel ? "true" : "false", ["aria-label"] = description
            }) : null) {
            using (clipGeometry ? builder.PushClip(plot) : null) {
            if (annotation.EndValue.HasValue) {
                var end = horizontal ? Math.Max(plot.Top, Math.Min(plot.Bottom, mapY(annotation.EndValue.Value))) : Math.Max(plot.Left, Math.Min(plot.Right, mapX(annotation.EndValue.Value)));
                bounds = horizontal ? new ChartRect(plot.Left, Math.Min(position, end), plot.Width, Math.Abs(end - position))
                    : new ChartRect(Math.Min(position, end), plot.Top, Math.Abs(end - position), plot.Height);
                if (visible) builder.Rect(bounds, ChartColorMath.WithOpacity(color, annotation.Opacity), role: "annotation-band",
                    paint: VisualChartPaint.Fill(SvgPaint.Of(color, annotationColorRole).WithOpacity(ChartColorMath.WithOpacity(color, annotation.Opacity), annotation.Opacity)));
            } else if (visible) {
                var start = horizontal ? new ChartPoint(plot.Left, position) : new ChartPoint(position, plot.Top);
                var end = horizontal ? new ChartPoint(plot.Right, position) : new ChartPoint(position, plot.Bottom);
                var stroke = VisualReferenceStroke.Paint(annotation.StrokeStyle, color, annotationColorRole, out var strokeColor);
                builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y) }),
                    stroke: strokeColor, strokeWidth: annotation.StrokeStyle?.Width ?? context.Theme.AxisStrokeWidth, role: "annotation-line", cap: VisualStrokeCap.Butt,
                    dash: annotation.StrokeStyle?.ResolveDash(new[] { ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap })
                        ?? new[] { ChartVisualPrimitives.AnnotationLineDash, ChartVisualPrimitives.AnnotationLineGap }, paint: stroke);
            }
            }
            if (describeAnnotations) builder.AddRegion(new VisualSemanticRegion(id, "annotation", bounds, description));
            if (visible && annotation.ShowLabel && !string.IsNullOrEmpty(annotation.Label)) {
                var plate = new ChartColorBlend(colors.Surface, SvgColorRole.Surface, color, annotationColorRole, .12);
                var backplate = plate.Color;
                var ink = ChartColorBlend.Contrast(plate);
                var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = ink.Color };
                var anchor = new ChartPoint(horizontal ? plot.Left + context.Theme.Spacing : position,
                    horizontal ? position : plot.Top + context.Theme.Spacing);
                const double labelPadding = 4;
                var metrics = builder.MeasureText(annotation.Label, style);
                var available = labelBounds ?? plot;
                var candidates = AnnotationCandidates(horizontal, bands, anchor, plot, available, context.Theme.Spacing,
                    metrics.Width + labelPadding * 2, metrics.Height + labelPadding * 2);
                var request = new LabelPlacementRequest(annotation.Label, anchor, style, candidates) {
                    Padding = labelPadding, MeasuredSize = metrics,
                    Fallback = LabelFallbackRule.Drop
                };
                var placement = new LabelPlacementService();
                var preferred = available.Top < plot.Top
                    ? new ChartRect(available.Left, plot.Top, available.Width, available.Bottom - plot.Top) : available;
                var label = placement.Place(new[] { request }, preferred, obstacles, 2, builder.MeasureText)[0];
                if (label.IsDropped && available.Top < preferred.Top) {
                    var titleStrip = new ChartRect(available.Left, available.Top, available.Width, preferred.Top - available.Top);
                    label = placement.Place(new[] { request }, titleStrip, obstacles, 2, builder.MeasureText)[0];
                }
                // Candidate alignments must remain available when a multiline caption
                // shortens to fewer rows. Its original height cannot prefilter them.
                if (label.IsDropped && annotation.Label.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length > 2) {
                    request.Fallback = LabelFallbackRule.EllipsisThenDrop;
                    label = placement.Place(new[] { request }, preferred, obstacles, 2, builder.MeasureText)[0];
                }
                if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic(overflowCode, "An annotation label was shortened or omitted to avoid marks and other labels within the plot."));
                if (!label.IsDropped) {
                    obstacles.Add(new LabelObstacle(id + "-label", label.Bounds));
                    if (horizontal && (label.Bounds.Left > plot.Right || label.Bounds.Top > position + context.Theme.Spacing + .001
                        || label.Bounds.Bottom < position - context.Theme.Spacing - .001)) {
                        var guideX = Math.Min(plot.Right, label.Bounds.Left + label.Bounds.Width / 2);
                        var endpoint = new ChartPoint(Math.Max(label.Bounds.Left, Math.Min(label.Bounds.Right, guideX)),
                            Math.Max(label.Bounds.Top, Math.Min(label.Bounds.Bottom, position)));
                        builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(guideX, position), ChartPathCommand.LineTo(endpoint.X, endpoint.Y) }),
                            stroke: color, strokeWidth: context.Theme.AxisStrokeWidth, role: "annotation-label-leader",
                            paint: VisualChartPaint.Stroke(SvgPaint.Of(color, annotationColorRole)));
                    }
                    builder.Rect(label.Bounds, backplate, ChartColorMath.WithOpacity(color, .36), radius: Math.Min(4, context.Theme.BarRadius),
                        role: "annotation-label-backplate", paint: new VisualScenePaintBinding(
                            fill: plate.Paint,
                            stroke: SvgPaint.Of(color, annotationColorRole).WithOpacity(ChartColorMath.WithOpacity(color, .36), .36)));
                    builder.Text(label.Text, label.Bounds.Left + labelPadding, label.Bounds.Top + labelPadding + builder.TextAscent(style), style,
                        role: "annotation-label", paint: ink.Paint);
                }
            }
            }
        }
    }

    /// <summary>Keeps captions close to their guide, then tries measured parallel lanes before shortening.</summary>
    private static IReadOnlyList<LabelCandidate> AnnotationCandidates(bool horizontal, bool band, ChartPoint anchor,
        ChartRect plot, ChartRect available, double spacing, double width, double height) {
        var candidates = new List<LabelCandidate>();
        if (horizontal) {
            candidates.Add(new LabelCandidate(0, band ? spacing : -spacing, 0, band ? 0 : 1));
            candidates.Add(new LabelCandidate(0, spacing, 0, 0));
            var right = plot.Right - spacing - anchor.X;
            candidates.Add(new LabelCandidate(right, band ? spacing : -spacing, 1, band ? 0 : 1));
            candidates.Add(new LabelCandidate(right, spacing, 1, 0));
            // Search complete measured captions from both ends of the guide, rather than
            // treating a collision at its corners as evidence that the words cannot fit.
            var travel = Math.Max(0, plot.Width - spacing * 2 - width);
            var steps = Math.Min(128, (int)Math.Ceiling(travel / Math.Max(2, spacing)));
            for (var lane = 0; lane <= 4; lane++) {
                var away = spacing + height * lane / 4;
                for (var step = 0; step <= steps; step++) {
                    var offset = steps == 0 ? 0 : travel * step / steps;
                    foreach (var x in new[] { offset, travel - offset }) {
                        candidates.Add(new LabelCandidate(x, -away, 0, 1));
                        candidates.Add(new LabelCandidate(x, away));
                    }
                }
            }
            if (available.Right > plot.Right + spacing + width) {
                var outside = plot.Right + spacing - anchor.X;
                candidates.Add(new LabelCandidate(outside, 0, 0, .5));
            }
            // A shallow plot can have room for the complete caption with a smaller
            // guide gap than the preferred spacing. Search its measured upper and
            // lower edges before treating those unused pixels as overflow.
            foreach (var top in new[] { plot.Top, plot.Bottom - height }) {
                if (!(top + height <= anchor.Y - 2 || top >= anchor.Y + 2)) continue;
                for (var step = 0; step <= steps; step++) {
                    var offset = steps == 0 ? 0 : travel * step / steps;
                    candidates.Add(new LabelCandidate(offset, top - anchor.Y));
                    candidates.Add(new LabelCandidate(travel - offset, top - anchor.Y));
                }
            }
            // A complete caption can use unused space beside a painted axis title.
            // The caller supplies the captured axis text as collision obstacles.
            if (available.Top < plot.Top && available.Top + height <= anchor.Y - 2) {
                for (var step = 0; step <= steps; step++) {
                    var offset = steps == 0 ? 0 : travel * step / steps;
                    candidates.Add(new LabelCandidate(offset, available.Top - anchor.Y));
                    candidates.Add(new LabelCandidate(travel - offset, available.Top - anchor.Y));
                }
            }
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
