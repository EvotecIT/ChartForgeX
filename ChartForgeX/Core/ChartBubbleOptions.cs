using System;

namespace ChartForgeX.Core;

/// <summary>Controls one shared source-size domain and logical radius range for all bubble series in a chart.</summary>
/// <remarks>
/// Sizes use square-root interpolation of the radius range. Equal source sizes have equal logical radii;
/// this does not promise proportional painted areas across marker shapes. A constant automatic domain uses
/// the radius midpoint. Source observations remain finite and positive even when a domain starts at zero.
/// </remarks>
public sealed class ChartBubbleOptions {
    private double _minimumRadius = 6;
    private double? _maximumRadius;

    /// <summary>Gets or sets the non-negative minimum logical radius in pixels. Default: six.</summary>
    public double MinimumRadius {
        get => _minimumRadius;
        set { NonNegative(value, nameof(value)); _minimumRadius = value; }
    }

    /// <summary>
    /// Gets or sets the non-negative maximum logical radius in pixels. Null retains the responsive family default,
    /// bounded between 14 and 32 pixels and raised to the minimum radius when necessary.
    /// The explicit maximum must be at least <see cref="MinimumRadius"/> at preparation.
    /// </summary>
    public double? MaximumRadius {
        get => _maximumRadius;
        set { if (value.HasValue) NonNegative(value.Value, nameof(value)); _maximumRadius = value; }
    }

    /// <summary>Gets the explicit minimum source size. Null infers one domain from every bubble series.</summary>
    public double? MinimumValue { get; private set; }

    /// <summary>Gets the explicit maximum source size. Null infers one domain from every bubble series.</summary>
    public double? MaximumValue { get; private set; }

    /// <summary>Gets or sets whether larger source sizes map to smaller radii. Default: false.</summary>
    public bool Reversed { get; set; }

    /// <summary>
    /// Sets finite, non-negative, increasing source-size bounds for all bubble series.
    /// Observations outside the domain use its radius endpoints; their raw sizes remain unchanged.
    /// </summary>
    /// <param name="minimumValue">The inclusive minimum source size.</param>
    /// <param name="maximumValue">The inclusive maximum source size, greater than the minimum.</param>
    /// <returns>The current options.</returns>
    public ChartBubbleOptions WithSizeDomain(double minimumValue, double maximumValue) {
        NonNegative(minimumValue, nameof(minimumValue)); NonNegative(maximumValue, nameof(maximumValue));
        if (maximumValue <= minimumValue)
            throw new ArgumentOutOfRangeException(nameof(maximumValue), maximumValue, "Bubble size-domain bounds must be increasing.");
        MinimumValue = minimumValue; MaximumValue = maximumValue;
        return this;
    }

    /// <summary>Infers a common source-size domain from every bubble observation, including hidden and secondary-axis series.</summary>
    /// <returns>The current options.</returns>
    public ChartBubbleOptions UseAutomaticSizeDomain() { MinimumValue = null; MaximumValue = null; return this; }

    internal void Validate() {
        if (MaximumRadius.HasValue && MaximumRadius.Value < MinimumRadius)
            throw new InvalidOperationException("The bubble maximum radius must be at least the minimum radius.");
    }

    private static void NonNegative(double value, string parameter) {
        ChartGuards.Finite(value, parameter);
        if (value < 0) throw new ArgumentOutOfRangeException(parameter, value, "Bubble scale bounds must be non-negative.");
    }
}
