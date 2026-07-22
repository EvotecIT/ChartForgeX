using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds an explicitly decimated line series and records its source-point provenance.</summary>
    public Chart AddDecimatedLine(string name, IEnumerable<ChartPoint> points, int maximumPoints, ChartDecimationMode mode = ChartDecimationMode.LargestTriangleThreeBuckets, ChartColor? color = null) {
        return AddDecimated(name, ChartSeriesKind.Line, points, maximumPoints, mode, color);
    }

    /// <summary>Adds an explicitly decimated area series and records its source-point provenance.</summary>
    public Chart AddDecimatedArea(string name, IEnumerable<ChartPoint> points, int maximumPoints, ChartDecimationMode mode = ChartDecimationMode.LargestTriangleThreeBuckets, ChartColor? color = null) {
        return AddDecimated(name, ChartSeriesKind.Area, points, maximumPoints, mode, color);
    }

    /// <summary>Adds an explicitly decimated scatter series and records its source-point provenance.</summary>
    public Chart AddDecimatedScatter(string name, IEnumerable<ChartPoint> points, int maximumPoints, ChartDecimationMode mode = ChartDecimationMode.LargestTriangleThreeBuckets, ChartColor? color = null) {
        return AddDecimated(name, ChartSeriesKind.Scatter, points, maximumPoints, mode, color);
    }

    /// <summary>Adds a line series that is decimated only when its source exceeds the width-aware policy budget.</summary>
    public Chart AddAdaptiveLine(string name, IEnumerable<ChartPoint> points, double viewportWidth, ChartResolutionPolicy? policy = null, ChartColor? color = null) {
        return AddAdaptive(name, ChartSeriesKind.Line, points, viewportWidth, policy, color);
    }

    /// <summary>Adds an area series that is decimated only when its source exceeds the width-aware policy budget.</summary>
    public Chart AddAdaptiveArea(string name, IEnumerable<ChartPoint> points, double viewportWidth, ChartResolutionPolicy? policy = null, ChartColor? color = null) {
        return AddAdaptive(name, ChartSeriesKind.Area, points, viewportWidth, policy, color);
    }

    /// <summary>Adds a scatter series that is decimated only when its source exceeds the width-aware policy budget.</summary>
    public Chart AddAdaptiveScatter(string name, IEnumerable<ChartPoint> points, double viewportWidth, ChartResolutionPolicy? policy = null, ChartColor? color = null) {
        return AddAdaptive(name, ChartSeriesKind.Scatter, points, viewportWidth, policy, color);
    }

    private Chart AddDecimated(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points, int maximumPoints, ChartDecimationMode mode, ChartColor? color) {
        var result = ChartDecimator.Decimate(points, maximumPoints, mode);
        Add(name, kind, result.Points, color);
        Series[Series.Count - 1].SetDecimation(result);
        return this;
    }

    private Chart AddAdaptive(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points, double viewportWidth, ChartResolutionPolicy? policy, ChartColor? color) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        var source = points as IReadOnlyList<ChartPoint> ?? points.ToArray();
        var effectivePolicy = policy ?? ChartResolutionPolicy.Trend();
        var maximumPoints = effectivePolicy.ResolvePointBudget(viewportWidth);
        if (source.Count > maximumPoints) AddDecimated(name, kind, source, maximumPoints, effectivePolicy.DecimationMode, color);
        else Add(name, kind, source, color);
        if (kind != ChartSeriesKind.Scatter && Series[Series.Count - 1].Points.Count > effectivePolicy.MaximumMarkerCount) {
            Series[Series.Count - 1].WithMarkerRadius(0d);
            Series[Series.Count - 1].PreserveHiddenMarkerInteractionTargets();
        }
        return this;
    }
}
