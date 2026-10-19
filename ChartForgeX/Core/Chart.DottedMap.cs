using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a dotted map from complete labeled longitude/latitude observations.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The complete map observations, retaining repeated coordinates.</param>
    /// <param name="color">An optional default point color.</param>
    /// <param name="id">Optional stable identity assigned before attachment.</param>
    /// <returns>The current chart.</returns>
    public Chart AddDottedMap(string name, IEnumerable<ChartMapPoint> points, ChartColor? color = null, ChartSeriesId? id = null) {
        var series = id.HasValue ? ChartSeries.CreateDottedMap(id.Value, name, points, color) : ChartSeries.CreateDottedMap(name, points, color);
        return AddSeries(series);
    }
}
