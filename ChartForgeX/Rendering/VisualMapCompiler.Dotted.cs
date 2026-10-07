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
        foreach (var point in series.Points) ValidateCoordinate(point);
        var weights = new double?[series.Points.Count];
        for (var index = 0; index < weights.Length; index++) {
            weights[index] = index < series.PointValues.Count ? series.PointValues[index] : null;
            if (weights[index].HasValue && (!Finite(weights[index]!.Value) || weights[index]!.Value < 0)) throw new InvalidOperationException("Dotted map weights must be finite and non-negative.");
        }
        var valued = weights.Where(value => value.HasValue).Select(value => value!.Value).ToArray();
        var min = valued.Length > 0 ? valued.Min() : 0; var max = valued.Length > 0 ? valued.Max() : 0;
        var initial = Fit(plot, longitudeSpan, latitudeSpan);
        var dot = Math.Min(Same(viewport, ChartMapViewport.World()) || Same(viewport, ChartMapViewport.Europe()) || Same(viewport, ChartMapViewport.Poland()) ? 3.6 : 5.2,
            Math.Min(initial.Width / Math.Max(1, longitudeSpan / 3.5), initial.Height / Math.Max(1, latitudeSpan / 3.5)) * .42);
        var maxRadius = series.MarkerRadius ?? PointRadius(dot, valued.Length > 0 ? max : (double?)null, min, max);
        var map = Fit(plot, longitudeSpan, latitudeSpan, Math.Max(dot * 2.45, maxRadius * 1.85) + Math.Max(1, dot * .55) / 2);
        var labels = new List<MapLabel>(); var obstacles = new List<LabelObstacle>();
        using (builder.PushGroup("series-0", "dotted-map", new Dictionary<string, string> {
            ["role"] = "group", ["aria-label"] = chart.Options.Labels.Describe(DescriptionFacts(chart, true)),
            ["data-cfx-series"] = "0", ["data-cfx-series-key"] = series.InteractionIdentityKey, ["data-cfx-label"] = series.Name,
            ["data-cfx-projection"] = "equirectangular", ["data-cfx-viewport"] = viewport.Name ?? "",
            ["data-cfx-point-count"] = N(series.Points.Count), ["data-cfx-visible-point-count"] = N(series.Points.Count(point => Visible(point, viewport))),
            ["data-cfx-valued-point-count"] = N(valued.Length), ["data-cfx-connector-count"] = N(chart.Options.MapConnectors.Count),
            ["data-cfx-viewport-min-longitude"] = N(viewport.MinimumLongitude), ["data-cfx-viewport-max-longitude"] = N(viewport.MaximumLongitude),
            ["data-cfx-viewport-min-latitude"] = N(viewport.MinimumLatitude), ["data-cfx-viewport-max-latitude"] = N(viewport.MaximumLatitude),
            ["data-cfx-min-value"] = N(min), ["data-cfx-max-value"] = N(max)
        })) using (builder.PushClip(plot)) {
            using (builder.PushClip(map)) {
                DottedLand(chart, context, builder, map, viewport, dot, colors);
                Routes(chart, context, builder, map, plot, viewport, dot, weights, min, max, colors, labels);
            }
            for (var index = 0; index < series.Points.Count; index++) {
                var source = series.Points[index]; var visible = Visible(source, viewport); var point = Geographic(source, viewport, map);
                var category = ChartAxisValueFormatter.FindExplicitLabel(chart.Options.XAxisLabels, index + 1) ?? "Point " + (index + 1);
                var formatted = weights[index].HasValue ? ChartNumericFormatter.FormatValue(chart.Options, weights[index]!.Value) : "";
                var full = category + (formatted.Length > 0 ? ": " + formatted : "") + "; " + N(source.X) + ", " + N(source.Y);
                var id = "series-0-point-" + index; var radius = series.MarkerRadius ?? PointRadius(dot, weights[index], min, max);
                var bounds = visible ? new ChartRect(point.X - radius, point.Y - radius, radius * 2, radius * 2) : map;
                builder.AddRegion(new VisualSemanticRegion(id, "dotted-map-point", bounds, full));
                using (builder.PushGroup(id, "dotted-map-point-source", new Dictionary<string, string> {
                    ["data-cfx-point"] = N(index), ["data-cfx-label"] = category, ["data-cfx-longitude"] = N(source.X), ["data-cfx-latitude"] = N(source.Y),
                    ["data-cfx-value"] = weights[index].HasValue ? N(weights[index]!.Value) : "", ["data-cfx-formatted-value"] = formatted,
                    ["data-cfx-visible"] = visible ? "true" : "false", ["aria-label"] = full
                })) {
                    if (!visible) continue;
                    var color = index < series.PointColors.Count && series.PointColors[index].HasValue ? series.PointColors[index]!.Value
                        : series.Color ?? ChartSeriesColours.State(series.StateRole, colors, colors.Status.Pass.Fill);
                    var paint = SvgPaint.Of(color, series.Color.HasValue || index < series.PointColors.Count && series.PointColors[index].HasValue ? SvgColorRole.Series : SvgColorRole.Status);
                    var halo = Math.Max(dot * 2.45, radius * 1.85);
                    builder.Ellipse(point.X, point.Y, halo, halo, ChartColorMath.WithOpacity(color, .18), role: "dotted-map-point-halo", paint: VisualChartPaint.Fill(paint.WithOpacity(ChartColorMath.WithOpacity(color, .18), .18)));
                    builder.Ellipse(point.X, point.Y, radius, radius, color, colors.Surface, Math.Max(1, dot * .55), "dotted-map-point", paint: new VisualScenePaintBinding(paint, SvgPaint.Of(colors.Surface, SvgColorRole.Surface)));
                    obstacles.Add(new LabelObstacle(id, new ChartRect(point.X - halo, point.Y - halo, halo * 2, halo * 2)));
                    if (series.ShowDataLabels ?? chart.Options.ShowDataLabels) {
                        var text = index < series.PointLabels.Count && series.PointLabels[index] != null ? series.PointLabels[index]!
                            : category + (formatted.Length > 0 ? " " + formatted : "");
                        AddMapLabel(builder, labels, text, text, point, MapDataStyle(chart, context, index), id + "-label", "dotted-map-data-label", plot, radius + context.Theme.Spacing, 50,
                            series.DataLabelPlacement ?? chart.Options.DataLabelPlacement, leaderColor: color, leaderPaint: paint,
                            leaderRadius: radius + Math.Max(1, dot * .55) / 2);
                    }
                }
            }
            DrawMapLabels(builder, plot, labels, obstacles, context.Theme.Spacing);
        }
        if (series.Points.Count == 0) builder.AddDiagnostic(new VisualDiagnostic("map.no-data", "The dotted map has no observations."));
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
    private static bool Same(ChartMapViewport a, ChartMapViewport b) => ChartMath.SameCoordinate(a.MinimumLongitude, b.MinimumLongitude) && ChartMath.SameCoordinate(a.MaximumLongitude, b.MaximumLongitude)
        && ChartMath.SameCoordinate(a.MinimumLatitude, b.MinimumLatitude) && ChartMath.SameCoordinate(a.MaximumLatitude, b.MaximumLatitude);
}
