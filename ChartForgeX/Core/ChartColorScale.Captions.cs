using System;
using System.Collections.Generic;

namespace ChartForgeX.Core;

public sealed partial class ChartColorScale {
    /// <summary>Gets the immutable numeric caption catalog, sorted by position, or an empty list.</summary>
    public IReadOnlyList<ChartColorScaleCaption> Captions => Array.AsReadOnly(_captions);

    /// <summary>Creates a continuous scale with copied captions at absolute measurement values.</summary>
    /// <remarks>
    /// Captions do not change the color mapping, observed domain or midpoint. They need not coincide with color
    /// anchors. Positions outside the current domain remain descriptive facts and are not clamped to its ends.
    /// Empty input clears the catalog. Discrete scales use their named bands instead.
    /// </remarks>
    /// <param name="captions">Non-null captions at distinct finite positions; input order is immaterial.</param>
    /// <returns>An immutable scale retaining the complete caption catalog.</returns>
    public ChartColorScale WithCaptions(IEnumerable<ChartColorScaleCaption> captions) {
        EnsureContinuous();
        if (captions == null) throw new ArgumentNullException(nameof(captions));
        var copy = new List<ChartColorScaleCaption>(captions);
        if (copy.Exists(caption => caption == null)) throw new ArgumentException("Color captions must not be null.", nameof(captions));
        copy.Sort((left, right) => left.Value.CompareTo(right.Value));
        for (var index = 1; index < copy.Count; index++)
            if (copy[index - 1].Value == copy[index].Value)
                throw new ArgumentException("Color caption positions must be distinct.", nameof(captions));
        return new(_colors, _midpointIndex, MidpointValue, MinimumValue, MaximumValue, NoDataColor,
            LowLabel, MidpointLabel, HighLabel, _bands, _stops, copy.ToArray());
    }
}
