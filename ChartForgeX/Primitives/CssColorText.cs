using System;
using System.Linq;

namespace ChartForgeX.Primitives;

// Host-authored colours are written into style declarations, so they keep to the characters a CSS colour uses.
internal static class CssColorText {
    internal static string? Validate(string? value, string parameterName, string owner) {
        var color = string.IsNullOrWhiteSpace(value) ? null : value!.Trim();
        if (color != null && (color.Length > 80 || !color.All(ch => char.IsLetterOrDigit(ch) || ch is '#' or '(' or ')' or ',' or '.' or '%' or '-' or ' ' or '/')))
            throw new ArgumentException(owner + " colors must be CSS colors such as #2563eb, rgb(37 99 235) or a named color.", parameterName);
        return color;
    }
}
