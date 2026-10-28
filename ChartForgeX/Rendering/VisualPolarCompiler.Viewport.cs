using System;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static partial class VisualPolarCompiler {
    /// <summary>Clips paint only; source groups and raw identities remain in the detached scene.</summary>
    private static IDisposable? PolarViewportClip(Chart chart, VisualSceneBuilder builder, ChartRect plot, PolarLayout geometry) {
        var request = chart.PolarPreparation?.Request;
        if (request == null) return null;
        var angle = request.Angle;
        var partial = angle != null && angle.Maximum - angle.Minimum < Math.PI * 2;
        if (!partial) return null;
        var start = partial ? PolarAngle(chart, chart.Options.XAxis.Reversed ? angle!.Minimum : angle!.Maximum) : 0;
        var sweep = partial ? angle!.Maximum - angle.Minimum : Math.PI * 2;
        // Angular clipping retains axis-caption space. Radius crops belong to their selected series/guide axis.
        var outer = Math.Max(plot.Width, plot.Height);
        return builder.PushClip(ChartSlicePathGeometry.ClipPath(geometry.Cx, geometry.Cy, outer, 0, start, sweep));
    }

    private static IDisposable? PolarRadiusClip(Chart chart, VisualSceneBuilder builder, PolarLayout geometry, ChartAxisSide side) {
        var preparation = chart.PolarPreparation;
        if (preparation == null || !preparation.HasRadiusWindow(side)) return null;
        return builder.PushClip(ChartSlicePathGeometry.ClipPath(geometry.Cx, geometry.Cy, geometry.Radius,
            geometry.Radius * preparation.InnerFraction(side), 0, Math.PI * 2));
    }

    private static double PolarGuideRadius(Chart chart, PolarLayout geometry, ChartAxisSide side, double fraction) =>
        chart.PolarPreparation?.HasRadiusWindow(side) == true
            ? geometry.Radius * chart.PolarPreparation.Guide(side, geometry.GridRadius / geometry.Radius * fraction)
            : geometry.GridRadius * fraction;

    private static double PolarRadius(Chart chart, ChartAxisSide side, RadialValueScale scale, double source) =>
        chart.PolarPreparation?.Normalize(side, scale, source) ?? scale.Normalize(source);
}
