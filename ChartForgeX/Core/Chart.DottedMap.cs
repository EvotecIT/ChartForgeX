using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a dotted world map with highlighted longitude/latitude points.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The labeled map points to render.</param>
    /// <param name="color">An optional point color. When null, the positive theme color is used.</param>
    /// <returns>The current chart.</returns>
    public Chart AddDottedMap(string name, IEnumerable<ChartMapPoint> points, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (points == null) throw new ArgumentNullException(nameof(points));
        var chartPoints = new List<ChartPoint>();
        var labels = new List<ChartAxisLabel>();
        var colors = new List<ChartColor?>();
        var values = new List<double?>();
        var index = 1;
        foreach (var point in points) {
            chartPoints.Add(new ChartPoint(point.Longitude, point.Latitude));
            labels.Add(new ChartAxisLabel(index, point.Label));
            colors.Add(point.Color);
            values.Add(point.Value);
            index++;
        }

        if (chartPoints.Count == 0) throw new ArgumentException("Dotted maps must contain at least one map point.", nameof(points));
        Options.XAxisLabels.Clear();
        Options.XAxisLabels.AddRange(labels);
        Add(name, ChartSeriesKind.DottedMap, chartPoints, color);
        Series[Series.Count - 1].PointColors.AddRange(colors);
        Series[Series.Count - 1].PointValues.AddRange(values);
        return this;
    }
}
