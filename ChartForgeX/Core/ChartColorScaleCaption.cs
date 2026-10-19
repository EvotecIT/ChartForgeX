using System;
using ChartForgeX.Rendering;

namespace ChartForgeX.Core;

/// <summary>An immutable caption at an absolute numeric position on a continuous color key.</summary>
public sealed class ChartColorScaleCaption {
    /// <summary>Creates a caption without changing the color mapping or its domain.</summary>
    /// <param name="value">The finite numeric position, in the measurement's units.</param>
    /// <param name="text">The nonblank caption. Surrounding whitespace is trimmed.</param>
    public ChartColorScaleCaption(double value, string text) {
        if (!ChartMath.IsFinite(value))
            throw new ArgumentOutOfRangeException(nameof(value), value, "Color caption values must be finite.");
        if (text == null) throw new ArgumentNullException(nameof(text));
        if (string.IsNullOrWhiteSpace(text)) throw new ArgumentException("A color caption must not be blank.", nameof(text));
        Value = value;
        Text = text.Trim();
    }

    /// <summary>Gets the absolute numeric position, independent of color-stop ordinals.</summary>
    public double Value { get; }

    /// <summary>Gets the complete caption retained when visible fitting shortens or omits it.</summary>
    public string Text { get; }
}
