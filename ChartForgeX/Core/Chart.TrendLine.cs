using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds a retained-source trend line with an atomically fitted model and optional pointwise interval.</summary>
    /// <param name="name">The display name.</param>
    /// <param name="points">The original source observations.</param>
    /// <param name="color">The optional series color.</param>
    /// <param name="configure">The independently captured model and interval configuration.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTrendLine(string name, IEnumerable<ChartPoint> points, ChartColor? color, Action<ChartTrendLineOptions> configure) {
        var series = ChartSeries.CreateTrendLine(name, points, color, configure);
        AppendSeries(series);
        return this;
    }

    /// <summary>
    /// Adds a least-squares trend line computed from the supplied points.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">The source points used to compute the trend line.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddTrendLine(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        AppendSeries(ChartSeries.CreateTrendLine(name, points, color));
        return this;
    }
}
