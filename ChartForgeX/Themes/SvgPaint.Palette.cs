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

    /// <summary>Uses a role's RGB channels at full opacity, even when its host variable changes alpha.</summary>
    internal static SvgPaint Opaque(ChartColor color, SvgColorRole role) =>
        new(Start + "R" + Digit(role) + Hex(color) + End, raw: true);

    private static string ResolvePalette(string body, SvgColorVariables? variables, bool keepLiterals) {
        var color = Color(body, 0);
        var index = uint.Parse(body.Substring(8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        if (index <= int.MaxValue && variables != null && variables.TryPalettePaint(color, (int)index, body[16] == '1', out var paint)) return paint;
        return keepLiterals ? Literal(color).Value! : color.ToCss();
    }
}
