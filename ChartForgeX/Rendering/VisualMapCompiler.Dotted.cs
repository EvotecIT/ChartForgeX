using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

internal static partial class VisualMapCompiler {
    private static void Dotted(Chart chart, VisualRenderContext context, VisualSceneBuilder builder, ChartRect plot, VisualThemeColors colors) {
        var series = chart.Series[0]; var viewport = chart.Options.MapViewport;
        var longitudeSpan = viewport.MaximumLongitude - viewport.MinimumLongitude; var latitudeSpan = viewport.MaximumLatitude - viewport.MinimumLatitude;
        if (!Finite(longitudeSpan) || !Finite(latitudeSpan) || longitudeSpan <= 0 || latitudeSpan <= 0)
            throw new InvalidOperationException("A dotted map viewport must have positive longitude and latitude spans.");
        foreach (var point in series.RenderPoints) ValidateCoordinate(point);
        var weights = new double?[series.RenderPoints.Count];
        for (var index = 0; index < weights.Length; index++) {
            weights[index] = series.MapPoints[index].Weight;
            if (weights[index].HasValue && (!Finite(weights[index]!.Value) || weights[index]!.Value < 0)) throw new InvalidOperationException("Dotted map weights must be finite and non-negative.");
        }
        var valued = weights.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        var min = valued.Length > 0 ? valued.Min() : 0; var max = valued.Length > 0 ? valued.Max() : 0;
        var initial = Fit(plot, longitudeSpan, latitudeSpan);
        var dot = Math.Min(UsesCompactMapDot(viewport) ? 3.6 : 5.2,
            Math.Min(initial.Width / Math.Max(1, longitudeSpan / 3.5), initial.Height / Math.Max(1, latitudeSpan / 3.5)) * .42);
        var maxRadius = series.MarkerRadius ?? PointRadius(dot, valued.Length > 0 ? max : (double?)null, min, max);
        var map = Fit(plot, longitudeSpan, latitudeSpan, Math.Max(dot * 2.45, maxRadius * 1.85) + Math.Max(1, dot * .55) / 2);
        var labels = new List<MapLabel>(); var obstacles = new List<LabelObstacle>();
        using (builder.PushGroup("series-0", "dotted-map", new Dictionary<string, string> {
            ["role"] = "group", ["aria-label"] = chart.Options.Labels.Describe(DescriptionFacts(chart, true)),
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-label"] = series.Name,
            ["data-cfx-projection"] = "equirectangular", ["data-cfx-viewport"] = viewport.Name ?? "",
            ["data-cfx-point-count"] = N(series.RenderPoints.Count), ["data-cfx-visible-point-count"] = N(series.RenderPoints.Count(point => Visible(point, viewport))),
            ["data-cfx-valued-point-count"] = N(valued.Length), ["data-cfx-connector-count"] = N(chart.Options.MapConnectors.Count),
            ["data-cfx-viewport-min-longitude"] = N(viewport.MinimumLongitude), ["data-cfx-viewport-max-longitude"] = N(viewport.MaximumLongitude),
            ["data-cfx-viewport-min-latitude"] = N(viewport.MinimumLatitude), ["data-cfx-viewport-max-latitude"] = N(viewport.MaximumLatitude),
            ["data-cfx-min-value"] = N(min), ["data-cfx-max-value"] = N(max)
        })) using (builder.PushClip(plot)) {
            using (builder.PushClip(map)) {
                DottedLand(chart, context, builder, map, viewport, dot, colors);
                Routes(chart, context, builder, map, plot, viewport, dot, weights, min, max, colors, labels);
            }
            DrawPoints(chart, context, builder, map, plot, dot, weights, min, max, colors, labels, obstacles,
                new MapDataset(chart, 0), point => Geographic(point, viewport, map), point => Visible(point, viewport));
            DrawMapLabels(builder, plot, labels, obstacles, context.Theme.Spacing);
        }
        if (series.RenderPoints.Count == 0) builder.AddDiagnostic(new VisualDiagnostic("map.no-data", "The dotted map has no observations."));
    }

    private static void ValidateCoordinate(ChartPoint point) {
        if (!Finite(point.X) || !Finite(point.Y) || point.X < -180 || point.X > 180 || point.Y < -90 || point.Y > 90)
            throw new InvalidOperationException("Map coordinates must be finite longitude/latitude in their geographic ranges.");
    }
    private static bool Visible(ChartPoint point, ChartMapViewport viewport) => point.X >= viewport.MinimumLongitude && point.X <= viewport.MaximumLongitude && point.Y >= viewport.MinimumLatitude && point.Y <= viewport.MaximumLatitude;
    private static ChartPoint Geographic(ChartPoint point, ChartMapViewport viewport, ChartRect map) => new(
        map.Left + (point.X - viewport.MinimumLongitude) / (viewport.MaximumLongitude - viewport.MinimumLongitude) * map.Width,
        map.Top + (viewport.MaximumLatitude - point.Y) / (viewport.MaximumLatitude - viewport.MinimumLatitude) * map.Height);
    private static double PointRadius(double dot, double? value, double min, double max) => value.HasValue
        ? Math.Max(Math.Min(3, dot * 1.35), dot * (1.15 + VisualRadialPrimitives.Clamp((value.Value - min) / (max > min ? max - min : 1)) * 1.05)) : dot * 1.35;

    // A series key uses the map default; observation keys and marks also honor their own literal overrides.
    private static (ChartColor Color, SvgPaint Paint) DottedPaint(ChartSeries series, int? index, VisualThemeColors colors) {
        var pointOverride = index.HasValue && index.Value < series.PointColors.Count ? series.PointColors[index.Value] : null;
        var itemColor = index.HasValue ? series.MapPoints[index.Value].Color : null;
        var color = pointOverride ?? itemColor ?? series.Color ?? ChartSeriesColours.State(series.StateRole, colors, colors.Status.Pass.Fill);
        var role = pointOverride.HasValue || itemColor.HasValue || series.Color.HasValue ? SvgColorRole.Series : SvgColorRole.Status;
        return (color, SvgPaint.Of(color, role));
    }

    private static bool Same(ChartMapViewport a, ChartMapViewport b) => ChartMath.SameCoordinate(a.MinimumLongitude, b.MinimumLongitude) && ChartMath.SameCoordinate(a.MaximumLongitude, b.MaximumLongitude)
        && ChartMath.SameCoordinate(a.MinimumLatitude, b.MinimumLatitude) && ChartMath.SameCoordinate(a.MaximumLatitude, b.MaximumLatitude);

    internal static bool UsesCompactMapDot(ChartMapViewport viewport) => Same(viewport, ChartMapViewport.World())
        || Same(viewport, ChartMapViewport.Europe()) || Same(viewport, ChartMapViewport.Poland());
}
