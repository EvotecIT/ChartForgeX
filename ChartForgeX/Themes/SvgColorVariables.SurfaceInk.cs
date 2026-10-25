using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

public sealed partial class SvgColorVariables {
    /// <summary>Evaluates the bounded composed-surface operands of an existing derived-ink identity.</summary>
    private static bool TryAdaptiveInk(string name, IReadOnlyDictionary<string, ChartColor> variables, out ChartColor ink) {
        ink = default;
        var encoded = name.Substring(DerivedInkPrefix.Length).Replace('-', '+').Replace('_', '/');
        if (encoded.Length > 1400) return false;
        encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
        string[] parts;
        try { parts = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split('|'); }
        catch (FormatException) { return false; }
        if (parts.Length < 4 || parts.Length > 256 || parts[0] != "A") return false;
        var position = 1; var budget = 128;
        if (!Operand(0, out var preferred) || position >= parts.Length
            || !int.TryParse(parts[position++], NumberStyles.None, CultureInfo.InvariantCulture, out var count) || count < 1 || count > 32) return false;
        var fill = ChartColor.FromRgb(255, 255, 255);
        for (var index = 0; index < count; index++) {
            if (!Operand(0, out var layer)) return false;
            fill = ChartColorMath.CompositeOverOpaque(layer, fill);
        }
        if (position != parts.Length) return false;
        ink = ChartColorMath.ContrastRatio(preferred, fill) >= 4.5 ? preferred
            : ChartColorMath.AccessibleTextOnBackground(fill).WithOpacity(preferred.A / 255.0);
        return true;

        bool Operand(int depth, out ChartColor color) {
            color = default;
            if (depth > 16 || --budget < 0 || position >= parts.Length) return false;
            var item = parts[position++];
            if (item.Length > 1 && item[0] == 'O' && variables.TryGetValue(item.Substring(1), out var opaque)) {
                color = ChartColor.FromRgb(opaque.R, opaque.G, opaque.B); return true;
            }
            if (item.Length == 9 && item[0] == 'L') return Literal(item.Substring(1), out color);
            if (item == "Q") {
                if (position + 2 >= parts.Length) return false;
                var variable = parts[position++]; var shareText = parts[position++]; var fallback = parts[position++];
                if (!FiniteShare(shareText, out var share) || !Literal(fallback, out color)) return false;
                if (variables.TryGetValue(variable, out var source)) color = source.WithOpacity(source.A / 255.0 * share);
                return true;
            }
            if (item == "X") {
                if (position >= parts.Length || !FiniteShare(parts[position++], out var opacity) || !Operand(depth + 1, out color)) return false;
                color = color.WithOpacity(color.A / 255.0 * opacity); return true;
            }
            if (item != "B" || position >= parts.Length || !FiniteShare(parts[position++], out var amount)
                || !Operand(depth + 1, out var from) || !Operand(depth + 1, out var to)) return false;
            color = ChartColorMath.Blend(from, to, amount); return true;
        }
    }

    private static bool Literal(string text, out ChartColor color) {
        color = default;
        if (text.Length != 8 || !uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba)) return false;
        color = ChartColor.FromRgba((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba); return true;
    }

    private static bool FiniteShare(string text, out double share) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out share)
        && !double.IsNaN(share) && !double.IsInfinity(share) && share >= 0 && share <= 1;
}
