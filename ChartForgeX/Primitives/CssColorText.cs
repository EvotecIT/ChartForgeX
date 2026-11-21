using System;
using System.Linq;

namespace ChartForgeX.Primitives;

// Host-authored colours are written into style declarations, so they keep to the characters a CSS colour uses.
internal static class CssColorText {
    internal static string? Validate(string? value, string parameterName, string owner) {
        // CSS whitespace between components becomes one space; custom-property references may hold underscores.
        var color = string.IsNullOrWhiteSpace(value) ? null : string.Join(" ", value!.Split(new[] { ' ', '\t', '\n', '\r', '\f' }, StringSplitOptions.RemoveEmptyEntries));
        if (color != null && (color.Length > 80 || !color.All(ch => char.IsLetterOrDigit(ch) || ch is '#' or '(' or ')' or ',' or '.' or '%' or '-' or '_' or ' ' or '/')))
            throw new ArgumentException(owner + " colors must be CSS colors such as #2563eb, rgb(37 99 235) or a named color.", parameterName);
        return color;
    }
}
