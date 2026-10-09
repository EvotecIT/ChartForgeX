using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a progress-ring series where each point renders as a circular progress ring.
    /// </summary>
    /// <param name="name">The progress-ring group name.</param>
    /// <param name="points">The ring values. The x values identify labels and y values must be between zero and 100.</param>
    /// <param name="color">An optional shared ring color. When null, the theme palette colors rings independently.</param>
    /// <returns>The current chart.</returns>
    public Chart AddProgressRing(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        if (points == null) throw new ArgumentNullException(nameof(points));
        var materialized = ChartGuards.Points(points, nameof(points));
        if (materialized.Count == 0) throw new ArgumentException("Progress-ring charts must contain at least one value.", nameof(points));
        for (var i = 0; i < materialized.Count; i++) {
            var value = materialized[i].Y;
            if (value < 0 || value > 100) throw new ArgumentOutOfRangeException(nameof(points), value, "Progress-ring values must be between zero and 100.");
        }

        return Add(name, ChartSeriesKind.ProgressRing, materialized, color);
    }
}
