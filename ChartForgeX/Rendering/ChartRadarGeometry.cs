using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Shares missing-category topology between standalone/mixed radar painting and curve fitting.</summary>
internal static class ChartRadarGeometry {
    internal static string MissingPolicy(ChartRadarOptions options) => options.MissingCategoryPolicy.ToString().ToLowerInvariant();

    /// <summary>Keeps area legends honest when a configured missing-data policy leaves only open outlines.</summary>
    internal static bool HasClosedArea(Chart chart, ChartSeries series) =>
        series.Radar.MissingCategoryPolicy == ChartRadarMissingCategoryPolicy.Zero
        || series.RenderPoints.Count >= 3 && (series.Radar.MissingCategoryPolicy == ChartRadarMissingCategoryPolicy.Connect
            || series.RenderPoints.Count == ChartBarCoordinateMap.Create(chart).Categories.Count);

    internal static void DiagnoseOpenArea(VisualSceneBuilder builder, ChartSeries series, IReadOnlyList<RadarPath> paths) {
        if (series.Radar.Form == ChartLineAreaForm.Area && !paths.Any(path => path.Closed))
            builder.AddDiagnostic(new VisualDiagnostic("radar.open-area", "Radar series '" + series.Name
                + "' retains its observed outline and points without area fill because its missing-category policy leaves no closed polygon."));
    }

    internal static IReadOnlyList<RadarPath> Paths(ChartRadarOptions options, IReadOnlyList<ChartPoint> points, IReadOnlyList<int> sources, bool cyclic = true) {
        var missing = -1;
        for (var index = 0; index < sources.Count; index++) if (sources[index] < 0) { missing = index; break; }
        var tension = options.CurveTension ?? 0;
        if (missing < 0 || options.MissingCategoryPolicy == ChartRadarMissingCategoryPolicy.Zero)
            return new[] { Closed(points, tension) };
        if (options.MissingCategoryPolicy == ChartRadarMissingCategoryPolicy.Connect)
            return new[] { Closed(points.Where((point, index) => sources[index] >= 0).ToArray(), tension) };
        if (!cyclic) return OpenRuns(points, sources, tension);
        var paths = new List<RadarPath>();
        var run = new List<ChartPoint>();
        // Starting after a gap keeps an authored run across the category seam intact.
        for (var offset = 1; offset <= points.Count; offset++) {
            var index = (missing + offset) % points.Count;
            if (sources[index] >= 0) run.Add(points[index]);
            else if (run.Count > 0) { paths.Add(new RadarPath(ChartPathBuilder.OpenCurve(run, tension), false)); run.Clear(); }
        }
        return paths;
    }

    /// <summary>Partial angular viewports have two physical ends; a Gap never joins the last category to the first across them.</summary>
    private static IReadOnlyList<RadarPath> OpenRuns(IReadOnlyList<ChartPoint> points, IReadOnlyList<int> sources, double tension) {
        var paths = new List<RadarPath>(); var run = new List<ChartPoint>();
        for (var index = 0; index <= points.Count; index++) {
            if (index < points.Count && sources[index] >= 0) run.Add(points[index]);
            else if (run.Count > 0) { paths.Add(new RadarPath(ChartPathBuilder.OpenCurve(run, tension), false)); run.Clear(); }
        }
        return paths;
    }

    private static RadarPath Closed(IReadOnlyList<ChartPoint> points, double tension) =>
        new(points.Count >= 3 ? ChartPathBuilder.ClosedCurve(points, tension) : ChartPathBuilder.FromPoints(points), points.Count >= 3);

    internal readonly struct RadarPath {
        internal RadarPath(ChartPath path, bool closed) { Path = path; Closed = closed; }
        internal ChartPath Path { get; }
        internal bool Closed { get; }
    }
}
