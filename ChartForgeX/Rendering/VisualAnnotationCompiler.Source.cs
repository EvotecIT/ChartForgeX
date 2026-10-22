using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;
using ChartForgeX.Typography;

namespace ChartForgeX.Rendering;

internal static partial class VisualAnnotationCompiler {
    /// <summary>Maps source drawings once into the shared scene consumed by every export backend.</summary>
    internal static void DrawSources(IReadOnlyList<ChartAnnotation> annotations, VisualRenderContext context,
        VisualSceneBuilder builder, ChartRect plot, Func<double, double> mapX, Func<ChartAnnotation, double, double> mapY,
        VisualThemeColors colors, bool bands, List<LabelObstacle> obstacles, Func<ChartAnnotation, bool>? isVisible = null) {
        for (var index = 0; index < annotations.Count; index++) {
            var annotation = annotations[index];
            if (!annotation.HasSourceGeometry || annotation.IsBand != bands) continue;
            var source = annotation.SourceStart!.Value;
            var sourceEnd = annotation.SourceEnd ?? source;
            var start = Project(source); var end = Project(sourceEnd);
            var upperStart = Project(new ChartPoint(source.X, source.Y + annotation.ChannelOffset));
            var upperEnd = Project(new ChartPoint(sourceEnd.X, sourceEnd.Y + annotation.ChannelOffset));
            var contour = annotation.IsBand ? new List<ChartPoint> { start, end, upperEnd, upperStart } : new List<ChartPoint> { start, end };
            var shape = new LabelMarkShape(new[] { contour }, annotation.IsBand,
                annotation.StrokeStyle?.Width ?? context.Theme.AxisStrokeWidth, plot);
            var left = Math.Max(plot.Left, Math.Min(start.X, end.X));
            var right = Math.Min(plot.Right, Math.Max(start.X, end.X));
            var top = Math.Max(plot.Top, Math.Min(Math.Min(start.Y, end.Y), Math.Min(upperStart.Y, upperEnd.Y)));
            var bottom = Math.Min(plot.Bottom, Math.Max(Math.Max(start.Y, end.Y), Math.Max(upperStart.Y, upperEnd.Y)));
            var visible = (annotation.Kind == ChartAnnotationKind.Note ? ChartPlotClip.Contains(plot, start.X, start.Y)
                : shape.Intersects(plot)) && (isVisible == null || isVisible(annotation));
            var bounds = new ChartRect(Math.Max(plot.Left, Math.Min(plot.Right, left)), Math.Max(plot.Top, Math.Min(plot.Bottom, top)),
                Math.Max(0, right - left), Math.Max(0, bottom - top));
            var id = annotation.Id is ChartAnnotationId authoredId ? VisualAnnotationIdentity.SourceIdFor(authoredId) : "annotation-" + Number(index);
            var description = annotation.Kind + ": (" + Number(source.X) + ", " + Number(source.Y) + ")"
                + (annotation.SourceEnd.HasValue ? " to (" + Number(sourceEnd.X) + ", " + Number(sourceEnd.Y) + ")" : "")
                + (annotation.Kind == ChartAnnotationKind.ParallelChannel ? ", Y offset " + Number(annotation.ChannelOffset) : "")
                + (annotation.Label.Length > 0 ? ": " + annotation.Label : "");
            var metadata = new Dictionary<string, string> {
                ["data-cfx-kind"] = annotation.Kind.ToString(), ["data-cfx-source-x"] = Number(source.X), ["data-cfx-source-y"] = Number(source.Y),
                ["data-cfx-source-end-x"] = annotation.SourceEnd.HasValue ? Number(sourceEnd.X) : "",
                ["data-cfx-source-end-y"] = annotation.SourceEnd.HasValue ? Number(sourceEnd.Y) : "",
                ["data-cfx-channel-offset"] = Number(annotation.ChannelOffset), ["data-cfx-y-axis"] = annotation.SourceYAxis.ToString(),
                ["data-cfx-label"] = annotation.Label, ["data-cfx-show-label"] = annotation.ShowLabel ? "true" : "false",
                ["data-cfx-visible"] = visible ? "true" : "false", ["aria-label"] = description
            };
            if (annotation.Id.HasValue) metadata[VisualAnnotationIdentity.Attribute] = annotation.Id.Value.Value;
            var color = annotation.UsesThemeColor ? colors.MutedForeground : annotation.Color;
            var role = annotation.UsesThemeColor ? SvgColorRole.Text : SvgColorRole.Axis;
            using (builder.PushGroup(id, "annotation", metadata)) {
                builder.AddRegion(new VisualSemanticRegion(id, "annotation", bounds, description));
                if (!visible) continue;
                using (builder.PushClip(plot)) {
                    if (annotation.Kind == ChartAnnotationKind.Note) builder.Ellipse(start.X, start.Y, 2, 2, color,
                        role: "annotation-anchor", paint: VisualChartPaint.Fill(SvgPaint.Of(color, role)));
                    else {
                        if (annotation.IsBand) {
                            builder.Path(new ChartPath(new[] { ChartPathCommand.MoveTo(start.X, start.Y), ChartPathCommand.LineTo(end.X, end.Y),
                                ChartPathCommand.LineTo(upperEnd.X, upperEnd.Y), ChartPathCommand.LineTo(upperStart.X, upperStart.Y) }),
                                fill: ChartColorMath.WithOpacity(color, annotation.Opacity), close: true, role: "annotation-channel",
                                paint: VisualChartPaint.Fill(SvgPaint.Of(color, role).WithOpacity(ChartColorMath.WithOpacity(color, annotation.Opacity), annotation.Opacity)));
                            Boundary(upperStart, upperEnd);
                        }
                        Boundary(start, end);
                    }
                }
                // A drawing caption belongs to its source midpoint; clipping never changes that source anchor.
                var anchor = new ChartPoint(start.X / 2 + end.X / 2, start.Y / 2 + end.Y / 2);
                if (annotation.ShowLabel && annotation.Label.Length > 0 && ChartPlotClip.Contains(plot, anchor.X, anchor.Y))
                    DrawSourceLabel(annotation, id, anchor, plot, context, builder, color, role, colors, obstacles);
            }

            ChartPoint Project(ChartPoint point) {
                var x = mapX(point.X); var y = mapY(annotation, point.Y);
                if (!ChartMath.IsFinite(x) || !ChartMath.IsFinite(y))
                    throw new InvalidOperationException("Source annotation coordinates cannot be projected into finite logical units.");
                return new ChartPoint(x, y);
            }
            void Boundary(ChartPoint first, ChartPoint last) {
                var stroke = VisualReferenceStroke.Paint(annotation.StrokeStyle, color, role, out var ink);
                builder.Line(first.X, first.Y, last.X, last.Y, ink, annotation.StrokeStyle?.Width ?? context.Theme.AxisStrokeWidth,
                    role: "annotation-source-line", dash: annotation.StrokeStyle?.ResolveDash(Array.Empty<double>()), paint: stroke);
            }
        }
    }

