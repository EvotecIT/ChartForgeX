using System;
using ChartForgeX.SvgRaster;
using ChartForgeX.Themes;

namespace ChartForgeX.Primitives;

/// <summary>Converts model colours to and from the CSS colour text that interchange documents and markup carry.</summary>
internal static class ChartColorText {
    /// <summary>Reads hex, rgb(), named colours, <c>none</c> and <c>var(--name, fallback)</c>; blank text is no colour.</summary>
    internal static ChartColor? Parse(string? value) {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var text = value!.Trim();
        if (string.Equals(text, "none", StringComparison.OrdinalIgnoreCase)) return ChartColor.Transparent;
        if (SvgRasterColor.TryParse(text, out var color)) return color;
        if (text.StartsWith("var(", StringComparison.Ordinal) && SvgPaint.TryCssVariable(text, ChartColor.Black, out var fallback, out _)) {
            var comma = text.IndexOf(',');
            var name = (comma < 0 ? text.Substring(4, text.Length - 5) : text.Substring(4, comma - 4)).Trim();
            return fallback.WithCssVariable(name);
        }
        throw new ArgumentException("'" + text + "' is not a colour: use hex, rgb(), a colour name, none or var(--name, fallback).", nameof(value));
    }

    /// <summary>Writes the canonical text the parser reads back: hex with alpha when translucent, inside <c>var()</c> for a
    /// CSS variable colour.</summary>
    internal static string? ToText(this ChartColor? color) => color is { } value ? ToText(value) : null;

    internal static string ToText(this ChartColor color) {
        var hex = color.A == 255 ? color.ToHex() : color.ToHexRgba();
        return color.CssVariable == null ? hex : "var(" + color.CssVariable + ", " + hex + ")";
    }
}
