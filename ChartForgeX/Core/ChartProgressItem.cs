using System;
using ChartForgeX.Primitives;

namespace ChartForgeX.Core;

/// <summary>
/// Represents one labeled value in a progress-bar chart.
/// </summary>
public readonly struct ChartProgressItem {
    /// <summary>
    /// Gets the item label.
    /// </summary>
    public readonly string Label;

    /// <summary>
    /// Gets the item value.
    /// </summary>
    public readonly double Value;

    /// <summary>Gets the row minimum. Shared-maximum items start at zero.</summary>
    public double Minimum { get; }

    /// <summary>Gets the explicit row maximum, or null to use the chart shared maximum.</summary>
    public double? Maximum { get; }

    /// <summary>
    /// Gets the optional item color.
    /// </summary>
    public readonly ChartColor? Color;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartProgressItem"/> struct.
    /// </summary>
    /// <param name="label">The item label.</param>
    /// <param name="value">The item value. Negative values are not allowed.</param>
    /// <param name="color">An optional color for this item.</param>
    public ChartProgressItem(string label, double value, ChartColor? color = null) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        ChartGuards.Finite(value, nameof(value));
        if (value < 0) throw new ArgumentOutOfRangeException(nameof(value), value, "Progress item values must be zero or greater.");
        Label = label;
        Value = value;
        Color = color; Minimum = 0; Maximum = null;
    }

    /// <summary>Creates a row with its own finite positive numeric span. Raw readings may lie outside the span; only paint and editing gestures clamp.</summary>
    public ChartProgressItem(string label, double value, double minimum, double maximum, ChartColor? color = null) {
        if (label == null) throw new ArgumentNullException(nameof(label));
        var reading = new ChartGaugeValue(value, minimum, maximum);
        Label = label; Value = reading.Value; Minimum = reading.Minimum; Maximum = reading.Maximum; Color = color;
    }
}
