using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures the value direction of range bars. Multiple range-bar series share distinct category slots.</summary>
    /// <remarks>Horizontal range bars use the primary X value axis and Y category axis, and require only range-bar series. Configure before or after adding the series.</remarks>
    public Chart ConfigureRangeBar(Action<ChartRangeBarOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        configure(Options.RangeBar);
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
        if (intervals == null) throw new ArgumentNullException(nameof(intervals));
        var points = new List<ChartPoint>();
        foreach (var interval in intervals) {
            points.Add(new ChartPoint(interval.X, interval.Start));
            points.Add(new ChartPoint(interval.X, interval.End));
        }

        if (points.Count == 0) throw new ArgumentException("Range bar intervals must contain at least one interval.", nameof(intervals));
        return Add(name, ChartSeriesKind.RangeBar, points, color);
    }
}
