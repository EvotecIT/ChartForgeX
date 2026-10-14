using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a single-value circular progress chart.
    /// </summary>
    /// <param name="name">The circle label.</param>
    /// <param name="value">The circle value.</param>
    /// <param name="min">The minimum circle value.</param>
    /// <param name="max">The maximum circle value.</param>
    /// <param name="color">An optional circle accent color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddCircle(string name, double value, double min = 0, double max = 100, ChartColor? color = null) {
        AppendSeries(ChartSeries.CreateCircle(name, new ChartGaugeValue(value, min, max), color));
        return this;
    }
}