    private static void DrawSourceLabel(ChartAnnotation annotation, string id, ChartPoint anchor, ChartRect plot,
        VisualRenderContext context, VisualSceneBuilder builder, ChartColor color, SvgColorRole role, VisualThemeColors colors,
        List<LabelObstacle> obstacles) {
        var plate = new ChartColorBlend(colors.Surface, SvgColorRole.Surface, color, role, .12);
        var ink = ChartColorBlend.Contrast(plate);
        var style = new TextStyle { Font = context.Font, FontSize = context.Theme.Typography.AxisSize, Color = ink.Color };
        var gap = context.Theme.Spacing;
        var candidates = new[] { new LabelCandidate(gap, -gap, 0, 1), new LabelCandidate(-gap, -gap, 1, 1),
            new LabelCandidate(gap, gap), new LabelCandidate(-gap, gap, 1), new LabelCandidate(0, -gap, .5, 1), new LabelCandidate(0, gap, .5) };
        var label = new LabelPlacementService().Place(new[] { new LabelPlacementRequest(annotation.Label, anchor, style, candidates) {
            Padding = 4, MeasuredSize = builder.MeasureText(annotation.Label, style), Fallback = LabelFallbackRule.EllipsisThenDrop
        } }, plot, obstacles, 2, builder.MeasureText)[0];
        if (label.IsDropped || label.IsEllipsized) builder.AddDiagnostic(new VisualDiagnostic("cartesian.annotation-label-overflow",
            "A source annotation label was shortened or omitted to avoid marks and other labels within the plot."));
        if (label.IsDropped) return;
        obstacles.Add(new LabelObstacle(id + "-label", label.Bounds));
        var nearest = new ChartPoint(Math.Max(label.Bounds.Left, Math.Min(label.Bounds.Right, anchor.X)),
            Math.Max(label.Bounds.Top, Math.Min(label.Bounds.Bottom, anchor.Y)));
        builder.Line(anchor.X, anchor.Y, nearest.X, nearest.Y, color, context.Theme.AxisStrokeWidth,
            role: "annotation-label-leader", paint: VisualChartPaint.Stroke(SvgPaint.Of(color, role)));
        builder.Rect(label.Bounds, plate.Color, ChartColorMath.WithOpacity(color, .36), radius: Math.Min(4, context.Theme.BarRadius),
            role: "annotation-label-backplate", paint: new VisualScenePaintBinding(fill: plate.Paint,
                stroke: SvgPaint.Of(color, role).WithOpacity(ChartColorMath.WithOpacity(color, .36), .36)));
        builder.Text(label.Text, label.Bounds.Left + 4, label.Bounds.Top + 4 + builder.TextAscent(style), style,
            role: "annotation-label", paint: ink.Paint);
    }
}
