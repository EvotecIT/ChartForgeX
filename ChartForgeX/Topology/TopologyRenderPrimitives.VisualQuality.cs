using System;
using System.Globalization;

namespace ChartForgeX.Topology;

internal static partial class TopologyRenderPrimitives {
    public const double MinimumReadableFineStrokeWidth = 0.8;
    public const double ForceGraphNormalEdgeStrokeWidth = 0.95;
    public const double DotNodeSymbolFontSize = 8;

    private static string Blend(string foreground, string background, double alpha) {
        if (!TryParseHex(foreground, out var fr, out var fg, out var fb) || !TryParseHex(background, out var br, out var bg, out var bb)) return background;
        var r = (int)Math.Round(fr * alpha + br * (1 - alpha));
        var g = (int)Math.Round(fg * alpha + bg * (1 - alpha));
        var b = (int)Math.Round(fb * alpha + bb * (1 - alpha));
        return "#" + r.ToString("X2", CultureInfo.InvariantCulture) + g.ToString("X2", CultureInfo.InvariantCulture) + b.ToString("X2", CultureInfo.InvariantCulture);
    }

    private static bool TryParseHex(string value, out int r, out int g, out int b) {
        r = 0;
        g = 0;
        b = 0;
        if (string.IsNullOrWhiteSpace(value) || value[0] != '#') return false;
        var hex = value.Substring(1);
        if (hex.Length == 3) {
            hex = string.Concat(hex[0], hex[0], hex[1], hex[1], hex[2], hex[2]);
        }

        if (hex.Length != 6 && hex.Length != 8) return false;
        return int.TryParse(hex.Substring(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out r)
            && int.TryParse(hex.Substring(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out g)
            && int.TryParse(hex.Substring(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out b);
    }
}
