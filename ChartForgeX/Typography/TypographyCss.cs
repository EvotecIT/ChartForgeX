using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartForgeX.Typography;

/// <summary>The text engine's explicit font controls shared by SVG and HTML role styling.</summary>
internal static class TypographyCss {
    internal static string Role(TextStyleOverride style) {
        var result = style.OpenTypeLanguageTag == null ? "" : "font-language-override:" + (style.OpenTypeLanguageTag == "normal" ? "normal" : "'" + style.OpenTypeLanguageTag + "'");
        if (style.Variations != null) result += (result.Length == 0 ? "" : ";") + "font-variation-settings:" + style.Variations.Css;
        return result;
    }
    internal static bool TryVariations(string value, out FontVariationSettings? settings) {
        settings = null;
        if (value.Trim().Equals("normal", StringComparison.OrdinalIgnoreCase)) { settings = FontVariationSettings.Default; return true; }
        if (value.Length > 4096) return false;
        var parts = value.Split(','); if (parts.Length > 32) return false;
        var axes = new List<KeyValuePair<string, double>>();
        foreach (var part in parts) {
            var match = Regex.Match(part, "^\\s*(['\"])([^'\"\\\\]{4})\\1\\s+([+-]?(?:[0-9]+(?:\\.[0-9]*)?|\\.[0-9]+)(?:[eE][+-]?[0-9]+)?)\\s*$", RegexOptions.CultureInvariant);
            if (!match.Success || !double.TryParse(match.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return false;
            axes.Add(new KeyValuePair<string, double>(match.Groups[2].Value, number));
        }
        try { settings = new FontVariationSettings(axes); return true; } catch (ArgumentException) { return false; }
    }
}
