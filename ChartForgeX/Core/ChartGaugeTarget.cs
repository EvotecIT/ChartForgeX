namespace ChartForgeX.Core;

/// <summary>Describes an authored numeric reference for a gauge reading.</summary>
public readonly struct ChartGaugeTarget {
    /// <summary>Gets the reference value, before display clamping.</summary>
    public double Value { get; }
    /// <summary>Gets the optional authored label. Null uses the formatted reference value.</summary>
    public string? Label { get; }

    /// <summary>Creates a finite reference value and optional label in the gauge's value units.</summary>
    /// <param name="value">The authored reference value, which may lie outside the gauge scale.</param>
    /// <param name="label">Optional text displayed for the reference.</param>
    public ChartGaugeTarget(double value, string? label = null) {
        ChartGuards.Finite(value, nameof(value));
        Value = value; Label = label;
    }
}
