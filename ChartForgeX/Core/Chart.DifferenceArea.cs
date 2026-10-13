using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>Adds two authored boundaries with separate positive and negative difference fills.</summary>
    /// <param name="name">The series name.</param>
    /// <param name="points">Finite pairs in increasing X order within each connected segment.</param>
    /// <param name="color">Optional shared boundary and fill color. Direction-specific fills override it.</param>
    /// <returns>The current chart.</returns>
    /// <remarks>Uses numeric/time X and linear primary or secondary Y. Explicit breaks cut both boundaries and fill.
    /// Positive means First is greater than Second, including when either axis is reversed.
    /// Linear interpolation splits crossings; Step uses the series step position for both held boundaries. Smooth is unsupported.</remarks>
    public Chart AddDifferenceArea(string name, IEnumerable<ChartDifferencePoint> points, ChartColor? color = null) {
        var series = ChartSeries.CreateDifferenceArea(name, points, color);
        if (series.ObservationCount == 0) throw new ArgumentException("The series must contain at least one observation.", nameof(points));
        AppendSeries(series);
        return this;
    }
}
