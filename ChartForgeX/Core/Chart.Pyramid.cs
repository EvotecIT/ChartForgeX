using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds ordered non-negative values as triangular partitions of a whole.</summary>
    /// <param name="name">The display name of the series.</param>
    /// <param name="points">The ordered source categories and values. Their sum must be finite.</param>
    /// <param name="color">An optional series color; point colors can override it.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>Height encoding is the default. Zero values retain source facts without receiving positive geometry.</remarks>
    public Chart AddPyramid(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        if (name == null) throw new ArgumentNullException(nameof(name));
        var values = ChartGuards.Points(points, nameof(points));
        ChartPyramidWeights.Total(values);
        return Add(name, ChartSeriesKind.Pyramid, values, color);
    }
}
