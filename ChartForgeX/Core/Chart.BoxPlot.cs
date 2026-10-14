using System;
using System.Collections.Generic;
using System.Linq;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Configures box plot value direction. Multiple box plot series share distinct category slots.</summary>
    /// <remarks>Horizontal box plots require only box plot series on the primary X value axis, with categories on Y. Configure before or after adding series.</remarks>
    public Chart ConfigureBoxPlot(Action<ChartBoxPlotOptions> configure) {
        if (configure == null) throw new ArgumentNullException(nameof(configure));
        Options.BoxPlot = ChartOptionConfiguration.Configure(Options.BoxPlot, configure, options => options.Clone());
        return this;
    }

    /// <summary>
    /// Adds one box plot from raw sample values, using linearly interpolated quartiles and minimum/maximum whiskers.
    /// </summary>
    /// <remarks>For sorted samples, quantiles interpolate at zero-based position <c>(n - 1) * p</c>,
    /// with <c>p</c> equal to 0.25, 0.5 and 0.75. A single sample supplies all five values; no outlier filtering is applied.
    /// The original samples are retained in <see cref="ChartSeries.BoxPlotSourceSamples"/>.
    /// </remarks>
    /// <param name="name">The series name.</param>
    /// <param name="x">The category coordinate, mapped to X vertically or Y horizontally.</param>
    /// <param name="values">The raw sample values used to compute the five-number summary.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBoxPlot(string name, double x, IEnumerable<double> values, ChartColor? color = null) {
        AppendSeries(ChartSeries.CreateBoxPlot(name, x, values, color));
        return this;
    }

    /// <summary>
    /// Adds a box plot series.
    /// </summary>
    /// <param name="name">The series name.</param>
    /// <param name="boxes">The box plot summaries to render.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddBoxPlot(string name, IEnumerable<ChartBoxPlot> boxes, ChartColor? color = null) {
        var series = ChartSeries.CreateBoxPlot(name, boxes, color);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(boxes));
        AppendSeries(series);
        return this;
    }

}
