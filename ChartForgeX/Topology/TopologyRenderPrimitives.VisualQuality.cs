using System;
using System.Globalization;
using ChartForgeX.Primitives;
using ChartForgeX.Themes;

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

    /// <summary>
    /// Returns <see cref="StatusFill(string, string, double)"/> as an SVG paint: with SVG colour variables, a tint of a
    /// token colour over a token surface is written as <c>color-mix()</c> of their properties instead of a literal blend.
    /// The foreground role preserves value-based mapping for explicit colour overrides.
    /// </summary>
    public static SvgPaint StatusPaint(string color, string background, double amount, SvgColorRole colorRole = SvgColorRole.Status) {
        var blended = StatusFill(color, background, amount);
        if (!TryParseHex(color, out var fr, out var fg, out var fb) || !TryParseHex(background, out var br, out var bg, out var bb) || !TryParseHex(blended, out var r, out var g, out var b)) return SvgPaint.Plain(blended);
        return SvgPaint.Mix(ChartColor.FromRgb((byte)r, (byte)g, (byte)b), ChartColor.FromRgb((byte)br, (byte)bg, (byte)bb), SvgColorRole.Surface, ChartColor.FromRgb((byte)fr, (byte)fg, (byte)fb), colorRole, amount);
    }

    /// <summary>Returns the default 10 % status tint as an SVG paint (see <see cref="StatusPaint(string, string, double, SvgColorRole)"/>).</summary>
    public static SvgPaint StatusPaint(string color, string background) => StatusPaint(color, background, 0.10);

    /// <summary>Returns <see cref="GroupFill"/> as an SVG paint.</summary>
    public static SvgPaint GroupPaint(string accent, TopologyTheme theme, TopologyRenderOptions options, SvgColorRole colorRole) =>
        UseNeutralGroupSurface(options) ? SvgPaint.Plain(theme.Card) : StatusPaint(accent, theme.Background, IsMonitoringDashboardStyle(options) ? 0.055 : 0.10, colorRole);

    /// <summary>Returns <see cref="NodeFill"/> as an SVG paint.</summary>
    public static SvgPaint NodePaint(TopologyNode node, TopologyTheme theme, string accent, TopologyRenderOptions options, SvgColorRole colorRole) {
        if (!string.IsNullOrWhiteSpace(node.BackgroundColor)) return SvgPaint.Plain(node.BackgroundColor!.Trim());
        return EffectiveNodeSurfaceStyle(options) == TopologyNodeSurfaceStyle.Card
            ? SvgPaint.Plain(theme.Card)
            : StatusPaint(accent, theme.Background, IsMonitoringDashboardStyle(options) ? 0.065 : 0.10, colorRole);
    }

    /// <summary>The white of contrast strokes and glyphs on coloured marks: derived, so it never takes a token property.</summary>
    public static SvgPaint ContrastWhite => SvgPaint.Literal(ChartColor.White);

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
