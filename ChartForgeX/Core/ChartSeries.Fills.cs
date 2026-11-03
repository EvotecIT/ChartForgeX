using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartSeries {
    /// <summary>Gets or sets the interior paint for this series. Null retains the ordinary color and theme fill.</summary>
    public ChartFill? Fill { get; set; }

    /// <summary>Sets the series interior paint and returns this series for fluent authoring.</summary>
    public ChartSeries WithFill(ChartFill? fill) { Fill = fill; return this; }

    internal Dictionary<int, ChartFill> PointFillOverrides { get; private set; } = new();

    internal ChartFill? ResolveFill(int pointIndex) =>
        PointFillOverrides.TryGetValue(pointIndex, out var fill) ? fill : Fill;
}
