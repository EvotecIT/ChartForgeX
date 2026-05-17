using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Rendering;

internal static class ChartRouteVisualStyles {
    public static ChartLineVisualStyle TreeLink() => ChartLineVisualStyle.Premium()
        .WithAmbientHalo(0.018, 2.6)
        .WithHalo(0.070, 1.4)
        .WithHighlight(0.040, 0.18);

    public static ChartLineVisualStyle TopologyEdge(bool monitoringStyle, bool muted, bool selected) {
        if (!monitoringStyle) return ChartLineVisualStyle.Premium()
            .WithAmbientHalo(selected ? 0.035 : muted ? 0.006 : 0.014, selected ? 4.0 : muted ? 1.6 : 2.4)
            .WithHalo(selected ? 0.11 : muted ? 0.025 : 0.055, selected ? 2.4 : muted ? 0.8 : 1.2)
            .WithHighlight(selected ? 0.080 : muted ? 0.018 : 0.035, 0.18);
        if (muted && !selected) return ChartLineVisualStyle.Premium()
            .WithAmbientHalo(0, 0)
            .WithHalo(0.030, 0.8)
            .WithHighlight(0.015, 0.18);
        return ChartLineVisualStyle.Premium()
            .WithAmbientHalo(selected ? 0.040 : 0.014, selected ? 4.2 : 2.2)
            .WithHalo(selected ? 0.12 : 0.052, selected ? 2.4 : 1.1)
            .WithHighlight(selected ? 0.085 : 0.034, 0.18);
    }

    public static ChartLineVisualStyle DottedMapLeader() => ChartLineVisualStyle.Premium()
        .WithAmbientHalo(0.012, 1.8)
        .WithHalo(0.052, 1.0)
        .WithHighlight(0.030, 0.18);

    public static ChartLeaderVisualStyle TopologyEdgeLabelLeader(bool monitoringStyle) => monitoringStyle
        ? new ChartLeaderVisualStyle(0.90, 4.0, 0.48, 1.35, 3.0, 4.0)
        : new ChartLeaderVisualStyle(0.76, 3.0, 0.42, 1.1, 3.0, 4.0);

    public static ChartLeaderVisualStyle TopologyGeographicCalloutLeader() =>
        new(0.76, 4.8, 0.72, 1.4, 4.0, 5.0);

    public static ChartLeaderVisualStyle TopologyGeographicMiniTopologyLeader() =>
        new(0, 0, 0.62, 1.1, 2.5, 3.0);

    public static IReadOnlyList<ChartPoint> SampleCubic(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, int segments) {
        if (segments < 1) throw new ArgumentOutOfRangeException(nameof(segments), segments, "Curve segments must be positive.");
        var points = new List<ChartPoint>(segments + 1) { new(x0, y0) };
        for (var step = 1; step <= segments; step++) {
            var t = step / (double)segments;
            points.Add(new ChartPoint(Cubic(x0, x1, x2, x3, t), Cubic(y0, y1, y2, y3, t)));
        }

        return points;
    }

    private static double Cubic(double p0, double p1, double p2, double p3, double t) {
        var u = 1 - t;
        return u * u * u * p0 + 3 * u * u * t * p1 + 3 * u * t * t * p2 + t * t * t * p3;
    }
}

internal readonly struct ChartLeaderVisualStyle {
    public ChartLeaderVisualStyle(double haloOpacity, double haloStrokeWidth, double strokeOpacity, double strokeWidth, double dash, double gap) {
        HaloOpacity = haloOpacity;
        HaloStrokeWidth = haloStrokeWidth;
        StrokeOpacity = strokeOpacity;
        StrokeWidth = strokeWidth;
        Dash = dash;
        Gap = gap;
    }

    public double HaloOpacity { get; }
    public double HaloStrokeWidth { get; }
    public double StrokeOpacity { get; }
    public double StrokeWidth { get; }
    public double Dash { get; }
    public double Gap { get; }
}
