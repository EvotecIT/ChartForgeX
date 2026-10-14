using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Creates a line, area or scatter series with explicit decimation and retained source-point provenance.</summary>
    public static ChartSeries CreateDecimated(ChartSeriesId id, string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points,
        int maximumPoints, ChartDecimationMode mode = ChartDecimationMode.LargestTriangleThreeBuckets, ChartColor? color = null) {
        ValidateResolutionKind(kind);
        var result = ChartDecimator.Decimate(points, maximumPoints, mode);
        var series = new ChartSeries(id, name, kind, result.Points) { Color = color };
        series.SetDecimation(result);
        return series;
    }

    internal static ChartSeries CreateDecimated(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points,
        int maximumPoints, ChartDecimationMode mode, ChartColor? color) {
        ValidateResolutionKind(kind);
        var result = ChartDecimator.Decimate(points, maximumPoints, mode);
        var series = new ChartSeries(name, kind, result.Points) { Color = color };
        series.SetDecimation(result);
        return series;
    }

    /// <summary>Creates a line, area or scatter series using a width-aware resolution policy and retained source provenance.</summary>
    public static ChartSeries CreateAdaptive(ChartSeriesId id, string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points,
        double viewportWidth, ChartResolutionPolicy? policy = null, ChartColor? color = null) =>
        Adaptive(id, name, kind, points, viewportWidth, policy, color);

    internal static ChartSeries CreateAdaptive(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points,
        double viewportWidth, ChartResolutionPolicy? policy, ChartColor? color) =>
        Adaptive(null, name, kind, points, viewportWidth, policy, color);

    private static ChartSeries Adaptive(ChartSeriesId? id, string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points,
        double viewportWidth, ChartResolutionPolicy? policy, ChartColor? color) {
        ValidateResolutionKind(kind);
        if (points == null) throw new ArgumentNullException(nameof(points));
        var source = points as IReadOnlyList<ChartPoint> ?? points.ToArray();
        var effective = policy ?? ChartResolutionPolicy.Trend();
        var budget = effective.ResolvePointBudget(viewportWidth);
        var series = source.Count > budget
            ? id.HasValue ? CreateDecimated(id.Value, name, kind, source, budget, effective.DecimationMode, color)
                : CreateDecimated(name, kind, source, budget, effective.DecimationMode, color)
            : id.HasValue ? new ChartSeries(id.Value, name, kind, source) { Color = color }
                : new ChartSeries(name, kind, source) { Color = color };
        if (kind != ChartSeriesKind.Scatter && series.Points.Count > effective.MaximumMarkerCount) {
            series.WithMarkerRadius(0);
            series.PreserveHiddenMarkerInteractionTargets();
        }
        return series;
    }

    private static void ValidateResolutionKind(ChartSeriesKind kind) {
        if (kind != ChartSeriesKind.Line && kind != ChartSeriesKind.Area && kind != ChartSeriesKind.Scatter)
            throw new ArgumentException("Resolution policies support line, area and scatter series.", nameof(kind));
    }
}
