using System;

namespace ChartForgeX.Interactivity.Html;

/// <summary>Controls how pointer movement acquires an HTML chart tooltip target.</summary>
public readonly struct HtmlChartTooltipRange : IEquatable<HtmlChartTooltipRange> {
    private readonly byte _kind;
    private readonly double _cssPixels;

    private HtmlChartTooltipRange(byte kind, double cssPixels) {
        _kind = kind;
        _cssPixels = cssPixels;
    }

    /// <summary>Gets a range that requires a native hit on a painted target. This is also the default value of the struct.</summary>
    public static HtmlChartTooltipRange Exact => default;

    /// <summary>Gets a range that acquires the nearest painted Cartesian observation anywhere inside the chart stage.</summary>
    public static HtmlChartTooltipRange Nearest => new HtmlChartTooltipRange(1, 0);

    /// <summary>Creates a range that acquires a painted Cartesian observation within the given distance from its screen centre.</summary>
    /// <param name="cssPixels">A finite, non-negative distance in CSS pixels, independent of SVG scaling. Native hits remain eligible at zero.</param>
    /// <returns>The bounded pointer acquisition range.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The distance is negative or non-finite.</exception>
    public static HtmlChartTooltipRange WithinDistance(double cssPixels) {
        if (double.IsNaN(cssPixels) || double.IsInfinity(cssPixels) || cssPixels < 0)
            throw new ArgumentOutOfRangeException(nameof(cssPixels), "Tooltip distance must be finite and non-negative.");
        return new HtmlChartTooltipRange(2, cssPixels);
    }

    internal string SerializedKind => _kind == 1 ? "nearest" : _kind == 2 ? "distance" : "exact";
    internal double? CssPixels => _kind == 2 ? _cssPixels : (double?)null;

    /// <summary>Returns whether another range has the same acquisition rule and distance.</summary>
    public bool Equals(HtmlChartTooltipRange other) => _kind == other._kind && _cssPixels.Equals(other._cssPixels);

    /// <summary>Returns whether an object is an equal tooltip range.</summary>
    public override bool Equals(object? obj) => obj is HtmlChartTooltipRange other && Equals(other);

    /// <summary>Returns a hash code for the acquisition rule and distance.</summary>
    public override int GetHashCode() => (_kind.GetHashCode() * 397) ^ _cssPixels.GetHashCode();

    /// <summary>Compares two tooltip ranges for equality.</summary>
    public static bool operator ==(HtmlChartTooltipRange left, HtmlChartTooltipRange right) => left.Equals(right);

    /// <summary>Compares two tooltip ranges for inequality.</summary>
    public static bool operator !=(HtmlChartTooltipRange left, HtmlChartTooltipRange right) => !left.Equals(right);
}
