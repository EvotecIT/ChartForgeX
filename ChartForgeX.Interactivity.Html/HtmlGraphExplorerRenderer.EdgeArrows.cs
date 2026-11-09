using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ChartForgeX.Interactivity;

namespace ChartForgeX.Interactivity.Html;

public sealed partial class HtmlGraphExplorerRenderer {
    // Mirrors graphArrowShapes in graph-explorer.03-canvas-edges.js. Each polygon is a flat list of
    // (back, across) pairs in arrow-size units measured from the tip; later polygons use the paper color.
    private static readonly Dictionary<GraphEdgeArrowShape, double[][]> ArrowPolygons = new() {
        [GraphEdgeArrowShape.Arrow] = new[] { new[] { 0, 0, .8660254, .5, .8660254, -.5 } },
        [GraphEdgeArrowShape.Vee] = new[] { new[] { 0, 0, 1.1, .6, .65, 0, 1.1, -.6 } },
        [GraphEdgeArrowShape.Triangle] = new[] { new[] { 0, 0, 1.2, .6, 1.2, -.6 }, new[] { .4, 0, 1.02, .31, 1.02, -.31 } },
        [GraphEdgeArrowShape.Bar] = new[] { new[] { 0, .7, .3, .7, .3, -.7, 0, -.7 } },
        [GraphEdgeArrowShape.Diamond] = new[] { new[] { 0, 0, .65, .42, 1.3, 0, .65, -.42 } },
        [GraphEdgeArrowShape.Circle] = new[] { Enumerable.Range(0, 16).SelectMany(index => new[] { .55 - .55 * Math.Cos(index * Math.PI / 8), .55 * Math.Sin(index * Math.PI / 8) }).ToArray() }
    };

    // The default target and source arrowheads keep their SVG markers; any other arrow layout is
    // written as the same polygons the browser runtime paints, so static and live output agree.
    private static bool HasCustomArrows(GraphSceneEdge edge) =>
        edge.MiddleArrow || Math.Abs(edge.Style.ArrowScale - 1) > 1e-9
        || (HasSourceArrow(edge) && edge.Style.SourceArrowShape != GraphEdgeArrowShape.Arrow)
        || (HasTargetArrow(edge) && edge.Style.TargetArrowShape != GraphEdgeArrowShape.Arrow);

    private static string? ArrowStyleData(GraphSceneEdge edge) => HasCustomArrows(edge)
        ? ArrowShapeToken(edge.Style.SourceArrowShape) + "," + ArrowShapeToken(edge.Style.TargetArrowShape) + "," + (edge.MiddleArrow ? ArrowShapeToken(edge.Style.MiddleArrowShape) : string.Empty) + "," + Number(edge.Style.ArrowScale)
        : null;

    private static string ArrowShapeToken(GraphEdgeArrowShape shape) => shape.ToString().ToLowerInvariant();

    private static Dictionary<string, double> EdgeValueWidths(GraphScene scene) {
        var widths = new Dictionary<string, double>(StringComparer.Ordinal);
        var valued = scene.Edges.Where(edge => edge.Style.Value.HasValue).ToArray();
        if (valued.Length == 0) return widths;
        var minimum = valued.Min(edge => edge.Style.Value!.Value);
        var maximum = valued.Max(edge => edge.Style.Value!.Value);
        var scale = scene.Options.EdgeWidthScale;
        foreach (var edge in valued.Where(edge => !edge.Style.Width.HasValue)) {
            var ratio = maximum > minimum ? (edge.Style.Value!.Value - minimum) / (maximum - minimum) : .5;
            widths[edge.Id] = scale.MinimumWidth + ratio * (scale.MaximumWidth - scale.MinimumWidth);
        }
        return widths;
    }

    private static double? ResolvedEdgeWidth(GraphSceneEdge edge, IReadOnlyDictionary<string, double> valueWidths) =>
        edge.Style.Width ?? (valueWidths.TryGetValue(edge.Id, out var width) ? width : null);

    private static void WriteEdgeArrowMarks(StringBuilder writer, GraphSceneEdge edge, string edgeClass, Point source, Point target, GraphSceneNode? sourceNode, GraphSceneNode? targetNode, double? targetBoundaryInset, double? sourceBoundaryInset) {
        writer.Append("<g");
        Attribute(writer, "class", edgeClass);
        writer.Append(" data-cfx-role=\"graph-edge-arrows\" data-cfx-runtime-overlay=\"true\"");
        Attribute(writer, "data-edge-arrows-for", edge.Id);
        writer.Append(" style=\"pointer-events:none\">");
        var size = 8 * edge.Style.ArrowScale;
        var color = "fill:" + (string.IsNullOrWhiteSpace(edge.Style.Color) ? "var(--cfx-color-edge)" : edge.Style.Color) + ";stroke:none";
        var geometry = new EdgeArrowGeometry(edge, source, target, sourceNode, targetNode, targetBoundaryInset, sourceBoundaryInset);
        if (HasSourceArrow(edge)) WriteArrowMark(writer, "source", edge.Style.SourceArrowShape, geometry.EndFrame(true), size, false, color);
        if (edge.MiddleArrow) WriteArrowMark(writer, "middle", edge.Style.MiddleArrowShape, geometry.MiddleFrame(), size, true, color);
        if (HasTargetArrow(edge)) WriteArrowMark(writer, "target", edge.Style.TargetArrowShape, geometry.EndFrame(false), size, false, color);
        writer.Append("</g>");
    }

