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
        if (style.ColorPaletteIndex.HasValue) result += (result.Length == 0 ? "" : ";") + "font-palette:" + TypographyPaletteCss.Name(style.ColorPaletteIndex.Value);
        return result;
    }
    /// <summary>
    /// Ends a CSS font-family stack with a generic family, so text whose named faces are missing on the
    /// viewing host (a registered proof font, a licensed face) degrades to a matching browser default
    /// instead of the user agent's serif. Stacks that already name a generic family, CSS-wide keywords and
    /// custom-property references are returned unchanged.
    /// </summary>
    internal static string FamilyWithGenericFallback(string family) {
        if (family == null) throw new ArgumentNullException(nameof(family));
        var trimmed = family.Trim();
        if (trimmed.Length == 0 || trimmed.IndexOf("var(", StringComparison.OrdinalIgnoreCase) >= 0) return family;
        switch (trimmed.ToLowerInvariant()) {
            case "inherit": case "initial": case "unset": case "revert": case "revert-layer": return family;
        }

        foreach (var name in FamilyNames(trimmed)) if (TypographyFontResolver.IsGenericFamily(name)) return family;
        var generic = Raster.TrueTypeFont.IsMonospaceFamily(trimmed) ? "monospace" : Raster.TrueTypeFont.IsSerifFamily(trimmed) ? "serif" : "sans-serif";
        return trimmed.TrimEnd(',', ' ') + ", " + generic;
    }

    private static IEnumerable<string> FamilyNames(string stack) {
        var name = new System.Text.StringBuilder();
        var quote = '\0';
        foreach (var ch in stack) {
            if (quote != '\0') { if (ch == quote) quote = '\0'; else name.Append(ch); }
            else if (ch == '\'' || ch == '"') quote = ch;
            else if (ch == ',') { yield return name.ToString().Trim(); name.Clear(); }
            else name.Append(ch);
        }

        yield return name.ToString().Trim();
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
