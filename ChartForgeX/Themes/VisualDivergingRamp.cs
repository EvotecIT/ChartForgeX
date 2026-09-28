using System;
using System.Collections.Generic;
using ChartForgeX.Core;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

/// <summary>
/// A diverging colour ramp for values around a meaningful midpoint: a negative arm, a neutral midpoint colour, and a
/// positive arm. Each arm runs from the weakest to the strongest magnitude.
/// </summary>
public sealed class VisualDivergingRamp {
    private readonly ChartColor[] _negative;
    private readonly ChartColor[] _positive;

    /// <summary>Initializes a diverging ramp.</summary>
    /// <param name="negative">The negative arm, weakest to strongest; at least one colour.</param>
    /// <param name="neutral">The midpoint colour.</param>
    /// <param name="positive">The positive arm, weakest to strongest; at least one colour.</param>
    public VisualDivergingRamp(IEnumerable<ChartColor> negative, ChartColor neutral, IEnumerable<ChartColor> positive) {
        _negative = Materialize(negative, nameof(negative));
        _positive = Materialize(positive, nameof(positive));
        Neutral = neutral;
    }

    /// <summary>Gets the negative arm, weakest to strongest.</summary>
    public IReadOnlyList<ChartColor> Negative => Array.AsReadOnly(_negative);

    /// <summary>Gets the midpoint colour.</summary>
    public ChartColor Neutral { get; }

    /// <summary>Gets the positive arm, weakest to strongest.</summary>
    public IReadOnlyList<ChartColor> Positive => Array.AsReadOnly(_positive);

    /// <summary>
    /// Creates a map colour scale from the strongest negative colour through the neutral midpoint to the strongest positive
    /// colour.
    /// </summary>
    /// <param name="midpointValue">The value drawn in the neutral colour; null uses the middle of the data range.</param>
    /// <remarks>Map colour scales hold three stops, so intermediate arm colours are dropped and each arm blends linearly.</remarks>
    /// <returns>A diverging map colour scale.</returns>
    public ChartMapColorScale ToMapColorScale(double? midpointValue = null) =>
        ChartMapColorScale.Diverging(_negative[_negative.Length - 1], Neutral, _positive[_positive.Length - 1], midpointValue);

    private static ChartColor[] Materialize(IEnumerable<ChartColor> colors, string parameterName) {
        if (colors == null) throw new ArgumentNullException(parameterName);
        var result = new List<ChartColor>(colors).ToArray();
        if (result.Length == 0) throw new ArgumentException("Each diverging arm needs at least one colour.", parameterName);
        return result;
    }
}
