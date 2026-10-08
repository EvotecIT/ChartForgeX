using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

internal readonly partial struct SvgPaint {
    /// <summary>Retains validated authored variables when deriving a tint from two typed source paints.</summary>
    internal static SvgPaint Mix(ChartColor result, SvgPaint from, SvgPaint to, double amount) {
        if (from.Value == null || to.Value == null) return Literal(result);
        var a = Convert.ToBase64String(Encoding.UTF8.GetBytes(from.Value));
        var b = Convert.ToBase64String(Encoding.UTF8.GetBytes(to.Value));
        if (a.Length > 1024 || b.Length > 1024) throw new ArgumentException("Paint blend expressions are too deeply nested.");
        return new SvgPaint(Start + "N" + Hex(result) + a + ":" + b + ":" + Clamp(amount).ToString("R", CultureInfo.InvariantCulture) + End, raw: true);
    }

    private static string ResolveSourceMix(string body, SvgColorVariables? variables, bool keepLiterals) {
        var result = Color(body, 0);
        var parts = body.Substring(8).Split(':');
        if (parts.Length != 3 || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount))
            return keepLiterals ? Literal(result).Value! : result.ToCss();
        string a; string b;
        try {
            a = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            b = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
        } catch (FormatException) { return keepLiterals ? Literal(result).Value! : result.ToCss(); }
        if (a.Length < 3 || b.Length < 3 || a[0] != Start || b[0] != Start || TokenLength(a, 0) != a.Length || TokenLength(b, 0) != b.Length)
            return keepLiterals ? Literal(result).Value! : result.ToCss();
        var from = Resolve(a, variables); var to = Resolve(b, variables);
        if (from.IndexOf("var(", StringComparison.Ordinal) < 0 && to.IndexOf("var(", StringComparison.Ordinal) < 0)
            return keepLiterals ? Literal(result).Value! : result.ToCss();
        return "color-mix(in srgb, " + from + " " + ((1 - Clamp(amount)) * 100).ToString("0.##", CultureInfo.InvariantCulture)
            + "%, " + to + ")";
    }

    private static bool SourceMixLength(string text, ref int index) {
        if (!Hex(text, ref index, 8)) return false;
        for (var operand = 0; operand < 2; operand++) {
            var count = 0;
            while (count <= 1024 && index < text.Length && IsBase64(text[index])) { count++; index++; }
            if (count < 1 || count > 1024 || index >= text.Length || text[index++] != ':') return false;
        }
        var amount = 0;
        while (amount <= 32 && index < text.Length && IsAmountChar(text[index])) { amount++; index++; }
        return amount > 0 && amount <= 32;
    }
}
