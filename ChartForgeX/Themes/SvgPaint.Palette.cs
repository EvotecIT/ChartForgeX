using System;
using System.Globalization;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

internal readonly partial struct SvgPaint {
    /// <summary>Retains the authored palette slot independently of equal RGB values in the prepared theme.</summary>
    internal static SvgPaint Palette(ChartColor color, int index, bool opaque = false) {
        if (index < 0) throw new ArgumentOutOfRangeException(nameof(index));
        return new SvgPaint(Start + "S" + Hex(color) + index.ToString("X8", CultureInfo.InvariantCulture) + (opaque ? "1" : "0") + End, raw: true);
    }

    /// <summary>
    /// A categorical series colour that keeps its palette slot, so two slots (or a slot and another role) with equal RGB
    /// values never share a property. The slot is <paramref name="ordinal"/> modulo the mapped palette length and is
    /// used only while its colour matches; otherwise the paint takes the series role by colour, as <see cref="Of"/> does.
    /// </summary>
    internal static SvgPaint SeriesSlot(ChartColor color, int ordinal) {
        if (ordinal < 0) throw new ArgumentOutOfRangeException(nameof(ordinal));
        if (color.CssVariable != null) return Of(color, SvgColorRole.Series);
        return new SvgPaint(Start + "S" + Hex(color) + ordinal.ToString("X8", CultureInfo.InvariantCulture) + "2" + End, raw: true);
    }

    /// <summary>Gets whether this paint is a <see cref="SeriesSlot"/> token.</summary>
    internal bool IsSeriesSlot => Value != null && Value.Length > 18 && Value[0] == Start && Value[1] == 'S' && Value[Value.Length - 2] == '2';

    /// <summary>Uses a role's RGB channels at full opacity, even when its host variable changes alpha.</summary>
    internal static SvgPaint Opaque(ChartColor color, SvgColorRole role) =>
        new(Start + "R" + Digit(role) + Hex(color) + End, raw: true);

    private static string ResolvePalette(string body, SvgColorVariables? variables, bool keepLiterals) {
        var color = Color(body, 0);
        var index = uint.Parse(body.Substring(8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (body[16] == '2') return index <= int.MaxValue && variables != null && variables.TrySeriesSlotPaint(color, (int)index, out var slot) ? slot : color.ToCss();
        if (index <= int.MaxValue && variables != null && variables.TryPalettePaint(color, (int)index, body[16] == '1', out var paint)) return paint;
        return keepLiterals ? Literal(color).Value! : color.ToCss();
    }
}
