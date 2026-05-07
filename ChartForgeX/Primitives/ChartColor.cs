using System;
using System.Globalization;

namespace ChartForgeX.Primitives;

/// <summary>
/// Represents an RGBA color used by ChartForgeX renderers.
/// </summary>
public readonly struct ChartColor {
    /// <summary>
    /// Gets the red channel.
    /// </summary>
    public readonly byte R;

    /// <summary>
    /// Gets the green channel.
    /// </summary>
    public readonly byte G;

    /// <summary>
    /// Gets the blue channel.
    /// </summary>
    public readonly byte B;

    /// <summary>
    /// Gets the alpha channel.
    /// </summary>
    public readonly byte A;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChartColor"/> struct.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The alpha channel.</param>
    public ChartColor(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }

    /// <summary>
    /// Creates an opaque RGB color.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <returns>An opaque chart color.</returns>
    public static ChartColor FromRgb(byte r, byte g, byte b) => new(r, g, b, 255);

    /// <summary>
    /// Creates a color with an explicit alpha channel.
    /// </summary>
    /// <param name="r">The red channel.</param>
    /// <param name="g">The green channel.</param>
    /// <param name="b">The blue channel.</param>
    /// <param name="a">The alpha channel.</param>
    /// <returns>A chart color.</returns>
    public static ChartColor FromRgba(byte r, byte g, byte b, byte a) => new(r, g, b, a);

    /// <summary>
    /// Creates a copy of this color with a different alpha channel.
    /// </summary>
    /// <param name="alpha">The alpha channel.</param>
    /// <returns>A chart color with the requested alpha channel.</returns>
    public ChartColor WithAlpha(byte alpha) => new(R, G, B, alpha);

    /// <summary>
    /// Creates a copy of this color with opacity expressed as a unit interval.
    /// </summary>
    /// <param name="opacity">The opacity from zero to one.</param>
    /// <returns>A chart color with the requested opacity.</returns>
    public ChartColor WithOpacity(double opacity) {
        if (double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity < 0 || opacity > 1) throw new ArgumentOutOfRangeException(nameof(opacity), opacity, "Opacity must be between zero and one.");
        return WithAlpha((byte)Math.Round(opacity * 255));
    }

    /// <summary>
    /// Creates a color from #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.
    /// </summary>
    /// <param name="hex">The hexadecimal color string.</param>
    /// <returns>A chart color.</returns>
    public static ChartColor FromHex(string hex) {
        if (string.IsNullOrWhiteSpace(hex)) throw new ArgumentException("Hex color must not be empty.", nameof(hex));
        var value = hex.Trim();
        if (value[0] == '#') value = value.Substring(1);
        if (value.Length == 3 || value.Length == 4) {
            var r = ParseHexByte(new string(value[0], 2), nameof(hex));
            var g = ParseHexByte(new string(value[1], 2), nameof(hex));
            var b = ParseHexByte(new string(value[2], 2), nameof(hex));
            var a = value.Length == 4 ? ParseHexByte(new string(value[3], 2), nameof(hex)) : (byte)255;
            return new ChartColor(r, g, b, a);
        }

        if (value.Length == 6 || value.Length == 8) {
            var r = ParseHexByte(value.Substring(0, 2), nameof(hex));
            var g = ParseHexByte(value.Substring(2, 2), nameof(hex));
            var b = ParseHexByte(value.Substring(4, 2), nameof(hex));
            var a = value.Length == 8 ? ParseHexByte(value.Substring(6, 2), nameof(hex)) : (byte)255;
            return new ChartColor(r, g, b, a);
        }

        throw new ArgumentException("Hex color must use #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.", nameof(hex));
    }

    /// <summary>
    /// Parses a named color or a hexadecimal color string.
    /// </summary>
    /// <param name="value">The named color or hexadecimal color string.</param>
    /// <returns>A chart color.</returns>
    public static ChartColor Parse(string value) {
        if (TryParse(value, out var color)) return color;
        throw new ArgumentException("Color must be a known ChartForgeX color name or use #RGB, #RGBA, #RRGGBB, or #RRGGBBAA notation.", nameof(value));
    }

    /// <summary>
    /// Attempts to parse a named color or a hexadecimal color string.
    /// </summary>
    /// <param name="value">The named color or hexadecimal color string.</param>
    /// <param name="color">The parsed chart color.</param>
    /// <returns>True when parsing succeeds.</returns>
    public static bool TryParse(string? value, out ChartColor color) {
        color = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var trimmed = value!.Trim();
        if (ChartColors.TryGet(trimmed, out color)) return true;
        try {
            color = FromHex(trimmed);
            return true;
        } catch (ArgumentException) {
            return false;
        }
    }

    /// <summary>
    /// Converts the color to a hexadecimal RGB string.
    /// </summary>
    /// <returns>A CSS-compatible hexadecimal color string.</returns>
    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    /// <summary>
    /// Converts the color to a hexadecimal RGBA string.
    /// </summary>
    /// <returns>A CSS-compatible hexadecimal color string with alpha.</returns>
    public string ToHexRgba() => $"#{R:X2}{G:X2}{B:X2}{A:X2}";

    /// <summary>
    /// Converts the color to a CSS color string.
    /// </summary>
    /// <returns>A hexadecimal or rgba CSS color string.</returns>
    public string ToCss() => A == 255 ? ToHex() : FormattableString.Invariant($"rgba({R},{G},{B},{A / 255.0:0.###})");

    /// <summary>
    /// Gets a fully transparent color.
    /// </summary>
    public static ChartColor Transparent => new(0,0,0,0);

    /// <summary>
    /// Gets opaque white.
    /// </summary>
    public static ChartColor White => new(255,255,255);

    /// <summary>
    /// Gets opaque black.
    /// </summary>
    public static ChartColor Black => new(0,0,0);

    private static byte ParseHexByte(string value, string parameterName) {
        if (!byte.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed)) {
            throw new ArgumentException("Hex color contains invalid characters.", parameterName);
        }

        return parsed;
    }
}
