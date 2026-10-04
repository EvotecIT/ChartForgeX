using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ChartForgeX.Typography;

/// <summary>Native font-palette rules and shared CSS family-name handling for base palettes.</summary>
internal static class TypographyPaletteCss {
    internal static string Name(int index) => "--cfx-font-palette-" + index.ToString(CultureInfo.InvariantCulture);

    internal static string Rules(string fallbackFamily, params TextStyleOverride[] styles) {
        var rules = new SortedDictionary<int, SortedSet<string>>();
        foreach (var style in styles) {
            if (!style.ColorPaletteIndex.HasValue) continue;
            var index = style.ColorPaletteIndex.Value;
            if (!rules.TryGetValue(index, out var families)) rules[index] = families = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var family in Families(style.FontFamily ?? fallbackFamily)) families.Add(family);
        }
        var result = new StringBuilder();
        foreach (var rule in rules) {
            if (rule.Value.Count == 0) continue;
            result.Append("@font-palette-values ").Append(Name(rule.Key)).Append("{font-family:");
            var comma = false;
            foreach (var family in rule.Value) { if (comma) result.Append(','); result.Append(Quote(family)); comma = true; }
            result.Append(";base-palette:").Append(rule.Key.ToString(CultureInfo.InvariantCulture)).Append('}');
        }
        return result.ToString();
    }

    internal static IEnumerable<string> Families(string? value) {
        if (string.IsNullOrWhiteSpace(value)) yield break;
        var name = new StringBuilder(); var quote = '\0'; var quoted = false;
        for (var i = 0; i <= value!.Length; i++) {
            var ch = i == value.Length ? ',' : value[i];
            if (ch == ',' && quote == '\0') {
                var family = name.ToString().Trim(); name.Clear();
                if (family.Length > 0 && (quoted || !Generic(family))) yield return family;
                quoted = false; continue;
            }
            if (ch == '\\' && i + 1 < value.Length) {
                var start = ++i; var count = 0;
                while (i < value.Length && count < 6 && Hex(value[i]) >= 0) { i++; count++; }
                if (count == 0) name.Append(value[i]);
                else {
                    var scalar = int.Parse(value.Substring(start, count), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    name.Append(scalar > 0 && scalar <= 0x10FFFF && (scalar < 0xD800 || scalar > 0xDFFF) ? char.ConvertFromUtf32(scalar) : "\uFFFD");
                    if (i < value.Length && char.IsWhiteSpace(value[i])) { if (value[i] == '\r' && i + 1 < value.Length && value[i + 1] == '\n') i++; }
                    else i--;
                }
            } else if (quote != '\0') { if (ch == quote) quote = '\0'; else name.Append(ch); }
            else if (ch == '\'' || ch == '"') { quote = ch; quoted = true; }
            else name.Append(ch);
        }
    }
    internal static bool IsName(string value) {
        if (value.Length < 3 || value.Length > 128 || !value.StartsWith("--", StringComparison.Ordinal)) return false;
        foreach (var ch in value) if (!(ch >= 'a' && ch <= 'z' || ch >= 'A' && ch <= 'Z' || ch >= '0' && ch <= '9' || ch == '-' || ch == '_')) return false;
        return true;
    }
    private static int Hex(char value) => value >= '0' && value <= '9' ? value - '0' : value >= 'a' && value <= 'f' ? value - 'a' + 10 : value >= 'A' && value <= 'F' ? value - 'A' + 10 : -1;
    private static bool Generic(string value) {
        switch (value.ToLowerInvariant()) {
            case "serif": case "sans-serif": case "monospace": case "cursive": case "fantasy": case "system-ui":
            case "ui-serif": case "ui-sans-serif": case "ui-monospace": case "ui-rounded": case "emoji": case "math": case "fangsong": return true;
            default: return false;
        }
    }
    private static string Quote(string value) {
        var result = new StringBuilder("\"");
        foreach (var ch in value) {
            if (ch == '\\' || ch == '"' || ch < 32 || ch == '<' || ch == '&') result.Append('\\').Append(((int)ch).ToString("x", CultureInfo.InvariantCulture)).Append(' ');
            else result.Append(ch);
        }
        return result.Append('"').ToString();
    }
}
