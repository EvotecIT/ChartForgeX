using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Flattens typed scene geometry at paint resolution, without parsing serialized path data.</summary>
internal static class VisualSceneGeometry {
    internal static List<List<ChartPoint>> Flatten(VisualScenePath path, double pixelsPerUnit) {
        var contours = new List<List<ChartPoint>>();
        List<ChartPoint>? current = null;
        var point = default(ChartPoint);
        foreach (var command in path.Commands) {
            if (command.Kind == ChartPathCommandKind.MoveTo) {
                Finish(current, path.Close);
                current = new List<ChartPoint>(); contours.Add(current);
                point = new ChartPoint(command.X, command.Y); current.Add(point);
            } else if (current != null && command.Kind == ChartPathCommandKind.LineTo) {
                point = new ChartPoint(command.X, command.Y); current.Add(point);
            } else if (current != null && command.Kind == ChartPathCommandKind.CubicTo) {
                var control1 = new ChartPoint(command.Control1X, command.Control1Y);
                var control2 = new ChartPoint(command.Control2X, command.Control2Y);
                var end = new ChartPoint(command.X, command.Y);
                var count = ChartCurveFlattening.CubicSegments(point, control1, control2, end, pixelsPerUnit);
                for (var i = 1; i <= count; i++) {
                    var t = i / (double)count; var u = 1 - t;
                    current.Add(new ChartPoint(u * u * u * point.X + 3 * u * u * t * control1.X + 3 * u * t * t * control2.X + t * t * t * end.X,
                        u * u * u * point.Y + 3 * u * u * t * control1.Y + 3 * u * t * t * control2.Y + t * t * t * end.Y));
                }
                point = end;
            }
        }
        Finish(current, path.Close);
        return contours;
    }

    internal static List<List<ChartPoint>> Flatten(VisualSceneSlice slice, double pixelsPerUnit) {
        var contours = new List<List<ChartPoint>>();
        if (slice.Outer <= 0 || slice.Sweep <= 0) return contours;
        if (slice.Sweep >= Math.PI * 2 - 0.000001) {
            var outer = ChartCurveFlattening.Arc(slice.Cx, slice.Cy, slice.Outer, slice.Start, Math.PI * 2, pixelsPerUnit);
            Finish(outer, true); contours.Add(outer);
            if (slice.Inner > 0) {
                var inner = ChartCurveFlattening.Arc(slice.Cx, slice.Cy, slice.Inner, slice.Start, -Math.PI * 2, pixelsPerUnit);
                Finish(inner, true); contours.Add(inner);
            }
        } else {
            var ring = ChartCurveFlattening.Arc(slice.Cx, slice.Cy, slice.Outer, slice.Start, slice.Sweep, pixelsPerUnit);
            if (slice.Inner > 0) ring.AddRange(ChartCurveFlattening.Arc(slice.Cx, slice.Cy, slice.Inner, slice.Start + slice.Sweep, -slice.Sweep, pixelsPerUnit));
            else ring.Add(new ChartPoint(slice.Cx, slice.Cy));
            Finish(ring, true); contours.Add(ring);
        }
        return contours;
    }

    private static void Finish(List<ChartPoint>? points, bool close) {
        if (!close || points == null || points.Count == 0) return;
        var first = points[0]; var last = points[points.Count - 1];
        if (first.X != last.X || first.Y != last.Y) points.Add(first);
    }
}
