using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures range-bar direction and captions. Multiple range-bar series share distinct category slots.</summary>
    /// <remarks>Horizontal range bars use the primary X value axis and Y category axis, and require only range-bar series. Configure before or after adding the series.</remarks>
    public Chart ConfigureRangeBar(Action<ChartRangeBarOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.RangeBar = ChartOptionConfiguration.Configure(Options.RangeBar, configure, options => options.Clone());
        return this;
    }

    /// <summary>
    /// Adds a range-bar series with the configured orientation and one distinct category slot per series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="intervals">The interval values to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddRangeBar(string name, IEnumerable<ChartInterval> intervals, ChartColor? color = null) {
        var series = ChartSeries.CreateRangeBar(name, intervals, color);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(intervals));
        AppendSeries(series);
        return this;
    }
}
