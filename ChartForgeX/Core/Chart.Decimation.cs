using System;
using System.Collections.Generic;
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

    private Chart AddDecimated(string name, ChartSeriesKind kind, IEnumerable<ChartPoint> points, int maximumPoints, ChartDecimationMode mode, ChartColor? color) {
        var result = ChartDecimator.Decimate(points, maximumPoints, mode);
        Add(name, kind, result.Points, color);
        Series[Series.Count - 1].SetDecimation(result);
        return this;
    }
}
