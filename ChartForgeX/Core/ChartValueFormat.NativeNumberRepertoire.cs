using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ChartForgeX.Core;

public sealed partial class ChartValueFormat {
    /// <summary>Observes finite admitted built-in policy through the actual formatter. Culture name alone never admits a modified provider.</summary>
    internal bool TryGetNativeTextRepertoire(out string owner, out IReadOnlyList<string> observations) {
        if (TryGetInvariantTextRepertoire(out owner, out observations)) return true;
        owner = string.Empty; observations = Array.Empty<string>();
        if (_formatter != null || _notation is not (Notation.Number or Notation.Compact) || _notation == Notation.Number && !NativeNumberGrammar(_format)) return false;
        try { _ = PortablePolicy(); } catch (NotSupportedException) { return false; }
        // The incumbent formatter owns numeric strings, named-culture separators/symbols/direction marks and exponent spelling.
        var values = InvariantNativeWitnesses().Concat(new[] { .1, .01, .001, 1e-4, 1e-5, 1e-6, 1e-7, 1e-9, 1e-10, 1e-99, 1e-100, 1e-308, 1e10, 1e99, 1e100, 1e308 });
        observations = Array.AsReadOnly(values.Concat(values.Where(value => value != 0).Select(value => -value)).Concat(new[] { BitConverter.Int64BitsToDouble(long.MinValue) })
            .Select(Format).Distinct(StringComparer.Ordinal).OrderBy(value => value, StringComparer.Ordinal).ToArray());
        owner = "value-format-native-" + _notation + ":" + _format + ":" + Culture.Name; return true;
    }
    /// <summary>Bounds native custom-format work; the incumbent formatter still interprets and produces every string.</summary>
    private static bool NativeNumberGrammar(string format) {
        if (format.Length == 0 || format.Length > 128) return false;
        var first = format[0];
        var standard = (first >= 'a' && first <= 'z' || first >= 'A' && first <= 'Z')
            && format.Skip(1).All(value => value >= '0' && value <= '9');
        if (standard) {
            var code = char.ToUpperInvariant(first);
            if (code == 'R') return format.Length == 1;
            if (code is not ('C' or 'E' or 'F' or 'G' or 'N' or 'P')) return false;
            if (format.Length == 1) return true;
            return format.Length <= 3 && int.TryParse(format.Substring(1), NumberStyles.None, CultureInfo.InvariantCulture, out var precision) && precision <= 17;
        }
        var sections = 1; var placeholders = 0; var exponent = false; var quote = '\0';
        for (var index = 0; index < format.Length; index++) {
            var value = format[index];
            if (value < ' ' || char.IsLowSurrogate(value)) return false;
            if (char.IsHighSurrogate(value)) {
                if (++index >= format.Length || !char.IsLowSurrogate(format[index])) return false;
                continue;
            }
            if (value == '\\') {
                if (++index >= format.Length || format[index] < ' ' || char.IsLowSurrogate(format[index])) return false;
                if (char.IsHighSurrogate(format[index]) && (++index >= format.Length || !char.IsLowSurrogate(format[index]))) return false;
                continue;
            }
            if (quote != '\0') { if (value == quote) quote = '\0'; continue; }
            if (value is '\'' or '"') { quote = value; continue; }
            if (value == ';') {
                if (++sections > 3) return false;
                placeholders = 0; exponent = false; continue;
            }
            if ((value == '0' || value == '#') && ++placeholders > 17) return false;
            if ((value == 'E' || value == 'e') && placeholders > 0) {
                var cursor = index + 1;
                if (cursor < format.Length && format[cursor] is '+' or '-') cursor++;
                var zeros = cursor; while (cursor < format.Length && format[cursor] == '0') cursor++;
                if (cursor != zeros) {
                    if (exponent || cursor - zeros > 3) return false;
                    exponent = true;
                }
            }
        }
        return quote == '\0';
    }
}
