using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

/// <summary>Owns source-ordered Polar run boundaries and uses the same open/closed curve builder as Radar.</summary>
internal static class ChartPolarPathGeometry {
    internal static IReadOnlyList<ChartRadarGeometry.RadarPath> Paths(ChartPolarOptions options, IReadOnlyList<ChartPoint> points) {
        var paths = new List<ChartRadarGeometry.RadarPath>();
        foreach (var run in ChartPointSegments.Split(points)) {
            if (run.Count == 0) continue;
            var closed = run.Count >= 3 && (options.Form == ChartLineAreaForm.Area || options.ClosePath);
            var tension = options.CurveTension ?? 0;
            paths.Add(new ChartRadarGeometry.RadarPath(closed ? ChartPathBuilder.ClosedCurve(run, tension)
                : ChartPathBuilder.OpenCurve(run, tension), closed));
        }
        return paths;
    }

    /// <summary>A legend promises fill only when at least one independently authored run can enclose it.</summary>
    internal static bool HasClosedArea(ChartSeries series) => ChartPointSegments.Split(series.RenderPoints).Any(run => run.Count >= 3);

    internal static void DiagnoseOpenArea(VisualSceneBuilder builder, ChartSeries series, IReadOnlyList<ChartRadarGeometry.RadarPath> paths) {
        if (series.Polar.Form == ChartLineAreaForm.Area && paths.Any(path => !path.Closed))
            builder.AddDiagnostic(new VisualDiagnostic("polar.open-area", "Polar series '" + series.Name
                + "' retains runs with fewer than three vertices as observations and open outlines without an invented area."));
    }
}
