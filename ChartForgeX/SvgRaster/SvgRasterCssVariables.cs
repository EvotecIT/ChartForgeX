using System;
using System.Collections.Generic;

namespace ChartForgeX.SvgRaster;

internal static class SvgRasterCssVariables {
    public static bool IsCustomProperty(string name) =>
        name.StartsWith("--", StringComparison.Ordinal) && name.Length > 2;

    public static string Resolve(string value, IReadOnlyDictionary<string, string> properties) =>
        Resolve(value, properties, 0);

    /// <summary>Freezes aliases against this element's winning values before they can be inherited.</summary>
    public static void Compute(Dictionary<string, string> properties) {
        // Read one snapshot so declaration/dictionary order cannot change alias meaning.
        var specified = new Dictionary<string, string>(properties, StringComparer.Ordinal);
        foreach (var property in specified) {
            if (property.Value.IndexOf("var(", StringComparison.OrdinalIgnoreCase) < 0) continue;
            var computed = Resolve(property.Value, specified);
            // Missing or cyclic substitutions remain unavailable, rather than rebinding in a child.
            // Resolve retains the existing bounded substitution depth and fallback handling.
            if (computed.IndexOf("var(", StringComparison.OrdinalIgnoreCase) >= 0) properties.Remove(property.Key);
            else properties[property.Key] = computed;
        }
    }

    private static string Resolve(string value, IReadOnlyDictionary<string, string> properties, int depth) {
        if (string.IsNullOrWhiteSpace(value) || depth >= 8 || value.IndexOf("var(", StringComparison.OrdinalIgnoreCase) < 0) return value;
        var result = new System.Text.StringBuilder(value.Length);
        var index = 0;
        while (index < value.Length) {
            var open = value.IndexOf("var(", index, StringComparison.OrdinalIgnoreCase);
            if (open < 0) {
                result.Append(value, index, value.Length - index);
                break;
            }

            var close = FindFunctionClose(value, open + 4);
            if (close < 0) {
                result.Append(value, index, value.Length - index);
                break;
            }

            result.Append(value, index, open - index);
            var body = value.Substring(open + 4, close - open - 4);
            result.Append(ResolveVariable(body, properties, depth));
            index = close + 1;
        }

        return result.ToString();
    }

    private static string ResolveVariable(string body, IReadOnlyDictionary<string, string> properties, int depth) {
        SplitVariable(body, out var name, out var fallback);
        if (IsCustomProperty(name) && properties.TryGetValue(name, out var value)) return Resolve(value, properties, depth + 1);
        return fallback == null ? "var(" + body + ")" : Resolve(fallback, properties, depth + 1);
    }

    private static void SplitVariable(string body, out string name, out string? fallback) {
        var comma = FindTopLevelComma(body);
        if (comma < 0) {
            name = body.Trim();
            fallback = null;
            return;
        }

        name = body.Substring(0, comma).Trim();
        fallback = body.Substring(comma + 1).Trim();
    }

    private static int FindTopLevelComma(string value) {
        var quote = '\0';
        var depth = 0;
        for (var i = 0; i < value.Length; i++) {
            var ch = value[i];
            if (quote != '\0') {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '\'' || ch == '"') {
                quote = ch;
                continue;
            }

            if (ch == '(') depth++;
            else if (ch == ')' && depth > 0) depth--;
            else if (ch == ',' && depth == 0) return i;
        }

        return -1;
    }

    private static int FindFunctionClose(string value, int start) {
        var quote = '\0';
        var depth = 0;
        for (var i = start; i < value.Length; i++) {
            var ch = value[i];
            if (quote != '\0') {
                if (ch == quote) quote = '\0';
                continue;
            }

            if (ch == '\'' || ch == '"') {
                quote = ch;
                continue;
            }

            if (ch == '(') depth++;
            else if (ch == ')') {
                if (depth == 0) return i;
                depth--;
            }
        }

        return -1;
    }
}
