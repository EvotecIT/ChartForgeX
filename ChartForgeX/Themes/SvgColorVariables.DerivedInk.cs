using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

public sealed partial class SvgColorVariables {
    private const string DerivedInkPrefix = "--cfx-derived-ink-v1-";
    private static readonly Regex DerivedInkReference = new(@"var\((?<name>--cfx-derived-ink-v1-[A-Za-z0-9_-]{1,1400}),", RegexOptions.CultureInvariant);

    /// <summary>Gets this theme's colour variables and the contrasting inks required by the supplied SVG.</summary>
    /// <remarks>
    /// Use the same SVG with each theme's variables. Derived ink identities retain their source token names and blend
    /// strength, so the other theme evaluates its own fill and readable ink without laying out or rendering again.
    /// Only referenced derived inks are included. The original variable collection remains unchanged.
    /// </remarks>
    /// <param name="svg">An SVG exported with colour variables enabled.</param>
    /// <returns>The base variables followed by the referenced derived ink variables, evaluated in this theme.</returns>
    public IReadOnlyList<SvgColorVariable> GetVariablesForSvg(string svg) {
        if (svg == null) throw new ArgumentNullException(nameof(svg));
        var result = new List<SvgColorVariable>(_variables);
        var names = new HashSet<string>(_variables.Select(variable => variable.Name), StringComparer.Ordinal);
        var source = _variables.GroupBy(variable => variable.Name, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Color, StringComparer.Ordinal);
        foreach (Match match in DerivedInkReference.Matches(svg)) {
            var name = match.Groups["name"].Value;
            if (names.Contains(name) || !TryDerivedFill(name, source, out var fill)) continue;
            names.Add(name);
            result.Add(new SvgColorVariable(name, ChartColorMath.AccessibleTextOnBackground(fill), SvgColorRole.Text));
        }
        return result.AsReadOnly();
    }

    internal string DerivedInk(string descriptor, ChartColor ink) {
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(descriptor)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        if (encoded.Length > 1400) return ink.ToCss();
        return "var(" + DerivedInkPrefix + encoded + ", " + ink.ToHex() + ")";
    }

    private static bool TryDerivedFill(string name, IReadOnlyDictionary<string, ChartColor> variables, out ChartColor fill) {
        fill = default;
        var encoded = name.Substring(DerivedInkPrefix.Length).Replace('-', '+').Replace('_', '/');
        if (encoded.Length > 1400) return false;
        encoded = encoded.PadRight((encoded.Length + 3) / 4 * 4, '=');
        string[] parts;
        try { parts = Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Split('|'); }
        catch (FormatException) { return false; }
        bool Operand(string value, out ChartColor color) {
            color = default;
            if (value.Length > 1 && value[0] == 'V') return variables.TryGetValue(value.Substring(1), out color) && color.A == 255;
            if (value.Length != 9 || value[0] != 'L') return false;
            if (!uint.TryParse(value.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var rgba)) return false;
            color = ChartColor.FromRgba((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
            return color.A == 255;
        }
        if (parts.Length == 2 && parts[0] == "S") return Operand(parts[1], out fill);
        if (parts.Length != 4 || parts[0] != "M" || !Operand(parts[1], out var from) || !Operand(parts[2], out var to)
            || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)
            || double.IsNaN(amount) || amount < 0 || amount > 1) return false;
        fill = ChartColorMath.Blend(from, to, amount); return true;
    }
}
