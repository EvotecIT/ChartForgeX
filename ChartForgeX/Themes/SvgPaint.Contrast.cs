using System;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

internal readonly partial struct SvgPaint {
    /// <summary>Retains the filled mark expression alongside its exact opaque ink fallback.</summary>
    internal static SvgPaint Contrast(ChartColor fill, SvgPaint source) {
        if (source.Value == null) return Literal(ChartColorMath.AccessibleTextOnBackground(fill));
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(source.Value));
        if (encoded.Length > 1024) return Literal(ChartColorMath.AccessibleTextOnBackground(fill));
        return new SvgPaint(Start + "C" + Hex(fill) + Hex(ChartColorMath.AccessibleTextOnBackground(fill)) + encoded + End, raw: true);
    }

    private static string ResolveContrastSource(string body, SvgColorVariables? variables, bool keepLiterals) {
        var fill = Color(body, 0); var ink = Color(body, 8);
        string source;
        try { source = Encoding.UTF8.GetString(Convert.FromBase64String(body.Substring(16))); }
        catch (FormatException) { return keepLiterals ? Literal(ink).Value! : ink.ToCss(); }
        if (variables != null && source.StartsWith("A|", StringComparison.Ordinal)
            && TrySurfaceContrastDescriptor(source, variables, out var adaptive)) return variables.DerivedInk(adaptive, ink);
        if (variables != null && source.Length > 2 && TokenLength(source, 0) == source.Length) {
            if (source[1] == 'P' && variables.TryInk(fill, Role(source[2])!.Value, out var direct)) return direct;
            if (TryContrastDescriptor(source, variables, out var descriptor)) return variables.DerivedInk(descriptor, ink);
        }
        return keepLiterals ? Literal(ink).Value! : ink.ToCss();
    }

    private static bool TryContrastDescriptor(string token, SvgColorVariables variables, out string descriptor) {
        descriptor = string.Empty;
        var body = token.Substring(2, token.Length - 3);
        if (token[1] == 'P') {
            var color = Color(body, 1); var role = Role(body[0]);
            if (color.A != 255 || !role.HasValue || !variables.TryVariable(color, role.Value, out var variable)) return false;
            descriptor = "S|V" + variable.Name; return true;
        }
        if (token[1] == 'N') {
            var parts = body.Substring(8).Split(':');
            if (parts.Length != 3 || !double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var blendAmount)) return false;
            string a; string b;
            try {
                a = Encoding.UTF8.GetString(Convert.FromBase64String(parts[0]));
                b = Encoding.UTF8.GetString(Convert.FromBase64String(parts[1]));
            } catch (FormatException) { return false; }
            if (!TryContrastOperand(a, variables, out var fromOperand, out var fromMapped) ||
                !TryContrastOperand(b, variables, out var toOperand, out var toMapped) || !fromMapped && !toMapped) return false;
            descriptor = "M|" + fromOperand + "|" + toOperand + "|" + Clamp(blendAmount).ToString("R", CultureInfo.InvariantCulture);
            return true;
        }
        if (token[1] != 'M') return false;
        var from = Color(body, 9); var to = Color(body, 18);
        if (from.A != 255 || to.A != 255) return false;
        if (!double.TryParse(body.Substring(26), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)) return false;
        var mapped = false;
        string Operand(ChartColor color, SvgColorRole? role) {
            if (role.HasValue && variables.TryVariable(color, role.Value, out var variable)) {
                mapped = true; return "O" + variable.Name;
            }
            return "L" + Hex(color);
        }
        descriptor = "M|" + Operand(from, Role(body[8])) + "|" + Operand(to, Role(body[17])) + "|" + Clamp(amount).ToString("R", CultureInfo.InvariantCulture);
        return mapped;
    }

    private static bool TryContrastOperand(string token, SvgColorVariables variables, out string descriptor, out bool mapped) {
        descriptor = string.Empty; mapped = false;
        if (token.Length < 3 || TokenLength(token, 0) != token.Length) return false;
        var body = token.Substring(2, token.Length - 3);
        ChartColor color; SvgColorVariable variable;
        if (token[1] == 'R') {
            color = Color(body, 1);
            mapped = variables.TryVariable(color, Role(body[0])!.Value, out variable);
        } else if (token[1] == 'S' && body[16] == '1') {
            color = Color(body, 0);
            var index = uint.Parse(body.Substring(8, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            variable = default;
            mapped = index <= int.MaxValue && variables.TryPaletteVariable(color, (int)index, out variable);
        } else if (token[1] == 'L') {
            color = Color(body, 0); variable = default;
        } else return false;
        if (color.A != 255) return false;
        descriptor = mapped ? "O" + variable.Name : "L" + Hex(color);
        return true;
    }
}
