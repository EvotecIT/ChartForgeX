using System;

namespace ChartForgeX.Core;

/// <summary>
/// Represents a display label for a numeric axis value.
/// </summary>
public readonly struct ChartAxisLabel {
    /// <summary>
    /// Gets the axis value that should display the label.
    /// </summary>
    public readonly double Value;

    /// <summary>
    /// Gets the text displayed for the axis value.
    /// </summary>
    public readonly string Text;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAxisLabel"/> struct.
    /// </summary>
    /// <param name="value">The numeric axis value.</param>
    /// <param name="text">The label text.</param>
    public ChartAxisLabel(double value, string text) {
        ChartGuards.Finite(value, nameof(value));
        Value = value;
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartAxisLabel"/> struct from a date/time axis value.
    /// </summary>
    /// <param name="value">The date/time axis value.</param>
    /// <param name="text">The label text.</param>
    public ChartAxisLabel(DateTime value, string text) {
        Value = value.ToOADate();
        Text = text ?? throw new ArgumentNullException(nameof(text));
    }
}
