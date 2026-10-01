using ChartForgeX.Primitives;
using ChartForgeX.Themes;

namespace ChartForgeX.Rendering;

/// <summary>
/// A colour the renderers derive by blending two colours, described once so PNG draws <see cref="Color"/> and SVG
/// writes <see cref="Paint"/>: the same literal colour, or <c>color-mix()</c> of the token properties of its operands
/// when SVG colour variables are set.
/// </summary>
internal readonly struct ChartColorBlend {
    private readonly ChartColor? _result;

    /// <summary>Describes a blend of <paramref name="from"/> towards <paramref name="to"/> by <paramref name="amount"/>.</summary>
    /// <param name="from">The colour at amount 0.</param>
    /// <param name="fromRole">Its role, or null for a derived colour that never takes a property (white, black).</param>
    /// <param name="to">The colour at amount 1.</param>
    /// <param name="toRole">Its role, or null for a derived colour.</param>
    /// <param name="amount">The share of <paramref name="to"/>, 0 to 1.</param>
    /// <param name="result">The blended colour when the caller rounds it its own way; null uses <see cref="ChartColorMath.Blend"/>.</param>
    public ChartColorBlend(ChartColor from, SvgColorRole? fromRole, ChartColor to, SvgColorRole? toRole, double amount, ChartColor? result = null) {
        From = from;
        FromRole = fromRole;
        To = to;
        ToRole = toRole;
        Amount = amount;
        _result = result;
    }

    public ChartColor From { get; }

    public SvgColorRole? FromRole { get; }

    public ChartColor To { get; }

    public SvgColorRole? ToRole { get; }

    public double Amount { get; }

    /// <summary>Gets the blended colour, as the raster renderer draws it.</summary>
    public ChartColor Color => _result ?? ChartColorMath.Blend(From, To, Amount);

    /// <summary>Gets the SVG paint of the blend.</summary>
    public SvgPaint Paint => SvgPaint.Mix(Color, From, FromRole, To, ToRole, Amount);

    /// <summary>
    /// Gets the SVG paint of the blend with its literal written opaque, for paints whose opacity is set separately
    /// (gradient stops with <c>stop-opacity</c>).
    /// </summary>
    public SvgPaint OpaquePaint {
        get {
            var color = Color;
            return SvgPaint.Mix(ChartColor.FromRgb(color.R, color.G, color.B), From, FromRole, To, ToRole, Amount);
        }
    }

    /// <summary>A colour that is not a blend: <paramref name="color"/> written for <paramref name="role"/>.</summary>
    public static ChartColorBlend Solid(ChartColor color, SvgColorRole role) => new(color, role, color, role, 0, color);
}
