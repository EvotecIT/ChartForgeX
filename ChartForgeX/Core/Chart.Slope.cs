using System;
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
