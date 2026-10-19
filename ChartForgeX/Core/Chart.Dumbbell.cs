using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a dumbbell comparison series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="values">The paired values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddDumbbell(string name, IEnumerable<ChartDumbbell> values, ChartColor? color = null) {
        var series = ChartSeries.CreateDumbbell(name, values, color);
        AppendSeries(series);
        return this;
    }
}
