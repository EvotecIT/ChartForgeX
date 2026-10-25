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
        DrawRoutes(chart, context, builder, map, plot, dot, weights, min, max, colors, labels, series, 0,
            chart.Options.MapConnectors.Concat(series.MapConnectors).ToArray(), point => Geographic(point, viewport, map), point => Visible(point, viewport));
    }

    /// <summary>Draws the same route geometry for standalone and composed datasets on one caller-owned coordinate frame.</summary>
    private static void DrawRoutes(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect map, ChartRect plot,
        double dot, double?[] weights, double min, double max, VisualThemeColors colors, List<MapLabel> labels,
        ChartSeries series, int seriesIndex, IReadOnlyList<ChartMapConnector> connectors, Func<ChartPoint, ChartPoint> project, Func<ChartPoint, bool> visiblePoint,
        ChartMapPointSizeScale? sizeScale = null, IReadOnlyList<MapDataset>? endpointDatasets = null,
        Func<IReadOnlyList<ChartPoint>, ChartPoint[]>? projectRoute = null, double? horizontalPeriod = null) {
        for (var index = 0; index < connectors.Count; index++) {
            var connector = connectors[index];
            if (string.IsNullOrWhiteSpace(connector.Label)) throw new InvalidOperationException("A map route needs a label.");
            var waypoints = connector.RoutePoints ?? Array.Empty<ChartMapPoint>();
            if (waypoints.Count == 1) throw new InvalidOperationException("A map route needs at least two waypoints.");
            var source = waypoints.Count > 0 ? waypoints.Select(point => new ChartPoint(point.Longitude, point.Latitude)).ToArray()
                : new[] { new ChartPoint(connector.FromLongitude, connector.FromLatitude), new ChartPoint(connector.ToLongitude, connector.ToLatitude) };
            foreach (var point in source) ValidateCoordinate(point);
            var visible = source.All(visiblePoint); var id = (seriesIndex == 0 ? "map-route-" : "series-" + N(seriesIndex) + "-map-route-") + index;
            var route = projectRoute != null ? projectRoute(source) : source.Select(project).ToArray(); var sourceBounds = Bounds(route);
            var color = connector.Color ?? series.Color ?? ChartSeriesColours.State(series.StateRole, colors, colors.Status.Medium.Fill);
            var paint = SvgPaint.Of(color, connector.Color.HasValue || series.Color.HasValue ? SvgColorRole.Series : SvgColorRole.Status);
            var width = series.HasExplicitStrokeWidth ? series.StrokeWidth : Math.Max(1.2, dot * .78);
            var length = 0d;
            var geometry = visible || projectRoute != null ? MapRouteGeometry(route, waypoints.Count > 0, map, series, source, weights, dot, min, max,
                sizeScale, !connector.EndpointClearance.HasValue, out length) : new ChartPath(Array.Empty<ChartPathCommand>());
            IReadOnlyList<ChartPath> painted = new[] { geometry }; var periodicClipped = false;
            if (horizontalPeriod is double period) {
                painted = geometry.PeriodicRectangleFragments(map, period, out periodicClipped);
                visible = painted.Count > 0;
            } else if (projectRoute != null) visible = geometry.IntersectsRectangle(map);
            var collapsed = false;
            if (visible && connector.EndpointClearance is double clearance) {
                var peers = endpointDatasets ?? new[] { new MapDataset(chart, seriesIndex) };
                // Canonical point/cluster marks can occupy different periodic representatives at the two ends.
                var startOffset = horizontalPeriod.HasValue ? route[0].X - project(source[0]).X : 0;
                var endOffset = horizontalPeriod.HasValue ? route[route.Length - 1].X - project(source[source.Length - 1]).X : 0;
                geometry = geometry.TrimEndpointDisks(MapEndpointDisks(peers, source[0], project, dot, clearance + (width + dot) / 2, startOffset),
                    MapEndpointDisks(peers, source[source.Length - 1], project, dot, clearance + (width + dot) / 2, endOffset));
                collapsed = geometry.Commands.Count < 2;
                if (collapsed) { visible = false; builder.AddDiagnostic(new VisualDiagnostic("map.route-clearance-collapsed", "Endpoint clearance covers the complete map route; complete source coordinates remain captured.")); }
            }
            if (horizontalPeriod is double trimmedPeriod && visible && connector.EndpointClearance.HasValue) {
                painted = geometry.PeriodicRectangleFragments(map, trimmedPeriod, out periodicClipped);
                visible = painted.Count > 0;
            } else if (!horizontalPeriod.HasValue) {
                painted = new[] { geometry };
                if (projectRoute != null && visible) visible = geometry.IntersectsRectangle(map);
            } else if (!visible) painted = Array.Empty<ChartPath>();
            var periodicFragments = horizontalPeriod.HasValue && (painted.Count != 1 || !ReferenceEquals(painted[0], geometry));
            if (visible && periodicFragments) {
                var bounds = painted.Select(part => part.Bounds()).ToArray();
                var left = bounds.Min(item => item.Left); var top = bounds.Min(item => item.Top);
                sourceBounds = new ChartRect(left, top, bounds.Max(item => item.Right) - left, bounds.Max(item => item.Bottom) - top);
            }
            builder.AddRegion(new VisualSemanticRegion(id, "dotted-map-connector", visible ? Intersect(sourceBounds, map) : map,
                connector.Label + ": " + string.Join(" → ", source.Select(point => N(point.X) + ", " + N(point.Y)))));
            var metadata = new Dictionary<string, string> {
                ["data-cfx-series"] = N(seriesIndex), ["data-cfx-connector"] = N(index), ["data-cfx-label"] = connector.Label, ["data-cfx-route-kind"] = waypoints.Count > 0 ? "waypoint" : "arc",
                ["data-cfx-waypoint-count"] = N(waypoints.Count), ["data-cfx-route-coordinates"] = string.Join(";", source.Select(point => N(point.X) + "," + N(point.Y))),
                ["data-cfx-visible"] = visible ? "true" : "false", ["aria-label"] = connector.Label
            };
            AddFeatureFacts(metadata, connector.Feature);
            if (projectRoute != null) {
                metadata["data-cfx-route-unwrapped"] = "true";
                var bounds = geometry.Bounds();
                metadata["data-cfx-route-view-clipped"] = (horizontalPeriod.HasValue ? periodicClipped
                    : bounds.Left < map.Left || bounds.Right > map.Right || bounds.Top < map.Top || bounds.Bottom > map.Bottom) ? "true" : "false";
            }
            if (connector.EndpointClearance is double requestedClearance) {
                metadata["data-cfx-endpoint-clearance"] = N(requestedClearance);
                metadata["data-cfx-route-collapsed"] = collapsed ? "true" : "false";
                if (!collapsed && geometry.Commands.Count > 1) {
                    metadata["data-cfx-route-start-x"] = N(geometry.Commands[0].X); metadata["data-cfx-route-start-y"] = N(geometry.Commands[0].Y);
                    metadata["data-cfx-route-end-x"] = N(geometry.Commands[geometry.Commands.Count - 1].X); metadata["data-cfx-route-end-y"] = N(geometry.Commands[geometry.Commands.Count - 1].Y);
                }
            }
            using (builder.PushGroup(id, "dotted-map-connector-source", metadata)) {
                if (!visible) continue;
                List<ChartPoint>? captionSamples = null; var captionLength = 0d;
                var showCaption = series.ShowDataLabels ?? (chart.Options.ShowDataLabels ?? false);
                for (var fragmentIndex = 0; fragmentIndex < painted.Count; fragmentIndex++) {
                    var fragment = painted[fragmentIndex];
                    builder.Path(fragment, stroke: ChartColorMath.WithOpacity(colors.Surface, .72), strokeWidth: width + dot, role: "dotted-map-connector-halo",
                        paint: VisualChartPaint.Stroke(SvgPaint.Of(colors.Surface, SvgColorRole.Surface).WithOpacity(ChartColorMath.WithOpacity(colors.Surface, .72), .72)));
                    builder.Path(fragment, stroke: ChartColorMath.WithOpacity(color, .72), strokeWidth: width, role: "dotted-map-connector", paint: VisualChartPaint.Stroke(paint.WithOpacity(ChartColorMath.WithOpacity(color, .72), .72)));
                    // Every decoration samples one connected fragment; no seam jump enters Along's distance.
                    var samples = fragment.Flatten(3); var tip = Along(samples, .63); var before = Along(samples, .58); var after = Along(samples, .68);
                    var paintedLength = 0d;
                    if (connector.EndpointClearance.HasValue || periodicFragments)
                        for (var part = 1; part < samples.Count; part++) paintedLength += Distance(samples[part - 1], samples[part]);
                    // One logical route has one caption fact. Only new periodic paint selects its longest connected visible piece.
                    if (showCaption && (!periodicFragments || paintedLength > captionLength)) { captionSamples = samples; captionLength = paintedLength; }
                    var direction = Distance(before, after);
                    if (direction > .000001) {
                        var ux = (after.X - before.X) / direction; var uy = (after.Y - before.Y) / direction;
                        var arrowLength = Math.Min(Math.Max(10, dot * 4.8), length * .25);
                        if (connector.EndpointClearance.HasValue || periodicFragments) arrowLength = Math.Min(arrowLength, paintedLength * .25);
                        var arrowWidth = Math.Min(Math.Max(6, dot * 2.4), arrowLength * .65);
                        var back = new ChartPoint(tip.X - ux * arrowLength, tip.Y - uy * arrowLength);
                        builder.Path(Path(new[] { (IReadOnlyList<ChartPoint>)new[] { tip, new ChartPoint(back.X - uy * arrowWidth / 2, back.Y + ux * arrowWidth / 2),
                            new ChartPoint(back.X + uy * arrowWidth / 2, back.Y - ux * arrowWidth / 2) } }), ChartColorMath.WithOpacity(color, .78), colors.Surface,
                            Math.Min(context.Theme.AxisStrokeWidth, arrowWidth / 4), "dotted-map-connector-arrow", close: true,
                            paint: new VisualScenePaintBinding(paint.WithOpacity(ChartColorMath.WithOpacity(color, .78), .78), SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                    }
                }
                if (captionSamples != null) {
                    var style = MapDataStyle(chart, context, -1, color, series);
                    AddMapLabel(builder, labels, connector.Label, ChartRouteLabelCompaction.Compact(connector.Label), Along(captionSamples, .36), style,
                        id + "-label", "dotted-map-connector-label", plot, context.Theme.Spacing + dot, 30,
                        paint: VisualChartPaint.ExplicitDataLabelColor(chart) ? VisualChartPaint.Text(style) : paint,
                        leaderColor: chart.Options.MapCallouts != null ? color : null, leaderPaint: paint, callouts: chart.Options.MapCallouts);
                }
            }
        }
    }

    /// <summary>Builds the one native route path; opt-in endpoint clearance slices this same curve after construction.</summary>
    private static ChartPath MapRouteGeometry(ChartPoint[] route, bool hasWaypoints, ChartRect map, ChartSeries series, ChartPoint[] source,
        double?[] weights, double dot, double min, double max, ChartMapPointSizeScale? sizeScale, bool legacyTrim, out double length) {
        var first = route[0]; var last = route[route.Length - 1];
        length = Distance(first, last); var center = new ChartPoint((first.X + last.X) / 2, (first.Y + last.Y) / 2);
        var px = length > 0 ? (last.Y - first.Y) / length : 0; var py = length > 0 ? -(last.X - first.X) / length : -1;
        if (Math.Abs(last.X - first.X) >= Math.Abs(last.Y - first.Y) ? py > 0 : (center.X > map.Left + map.Width / 2 ? px > 0 : px < 0)) { px = -px; py = -py; }
        var curve = Math.Min(length * .16, Math.Min(map.Width, map.Height) * .18);
        var control = new ChartPoint(VisualRadialPrimitives.Clamp(center.X + px * curve, map.Left, map.Right), VisualRadialPrimitives.Clamp(center.Y + py * curve, map.Top, map.Bottom));
        if (legacyTrim) route[0] = Trim(first, hasWaypoints ? route[1] : control, EndpointTrim(series, source[0], weights, dot, min, max, sizeScale));
        if (legacyTrim) route[route.Length - 1] = Trim(last, hasWaypoints ? route[route.Length - 2] : control, EndpointTrim(series, source[source.Length - 1], weights, dot, min, max, sizeScale));
        ChartPath geometry;
        if (hasWaypoints) geometry = ChartPathBuilder.FromPoints(route, ChartInterpolation.Smooth);
        else geometry = new ChartPath(new[] { ChartPathCommand.MoveTo(route[0].X, route[0].Y), ChartPathCommand.CubicTo(
            route[0].X + (control.X - route[0].X) * 2 / 3, route[0].Y + (control.Y - route[0].Y) * 2 / 3,
            route[1].X + (control.X - route[1].X) * 2 / 3, route[1].Y + (control.Y - route[1].Y) * 2 / 3, route[1].X, route[1].Y) });
        return geometry;
    }

    private static double EndpointTrim(ChartSeries series, ChartPoint coordinate, double?[] weights, double dot, double min, double max, ChartMapPointSizeScale? sizeScale) {
        var trim = dot * 2;
        if (series.Kind != ChartSeriesKind.DottedMap) return trim;
        for (var index = 0; index < series.RenderPoints.Count; index++) if (ChartMath.SameCoordinate(series.RenderPoints[index].X, coordinate.X) && ChartMath.SameCoordinate(series.RenderPoints[index].Y, coordinate.Y))
            trim = Math.Max(trim, (sizeScale?.Radius(weights[index], dot) ?? series.MarkerRadius ?? PointRadius(dot, weights[index], min, max))
                + Math.Max(1, dot * .55) + Math.Max(1.5, dot * .45));
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
