using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    // Use the text already shaped for paint, including axis rotations, rather than
    // measuring an authored title again or treating its whole reserved strip as ink.
    private static void CaptureAxisTextObstacles(VisualScene axes, List<LabelObstacle> obstacles) {
        var transforms = new Stack<VisualSceneTransform>();
        var transform = VisualSceneTransform.Identity;
        foreach (var node in axes.Nodes) {
            if (node is VisualSceneGroup group) {
                transforms.Push(transform);
                if (group.Rotation.HasValue) transform = transform.Rotate(group.Rotation.Value);
                if (group.Translation.HasValue) transform = transform.Translate(group.Translation.Value);
            } else if (node is VisualSceneEndGroup) transform = transforms.Pop();
            else if (node is VisualSceneText text) {
                var left = double.PositiveInfinity; var right = double.NegativeInfinity;
                foreach (var line in text.Text.Lines) {
                    left = Math.Min(left, text.LineLeft(line));
                    right = Math.Max(right, text.LineLeft(line) + line.Width);
                }
                var top = text.Baseline - text.Text.Ascent;
                var bottom = top + text.Text.Metrics.Height;
                var stroke = text.Stroke.HasValue ? text.StrokeWidth / 2 : 0;
                left -= stroke; right += stroke; top -= stroke; bottom += stroke;
                var x = double.PositiveInfinity; var y = double.PositiveInfinity;
                var endX = double.NegativeInfinity; var endY = double.NegativeInfinity;
                foreach (var corner in new[] { new ChartPoint(left, top), new ChartPoint(right, top), new ChartPoint(left, bottom), new ChartPoint(right, bottom) }) {
                    var point = transform.Apply(corner);
                    x = Math.Min(x, point.X); y = Math.Min(y, point.Y);
                    endX = Math.Max(endX, point.X); endY = Math.Max(endY, point.Y);
                }
                obstacles.Add(new LabelObstacle("axis-text-" + obstacles.Count,
                    new ChartRect(x, y, endX - x, endY - y)));
            }
        }
    }

    private static void DrawAnnotations(Chart chart, VisualRenderContext context, VisualSceneBuilder builder,
        ChartRect plot, ChartMapper map, VisualThemeColors colors, bool bands, List<LabelObstacle> obstacles,
        bool constrainXToPlot, ChartAxisWindow? xViewport = null, ChartRect? labelBounds = null, bool clipGeometry = false,
        ChartMapper? secondaryMap = null) {
        var horizontalValues = ChartCartesianOrientation.IsHorizontal(chart);
        VisualAnnotationCompiler.Draw(chart.Annotations, context, builder, plot, map.X, map.Y, colors, bands, obstacles,
            kindSelector: annotation => ChartCartesianOrientation.AnnotationKind(annotation, horizontalValues),
            isVisible: constrainXToPlot ? annotation => IsAnnotationVisible(annotation, plot, map, xViewport,
                ChartCartesianOrientation.AnnotationKind(annotation, horizontalValues)) : null,
            labelBounds: labelBounds, clipGeometry: clipGeometry);
        VisualAnnotationCompiler.DrawSources(chart.Annotations, context, builder, plot, map.X,
            (annotation, value) => (annotation.SourceYAxis == ChartAxisSide.Secondary ? secondaryMap! : map).Y(value), colors, bands, obstacles);
    }

    private static bool IsAnnotationVisible(ChartAnnotation annotation, ChartRect plot, ChartMapper map, ChartAxisWindow? xViewport, ChartAnnotationKind kind) {
        if (kind == ChartAnnotationKind.HorizontalLine || kind == ChartAnnotationKind.HorizontalBand) return true;
        if (xViewport != null && map.HasCalendarBandAxis(horizontalAxis: true)) return map.OverlapsSourceWindow(xViewport, annotation.Value, annotation.EndValue ?? annotation.Value, horizontalAxis: true);
        var minimum = xViewport?.Minimum ?? plot.Left;
        var maximum = xViewport?.Maximum ?? plot.Right;
        var value = xViewport != null ? annotation.Value : map.X(annotation.Value);
        if (!annotation.EndValue.HasValue) return value >= minimum && value <= maximum;
        var end = xViewport != null ? annotation.EndValue.Value : map.X(annotation.EndValue.Value);
        return Math.Max(value, end) > minimum && Math.Min(value, end) < maximum;
    }
}
