using System;

namespace ChartForgeX.Primitives;

public readonly partial struct ChartColor : IEquatable<ChartColor> {
    /// <summary>Compares the channels and the CSS variable name.</summary>
    public bool Equals(ChartColor other) => R == other.R && G == other.G && B == other.B && A == other.A
        && string.Equals(CssVariable, other.CssVariable, StringComparison.Ordinal);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is ChartColor other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => unchecked((((R * 397) ^ G) * 397 ^ B) * 397 ^ A) ^ (CssVariable == null ? 0 : StringComparer.Ordinal.GetHashCode(CssVariable));

    /// <summary>Returns the CSS form, the same as <see cref="ToCss"/>.</summary>
    public override string ToString() => ToCss();

    /// <summary>Returns whether two colours are equal.</summary>
    public static bool operator ==(ChartColor left, ChartColor right) => left.Equals(right);

    /// <summary>Returns whether two colours differ.</summary>
    public static bool operator !=(ChartColor left, ChartColor right) => !left.Equals(right);
}