    private static void WriteArrowMark(StringBuilder writer, string side, GraphEdgeArrowShape shape, ArrowFrame frame, double size, bool centered, string color) {
        var polygons = ArrowPolygons[shape];
        var shift = 0d;
        if (centered) for (var i = 0; i < polygons[0].Length; i += 2) shift = Math.Max(shift, polygons[0][i] * size / 2);
        var x = frame.X + frame.UnitX * shift;
        var y = frame.Y + frame.UnitY * shift;
        for (var index = 0; index < polygons.Length; index++) {
            var polygon = polygons[index];
            var points = new StringBuilder();
            for (var i = 0; i < polygon.Length; i += 2) {
                if (i > 0) points.Append(' ');
                points.Append(Number(x - (frame.UnitX * polygon[i] + frame.UnitY * polygon[i + 1]) * size)).Append(',')
                    .Append(Number(y - (frame.UnitY * polygon[i] - frame.UnitX * polygon[i + 1]) * size));
            }
            writer.Append("<polygon");
            Attribute(writer, "data-arrow-side", side);
            Attribute(writer, "points", points.ToString());
            Attribute(writer, "style", index == 0 ? color : "fill:var(--cfx-color-paper,#ffffff);stroke:none");
            writer.Append("></polygon>");
        }
    }

    private readonly struct ArrowFrame {
        internal ArrowFrame(double x, double y, double unitX, double unitY) { X = x; Y = y; UnitX = unitX; UnitY = unitY; }
        internal double X { get; }
        internal double Y { get; }
        internal double UnitX { get; }
        internal double UnitY { get; }
    }

    // Mirrors edgeArrowFrame, edgeMiddleFrame and edgePointAt in graph-explorer.02-geometry.js.
    private sealed class EdgeArrowGeometry {
        private readonly GraphSceneEdge _edge;
        private readonly Point _source, _target;
        private readonly GraphSceneNode? _sourceNode, _targetNode;
        private readonly double? _targetInset, _sourceInset;
        private readonly Point[]? _loop;
        private readonly IReadOnlyList<GraphScenePoint>? _route;
        private readonly Point? _control;

        internal EdgeArrowGeometry(GraphSceneEdge edge, Point source, Point target, GraphSceneNode? sourceNode, GraphSceneNode? targetNode, double? targetInset, double? sourceInset) {
            _edge = edge; _source = source; _target = target; _sourceNode = sourceNode; _targetNode = targetNode; _targetInset = targetInset; _sourceInset = sourceInset;
            if (string.Equals(edge.SourceNodeId, edge.TargetNodeId, StringComparison.Ordinal)) {
                var right = TargetBoundaryInset(targetNode, 1, 0) + 5;
                var left = TargetBoundaryInset(targetNode, -1, 0) + 5;
                var top = TargetBoundaryInset(targetNode, 0, -1) + 42;
                _loop = new[] { new Point(target.X + right, target.Y), new Point(target.X + right + 44, target.Y - top), new Point(target.X - left - 44, target.Y - top), new Point(target.X - left, target.Y) };
            } else if (edge.RoutePoints.Count > 1 && !targetInset.HasValue && !sourceInset.HasValue) {
                _route = PolylineRenderPoints(edge, source, target, sourceNode, targetNode, targetInset, sourceInset);
            } else {
                _control = EdgeControl(edge, source, target);
            }
        }

        internal ArrowFrame EndFrame(bool sourceSide) {
            Point from, tip;
            if (_loop != null) { from = sourceSide ? _loop[1] : _loop[2]; tip = sourceSide ? _loop[0] : _loop[3]; }
            else if (_route != null) {
                var near = sourceSide ? _route[1] : _route[_route.Count - 2];
                var end = sourceSide ? _route[0] : _route[_route.Count - 1];
                from = new Point(near.X, near.Y); tip = new Point(end.X, end.Y);
            } else { from = _control ?? (sourceSide ? _target : _source); tip = sourceSide ? _source : _target; }
            var dx = tip.X - from.X;
            var dy = tip.Y - from.Y;
            var length = Math.Max(1, Math.Sqrt(dx * dx + dy * dy));
            var unitX = dx / length;
            var unitY = dy / length;
            var inset = _loop != null || _route != null ? 0 : (sourceSide ? _sourceInset : _targetInset) ?? TargetBoundaryInset(sourceSide ? _sourceNode : _targetNode, unitX, unitY);
            return new ArrowFrame(tip.X - unitX * inset, tip.Y - unitY * inset, unitX, unitY);
        }

        internal ArrowFrame MiddleFrame() {
            var before = PointAt(.49);
            var after = PointAt(.51);
            var middle = PointAt(.5);
            var length = Math.Sqrt((after.X - before.X) * (after.X - before.X) + (after.Y - before.Y) * (after.Y - before.Y));
            if (length <= 0) length = 1;
            return new ArrowFrame(middle.X, middle.Y, (after.X - before.X) / length, (after.Y - before.Y) / length);
        }

        private Point PointAt(double t) {
            var u = 1 - t;
            if (_loop != null) return new Point(
                u * u * u * _loop[0].X + 3 * u * u * t * _loop[1].X + 3 * u * t * t * _loop[2].X + t * t * t * _loop[3].X,
                u * u * u * _loop[0].Y + 3 * u * u * t * _loop[1].Y + 3 * u * t * t * _loop[2].Y + t * t * t * _loop[3].Y);
            if (_route != null) return PolylinePointAt(_route, t, 0);
            var start = SourceBoundaryPoint(_edge, _source, _target, _control, _sourceNode, _sourceInset);
            var end = TargetBoundaryPoint(_edge, _source, _target, _control, _targetNode, _targetInset);
            var control = _control ?? new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            return new Point(u * u * start.X + 2 * u * t * control.X + t * t * end.X, u * u * start.Y + 2 * u * t * control.Y + t * t * end.Y);
        }
    }
}
