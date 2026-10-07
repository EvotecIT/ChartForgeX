using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ChartForgeX.Primitives;
using ChartForgeX.Rendering;

namespace ChartForgeX.Themes;

/// <summary>
/// A paint a renderer writes with its colour role, resolved against <see cref="SvgColorVariables"/> when the SVG is
/// finished. <see cref="Literal"/> is a colour the renderer derives (a white sheen, a contrast stroke) that must never
/// take a token's property because it happens to equal its colour; <see cref="Of"/> is a token colour written for a
/// role; blend operations write <c>color-mix()</c> of the two colours' properties when one of them
/// is mapped. <see cref="Plain(ChartColor)"/> is an ordinary literal that the value-based <see cref="SvgColorVariables.Apply"/>
/// still maps.
/// </summary>
/// <remarks>
/// Typed paints travel through the markup as tokens made of the Unicode noncharacters U+FDD0 and U+FDD1, which the SVG
/// writers replace in any escaped text, so no chart content can forge one. Every renderer resolves them before it
/// returns markup (<see cref="Resolve"/>); without host variables role paints become literal colours. Authored CSS
/// variable paints retain their validated expression and the same concrete fallback as native raster rendering.
/// </remarks>
internal readonly partial struct SvgPaint {
    private const char Start = '\uFDD0';
    private const char End = '\uFDD1';
    // Only well-formed tokens match; anything else between the noncharacters is left as it is.
    private static readonly Regex Token = new(
        "\uFDD0(?:(?<kind>L)(?<body>[0-9A-F]{8})|(?<kind>P)(?<body>[0-7][0-9A-F]{8})|(?<kind>I)(?<body>[0-7][0-9A-F]{16})|(?<kind>C)(?<body>[0-9A-F]{16}[A-Za-z0-9+/=]{1,1024})|(?<kind>V)(?<body>[0-9A-F]{8}[A-Za-z0-9+/=]{1,1024})|(?<kind>M)(?<body>[0-9A-F]{8}[0-7L][0-9A-F]{8}[0-7L][0-9A-F]{8}[0-9.Ee+-]{1,32})|(?<kind>N)(?<body>[0-9A-F]{8}[A-Za-z0-9+/=]{1,1024}:[A-Za-z0-9+/=]{1,1024}:[0-9.Ee+-]{1,32})|(?<kind>O)(?<body>[0-9A-F]{8}[A-Za-z0-9+/=]{1,1024}:[0-9.Ee+-]{1,32}))\uFDD1",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private SvgPaint(string? value, bool raw) {
        Value = value;
        IsRaw = raw;
    }

    /// <summary>Gets the attribute value: a CSS colour, a token when <see cref="IsRaw"/>, or null for no attribute.</summary>
    public string? Value { get; }

    /// <summary>Gets whether <see cref="Value"/> is a token that must be written without escaping.</summary>
    public bool IsRaw { get; }

    /// <summary>An ordinary literal colour, still subject to value-based mapping.</summary>
    public static SvgPaint Plain(ChartColor color) => new(color.ToCss(), raw: false);

    /// <summary>An ordinary CSS paint string, still subject to value-based mapping.</summary>
    /// <remarks>A null paint writes no attribute, as a null attribute value does.</remarks>
    public static SvgPaint Plain(string? css) => new(css, raw: false);

    /// <summary>Rebuilds a typed paint from its token, for markup trees that store attribute values as text.</summary>
    public static SvgPaint FromToken(string token) => new(token ?? throw new ArgumentNullException(nameof(token)), raw: true);

    /// <summary>Replaces the noncharacters of paint tokens in markup that is inserted without escaping (icon artwork).</summary>
    public static string Sanitize(string markup) => markup.IndexOf(Start) < 0 && markup.IndexOf(End) < 0 ? markup : markup.Replace(Start, '\uFFFD').Replace(End, '\uFFFD');

    /// <summary>A derived colour that stays literal.</summary>
    public static SvgPaint Literal(ChartColor color) => new(Start + "L" + Hex(color) + End, raw: true);

    /// <summary>A token colour written for <paramref name="role"/>.</summary>
    public static SvgPaint Of(ChartColor color, SvgColorRole role) => new(Start + "P" + Digit(role) + Hex(color) + End, raw: true);

    /// <summary>Black or white ink paired to this fill; an explicitly configured ink property follows theme changes.</summary>
    public static SvgPaint Contrast(ChartColor fill, SvgColorRole role) =>
        new(Start + "I" + Digit(role) + Hex(fill) + Hex(ChartColorMath.AccessibleTextOnBackground(fill)) + End, raw: true);

    /// <summary>
    /// A blend of <paramref name="from"/> towards <paramref name="to"/> by <paramref name="amount"/> (0 is
    /// <paramref name="from"/>). <paramref name="result"/> is the blended colour the renderer computes, written when no
    /// variable applies, so the literal output equals the raster output exactly. A null role marks an operand the
    /// renderer derives (white, black) that is always written literally.
    /// </summary>
    public static SvgPaint Mix(ChartColor result, ChartColor from, SvgColorRole? fromRole, ChartColor to, SvgColorRole? toRole, double amount) =>
        new(Start + "M" + Hex(result) + Digit(fromRole) + Hex(from) + Digit(toRole) + Hex(to) + Clamp(amount).ToString("R", CultureInfo.InvariantCulture) + End, raw: true);

    /// <summary>Multiplies a source paint's opacity without losing its role or blend operands.</summary>
    /// <param name="result">The exact resolved raster colour, retained as the static fallback.</param>
    /// <param name="opacity">The multiplier applied to the source paint's alpha.</param>
    internal SvgPaint WithOpacity(ChartColor result, double opacity) {
        if (Value == null) return Literal(result);
        var source = Convert.ToBase64String(Encoding.UTF8.GetBytes(Value));
        if (source.Length > 1024) throw new ArgumentException("Paint opacity expressions are too deeply nested.", nameof(opacity));
        return new SvgPaint(Start + "O" + Hex(result) + source + ":" + Clamp(opacity).ToString("R", CultureInfo.InvariantCulture) + End, raw: true);
    }

    /// <summary>
    /// Replaces the paint tokens in <paramref name="svg"/>. With <paramref name="keepLiterals"/> derived literals stay
    /// tokens, so a host renderer that applies its own variables afterwards (a chart grid) does not map them by value.
    /// </summary>
    public static string Resolve(string svg, SvgColorVariables? variables, bool keepLiterals = false) {
        var start = svg.IndexOf(Start);
        if (start < 0) return svg;
        // Matches exactly what the Token pattern matches, left to right without overlaps. A token's replacement depends
        // only on the token, the variables and keepLiterals, so each distinct token is resolved once per call.
        StringBuilder? builder = null;
        Dictionary<string, string>? resolved = null;
        var copied = 0;
        while (start >= 0) {
            var length = TokenLength(svg, start);
            if (length == 0) {
                start = svg.IndexOf(Start, start + 1);
                continue;
            }

            var token = svg.Substring(start, length);
            resolved ??= new Dictionary<string, string>(StringComparer.Ordinal);
            if (!resolved.TryGetValue(token, out var replacement)) {
                replacement = ResolveToken(token, variables, keepLiterals);
                resolved[token] = replacement;
            }

            builder ??= new StringBuilder(svg.Length);
            builder.Append(svg, copied, start - copied).Append(replacement);
            copied = start + length;
            start = svg.IndexOf(Start, copied);
        }

        if (builder == null) return svg;
        builder.Append(svg, copied, svg.Length - copied);
        return builder.ToString();
    }

    /// <summary>As <see cref="Resolve"/> through the <see cref="Token"/> pattern; the reference the scanner must equal.</summary>
    internal static string ResolveWithPattern(string svg, SvgColorVariables? variables, bool keepLiterals = false) =>
        svg.IndexOf(Start) < 0 ? svg : Token.Replace(svg, match => ResolveToken(match.Value, variables, keepLiterals));

    // The length of the well-formed token at index (which holds Start), or 0 when none starts there.
    internal static int TokenLength(string text, int index) {
        var i = index + 1;
        if (i >= text.Length) return 0;
        switch (text[i++]) {
            case 'L':
                return Hex(text, ref i, 8) && Close(text, i) ? i + 1 - index : 0;
            case 'P':
                return RoleDigit(text, ref i, false) && Hex(text, ref i, 8) && Close(text, i) ? i + 1 - index : 0;
            case 'I':
                return RoleDigit(text, ref i, false) && Hex(text, ref i, 16) && Close(text, i) ? i + 1 - index : 0;
            case 'V':
            case 'C': {
                if (!Hex(text, ref i, text[index + 1] == 'C' ? 16 : 8)) return 0;
                var source = 0;
                while (source <= 1024 && i + source < text.Length && IsBase64(text[i + source])) source++;
                i += source;
                return source > 0 && source <= 1024 && Close(text, i) ? i + 1 - index : 0;
            }
            case 'M': {
                if (!Hex(text, ref i, 8) || !RoleDigit(text, ref i, true) || !Hex(text, ref i, 8) || !RoleDigit(text, ref i, true) || !Hex(text, ref i, 8)) return 0;
                var amount = 0;
                while (amount <= 32 && i + amount < text.Length && IsAmountChar(text[i + amount])) amount++;
                if (amount < 1 || amount > 32) return 0;
                i += amount;
                return Close(text, i) ? i + 1 - index : 0;
            }
            case 'N': return SourceMixLength(text, ref i) && Close(text, i) ? i + 1 - index : 0;
            case 'O': {
                if (!Hex(text, ref i, 8)) return 0;
                var source = 0;
                while (source <= 1024 && i + source < text.Length && IsBase64(text[i + source])) source++;
                if (source < 1 || source > 1024 || i + source >= text.Length || text[i + source] != ':') return 0;
                i += source + 1;
                var amount = 0;
                while (amount <= 32 && i + amount < text.Length && IsAmountChar(text[i + amount])) amount++;
                if (amount < 1 || amount > 32) return 0;
                i += amount;
                return Close(text, i) ? i + 1 - index : 0;
            }
            default:
                return 0;
        }
    }

    private static bool Hex(string text, ref int index, int count) {
        if (index + count > text.Length) return false;
        for (var end = index + count; index < end; index++) {
            var c = text[index];
            if (!(c is >= '0' and <= '9' || c is >= 'A' and <= 'F')) return false;
        }

        return true;
    }

    private static bool RoleDigit(string text, ref int index, bool literal) {
        if (index >= text.Length) return false;
        var c = text[index];
        if (!(c is >= '0' and <= '7' || literal && c == LiteralOperand)) return false;
        index++;
        return true;
    }

    private static bool IsAmountChar(char c) => c is >= '0' and <= '9' || c is '.' or 'E' or 'e' or '+' or '-';
    private static bool IsBase64(char c) => c is >= 'A' and <= 'Z' || c is >= 'a' and <= 'z' || c is >= '0' and <= '9' || c is '+' or '/' or '=';

    private static bool Close(string text, int index) => index < text.Length && text[index] == End;

    private static string ResolveToken(string token, SvgColorVariables? variables, bool keepLiterals) {
        var body = token.Substring(2, token.Length - 3);
        switch (token[1]) {
            case 'L':
                return keepLiterals ? token : Color(body, 0).ToCss();
            case 'P': {
                var color = Color(body, 1);
                var role = Role(body[0]);
                return variables != null && role.HasValue && variables.TryPaint(color, role.Value, out var paint) ? paint : color.ToCss();
            }
            case 'I': {
                var fill = Color(body, 1);
                var ink = Color(body, 9);
                var role = Role(body[0])!.Value;
                if (variables != null && variables.TryInk(fill, role, out var paint)) return paint;
                return keepLiterals ? Literal(ink).Value! : ink.ToCss();
            }
            case 'C': return ResolveContrastSource(body, variables, keepLiterals);
            case 'V': return ResolveCssVariable(body);
            case 'N': return ResolveSourceMix(body, variables, keepLiterals);
            case 'O': {
                var result = Color(body, 0);
                var separator = body.IndexOf(':', 8);
                string source;
                try { source = Encoding.UTF8.GetString(Convert.FromBase64String(body.Substring(8, separator - 8))); }
                catch (FormatException) { return keepLiterals ? Literal(result).Value! : result.ToCss(); }
                if (!double.TryParse(body.Substring(separator + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var opacity)) opacity = 0;
                var paint = Resolve(source, variables);
                if (source.Length == 0 || source[0] != Start || TokenLength(source, 0) != source.Length ||
                    paint.IndexOf("var(", StringComparison.Ordinal) < 0)
                    return keepLiterals ? Literal(result).Value! : result.ToCss();
                if (opacity >= 1) return paint;
                return "color-mix(in srgb, " + paint + " " + (Clamp(opacity) * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%, transparent)";
            }
            default: {
                var result = Color(body, 0);
                var from = Color(body, 9);
                var to = Color(body, 18);
                if (!double.TryParse(body.Substring(26), NumberStyles.Float, CultureInfo.InvariantCulture, out var amount)) amount = 0;
                if (variables != null && TryMix(variables, from, Role(body[8]), to, Role(body[17]), amount, out var mix))
                    return result.A == 255 ? mix : "color-mix(in srgb, " + mix + " " + (result.A / 255d * 100).ToString("0.##", CultureInfo.InvariantCulture) + "%, transparent)";
                return keepLiterals ? Literal(result).Value! : result.ToCss();
            }
        }
    }

    private static bool TryMix(SvgColorVariables variables, ChartColor from, SvgColorRole? fromRole, ChartColor to, SvgColorRole? toRole, double amount, out string paint) {
        paint = string.Empty;
        // color-mix interpolates premultiplied colours; only opaque operands give exactly the raster blend.
        if (from.A != 255 || to.A != 255) return false;
        var fromPaint = string.Empty;
        var toPaint = string.Empty;
        var fromMapped = fromRole.HasValue && variables.TryPaint(from, fromRole.Value, out fromPaint);
        var toMapped = toRole.HasValue && variables.TryPaint(to, toRole.Value, out toPaint);
        if (!fromMapped && !toMapped) return false;
        if (!fromMapped) fromPaint = from.ToHex();
        if (!toMapped) toPaint = to.ToHex();
        if (amount <= 0.00005) {
            paint = fromPaint;
            return true;
        }

        if (amount >= 0.99995) {
            paint = toPaint;
            return true;
        }

        // Only the second operand carries a percentage: CSS gives the first the remainder, so the shares always add up to
        // 100% and the mix stays opaque (two separately rounded percentages can total 99.99%).
        var builder = new StringBuilder("color-mix(in srgb, ", 96);
        builder.Append(fromPaint).Append(", ");
        builder.Append(toPaint).Append(' ').Append((amount * 100).ToString("0.##", CultureInfo.InvariantCulture)).Append("%)");
        paint = builder.ToString();
        return true;
    }

    private static string Hex(ChartColor color) => color.ToHexRgba().Substring(1);

    private static ChartColor Color(string body, int offset) => ChartColor.FromRgba(uint.Parse(body.Substring(offset, 8), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    private const char LiteralOperand = 'L';

    private static char Digit(SvgColorRole? role) => role.HasValue ? (char)('0' + (int)role.Value) : LiteralOperand;

    private static SvgColorRole? Role(char digit) => digit == LiteralOperand ? null : (SvgColorRole)(digit - '0');

    private static double Clamp(double value) => double.IsNaN(value) ? 0 : Math.Max(0, Math.Min(1, value));
}
