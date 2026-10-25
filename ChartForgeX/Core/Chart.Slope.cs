using System;
using System.Collections.Generic;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

public sealed partial class Chart {
    /// <summary>
    /// Adds a slope series comparing one start value to one end value.
    /// </summary>
    /// <param name="name">The comparison name.</param>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddSlope(string name, double start, double end, ChartColor? color = null) {
        EnsureCanAddSeries();
        EnsureSlopeAxisLabels("Start", "End");
        return AddSlopeCore(name, start, end, color);
    }

    /// <summary>
    /// Adds a slope series comparing one start value to one end value with explicit endpoint labels.
    /// </summary>
    /// <param name="name">The comparison name.</param>
    /// <param name="start">The start value.</param>
    /// <param name="end">The end value.</param>
    /// <param name="startLabel">The x-axis label for the start value.</param>
    /// <param name="endLabel">The x-axis label for the end value.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddSlope(string name, double start, double end, string startLabel, string endLabel, ChartColor? color = null) {
        EnsureCanAddSeries();
        if (startLabel == null) throw new ArgumentNullException(nameof(startLabel));
        if (endLabel == null) throw new ArgumentNullException(nameof(endLabel));
        EnsureSlopeAxisLabels(startLabel, endLabel);
        return AddSlopeCore(name, start, end, color);
    }

    /// <summary>Adds an ordered slope sequence whose X coordinates identify stages and Y coordinates carry values.</summary>
    /// <remarks>The input is captured before attaching the series. Empty sequences retain a no-data series; one point is rejected.
    /// Two or more points retain authored order and explicit segment breaks. Configure category labels explicitly for this overload.</remarks>
    /// <param name="name">The series name.</param>
    /// <param name="points">The finite stage observations, including any explicit breaks.</param>
    /// <param name="color">An optional series color.</param>
    /// <returns>The current chart.</returns>
    public Chart AddSlope(string name, IEnumerable<ChartPoint> points, ChartColor? color = null) {
        EnsureCanAddSeries();
        var series = new ChartSeries(name, ChartSeriesKind.Slope, points);
        if (series.Points.Count == 1) throw new ArgumentException("A slope sequence must be empty or contain at least two stage observations.", nameof(points));
        series.Color = color;
        AppendSeries(series);
        return this;
    }

    private Chart AddSlopeCore(string name, double start, double end, ChartColor? color) {
        ChartGuards.Finite(start, nameof(start));
        ChartGuards.Finite(end, nameof(end));
        return Add(name, ChartSeriesKind.Slope, new[] { new ChartPoint(1, start), new ChartPoint(2, end) }, color);
    }

    private void EnsureSlopeAxisLabels(string startLabel, string endLabel) {
        if (Options.XAxisLabels.Count > 0) return;
        Options.XAxisLabels.Add(new ChartAxisLabel(1, startLabel));
        Options.XAxisLabels.Add(new ChartAxisLabel(2, endLabel));
    }
}
