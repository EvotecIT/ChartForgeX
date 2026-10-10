using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ChartForgeX.Mermaid;

/// <summary>Reads finite configuration scalars without discarding their original spelling.</summary>
internal static class MermaidConfigurationNumber {
    internal static bool TryParse(string? text, out double number) {
        number = 0;
        if (text == null) return false;
        if (Regex.IsMatch(text, @"^(?:[-+]?[0-9]+(?:\.[0-9]*)?(?:[eE][-+]?[0-9]+)?|\.[0-9]+(?:[eE][-+]?[0-9]+)?)$", RegexOptions.CultureInvariant))
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                && !double.IsInfinity(number) && !double.IsNaN(number);
        var radixMatch = Regex.Match(text, @"^[-+]?0(?:(b)[01]+|(o)[0-7]+|(x)[0-9a-fA-F]+)$", RegexOptions.CultureInvariant);
        if (!radixMatch.Success) return false;
        var radix = radixMatch.Groups[1].Success ? 2 : radixMatch.Groups[2].Success ? 8 : 16;
        var start = text[0] == '+' || text[0] == '-' ? 3 : 2;
        for (var index = start; index < text.Length; index++) {
            var digit = text[index];
            number = number * radix + (digit <= '9' ? digit - '0' : char.ToUpperInvariant(digit) - 'A' + 10);
            if (double.IsInfinity(number)) return false;
        }
        if (text[0] == '-') number = -number;
        return true;
    }
}
