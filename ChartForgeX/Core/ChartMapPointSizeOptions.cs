using System;

namespace ChartForgeX.Core;

/// <summary>Controls one dotted-map dataset's independent weight domain and logical marker radius range.</summary>
public sealed class ChartMapPointSizeOptions {
    private double? _minimumRadius, _maximumRadius;
    internal bool IsConfigured { get; private set; }
    internal bool HasOverrides => IsConfigured || Reversed || SizeDimension.Name != null || SizeDimension.ValueFormat != null || SizeDimension.ShowLegend;

    /// <summary>Gets or sets an optional nonnegative minimum radius. Null preserves the responsive map family floor.</summary>
    public double? MinimumRadius { get => _minimumRadius; set { NonNegative(value, nameof(value)); _minimumRadius = value; IsConfigured = true; } }
    /// <summary>Gets or sets an optional nonnegative maximum radius. Null preserves the responsive map family maximum.</summary>
    public double? MaximumRadius { get => _maximumRadius; set { NonNegative(value, nameof(value)); _maximumRadius = value; IsConfigured = true; } }
    /// <summary>Gets the authored minimum weight, or null to infer it from this dataset's valued points.</summary>
    public double? MinimumValue { get; private set; }
    /// <summary>Gets the authored maximum weight, or null to infer it from this dataset's valued points.</summary>
    public double? MaximumValue { get; private set; }
    /// <summary>Gets or sets whether larger weights use smaller radii. Source weights and color values are unchanged.</summary>
    public bool Reversed { get; set; }
    /// <summary>Gets independent weight naming, formatting and optional numeric size-key options.</summary>
    public ChartSizeDimensionOptions SizeDimension { get; private set; } = new();

    /// <summary>Sets increasing finite nonnegative weight bounds. Values outside them use the endpoint radii.</summary>
    /// <param name="minimumValue">The inclusive minimum weight.</param>
    /// <param name="maximumValue">The inclusive maximum weight, greater than the minimum.</param>
    /// <returns>The current options.</returns>
    public ChartMapPointSizeOptions WithSizeDomain(double minimumValue, double maximumValue) {
        NonNegative(minimumValue, nameof(minimumValue)); NonNegative(maximumValue, nameof(maximumValue));
        if (maximumValue <= minimumValue) throw new ArgumentOutOfRangeException(nameof(maximumValue), "Map weight-domain bounds must be increasing.");
        MinimumValue = minimumValue; MaximumValue = maximumValue; IsConfigured = true;
        return this;
    }

    /// <summary>Infers the weight domain from every point in this dataset, including points outside the viewport.</summary>
    /// <returns>The current options.</returns>
    public ChartMapPointSizeOptions UseAutomaticSizeDomain() { MinimumValue = MaximumValue = null; return this; }

    internal ChartMapPointSizeOptions Clone() {
        var copy = (ChartMapPointSizeOptions)MemberwiseClone(); copy.SizeDimension = SizeDimension.Clone(); return copy;
    }
    internal void Validate() {
        if (MinimumRadius.HasValue && MaximumRadius.HasValue && MaximumRadius.Value < MinimumRadius.Value)
            throw new InvalidOperationException("The map maximum radius must be at least the minimum radius.");
    }
    private static void NonNegative(double? value, string parameter) {
        if (!value.HasValue) return;
        ChartGuards.Finite(value.Value, parameter);
        if (value.Value < 0) throw new ArgumentOutOfRangeException(parameter, value, "Map size bounds must be nonnegative.");
    }
}
