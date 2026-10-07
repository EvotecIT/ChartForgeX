using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Topology;

/// <summary>Numeric diagram surfaces shared by the prepared exporters, without an SVG parsing round trip.</summary>
internal static partial class TopologyNodeShapeGeometry {
    internal readonly struct ScenePart {
        internal ScenePart(ChartPath path, bool close, bool fill) { Path = path; Close = close; Fill = fill; }
        internal ChartPath Path { get; }
        internal bool Close { get; }
        internal bool Fill { get; }
    }

    internal static IReadOnlyList<ScenePart> SceneParts(TopologyNode node) {
        var x = node.X; var y = node.Y; var w = node.Width; var h = node.Height;
        var inset = Math.Min(w * 0.18, h * 0.3);
        var parts = new List<ScenePart>();
        void Polygon(params double[] points) {
            var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(points[0], points[1]) };
            for (var i = 2; i < points.Length; i += 2) commands.Add(ChartPathCommand.LineTo(points[i], points[i + 1]));
            parts.Add(new ScenePart(new ChartPath(commands), true, true));
        }
        void Open(params double[] points) {
            var commands = new List<ChartPathCommand> { ChartPathCommand.MoveTo(points[0], points[1]) };
            for (var i = 2; i < points.Length; i += 2) commands.Add(ChartPathCommand.LineTo(points[i], points[i + 1]));
            parts.Add(new ScenePart(new ChartPath(commands), false, false));
        }
        void Ellipse(double cx, double cy, double rx, double ry, bool fill = true) {
            const double k = 0.5522847498307936;
            parts.Add(new ScenePart(new ChartPath(new[] {
                ChartPathCommand.MoveTo(cx, cy - ry),
                ChartPathCommand.CubicTo(cx + k * rx, cy - ry, cx + rx, cy - k * ry, cx + rx, cy),
                ChartPathCommand.CubicTo(cx + rx, cy + k * ry, cx + k * rx, cy + ry, cx, cy + ry),
                ChartPathCommand.CubicTo(cx - k * rx, cy + ry, cx - rx, cy + k * ry, cx - rx, cy),
                ChartPathCommand.CubicTo(cx - rx, cy - k * ry, cx - k * rx, cy - ry, cx, cy - ry)
            }), true, fill));
        }
        switch (node.Shape ?? TopologyNodeShape.Rounded) {
            case TopologyNodeShape.Actor:
                var cx = x + w / 2; var unit = Math.Min(w / 48, h / 76);
                Ellipse(cx, y + unit * 15, unit * 9, unit * 9);
                Open(cx, y + unit * 24, cx, y + unit * 50);
                Open(cx - unit * 17, y + unit * 34, cx + unit * 17, y + unit * 34);
                Open(cx - unit * 15, y + unit * 68, cx, y + unit * 50, cx + unit * 15, y + unit * 68);
                break;
            case TopologyNodeShape.Ellipse:
            case TopologyNodeShape.Circle:
            case TopologyNodeShape.DoubleCircle:
                var rx = node.Shape == TopologyNodeShape.Ellipse ? w / 2 : Math.Min(w, h) / 2;
                var ry = node.Shape == TopologyNodeShape.Ellipse ? h / 2 : rx;
                Ellipse(x + w / 2, y + h / 2, rx, ry);
                if (node.Shape == TopologyNodeShape.DoubleCircle) Ellipse(x + w / 2, y + h / 2, Math.Max(0, rx - 5), Math.Max(0, ry - 5), false);
                break;
            case TopologyNodeShape.Diamond: Polygon(x + w / 2, y, x + w, y + h / 2, x + w / 2, y + h, x, y + h / 2); break;
            case TopologyNodeShape.Hexagon: Polygon(x + inset, y, x + w - inset, y, x + w, y + h / 2, x + w - inset, y + h, x + inset, y + h, x, y + h / 2); break;
            case TopologyNodeShape.Parallelogram: Polygon(x + inset, y, x + w, y, x + w - inset, y + h, x, y + h); break;
            case TopologyNodeShape.ParallelogramAlt: Polygon(x, y, x + w - inset, y, x + w, y + h, x + inset, y + h); break;
            case TopologyNodeShape.Trapezoid: Polygon(x + inset, y, x + w - inset, y, x + w, y + h, x, y + h); break;
            case TopologyNodeShape.TrapezoidAlt: Polygon(x, y, x + w, y, x + w - inset, y + h, x + inset, y + h); break;
            case TopologyNodeShape.Asymmetric: Polygon(x, y, x + w - inset, y, x + w, y + h / 2, x + w - inset, y + h, x, y + h, x + inset, y + h / 2); break;
            case TopologyNodeShape.Subroutine:
                Polygon(x, y, x + w, y, x + w, y + h, x, y + h);
                Open(x + 9, y, x + 9, y + h); Open(x + w - 9, y, x + w - 9, y + h);
                break;
            case TopologyNodeShape.Cylinder:
                var cap = Math.Min(14, h * 0.2);
                parts.Add(new ScenePart(new ChartPath(new[] {
                    ChartPathCommand.MoveTo(x, y + cap), ChartPathCommand.CubicTo(x, y - cap / 3, x + w, y - cap / 3, x + w, y + cap),
                    ChartPathCommand.LineTo(x + w, y + h - cap), ChartPathCommand.CubicTo(x + w, y + h + cap / 3, x, y + h + cap / 3, x, y + h - cap)
                }), true, true));
                parts.Add(new ScenePart(new ChartPath(new[] { ChartPathCommand.MoveTo(x, y + cap), ChartPathCommand.CubicTo(x, y + cap * 2.3, x + w, y + cap * 2.3, x + w, y + cap) }), false, false));
                break;
            case TopologyNodeShape.Cloud:
                parts.Add(new ScenePart(new ChartPath(new[] {
                    ChartPathCommand.MoveTo(x + w * .16, y + h * .8),
                    ChartPathCommand.CubicTo(x - w * .08, y + h * .8, x - w * .02, y + h * .25, x + w * .2, y + h * .28),
                    ChartPathCommand.CubicTo(x + w * .17, y - h * .06, x + w * .56, y - h * .06, x + w * .61, y + h * .18),
                    ChartPathCommand.CubicTo(x + w * .86, y - h * .02, x + w * 1.04, y + h * .25, x + w * .9, y + h * .45),
                    ChartPathCommand.CubicTo(x + w * 1.1, y + h * .67, x + w * .92, y + h * 1.04, x + w * .7, y + h * .85),
                    ChartPathCommand.CubicTo(x + w * .57, y + h * 1.08, x + w * .29, y + h * 1.02, x + w * .16, y + h * .8)
                }), true, true));
                break;
            default:
                var r = node.Shape == TopologyNodeShape.Stadium ? Math.Min(w, h) / 2 : node.Shape == TopologyNodeShape.Rectangle ? 0 : Math.Min(10, Math.Min(w, h) / 2);
                if (r == 0) { Polygon(x, y, x + w, y, x + w, y + h, x, y + h); break; }
                // Quadratic rounded corners expressed exactly as cubic segments.
                const double q = 2d / 3;
                parts.Add(new ScenePart(new ChartPath(new[] {
                    ChartPathCommand.MoveTo(x + r, y), ChartPathCommand.LineTo(x + w - r, y),
                    ChartPathCommand.CubicTo(x + w - r + q * r, y, x + w, y + r - q * r, x + w, y + r),
                    ChartPathCommand.LineTo(x + w, y + h - r), ChartPathCommand.CubicTo(x + w, y + h - r + q * r, x + w - r + q * r, y + h, x + w - r, y + h),
                    ChartPathCommand.LineTo(x + r, y + h), ChartPathCommand.CubicTo(x + r - q * r, y + h, x, y + h - r + q * r, x, y + h - r),
                    ChartPathCommand.LineTo(x, y + r), ChartPathCommand.CubicTo(x, y + r - q * r, x + r - q * r, y, x + r, y)
                }), true, true));
                break;
        }
        return parts;
    }
}
