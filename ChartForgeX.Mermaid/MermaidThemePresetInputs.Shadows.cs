using System;
using System.Globalization;
using System.Text.RegularExpressions;
using ChartForgeX.Effects;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Mermaid;

internal sealed partial class MermaidThemePresetInputs {
    private VisualDropShadow? _authoredShadow;
    private bool _shadowAuthored;
    internal bool HasSupportedShadowSetting => StyleShadowDisabled || _authoredShadow != null;

    private void ReadShadow(MermaidConfigurationSetting? setting) {
        _shadowAuthored = setting != null;
        if (setting?.IsString != true || StyleShadowDisabled) return;
        // One finite drop-shadow function, not a CSS interpreter or a URL alias resolver.
        // Pixel units and integral offsets match the native descriptor's existing contract.
        var value = setting.Value ?? string.Empty;
        if (value.Length > 512) return;
        var match = Regex.Match(value,
            @"^drop-shadow\(\s*([+-]?(?:\d+(?:\.\d*)?|\.\d+))px\s+([+-]?(?:\d+(?:\.\d*)?|\.\d+))px\s+((?:\d+(?:\.\d*)?|\.\d+))px\s+(.+)\)\s*;?\s*$",
            RegexOptions.CultureInvariant);
        if (!match.Success || !Offset(match.Groups[1].Value, out var x) ||
            !Offset(match.Groups[2].Value, out var y) ||
            !double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var sigma) ||
            double.IsInfinity(sigma) || sigma < 0 || sigma > 128 ||
            !SvgRasterColor.TryParsePrecise(match.Groups[4].Value, out var tint)) return;
        // Keep precise source alpha separate from the RGB byte representation. Applying a
        // rounded alpha byte and then opacity would attenuate it twice.
        _authoredShadow = new VisualDropShadow(tint.ToColor().WithAlpha(255), x, y, sigma,
            opacity: tint.A);
    }

    internal VisualDropShadow? ResolveStyleShadow(string? name, bool hostTheme) {
        if (StyleShadowDisabled) return null;
        if (_authoredShadow != null) return _authoredShadow;
        if (hostTheme || _shadowAuthored) return null;
        // Exact maintained preset function literals, materialized once. The known bare
        // URL presets have no local target in the pinned standalone Sequence output
        // (actual164); preserve their no-paint result rather than invent a glyph-filter alias.
        return name switch {
            "base" or "default" or "dark" or "neutral" =>
                new VisualDropShadow(ChartColor.FromRgb(185, 185, 185), 1, 2, 2),
            "forest" => new VisualDropShadow(ChartColor.FromRgb(185, 185, 185), 1, 2, 2, opacity: .5),
            "neo-dark" => new VisualDropShadow(ChartColor.FromRgb(185, 185, 185), 1, 2, 2, opacity: .2),
            "neo" => new VisualDropShadow(ChartColor.FromRgb(0, 0, 0), 0, 1, 2, opacity: .25),
            _ => null
        };
    }

    private static bool Offset(string text, out int result) {
        result = 0;
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ||
            double.IsNaN(value) || double.IsInfinity(value) || value < int.MinValue || value > int.MaxValue ||
            value != Math.Truncate(value)) return false;
        result = (int)value;
        return true;
    }
}
