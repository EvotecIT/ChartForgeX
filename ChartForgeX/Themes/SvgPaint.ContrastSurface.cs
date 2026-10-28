using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;

namespace ChartForgeX.Themes;

internal readonly partial struct SvgPaint {
    /// <summary>Uses the existing contrast token to retain preferred ink and composited surface operands.</summary>
    internal static SvgPaint ContrastOver(ChartColor ink, SvgPaint preferred, IReadOnlyList<SvgPaint> layers) {
        if (preferred.Value == null || preferred.Value.Length > 2048 || layers.Count == 0 || layers.Count > 32) return Literal(ink);
        var source = new StringBuilder("A|").Append(Encode(preferred.Value));
        foreach (var layer in layers) {
            if (layer.Value == null || layer.Value.Length > 2048) return Literal(ink);
            source.Append('|').Append(Encode(layer.Value));
            if (source.Length > 768) return Literal(ink);
        }
        var encoded = Encode(source.ToString());
        // Retain the existing contrast token's wire limit and exact native fallback.
        if (encoded.Length > 1024) return Literal(ink);
        return new SvgPaint(Start + "C" + Hex(ink) + Hex(ink) + encoded + End, raw: true);
    }

    private static string Encode(string text) => Convert.ToBase64String(Encoding.UTF8.GetBytes(text));

    private static bool TrySurfaceContrastDescriptor(string source, SvgColorVariables variables, out string descriptor) {
        descriptor = string.Empty;
        if (source.Length > 768) return false;
        var parts = source.Split('|');
        if (parts.Length < 3 || parts.Length > 34 || parts[0] != "A") return false;
        var operands = new List<string>(); var budget = 128;
        try {
            for (var index = 1; index < parts.Length; index++) {
                var token = Encoding.UTF8.GetString(Convert.FromBase64String(parts[index]));
                if (!SurfaceOperand(token, variables, 0, ref budget, out var operand)) return false;
                operands.Add(operand);
            }
        } catch (FormatException) { return false; }
        descriptor = "A|" + operands[0] + "|" + (operands.Count - 1).ToString(CultureInfo.InvariantCulture)
            + "|" + string.Join("|", operands.GetRange(1, operands.Count - 1));
        return descriptor.Length <= 1050;
    }

    private static bool SurfaceOperand(string token, SvgColorVariables variables, int depth, ref int budget, out string operand) {
        operand = string.Empty;
        if (depth > 16 || --budget < 0 || token.Length < 3 || token.Length > 2048 || token[0] != Start || TokenLength(token, 0) != token.Length) return false;
        var body = token.Substring(2, token.Length - 3);
        var kind = token[1];
        if (kind is 'L' or 'P' or 'R' or 'S' or 'T' or 'V') {
            var color = Color(body, kind == 'T' ? 9 : kind is 'P' or 'R' ? 1 : 0);
            SvgColorVariable variable;
            var mapped = false; var opaque = kind == 'R';
            if (kind is 'P' or 'R' or 'T') mapped = variables.TryVariable(color, Role(body[kind == 'T' ? 8 : 0])!.Value, out variable);
            else variable = default;
            if (kind == 'T') {
                var themeToken = (VisualThemeColorToken)uint.Parse(body.Substring(0, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (variables.TryThemeTokenPaint(themeToken, color, out var paint)) {
                    var comma = paint.IndexOf(','); var name = paint.Substring(4, comma - 4);
                    operand = Named(name, 1, color); return true;
                }
            } else if (kind == 'V') {
                var expression = Encoding.UTF8.GetString(Convert.FromBase64String(body.Substring(8)));
                if (!TryCssVariableExpression(expression, color, out _, out var canonical)) return false;
                var comma = canonical.IndexOf(',');
                operand = Named(canonical.Substring(4, comma - 4), 1, color); return true;
            } else if (kind == 'S') {
                var index = uint.Parse(body.Substring(8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                mapped = index <= int.MaxValue && variables.TryPaletteVariable(color, (int)index, out variable);
                opaque = body[16] == '1';
            }
            if (mapped && opaque) { operand = "O" + variable.Name; return true; }
            if (mapped && variable.Color.A > 0 && color.A <= variable.Color.A) {
                operand = Named(variable.Name, color.A / (double)variable.Color.A, color); return true;
            }
            operand = "L" + Hex(color); return true;
        }
        if (kind == 'O') {
            var separator = body.IndexOf(':', 8);
            if (separator < 0 || !Amount(body.Substring(separator + 1), out var opacity)) return false;
            var nested = Encoding.UTF8.GetString(Convert.FromBase64String(body.Substring(8, separator - 8)));
            if (!SurfaceOperand(nested, variables, depth + 1, ref budget, out var inner)) return false;
            operand = "X|" + opacity.ToString("R", CultureInfo.InvariantCulture) + "|" + inner; return true;
        }
        if (kind == 'N') {
            var parts = body.Substring(8).Split(':');
            if (parts.Length != 3 || !Amount(parts[2], out var amount)) return false;
            var a = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
            var b = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            if (!SurfaceOperand(a, variables, depth + 1, ref budget, out var from)
                || !SurfaceOperand(b, variables, depth + 1, ref budget, out var to)) return false;
            operand = "B|" + amount.ToString("R", CultureInfo.InvariantCulture) + "|" + from + "|" + to; return true;
        }
        if (kind == 'M' && Amount(body.Substring(26), out var blend)) {
            var from = Color(body, 9); var to = Color(body, 18);
            var a = variables.TryVariable(from, Role(body[8]) ?? SvgColorRole.Any, out var av) ? "O" + av.Name : "L" + Hex(from);
            var b = variables.TryVariable(to, Role(body[17]) ?? SvgColorRole.Any, out var bv) ? "O" + bv.Name : "L" + Hex(to);
            operand = "B|" + blend.ToString("R", CultureInfo.InvariantCulture) + "|" + a + "|" + b; return true;
        }
        return false;
    }

    private static string Named(string name, double share, ChartColor fallback) =>
        "Q|" + name + "|" + share.ToString("R", CultureInfo.InvariantCulture) + "|" + Hex(fallback);

    private static bool Amount(string text, out double amount) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out amount)
        && !double.IsNaN(amount) && !double.IsInfinity(amount) && amount >= 0 && amount <= 1;
}
