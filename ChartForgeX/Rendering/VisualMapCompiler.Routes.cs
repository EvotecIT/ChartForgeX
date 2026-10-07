using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static void Routes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect map, ChartRect plot, ChartMapViewport viewport,
        double dot, double?[] weights, double min, double max, VisualThemeColors colors, List<MapLabel> labels) {
        var series = chart.Series[0];
        for (var index = 0; index < chart.Options.MapConnectors.Count; index++) {
            var connector = chart.Options.MapConnectors[index];
            if (string.IsNullOrWhiteSpace(connector.Label)) throw new InvalidOperationException("A map route needs a label.");
            var waypoints = connector.RoutePoints ?? Array.Empty<ChartMapPoint>();
            if (waypoints.Length == 1) throw new InvalidOperationException("A map route needs at least two waypoints.");
            var source = waypoints.Length > 0 ? waypoints.Select(point => new ChartPoint(point.Longitude, point.Latitude)).ToArray()
                : new[] { new ChartPoint(connector.FromLongitude, connector.FromLatitude), new ChartPoint(connector.ToLongitude, connector.ToLatitude) };
            foreach (var point in source) ValidateCoordinate(point);
            var visible = source.All(point => Visible(point, viewport)); var id = "map-route-" + index;
            var route = source.Select(point => Geographic(point, viewport, map)).ToArray();
            var color = connector.Color ?? series.Color ?? ChartSeriesColours.State(series.StateRole, colors, colors.Status.Medium.Fill);
            builder.AddRegion(new VisualSemanticRegion(id, "dotted-map-connector", visible ? Intersect(Bounds(route), map) : map,
                connector.Label + ": " + string.Join(" → ", source.Select(point => N(point.X) + ", " + N(point.Y)))));
            using (builder.PushGroup(id, "dotted-map-connector-source", new Dictionary<string, string> {
                ["data-cfx-connector"] = N(index), ["data-cfx-label"] = connector.Label, ["data-cfx-route-kind"] = waypoints.Length > 0 ? "waypoint" : "arc",
                ["data-cfx-waypoint-count"] = N(waypoints.Length), ["data-cfx-route-coordinates"] = string.Join(";", source.Select(point => N(point.X) + "," + N(point.Y))),
                ["data-cfx-visible"] = visible ? "true" : "false", ["aria-label"] = connector.Label
            })) {
                if (!visible) continue;
                var first = route[0]; var last = route[route.Length - 1];
                var length = Distance(first, last); var center = new ChartPoint((first.X + last.X) / 2, (first.Y + last.Y) / 2);
                var px = length > 0 ? (last.Y - first.Y) / length : 0; var py = length > 0 ? -(last.X - first.X) / length : -1;
                if (Math.Abs(last.X - first.X) >= Math.Abs(last.Y - first.Y) ? py > 0 : (center.X > map.Left + map.Width / 2 ? px > 0 : px < 0)) { px = -px; py = -py; }
                var curve = Math.Min(length * .16, Math.Min(map.Width, map.Height) * .18);
                var control = new ChartPoint(VisualRadialPrimitives.Clamp(center.X + px * curve, map.Left, map.Right), VisualRadialPrimitives.Clamp(center.Y + py * curve, map.Top, map.Bottom));
                route[0] = Trim(first, waypoints.Length > 0 ? route[1] : control, EndpointTrim(series, source[0], weights, dot, min, max));
                route[route.Length - 1] = Trim(last, waypoints.Length > 0 ? route[route.Length - 2] : control, EndpointTrim(series, source[source.Length - 1], weights, dot, min, max));
                ChartPath geometry;
                if (waypoints.Length > 0) geometry = ChartPathBuilder.FromPoints(route, ChartSeriesKind.Line, true);
                else geometry = new ChartPath(new[] { ChartPathCommand.MoveTo(route[0].X, route[0].Y), ChartPathCommand.CubicTo(
                    route[0].X + (control.X - route[0].X) * 2 / 3, route[0].Y + (control.Y - route[0].Y) * 2 / 3,
                    route[1].X + (control.X - route[1].X) * 2 / 3, route[1].Y + (control.Y - route[1].Y) * 2 / 3, route[1].X, route[1].Y) });
                var width = series.HasExplicitStrokeWidth ? series.StrokeWidth : Math.Max(1.2, dot * .78);
                builder.Path(geometry, stroke: ChartColorMath.WithOpacity(colors.Surface, .72), strokeWidth: width + dot, role: "dotted-map-connector-halo");
                builder.Path(geometry, stroke: ChartColorMath.WithOpacity(color, .72), strokeWidth: width, role: "dotted-map-connector");
                var samples = geometry.Flatten(3); var tip = Along(samples, .63); var before = Along(samples, .58); var after = Along(samples, .68);
                var direction = Distance(before, after);
                if (direction > .000001) {
                    var ux = (after.X - before.X) / direction; var uy = (after.Y - before.Y) / direction;
                    var arrowLength = Math.Min(Math.Max(10, dot * 4.8), length * .25); var arrowWidth = Math.Min(Math.Max(6, dot * 2.4), arrowLength * .65);
                    var back = new ChartPoint(tip.X - ux * arrowLength, tip.Y - uy * arrowLength);
                    builder.Path(Path(new[] { (IReadOnlyList<ChartPoint>)new[] { tip, new ChartPoint(back.X - uy * arrowWidth / 2, back.Y + ux * arrowWidth / 2),
                        new ChartPoint(back.X + uy * arrowWidth / 2, back.Y - ux * arrowWidth / 2) } }), ChartColorMath.WithOpacity(color, .78), colors.Surface,
                        Math.Min(width * .48, arrowWidth / 4), "dotted-map-connector-arrow", close: true);
                }
                if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) {
                    var style = MapDataStyle(chart, context, -1, color);
                    AddMapLabel(builder, labels, connector.Label, ChartRouteLabelCompaction.Compact(connector.Label), Along(samples, .36), style,
                        id + "-label", "dotted-map-connector-label", plot, context.Theme.Spacing + dot, 30);
                }
            }
        }
    }

    private static double EndpointTrim(ChartSeries series, ChartPoint coordinate, double?[] weights, double dot, double min, double max) {
        var trim = dot * 2;
        for (var index = 0; index < series.Points.Count; index++) if (ChartMath.SameCoordinate(series.Points[index].X, coordinate.X) && ChartMath.SameCoordinate(series.Points[index].Y, coordinate.Y))
            trim = Math.Max(trim, (series.MarkerRadius ?? PointRadius(dot, weights[index], min, max)) + Math.Max(1, dot * .55) + Math.Max(1.5, dot * .45));
        return trim;
    }
    private static ChartPoint Trim(ChartPoint point, ChartPoint toward, double distance) {
        var length = Distance(point, toward); var ratio = length > 0 ? Math.Min(distance / length, .42) : 0;
        return new ChartPoint(point.X + (toward.X - point.X) * ratio, point.Y + (toward.Y - point.Y) * ratio);
    }
    private static double Distance(ChartPoint a, ChartPoint b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    private static ChartPoint Along(IReadOnlyList<ChartPoint> points, double progress) {
        if (points.Count == 0) return new ChartPoint(0, 0);
        var length = 0d; for (var index = 1; index < points.Count; index++) length += Distance(points[index - 1], points[index]);
        var remaining = length * progress;
        for (var index = 1; index < points.Count; index++) {
            var segment = Distance(points[index - 1], points[index]);
            if (remaining <= segment) {
                var ratio = segment > 0 ? remaining / segment : 0;
                return new ChartPoint(points[index - 1].X + (points[index].X - points[index - 1].X) * ratio, points[index - 1].Y + (points[index].Y - points[index - 1].Y) * ratio);
            }
            remaining -= segment;
        }
        return points[points.Count - 1];
    }
}
