using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualCartesianCompiler {
    /// <summary>Routes source category/value comparisons through the same physical mapping, marker and label owners.</summary>
    private static void DrawComparisons(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot,
        ChartMapper map, int index, VisualThemeColors colors, List<LabelPlacementRequest> labels, List<LabelObstacle> obstacles,
        ChartAxisWindow? xViewport, ChartAxisWindow? yViewport) {
        var series = chart.Series[index]; var dumbbell = series.Kind == ChartSeriesKind.Dumbbell;
        var horizontal = ChartCartesianOrientation.IsHorizontal(chart, series); var stride = dumbbell ? 2 : 1;
        if (horizontal && chart.Options.YAxis.Scale is not (ChartScaleKind.Linear or ChartScaleKind.Time))
            throw new NotSupportedException("Horizontal comparison charts require a linear or time category axis.");
        var startMarker = dumbbell ? series.ResolveDumbbellMarker(start: true) : series.Markers;
        var endMarker = dumbbell ? series.ResolveDumbbellMarker(start: false) : series.Markers;
        var startRadius = VisualMarkerScene.Radius(series, context, startMarker);
        var endRadius = VisualMarkerScene.Radius(series, context, endMarker);
        var endExtent = VisualMarkerScene.Extent(series, endRadius, endMarker);
        var axis = ChartCartesianOrientation.ValueAxis(chart, series.ValueAxis);
        for (var item = 0; item < series.RenderPoints.Count / stride; item++) {
            var raw = item * stride; var first = series.RenderPoints[raw]; var end = series.RenderPoints[raw + stride - 1].Y;
            var category = horizontal ? map.Y(first.X) : map.X(first.X);
            var start = dumbbell ? horizontal ? map.X(first.Y) : map.Y(first.Y) : horizontal ? map.XBaseline() : map.YBaseline();
            var finish = horizontal ? map.X(end) : map.Y(end);
            var startPoint = horizontal ? new ChartPoint(start, category) : new ChartPoint(category, start);
            var endPoint = horizontal ? new ChartPoint(finish, category) : new ChartPoint(category, finish);
            var visible = ComparisonVisible(horizontal, dumbbell, plot, map, xViewport, yViewport, first.X, first.Y, end, start, finish);
            var connectorStroke = dumbbell ? series.HasExplicitStrokeWidth ? series.StrokeWidth : ChartVisualPrimitives.DumbbellConnectorStrokeWidth
                : Math.Max(ChartVisualPrimitives.LollipopStemMinStrokeWidth, SeriesStroke(series, context) * .62);
            // Hidden glyphs retain the family's existing descriptive/hit-region footprint around their source endpoints.
            var regionExtent = Math.Max(dumbbell ? VisualMarkerScene.FootprintExtent(series, startRadius, startMarker) : 0,
                VisualMarkerScene.FootprintExtent(series, endRadius, endMarker));
            var bounds = Extents(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, regionExtent);
            if (startMarker.CustomPath != null || endMarker.CustomPath != null) {
                bounds = Extents(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, connectorStroke / 2);
                if (dumbbell) bounds = VisualMarkerScene.UnionBounds(bounds, VisualMarkerScene.Bounds(series, startPoint.X, startPoint.Y, startRadius, startMarker));
                bounds = VisualMarkerScene.UnionBounds(bounds, VisualMarkerScene.Bounds(series, endPoint.X, endPoint.Y, endRadius, endMarker));
            }
            var color = PointColor(series, index, item, colors); var paint = VisualChartPaint.Series(series, color, item);
            var label = ResolveObservationLabel(chart, context, series, item, colors, format => dumbbell
                ? format("start", first.Y) + "–" + format("end", end) : format("value", end));
            var facts = new Dictionary<string, string> { ["data-cfx-orientation"] = horizontal ? "horizontal" : "vertical" };
            var values = dumbbell ? new[] { ("x", first.X), ("start", first.Y), ("end", end), ("delta", end - first.Y) }
                : new[] { ("x", first.X), ("value", end) };
            using (ObservationGroup(builder, series, index, item, raw, stride, bounds, label, visible, facts, values)) {
                if (visible) {
                    var opacity = dumbbell ? ChartVisualPrimitives.DumbbellConnectorOpacity : ChartVisualPrimitives.LollipopStemOpacity;
                    var stroke = connectorStroke;
                    var lineColor = ChartColorMath.WithOpacity(color, opacity);
                    builder.Line(startPoint.X, startPoint.Y, endPoint.X, endPoint.Y, lineColor, stroke,
                        role: dumbbell ? "dumbbell-connector" : "lollipop-stem", paint: VisualChartPaint.Stroke(paint.WithOpacity(lineColor, opacity)));
                    if (dumbbell) VisualMarkerScene.Draw(builder, series, item, startPoint.X, startPoint.Y, startRadius,
                        colors.MutedForeground, SvgPaint.Of(colors.MutedForeground, SvgColorRole.Text), "dumbbell-start", markerOptions: startMarker);
                    VisualMarkerScene.Draw(builder, series, item, endPoint.X, endPoint.Y, endRadius, color, paint,
                        dumbbell ? "dumbbell-end" : "lollipop-marker", dumbbell ? null : colors.Surface,
                        dumbbell ? 1 : VisualMarkerScene.FamilyStrokeWidth(series), dumbbell ? null : SvgPaint.Of(colors.Surface, SvgColorRole.Surface),
                        ObservationPattern(series, item), ChartStateMark.Backdrop(chart.Options, colors, context.Frame),
                        dumbbell ? "dumbbell-pattern" : "lollipop-pattern", markerOptions: endMarker);
                }
            }
            if (!visible) continue;
            var direction = MappedBarDirection(axis, dumbbell ? end - first.Y : end, start, finish, horizontal);
            if (horizontal) {
                // The whole stem remains an obstacle; automatic captions stay beside their own endpoint.
                obstacles.Add(new LabelObstacle(PointId(index, item), bounds));
                var endpointBounds = endMarker.CustomPath == null ? Extents(endPoint.X, endPoint.Y, endPoint.X, endPoint.Y, endExtent)
                    : VisualMarkerScene.Bounds(series, endPoint.X, endPoint.Y, endRadius, endMarker);
                AddHorizontalLabel(chart, context, series, index, item, endPoint, endpointBounds, end, direction, label, labels);
            } else ObservationLabel(chart, context, series, index, item, endPoint, bounds, end, label, labels, obstacles, direction);
        }
    }

    private static bool ComparisonVisible(bool horizontal, bool dumbbell, ChartRect plot, ChartMapper map,
        ChartAxisWindow? xViewport, ChartAxisWindow? yViewport, double category, double first, double end, double startPixel, double endPixel) {
        if (horizontal) {
            if (!map.OverlapsSourceWindow(yViewport, category, category, horizontalAxis: false)) return false;
            return dumbbell ? map.OverlapsSourceWindow(xViewport, Math.Min(first, end), Math.Max(first, end), horizontalAxis: true)
                : xViewport == null || Math.Max(startPixel, endPixel) >= plot.Left && Math.Min(startPixel, endPixel) <= plot.Right;
        }
        return dumbbell ? ViewportObservationVisible(xViewport, yViewport, category, Math.Min(first, end), Math.Max(first, end), map)
            : ViewportObservationVisible(xViewport, null, category, end, end, map) && ViewportLogicalYVisible(yViewport, plot, startPixel, endPixel);
    }
}
