using System;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.SvgRaster;

namespace ChartForgeX.Themes;

internal readonly partial struct SvgPaint {
    /// <summary>Retains an authored CSS variable with an explicit, identical standalone SVG and raster fallback.</summary>
    internal static bool TryCssVariable(string? css, ChartColor defaultFallback, out ChartColor resolved, out SvgPaint paint) {
        resolved = defaultFallback;
        paint = default;
        if (!TryCssVariableExpression(css, defaultFallback, out resolved, out var expression)) return false;
        paint = new SvgPaint(Start + "V" + Hex(resolved) + Convert.ToBase64String(Encoding.UTF8.GetBytes(expression)) + End, raw: true);
        return true;
    }

    private static bool TryCssVariableExpression(string? css, ChartColor fallback, out ChartColor resolved, out string expression) {
        resolved = fallback;
        expression = string.Empty;
        if (css == null || css.Length > 512) return false;
        var value = css.Trim();
        if (!value.StartsWith("var(", StringComparison.Ordinal) || value[value.Length - 1] != ')') return false;
        var body = value.Substring(4, value.Length - 5);
        var comma = body.IndexOf(',');
        var name = (comma < 0 ? body : body.Substring(0, comma)).Trim();
        if (name.Length < 3 || name.Length > 128 || !name.StartsWith("--", StringComparison.Ordinal)) return false;
        for (var index = 2; index < name.Length; index++) {
            var c = name[index];
            if (!(c is >= 'A' and <= 'Z' || c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c is '_' or '-')) return false;
        }
        if (comma >= 0 && !SvgRasterColor.TryParse(body.Substring(comma + 1).Trim(), out resolved)) return false;
        expression = "var(" + name + ", " + resolved.ToCss() + ")";
        return true;
    }

    private static string ResolveCssVariable(string body) {
        var fallback = Color(body, 0);
        string expression;
        try { expression = Encoding.UTF8.GetString(Convert.FromBase64String(body.Substring(8))); }
        catch (FormatException) { return fallback.ToCss(); }
        return TryCssVariableExpression(expression, fallback, out var resolved, out var canonical) && resolved.Equals(fallback)
            ? canonical : fallback.ToCss();
    }

    /// <summary>Identifies authored variables, including bounded opacity wrappers, independently of host theme mapping.</summary>
    internal bool HasCssVariable => HasCssVariableToken(Value, 0);

    private static bool HasCssVariableToken(string? token, int depth) {
        if (token == null || token.Length < 3 || depth > 32 || token[0] != Start || TokenLength(token, 0) != token.Length) return false;
        if (token[1] == 'V') return true;
        if (token[1] == 'N') {
            var parts = token.Substring(10, token.Length - 11).Split(':');
            if (parts.Length != 3) return false;
            try {
                return HasCssVariableToken(Encoding.UTF8.GetString(Convert.FromBase64String(parts[0])), depth + 1)
                    || HasCssVariableToken(Encoding.UTF8.GetString(Convert.FromBase64String(parts[1])), depth + 1);
            } catch (FormatException) { return false; }
        }
        if (token[1] != 'O') return false;
        var separator = token.IndexOf(':', 10);
        try {
            return HasCssVariableToken(Encoding.UTF8.GetString(Convert.FromBase64String(token.Substring(10, separator - 10))), depth + 1);
        } catch (FormatException) { return false; }
    }
}
