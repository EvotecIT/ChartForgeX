using System;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>An immutable discrete numeric band. Its upper bound is exclusive; null ends the final unbounded band.</summary>
public sealed class ChartColorBand {
    /// <summary>Creates a band ending before <paramref name="upperBound"/>, or an unbounded final band.</summary>
    /// <param name="upperBound">A finite exclusive upper bound, or null for the final band.</param>
    /// <param name="color">The color for every value in the band.</param>
    /// <param name="label">An optional band name shown with its interval in legends.</param>
    public ChartColorBand(double? upperBound, ChartColor color, string? label = null) {
        if (upperBound.HasValue && !ChartMath.IsFinite(upperBound.Value))
            throw new ArgumentOutOfRangeException(nameof(upperBound), upperBound, "Color band bounds must be finite.");
        UpperBound = upperBound;
        Color = color;
        Label = string.IsNullOrWhiteSpace(label) ? null : label!.Trim();
    }

    /// <summary>Gets the exclusive upper bound, or null for the final unbounded band.</summary>
    public double? UpperBound { get; }

    /// <summary>Gets the color assigned to the band.</summary>
    public ChartColor Color { get; }

    /// <summary>Gets the optional normalized band name.</summary>
    public string? Label { get; }
}
